using UnityEngine;
using UnityEngine.InputSystem;
using PlayVisualizer.Gameplay;
using PlayVisualizer.Weapons;

namespace PlayVisualizer.Player
{
    /// <summary>
    /// Wires the on-screen thumbsticks into PlayerInputReader on touch platforms, and turns a
    /// double-tap anywhere into a weapon cycle (the touch stand-in for the spacebar). Enables the
    /// touch UI only on a touch device (or when forced in the editor for testing).
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        [SerializeField] private VirtualJoystick _moveStick;
        [SerializeField] private VirtualJoystick _aimStick;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private WeaponController _weapons;
        [SerializeField] private GameObject _touchCanvas;
        [SerializeField] private GameManager _gameManager;

        [Tooltip("Force touch controls on in the editor so they can be tested with the mouse.")]
        [SerializeField] private bool _forceInEditor = false;

        [Header("Double-tap → cycle weapon")]
        [SerializeField] private float _doubleTapTime = 0.3f;
        [SerializeField] private float _doubleTapMaxMove = 90f;

        private bool _touchMode;
        private float _lastTapTime = -10f;
        private Vector2 _lastTapPos;

        /// <summary>True when on-screen touch controls are active (mobile, or forced in editor).</summary>
        public bool TouchMode => _touchMode;

        private void Awake()
        {
            if (_input == null) _input = FindFirstObjectByType<PlayerInputReader>();
            if (_weapons == null) _weapons = FindFirstObjectByType<WeaponController>();
            if (_gameManager == null) _gameManager = FindFirstObjectByType<GameManager>();

            _touchMode = Application.isMobilePlatform || (_forceInEditor && Application.isEditor);

            if (_input != null) _input.SetTouchActive(_touchMode);
        }

        private void Update()
        {
            if (!_touchMode || _input == null) return;

            // Show the sticks ONLY while actually playing. On the results/menu screens the touch
            // zones would otherwise capture taps and block their buttons.
            bool playing = _gameManager == null || _gameManager.State == GameManager.GameState.Playing;
            if (_touchCanvas != null && _touchCanvas.activeSelf != playing)
            {
                _touchCanvas.SetActive(playing);
            }

            if (!playing)
            {
                _input.SetVirtualMove(Vector2.zero);
                _input.SetVirtualAim(Vector2.zero, false);
                return;
            }

            _input.SetVirtualMove(_moveStick != null ? _moveStick.Value : Vector2.zero);
            _input.SetVirtualAim(
                _aimStick != null ? _aimStick.Value : Vector2.zero,
                _aimStick != null && _aimStick.Active);

            if (Time.timeScale > 0f) // ignore taps while paused
            {
                DetectDoubleTap();
            }
        }

        private void DetectDoubleTap()
        {
            Touchscreen ts = Touchscreen.current;
            if (ts == null) return;

            foreach (var touch in ts.touches)
            {
                if (!touch.press.wasPressedThisFrame) continue;

                Vector2 pos = touch.position.ReadValue();
                float now = Time.unscaledTime;
                if (now - _lastTapTime <= _doubleTapTime && (pos - _lastTapPos).magnitude <= _doubleTapMaxMove)
                {
                    if (_weapons != null) _weapons.CycleWeapon();
                    _lastTapTime = -10f; // consume, so a triple-tap isn't two cycles
                }
                else
                {
                    _lastTapTime = now;
                    _lastTapPos = pos;
                }
            }
        }
    }
}
