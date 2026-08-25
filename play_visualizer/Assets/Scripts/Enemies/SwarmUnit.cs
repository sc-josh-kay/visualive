using UnityEngine;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// Enemy 3 — Swarm unit (spec §7). Individually weak (1 HP, tiny consume) but dangerous in
    /// numbers. Units flock: they seek dense smoke together (cohesion toward same-type neighbors +
    /// separation so they don't fully overlap), forming a drifting cloud of visualizer-eating
    /// organisms. Each death is a small burst; clearing a whole swarm is a flurry of them. Turns on
    /// the player only when very close.
    /// </summary>
    public class SwarmUnit : EnemyBase
    {
        protected override void Behave(float dt)
        {
            Vector2 pos = transform.position;

            // Seek the smoke, or the player if they stray into the cloud.
            Vector2 seek = DistanceToPlayer(pos) < _config.PlayerChaseRadius && _target != null
                ? (Vector2)_target.position - pos
                : SmokeTargetWorld() - pos;
            if (seek.sqrMagnitude > 1e-6f) seek.Normalize();

            Vector2 sep = SeparationForce(_config.SeparationRadius, _config.SeparationStrength);
            Vector2 coh = CohesionForce(_config.SwarmCohesionRadius, _config.SwarmCohesionStrength);

            Vector2 dir = seek + sep + coh;
            if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
            _rb.linearVelocity = dir * (_config.Speed * SpeedMultiplier);

            if (VisualizerField.Instance != null)
            {
                VisualizerField.Instance.Consume(
                    pos, _config.ConsumeRadius, _config.ConsumeStrengthPerSecond * dt);
            }
        }
    }
}
