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
        public float ConsumeStrengthPerSecond = 8f;

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
    }
}
