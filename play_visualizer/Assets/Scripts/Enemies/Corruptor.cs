using UnityEngine;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// Enemy 2 — Corruptor (spec §6). Slow and territorial: it drifts to a high-value smoke region,
    /// entrenches, and grows an expanding area of blackness that can gut coverage if ignored — a
    /// "deal with this now" threat. It targets the smoke, not the player. It is tanky, and killing it
    /// pays off with a much larger color explosion than a Color Eater (via its config's big
    /// DeathPaint values). The corruption uses the same non-uniform consume, so the blackened region
    /// is organic and alive, not a clean disc.
    /// </summary>
    public class Corruptor : EnemyBase
    {
        private float _age;

        protected override void OnEnable()
        {
            base.OnEnable();
            _age = 0f;
        }

        protected override void Behave(float dt)
        {
            _age += dt;
            float grow = _config.CorruptGrowSeconds > 0f
                ? Mathf.Clamp01(_age / _config.CorruptGrowSeconds)
                : 1f;
            float radius = Mathf.Lerp(_config.CorruptStartRadius, _config.CorruptMaxRadius, grow);

            Vector2 pos = transform.position;

            // Slow drift toward the smoke, slowing further as it entrenches and spreads.
            Vector2 dir = SmokeTargetWorld() - pos;
            if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
            dir += SeparationForce(_config.SeparationRadius, _config.SeparationStrength);
            if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
            _rb.linearVelocity = dir * (_config.Speed * SpeedMultiplier * (1f - 0.7f * grow));

            // The expanding corruption: a growing (non-uniform) consume area.
            if (VisualizerField.Instance != null)
            {
                VisualizerField.Instance.Consume(pos, radius, _config.ConsumeStrengthPerSecond * dt);
            }
        }
    }
}
