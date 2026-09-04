using UnityEngine;
using PlayVisualizer.Enemies;
using PlayVisualizer.Gameplay;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Player
{
    /// <summary>How to launch a projectile. Weapons fill this in per shot (music-tinted).</summary>
    public struct ProjectileSpec
    {
        public Vector2 Direction;
        public float Speed;
        public float Lifetime;
        public int Damage;
        public Color Color;            // energy/trail color, from MusicState

        public float StartScale;
        public float GrowthPerSecond;  // 0 = no growth (Growth Stream sets this)
        public float MaxScale;

        public float HomingStrength;   // 0 = straight; higher = curves harder toward enemies
        public float HomingRange;

        public float ImpactDistortion; // transient ripple strength on impact (energy, no coverage)
    }

    /// <summary>
    /// A weapon projectile — pure ENERGY (spec7): a bright, short-lived mark that damages enemies and
    /// transiently ripples the visualizer on impact, but NEVER paints persistent coverage. The physics
    /// live here (straight, predictable); the glowing "alive" look (soft core+halo head, wobbling
    /// tail, music pulse) lives on the <see cref="ProjectileVisual"/> child so it never affects flight
    /// or hits. Supports optional growth and homing so all weapons reuse one projectile.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private ProjectileVisual _visual;
        [Tooltip("Soft additive flash spawned where a shot lands (energy, no coverage).")]
        [SerializeField] private DeathPop _impactBloomPrefab;
        [SerializeField] private float _impactBloomScale = 0.35f;

        private Rigidbody2D _rb;
        private ProjectileSpec _spec;
        private float _scale;
        private EnemyBase _homingTarget;

        private ProjectilePool _pool;
        private Projectile _prototype;
        private float _despawnTime;
        private bool _alive;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
        }

        /// <summary>Set by the pool when this instance is created, so it can return itself.</summary>
        internal void SetPool(ProjectilePool pool, Projectile prototype)
        {
            _pool = pool;
            _prototype = prototype;
        }

        /// <summary>Launch with a full spec (weapons use this). Resets all per-shot state for reuse.</summary>
        public void Launch(ProjectileSpec spec)
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();

            _spec = spec;
            _scale = spec.StartScale > 0f ? spec.StartScale : 1f;
            transform.localScale = Vector3.one * _scale;
            _homingTarget = null;                       // reset (pooled instances are reused)
            _despawnTime = Time.time + spec.Lifetime;
            _alive = true;

            _rb.linearVelocity = spec.Direction.normalized * spec.Speed;

            if (_visual != null) _visual.Configure(_rb, spec.Color); // Configure clears the trail

        }

        /// <summary>Deactivate and return to the pool (or destroy if this wasn't pooled).</summary>
        private void Despawn()
        {
            if (!_alive) return;
            _alive = false;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            if (_pool != null) _pool.Release(this, _prototype);
            else Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (!_alive) return;
            if (Time.time >= _despawnTime) { Despawn(); return; }

            float dt = Time.deltaTime;

            // Growth: expand over distance, up to a cap (Growth Stream).
            if (_spec.GrowthPerSecond > 0f && _scale < _spec.MaxScale)
            {
                _scale = Mathf.Min(_spec.MaxScale, _scale + _spec.GrowthPerSecond * dt);
                transform.localScale = Vector3.one * _scale;
            }

            // Homing: steer velocity toward the nearest enemy (visibly curving, not snapping).
            if (_spec.HomingStrength > 0f)
            {
                if (_homingTarget == null)
                {
                    _homingTarget = EnemyBase.FindNearest(transform.position, _spec.HomingRange);
                }
                if (_homingTarget != null)
                {
                    Vector2 desired = ((Vector2)_homingTarget.transform.position - (Vector2)transform.position).normalized;
                    Vector2 vel = _rb.linearVelocity;
                    Vector2 newDir = Vector2.Lerp(vel.normalized, desired, _spec.HomingStrength * dt).normalized;
                    _rb.linearVelocity = newDir * _spec.Speed;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_alive) return;
            if (other.TryGetComponent(out IDamageable damageable))
            {
                int dmg = _spec.StartScale > 0f
                    ? Mathf.Max(1, Mathf.RoundToInt(_spec.Damage * (_scale / _spec.StartScale)))
                    : Mathf.Max(1, _spec.Damage);
                damageable.TakeDamage(dmg);

                // Energy interacts with the visualizer transiently — a ripple, never coverage.
                if (_spec.ImpactDistortion > 0f && VisualizerField.Instance != null)
                {
                    VisualizerField.Instance.Distort(transform.position, _spec.ImpactDistortion);
                }

                // Soft impact bloom (energy flash) in the shot's color.
                if (_impactBloomPrefab != null)
                {
                    DeathPop bloom = Instantiate(_impactBloomPrefab, transform.position, Quaternion.identity);
                    bloom.transform.localScale = Vector3.one * _impactBloomScale;
                    bloom.Play(_spec.Color);
                }

                Despawn();
            }
        }
    }
}
