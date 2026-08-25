using UnityEngine;

namespace PlayVisualizer.Player
{
    /// <summary>
    /// Moves and orients the player ship on the XY plane.
    ///
    /// Movement and aiming are intentionally DECOUPLED (twin-stick style): the ship travels
    /// in the Move direction while facing the Aim point, independently. Motion is arcade-snappy
    /// (velocity set directly, no acceleration) rather than a physics simulation, per the spec.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;

        [Tooltip("Degrees to add so the sprite's 'forward' points at the aim. A sprite that " +
                 "points UP (+Y) by default needs -90 to align its tip with the aim direction.")]
        [SerializeField] private float _spriteFacingOffset = -90f;

        private Rigidbody2D _rb;
        private PlayerInputReader _input;
        private Camera _camera;

        /// <summary>Current normalized aim direction in world space. Read by the weapon system.</summary>
        public Vector2 AimDirection { get; private set; } = Vector2.right;

        /// <summary>Whether the player is actively aiming (weapons auto-fire while true).</summary>
        public bool IsAiming => _input != null && _input.IsAiming;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _input = GetComponent<PlayerInputReader>();
            _camera = Camera.main;

            // Top-down arcade motion: no gravity, and we drive rotation ourselves.
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            if (_config == null)
            {
                Debug.LogError("PlayerController: PlayerConfig is not assigned.", this);
            }
            if (_camera == null)
            {
                Debug.LogError("PlayerController: no Camera tagged 'MainCamera' found.", this);
            }
        }

        private void Update()
        {
            // Freeze aiming while the game is paused / over (timeScale 0), so a dead ship
            // doesn't keep tracking the mouse.
            if (Time.timeScale == 0f)
            {
                return;
            }

            UpdateAim();
        }

        private void FixedUpdate()
        {
            float speed = _config != null ? _config.MoveSpeed : 0f;
            _rb.linearVelocity = _input.MoveInput * speed;
        }

        private void UpdateAim()
        {
            // Right stick wins when deflected (true twin-stick / future touch zone); otherwise aim
            // toward the pointer (the desktop right-stick stand-in).
            if (_input.HasStickAim)
            {
                AimDirection = _input.AimStick.normalized;
            }
            else if (_camera != null)
            {
                Vector3 screen = _input.AimScreenPosition;
                screen.z = transform.position.z - _camera.transform.position.z;
                Vector3 world = _camera.ScreenToWorldPoint(screen);

                Vector2 toAim = (Vector2)(world - transform.position);
                if (toAim.sqrMagnitude < 0.0001f)
                {
                    return; // Pointer is essentially on the ship; keep the last facing.
                }
                AimDirection = toAim.normalized;
            }

            float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + _spriteFacingOffset);
        }
    }
}
