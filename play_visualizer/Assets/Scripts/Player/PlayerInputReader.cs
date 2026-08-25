using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayVisualizer.Player
{
    /// <summary>
    /// The single boundary between input DEVICES and gameplay.
    ///
    /// Gameplay code (PlayerController, WeaponController) reads abstract INTENTS from here
    /// (Move / Aim / IsAiming) and never touches a keyboard, mouse, gamepad, or touchscreen
    /// directly. Adding phone/touch support later means adding bindings in this one file.
    ///
    /// TWIN-STICK + AUTO-FIRE (spec7): the left stick moves, the RIGHT stick aims, and the weapon
    /// fires automatically while the player is aiming — there is deliberately no fire button. On the
    /// phone the right stick becomes a right touch zone; on desktop the MOUSE POINTER stands in for
    /// the right stick (aim toward the pointer), and aiming is always considered active.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Tooltip("Right-stick magnitude above which the stick (not the pointer) drives aim.")]
        [SerializeField] private float _stickDeadzone = 0.25f;

        private InputAction _move;
        private InputAction _aimPointer;  // mouse/touch screen position (desktop stand-in)
        private InputAction _aimStick;    // gamepad right stick (the real twin-stick aim)

        // Virtual (on-screen touch) input, injected each frame by TouchControls on mobile.
        private Vector2 _virtualMove;
        private Vector2 _virtualAim;
        private bool _virtualAimActive;
        private bool _touchActive;

        /// <summary>Feed the left virtual stick (mobile). Ignored on desktop when zero.</summary>
        public void SetVirtualMove(Vector2 v) => _virtualMove = v;

        /// <summary>Feed the right virtual stick (mobile): direction + whether it's engaged.</summary>
        public void SetVirtualAim(Vector2 v, bool active) { _virtualAim = v; _virtualAimActive = active; }

        /// <summary>When true, the pointer no longer counts as "always aiming" (touch drives aim/fire).</summary>
        public void SetTouchActive(bool active) => _touchActive = active;

        private Vector2 GamepadStick => _aimStick != null ? _aimStick.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>Desired movement direction, roughly [-1, 1] per axis. Keyboard/stick or touch.</summary>
        public Vector2 MoveInput
        {
            get
            {
                Vector2 kb = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
                return _virtualMove.sqrMagnitude > kb.sqrMagnitude ? _virtualMove : kb;
            }
        }

        /// <summary>Raw pointer position in SCREEN pixels (mouse or primary touch).</summary>
        public Vector2 AimScreenPosition => _aimPointer != null ? _aimPointer.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>Aim direction from the right stick (gamepad) or the right virtual stick (touch).</summary>
        public Vector2 AimStick => _virtualAimActive ? _virtualAim : GamepadStick;

        /// <summary>True when a stick (gamepad or touch) is driving aim past the deadzone.</summary>
        public bool HasStickAim =>
            _virtualAimActive || GamepadStick.sqrMagnitude > _stickDeadzone * _stickDeadzone;

        /// <summary>
        /// Whether the player is actively aiming (weapon auto-fires). On touch this is only while the
        /// right stick is engaged; on desktop the mouse pointer counts as always-aiming.
        /// </summary>
        public bool IsAiming => HasStickAim || (!_touchActive && Pointer.current != null);

        private void Awake()
        {
            // Move: WASD / arrow keys / left stick. Extend later with a touch virtual stick.
            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");

            // Aim (pointer): mouse now, touchscreen later — the desktop right-stick stand-in.
            _aimPointer = new InputAction("AimPointer", InputActionType.Value, expectedControlType: "Vector2");
            _aimPointer.AddBinding("<Pointer>/position");

            // Aim (stick): the real twin-stick aim → a right touch zone on phone later.
            _aimStick = new InputAction("AimStick", InputActionType.Value, expectedControlType: "Vector2");
            _aimStick.AddBinding("<Gamepad>/rightStick");
        }

        private void OnEnable()
        {
            _move?.Enable();
            _aimPointer?.Enable();
            _aimStick?.Enable();
        }

        private void OnDisable()
        {
            _move?.Disable();
            _aimPointer?.Disable();
            _aimStick?.Disable();
        }

        private void OnDestroy()
        {
            _move?.Dispose();
            _aimPointer?.Dispose();
            _aimStick?.Dispose();
        }
    }
}
