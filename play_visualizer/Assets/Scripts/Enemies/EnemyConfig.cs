using UnityEngine;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// Per-enemy tuning. These values are plain data today; the music mapper (Phase 6) will
    /// be able to modulate some of them (e.g. Speed, Scale) without the enemy knowing.
    /// Create via: Assets → Create → PlayVisualizer → Enemy Config.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "PlayVisualizer/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [Tooltip("Chase speed in units per second.")]
        public float Speed = 3f;

        [Tooltip("Hit points. Projectiles deal 1 by default.")]
        public int Health = 3;

        [Tooltip("Damage dealt to the player on contact.")]
        public int ContactDamage = 25;

        [Tooltip("Uniform world scale of the enemy sprite.")]
        public float Scale = 0.8f;

        [Tooltip("Points awarded when destroyed by the player.")]
        public int ScoreValue = 100;

        [Header("Field interaction — consume")]
        [Tooltip("World radius of the smoke the enemy eats around itself each frame (a non-uniform, " +
                 "wobbling aura, not just what it touches).")]
        public float ConsumeRadius = 1.6f;

        [Tooltip("How fast it eats coverage (per second). Applied as strength = this × deltaTime.")]
        public float ConsumeStrengthPerSecond = 9.2f;

        [Header("Death — visualizer explosion (spec §8)")]
        [Tooltip("World radius of the color burst painted into the field when destroyed.")]
        public float DeathPaintRadius = 1.6f;

        [Tooltip("Strength/brightness of that color burst. Bigger enemies should paint more.")]
        public float DeathPaintIntensity = 1.2f;

        [Header("Targeting")]
        [Tooltip("If the player is within this distance, the enemy chases the player instead of the " +
                 "densest smoke. Higher = more aggressive toward the player. 0 = never chase player.")]
        public float PlayerChaseRadius = 4f;

        [Tooltip("Random offset applied to the smoke target so enemies don't all converge on the " +
                 "exact same point (anti-clumping). Re-rolled periodically.")]
        public float TargetJitter = 2.5f;

        [Tooltip("Enemies steer away from other enemies within this radius (anti-clumping).")]
        public float SeparationRadius = 1.2f;

        [Tooltip("Strength of that separation push relative to the seek direction.")]
        public float SeparationStrength = 1.5f;

        [Header("Collision with player — puff of blackness")]
        [Tooltip("When it reaches the player the enemy spends itself: it consumes a burst of smoke " +
                 "here (a black hole, no color, no score) and disrupts the player's trail. World radius.")]
        public float HitConsumeRadius = 1.8f;

        [Tooltip("Strength of that collision consume. 1 = near-total black at the center. Should make " +
                 "getting hit clearly WORSE than the enemy's passive nibbling.")]
        public float HitConsumeStrength = 1f;

        [Tooltip("Seconds the collision burst takes to expand its outline and scoop the smoke to " +
                 "black. Fast — think a quick pop.")]
        public float HitBurstDuration = 0.35f;

        [Header("Corruptor — expanding corruption")]
        [Tooltip("World radius of the corruption when it first arrives.")]
        public float CorruptStartRadius = 0.8f;

        [Tooltip("World radius the corruption grows to if left alive.")]
        public float CorruptMaxRadius = 4f;

        [Tooltip("Seconds to grow from start to max radius.")]
        public float CorruptGrowSeconds = 8f;

        [Header("Swarm — flocking")]
        [Tooltip("Swarm units pull toward the average position of same-type neighbors within this " +
                 "radius (cloud cohesion).")]
        public float SwarmCohesionRadius = 3f;

        [Tooltip("Strength of that cohesion pull relative to the seek direction.")]
        public float SwarmCohesionStrength = 0.6f;

        // ── Music reaction (spec8) ──────────────────────────────────────────────
        // Bounded per-type reactions so each enemy has a musical personality without becoming
        // unpredictable. Each type only uses the fields for its channel.

        [Header("Music reaction — Color Eater (Energy/Beat)")]
        [Tooltip("Energy → extra move speed, as a fraction (0.4 = up to +40% at full energy).")]
        [Range(0f, 1f)] public float EnergySpeedInfluence = 0.4f;
        [Tooltip("Visual scale bump on each beat (cosmetic; no collider change).")]
        [Range(0f, 0.6f)] public float BeatPulseScale = 0.18f;
        [Tooltip("Small forward speed nudge on a beat (units/sec, decays quickly). Along its heading.")]
        public float BeatImpulse = 2f;

        [Header("Music reaction — Corruptor (Bass)")]
        [Tooltip("Bass → visual scale 'thump' (cosmetic).")]
        [Range(0f, 0.8f)] public float BassPulseScale = 0.3f;
        [Tooltip("Forward lunge speed on a bass onset (units/sec, decays). Along its heading.")]
        public float BassLungeImpulse = 2.5f;
        [Tooltip("Bass → temporary extra corruption radius, as a fraction of the current radius (bounded).")]
        [Range(0f, 0.8f)] public float BassCorruptRadius = 0.35f;

        [Header("Music reaction — Swarm (Treble/Flux)")]
        [Tooltip("Treble → movement + visual jitter amplitude (bounded 'buzz').")]
        [Range(0f, 1f)] public float TrebleJitter = 0.35f;
        [Tooltip("Spectral flux → extra agitation on top of treble.")]
        [Range(0f, 1f)] public float FluxAgitation = 0.35f;
        [Tooltip("Fraction of flock cohesion lost at full treble (frantic scattering).")]
        [Range(0f, 1f)] public float TrebleCohesionLoss = 0.5f;

        [Header("Swarm — turbulent tear")]
        [Tooltip("World radius of the turbulent-tear field each unit emits (displaces/stretches/eats " +
                 "the smoke along its motion). Nearby units are aggregated into shared zones.")]
        public float TurbulenceRadius = 1.1f;

        [Tooltip("Extra per-unit consume rate at full agitation (treble/flux), as a fraction " +
                 "(1 = up to +100% shred rate on a high-treble section).")]
        [Range(0f, 3f)] public float AgitationConsumeBoost = 1.2f;
    }
}
