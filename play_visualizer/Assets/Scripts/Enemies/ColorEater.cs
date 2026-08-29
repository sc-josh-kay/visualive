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
        private static readonly int BaseHueId = Shader.PropertyToID("_BaseHue");
        private static readonly int RingCountId = Shader.PropertyToID("_RingCount");
        private static readonly int Time0Id = Shader.PropertyToID("_Time0");
        private static readonly int WobbleId = Shader.PropertyToID("_Wobble");
        private static readonly int WobbleTrebleId = Shader.PropertyToID("_WobbleTreble");
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");

        private float _seed;
        private int _ringCount;

        protected override void OnEnable()
        {
            base.OnEnable();
            _seed = Random.value * 100f;      // per-enemy ring arrangement
            _ringCount = Random.Range(3, 6);  // 3..5 stacked rings
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
            // Rhythmic scale pulse on the beat (cosmetic; collider unchanged).
            SetVisualScale(1f + BeatEnv * _config.BeatPulseScale);

            // Stacked color rings: base hue drifts with the music; each ring picks its own hue,
            // offset, and wobble from the seed. Energy drives wobble, treble adds finer vibration.
            float energy = s != null ? s.Energy : 0f;
            float treble = s != null ? s.Treble : 0f;
            float centroid = s != null ? s.SpectralCentroid : 0.7f;
            float baseHue = Mathf.Repeat(centroid + Time.time * 0.02f, 1f);
            CurrentColor = Color.HSVToRGB(baseHue, 0.9f, 1f);

            var mpb = VisualBlock;
            mpb.SetFloat(BaseHueId, baseHue);
            mpb.SetFloat(RingCountId, _ringCount);
            mpb.SetFloat(SeedId, _seed);
            mpb.SetFloat(WobbleId, 0.015f + energy * 0.045f + BeatEnv * 0.03f);
            mpb.SetFloat(WobbleTrebleId, treble * 0.035f);
            mpb.SetFloat(PulseId, BeatEnv);
            mpb.SetFloat(Time0Id, Time.time);
            ApplyVisualBlock();
        }
    }
}
