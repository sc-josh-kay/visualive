using System.Collections.Generic;
using UnityEngine;
using PlayVisualizer.Audio;
using PlayVisualizer.Gameplay;
using PlayVisualizer.Player;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// Shared plumbing for every enemy type (Color Eater, Corruptor, Swarm): health, projectile
    /// damage, targeting, the non-lethal player-contact disruption, and the death event — which is
    /// a VISUALIZER event, not a particle effect: destroying an enemy injects a color burst into the
    /// field via VisualizerField.Paint (spec §8), inheriting the current musical moment.
    ///
    /// Enemies interact with the visual world ONLY through VisualizerField (Consume / Paint) — never
    /// shaders. Movement and field interaction per type live in <see cref="Behave"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] protected EnemyConfig _config;
        [Tooltip("Optional small sprite flash on death. The real explosion is the field paint.")]
        [SerializeField] private DeathPop _deathPopPrefab;
        [Tooltip("Spark burst spawned when the enemy reaches the player (the 'puff of blackness'). " +
                 "Falls back to a plain consume if unset.")]
        [SerializeField] private CollisionBurst _collisionBurstPrefab;
        [Tooltip("Child transform holding the procedural visual. Music pulses scale/offset THIS, " +
                 "never the root (so the collider is never resized). Auto-found if unset.")]
        [SerializeField] private Transform _visualRoot;
        [Tooltip("Renderer of the procedural visual quad. Per-enemy shader uniforms are pushed to it " +
                 "via a MaterialPropertyBlock (no per-enemy material instances). Auto-found if unset.")]
        [SerializeField] private Renderer _visualRenderer;

        protected Rigidbody2D _rb;
        protected Transform _target;   // usually the player — the fallback steer target
        private AudioAnalyzer _analyzer;
        private MaterialPropertyBlock _mpb;
        private int _health;
        private bool _dead;

        /// <summary>The enemy's current music-driven color — inherited by its death explosion.</summary>
        protected Color CurrentColor = Color.white;

        // Cached base visual transform (music reactions modulate around these).
        private Vector3 _baseVisualScale = Vector3.one;
        private Vector3 _baseVisualPos = Vector3.zero;

        // Shared music envelopes (decaying), so per-type reactions read a smooth pulse, not a flag.
        /// <summary>1 on a beat, decaying to 0 (~0.18s). For rhythmic visual/movement pulses.</summary>
        protected float BeatEnv { get; private set; }
        /// <summary>Rises on a bass onset (by strength), decaying to 0 (~0.22s).</summary>
        protected float BassOnsetEnv { get; private set; }

        // Per-enemy random offset on the smoke target, so a crowd doesn't converge on one point.
        private Vector2 _jitter;
        private float _jitterTimer;

        // Live registry, used for cheap separation (anti-clumping) between enemies.
        private static readonly List<EnemyBase> All = new List<EnemyBase>();

        public EnemyConfig Config => _config;

        /// <summary>Damage every live enemy within radius (used by the Vortex Wave).</summary>
        public static void ApplyRadialDamage(Vector2 center, float radius, int damage)
        {
            if (damage <= 0 || radius <= 0f) return;
            float r2 = radius * radius;
            for (int i = All.Count - 1; i >= 0; i--)
            {
                EnemyBase e = All[i];
                if (e == null) continue;
                if (((Vector2)e.transform.position - center).sqrMagnitude <= r2) e.TakeDamage(damage);
            }
        }

        /// <summary>Nearest live enemy to a point within maxDist (null if none). Used by homing weapons.</summary>
        public static EnemyBase FindNearest(Vector2 pos, float maxDist)
        {
            EnemyBase best = null;
            float bestSq = maxDist * maxDist;
            for (int i = 0; i < All.Count; i++)
            {
                EnemyBase e = All[i];
                if (e == null) continue;
                float sq = ((Vector2)e.transform.position - pos).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = e; }
            }
            return best;
        }

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_visualRenderer == null) _visualRenderer = GetComponentInChildren<Renderer>();
            if (_visualRoot == null) _visualRoot = _visualRenderer != null ? _visualRenderer.transform : transform;
            _mpb = new MaterialPropertyBlock();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _analyzer = FindFirstObjectByType<AudioAnalyzer>();
        }

        protected virtual void OnEnable()
        {
            if (_config != null)
            {
                _health = _config.Health;
                transform.localScale = Vector3.one * _config.Scale;
            }
            if (_visualRoot != null)
            {
                _baseVisualScale = _visualRoot.localScale;
                _baseVisualPos = _visualRoot.localPosition;
            }
            BeatEnv = 0f;
            BassOnsetEnv = 0f;
            RerollJitter();
            All.Add(this);
        }

        private void Update()
        {
            if (_dead || _config == null) return;

            MusicState s = Music;
            float dt = Time.deltaTime;

            // Decaying music envelopes shared by the per-type visual/movement reactions.
            BeatEnv = Mathf.Max(0f, BeatEnv - dt / 0.18f);
            if (s != null && s.Beat) BeatEnv = 1f;
            BassOnsetEnv = Mathf.Max(0f, BassOnsetEnv - dt / 0.22f);
            if (s != null && s.BassOnset)
            {
                float strength = s.BassOnsetStrength > 0f ? Mathf.Clamp01(s.BassOnsetStrength) : 1f;
                BassOnsetEnv = Mathf.Max(BassOnsetEnv, strength);
            }

            UpdateVisual(s, dt);
        }

        /// <summary>Per-type cosmetic reaction to the music (pulse/jitter). Modulates the VISUAL child.</summary>
        protected virtual void UpdateVisual(MusicState s, float dt) { }

        /// <summary>Set the visual child's scale as a multiplier of its base (cosmetic only).</summary>
        protected void SetVisualScale(float multiplier)
        {
            if (_visualRoot != null) _visualRoot.localScale = _baseVisualScale * multiplier;
        }

        /// <summary>Offset the visual child in local space (cosmetic jitter; never moves the collider).</summary>
        protected void SetVisualOffset(Vector2 localOffset)
        {
            if (_visualRoot != null) _visualRoot.localPosition = _baseVisualPos + (Vector3)localOffset;
        }

        /// <summary>
        /// Set the visual child's BASE scale (which pulses then multiply). Use a non-uniform value
        /// for per-enemy shape variety (e.g. an oblong black hole). Cosmetic; collider unchanged.
        /// </summary>
        protected void SetBaseVisualScale(Vector3 scale)
        {
            _baseVisualScale = scale;
            if (_visualRoot != null) _visualRoot.localScale = scale;
        }

        /// <summary>Rotate the visual child in-plane (cosmetic; e.g. tilt an oblong/disk).</summary>
        protected void SetVisualRotation(float degrees)
        {
            if (_visualRoot != null) _visualRoot.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        /// <summary>Get the per-enemy property block (pre-loaded with current values) to set uniforms on.</summary>
        protected MaterialPropertyBlock VisualBlock
        {
            get
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                if (_visualRenderer != null) _visualRenderer.GetPropertyBlock(_mpb);
                return _mpb;
            }
        }

        /// <summary>Push the property block back to the visual renderer.</summary>
        protected void ApplyVisualBlock()
        {
            if (_visualRenderer != null && _mpb != null) _visualRenderer.SetPropertyBlock(_mpb);
        }

        protected virtual void OnDisable()
        {
            All.Remove(this);
        }

        /// <summary>Called by the spawner right after instantiation.</summary>
        public void SetTarget(Transform target) => _target = target;

        /// <summary>Generic music-driven speed multiplier (written by the MusicMapper).</summary>
        protected float SpeedMultiplier =>
            EnemyModulation.Instance != null ? EnemyModulation.Instance.SpeedMultiplier : 1f;

        /// <summary>Current music snapshot (may be null before the analyzer exists).</summary>
        protected MusicState Music => _analyzer != null ? _analyzer.State : null;

        /// <summary>
        /// The smoke the enemy wants to eat: the densest painted "hot" region (else the player),
        /// nudged by this enemy's random jitter so a crowd spreads out instead of stacking.
        /// </summary>
        protected Vector2 SmokeTargetWorld()
        {
            VisualizerField field = VisualizerField.Instance;
            Vector2 baseTarget = field != null && field.HasHotPoint
                ? field.HotWorldPos
                : (_target != null ? (Vector2)_target.position : (Vector2)transform.position);
            return baseTarget + _jitter;
        }

        /// <summary>Distance to the player (∞ if there is none), for proximity decisions.</summary>
        protected float DistanceToPlayer(Vector2 pos)
        {
            return _target != null ? Vector2.Distance(pos, _target.position) : float.PositiveInfinity;
        }

        /// <summary>
        /// A steer-away push from nearby enemies (anti-clumping). Stronger the closer they are;
        /// zero beyond <paramref name="radius"/>. Cheap O(n²) over the live registry (n is small).
        /// </summary>
        protected Vector2 SeparationForce(float radius, float strength)
        {
            if (radius <= 0f || strength <= 0f) return Vector2.zero;
            Vector2 pos = transform.position;
            Vector2 sum = Vector2.zero;
            float r2 = radius * radius;
            for (int i = 0; i < All.Count; i++)
            {
                EnemyBase o = All[i];
                if (o == null || o == this) continue;
                Vector2 d = pos - (Vector2)o.transform.position;
                float sq = d.sqrMagnitude;
                if (sq < r2 && sq > 1e-6f)
                {
                    float dist = Mathf.Sqrt(sq);
                    sum += (d / dist) * (1f - dist / radius); // closer → stronger push
                }
            }
            return sum * strength;
        }

        /// <summary>
        /// A pull toward the average position of nearby enemies of the SAME type (flock cohesion,
        /// used by the Swarm). Normalized direction × <paramref name="strength"/>.
        /// </summary>
        protected Vector2 CohesionForce(float radius, float strength)
        {
            if (radius <= 0f || strength <= 0f) return Vector2.zero;
            Vector2 pos = transform.position;
            Vector2 sum = Vector2.zero;
            int count = 0;
            float r2 = radius * radius;
            System.Type type = GetType();
            for (int i = 0; i < All.Count; i++)
            {
                EnemyBase o = All[i];
                if (o == null || o == this || o.GetType() != type) continue;
                Vector2 op = o.transform.position;
                if ((op - pos).sqrMagnitude < r2)
                {
                    sum += op;
                    count++;
                }
            }
            if (count == 0) return Vector2.zero;
            Vector2 toCentroid = sum / count - pos;
            if (toCentroid.sqrMagnitude > 1e-6f) toCentroid.Normalize();
            return toCentroid * strength;
        }

        private void RerollJitter()
        {
            float j = _config != null ? _config.TargetJitter : 0f;
            _jitter = Random.insideUnitCircle * j;
            _jitterTimer = Random.Range(1.5f, 3f);
        }

        private void FixedUpdate()
        {
            if (_config == null || _dead) return;

            _jitterTimer -= Time.deltaTime;
            if (_jitterTimer <= 0f) RerollJitter();

            Behave(Time.deltaTime);
            ApplyVortexForces();
        }

        /// <summary>
        /// External pull from any active Vortex Wave: an additive influence on the velocity Behave
        /// just set (pull inward + slow), so vortices bend enemy movement without touching their AI.
        /// </summary>
        private void ApplyVortexForces()
        {
            var list = Weapons.Vortex.Active;
            if (list.Count == 0) return;

            Vector2 pos = transform.position;
            Vector2 vel = _rb.linearVelocity;
            for (int i = 0; i < list.Count; i++)
            {
                Weapons.Vortex v = list[i];
                if (v == null || !v.IsPulling) continue;
                Vector2 to = v.Center - pos;
                float d = to.magnitude;
                if (d > 0.01f && d < v.CurrentRadius)
                {
                    float falloff = 1f - d / v.CurrentRadius;
                    vel = vel * (1f - v.SlowFactor * falloff) + (to / d) * (v.PullStrength * falloff);
                }
            }
            _rb.linearVelocity = vel;
        }

        /// <summary>Per-physics-step behavior: movement + field interaction. Implemented per type.</summary>
        protected abstract void Behave(float dt);

        public void TakeDamage(int amount)
        {
            if (_dead) return;
            _health -= amount;
            if (_health <= 0) Die();
        }

        // Unity invokes this on the concrete instance even though it lives on the base.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_dead) return;

            // Reaching the player is the enemy's OTHER way to hurt the world (spec §4/§15 — still no
            // damage to the player). It spends itself in a "puff of blackness": a strong instantaneous
            // consume right here (a black hole, no color, no score) plus a trail disruption. This is
            // deliberately WORSE than its passive nibbling, and gives the player nothing — so it's
            // always better to shoot enemies than to let them reach you.
            if (other.TryGetComponent(out PlayerTrail trail))
            {
                trail.Disrupt();

                Vector2 at = transform.position;
                if (_collisionBurstPrefab != null)
                {
                    // The burst owns both the spark visual and the growing black-hole consume, and
                    // outlives this enemy (spawned slightly in front of the gameplay plane).
                    Color c = CurrentColor;
                    var burst = Instantiate(_collisionBurstPrefab,
                        new Vector3(at.x, at.y, -0.1f), Quaternion.identity);
                    burst.Play(c, HitConsumeRadius, HitBurstDuration, HitConsumeStrength);
                }
                else if (VisualizerField.Instance != null)
                {
                    VisualizerField.Instance.Consume(at, HitConsumeRadius, HitConsumeStrength);
                }

                _dead = true;
                Destroy(gameObject);
            }
        }

        // Death-explosion size. Types with bigger threats (Corruptor) override these.
        protected virtual float DeathPaintRadius => _config != null ? _config.DeathPaintRadius : 1.5f;
        protected virtual float DeathPaintIntensity => _config != null ? _config.DeathPaintIntensity : 1f;

        // Collision "puff of blackness" size.
        protected virtual float HitConsumeRadius => _config != null ? _config.HitConsumeRadius : 1.8f;
        protected virtual float HitConsumeStrength => _config != null ? _config.HitConsumeStrength : 1f;
        protected virtual float HitBurstDuration => _config != null ? _config.HitBurstDuration : 0.35f;

        private void Die()
        {
            if (_dead) return;
            _dead = true;

            MusicState s = Music;
            float bass = s != null ? s.Bass : 0f;
            float energy = s != null ? s.Energy : 0f;
            bool beat = s != null && s.Beat;

            // Advance the combo (this kill included), then score with the multiplier.
            float mult = 1f;
            ScoreManager sm = ScoreManager.Instance;
            if (sm != null)
            {
                sm.RegisterEnemyKill();
                mult = sm.Multiplier;
                if (_config != null) sm.AddScore(Mathf.RoundToInt(_config.ScoreValue * mult));
            }

            // The EXPLOSION is the payoff (spec7 §11): a persistent color burst painted into the field,
            // scaled by the music (Bass → radius, Energy → brightness, Beat → pulse) AND the combo, so
            // rapid kills produce dramatically bigger coverage bursts. Killing = the real way to paint.
            Vector2 at = transform.position;
            float radius = DeathPaintRadius * (1f + 0.6f * bass) * (0.7f + 0.3f * mult);
            float intensity = DeathPaintIntensity * (0.7f + 0.6f * energy) * (beat ? 1.3f : 1f) * (0.6f + 0.4f * mult);
            if (VisualizerField.Instance != null)
            {
                VisualizerField.Instance.Paint(at, radius, intensity, s);
            }

            // The energetic initial flash — the firework in the enemy's own color, VISUAL ONLY (no
            // consume), which the persistent paint above settles behind.
            Color c = CurrentColor;
            if (_collisionBurstPrefab != null)
            {
                var burst = Instantiate(_collisionBurstPrefab, new Vector3(at.x, at.y, -0.1f), Quaternion.identity);
                burst.Play(c, radius * 1.1f, 0.4f, 0f); // consumeStrength 0 → paints nothing
            }
            else if (_deathPopPrefab != null)
            {
                DeathPop pop = Instantiate(_deathPopPrefab, transform.position, Quaternion.identity);
                pop.Play(c);
            }

            Destroy(gameObject);
        }
    }
}
