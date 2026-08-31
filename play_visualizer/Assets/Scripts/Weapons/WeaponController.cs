using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayVisualizer.Audio;
using PlayVisualizer.Gameplay;
using PlayVisualizer.Player;

namespace PlayVisualizer.Weapons
{
    /// <summary>
    /// Drives the active auto-fire weapon (spec7). Reads aim + aiming-state from the PlayerController
    /// and MusicState from the analyzer, builds a per-frame WeaponContext, and ticks the active
    /// weapon. Space cycles weapons (desktop testing) and a debug label shows the current one.
    ///
    /// The controller is the ONE place that reads audio for weapons; individual weapons never do.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class WeaponController : MonoBehaviour
    {
        [Tooltip("Where projectiles spawn from. Defaults to this transform.")]
        [SerializeField] private Transform _muzzle;
        [SerializeField] private AudioAnalyzer _analyzer;

        private PlayerController _controller;
        private WeaponBase[] _weapons;
        private int _active;
        private int _startWeapon;  // the weapon equipped at spawn; excluded from the Overdrive reward
        private bool _wasOverdrive; // rising-edge detector for the Overdrive weapon reward
        private readonly List<int> _candidates = new List<int>(); // reusable, no per-switch alloc

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            if (_analyzer == null) _analyzer = FindFirstObjectByType<AudioAnalyzer>();
            if (_muzzle == null) _muzzle = transform;

            var found = new List<WeaponBase>(GetComponents<WeaponBase>());
            found.Sort((a, b) => a.Order.CompareTo(b.Order));
            _weapons = found.ToArray();

            _active = 0;
            _startWeapon = _active;
            if (_weapons.Length > 0) _weapons[_active].OnEquip();
        }

        private void Update()
        {
            if (_weapons == null || _weapons.Length == 0 || Time.timeScale == 0f)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Cycle();
            }

            // Overdrive reward: on the frame Overdrive begins, swap to a random new weapon. The manual
            // Space / double-tap cycling stays available for testing.
            VisualizerMomentum momentum = VisualizerMomentum.Instance;
            bool overdrive = momentum != null && momentum.IsOverdrive;
            if (overdrive && !_wasOverdrive) SwitchToRandom();
            _wasOverdrive = overdrive;

            var ctx = new WeaponContext
            {
                AimDirection = _controller.AimDirection,
                MuzzlePosition = _muzzle.position,
                IsAiming = _controller.IsAiming,
                Music = _analyzer != null ? _analyzer.State : null,
                Dt = Time.deltaTime
            };
            _weapons[_active].Tick(ctx);
        }

        /// <summary>Cycle to the next weapon. Called by spacebar (desktop) or double-tap (touch).</summary>
        public void CycleWeapon() => Cycle();

        private void Cycle()
        {
            _weapons[_active].OnUnequip();
            _active = (_active + 1) % _weapons.Length;
            _weapons[_active].OnEquip();
        }

        /// <summary>
        /// Switch to a random weapon for the Overdrive reward: different from the current one AND
        /// never the starting weapon (once you've earned an upgrade, you don't get handed the starter
        /// back). Manual Space / double-tap cycling can still pass through the starter for testing.
        /// </summary>
        private void SwitchToRandom()
        {
            if (_weapons.Length <= 1) return;

            _candidates.Clear();
            for (int i = 0; i < _weapons.Length; i++)
                if (i != _active && i != _startWeapon) _candidates.Add(i);

            // Fallback for tiny weapon sets (e.g. only the starter + one other): if excluding both
            // leaves nothing, allow any weapon that at least differs from the current one.
            if (_candidates.Count == 0)
                for (int i = 0; i < _weapons.Length; i++)
                    if (i != _active) _candidates.Add(i);
            if (_candidates.Count == 0) return;

            int next = _candidates[Random.Range(0, _candidates.Count)];
            _weapons[_active].OnUnequip();
            _active = next;
            _weapons[_active].OnEquip();
        }

#if UNITY_EDITOR
        // Editor-only debug overlay: compiled out of device builds so OnGUI never dispatches there.
        private void OnGUI()
        {
            if (_weapons == null || _weapons.Length == 0) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            style.normal.textColor = new Color(1f, 0.9f, 0.35f);
            GUI.Label(new Rect(20, 300, 520, 26),
                $"WEAPON: {_weapons[_active].DisplayName.ToUpperInvariant()}   (Space to cycle)", style);
        }
#endif
    }
}
