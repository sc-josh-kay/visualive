using UnityEngine;
using PlayVisualizer.Audio;
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
        private ColorEaterVisualizer _visualizer;

        protected override void Awake()
        {
            base.Awake();
            _visualizer = GetComponentInChildren<ColorEaterVisualizer>();
        }

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

            // Rhythm/Energy personality (spec8): Energy → modestly faster; Beat → a brief FORWARD
            // nudge (along the heading, never a redirect). Both bounded.
            MusicState s = Music;
            float energy = s != null ? s.Energy : 0f;
            float speed = _config.Speed * (1f + energy * _config.EnergySpeedInfluence) * SpeedMultiplier;
            float impulse = _config.BeatImpulse * BeatEnv;
            _rb.linearVelocity = dir * (speed + impulse);

            // Passive nibbling: eat a little coverage wherever it is → a blackened region in its wake.
            if (VisualizerField.Instance != null)
            {
                VisualizerField.Instance.Consume(
                    pos, _config.ConsumeRadius, _config.ConsumeStrengthPerSecond * dt);
            }
        }

        protected override void UpdateVisual(MusicState s, float dt)
        {
            // Subtle overall bass pulse (weaker than the Corruptor) + a tiny beat lift — scales the
            // whole ring creature (visual child only; collider unchanged).
            float bass = s != null ? s.Bass : 0f;
            SetVisualScale(1f + bass * 0.08f + BeatEnv * _config.BeatPulseScale * 0.4f);

            // Death explosion inherits the enemy's fixed (spawn-chosen) color.
            if (_visualizer != null) CurrentColor = _visualizer.BaseColor;

            // It's always eating — tell the visualizer so the rings stay a touch more alive.
            if (_visualizer != null) _visualizer.Consuming = 1f;
        }
    }
}
