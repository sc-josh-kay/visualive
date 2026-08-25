using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayVisualizer.Audio;
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

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            if (_analyzer == null) _analyzer = FindFirstObjectByType<AudioAnalyzer>();
            if (_muzzle == null) _muzzle = transform;

            var found = new List<WeaponBase>(GetComponents<WeaponBase>());
            found.Sort((a, b) => a.Order.CompareTo(b.Order));
            _weapons = found.ToArray();

            _active = 0;
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

        private void OnGUI()
        {
            if (Application.isMobilePlatform) return; // debug overlay: editor/desktop only
            if (_weapons == null || _weapons.Length == 0) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            style.normal.textColor = new Color(1f, 0.9f, 0.35f);
            GUI.Label(new Rect(20, 300, 520, 26),
                $"WEAPON: {_weapons[_active].DisplayName.ToUpperInvariant()}   (Space to cycle)", style);
        }
    }
}
