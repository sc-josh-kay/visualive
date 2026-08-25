using UnityEngine;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// Enemy 1 — Color Eater (spec §5). The basic threat: drifts toward the densest painted region
    /// and eats coverage as it moves, leaving a blackened trail behind it. It never targets or harms
    /// the player; it only threatens the world's color (and therefore the score). A simple, always-
    /// present reason to keep moving and keep shooting.
    /// </summary>
    public class ColorEater : EnemyBase
    {
        protected override void Behave(float dt)
        {
            Vector2 pos = transform.position;

            // Seek the densest smoke, but turn on the player when they get close — so being near a
            // Color Eater is dangerous (it will come corrupt you), while at range it eats your art.
            Vector2 target = DistanceToPlayer(pos) < _config.PlayerChaseRadius && _target != null
                ? (Vector2)_target.position
                : SmokeTargetWorld();

            Vector2 seek = target - pos;
            if (seek.sqrMagnitude > 1e-6f) seek.Normalize();

            // Blend in separation so a crowd spreads out instead of stacking into one harmless blob.
            Vector2 dir = seek + SeparationForce(_config.SeparationRadius, _config.SeparationStrength);
            if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
            _rb.linearVelocity = dir * (_config.Speed * SpeedMultiplier);

            // Passive nibbling: eat a little coverage wherever it is → a blackened region in its wake.
            if (VisualizerField.Instance != null)
            {
                VisualizerField.Instance.Consume(
                    pos, _config.ConsumeRadius, _config.ConsumeStrengthPerSecond * dt);
            }
        }
    }
}
