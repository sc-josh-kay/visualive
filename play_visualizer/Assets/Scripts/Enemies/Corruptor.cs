using UnityEngine;
using PlayVisualizer.Audio;
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
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int Time0Id = Shader.PropertyToID("_Time0");
        private static readonly int BassId = Shader.PropertyToID("_Bass");
        private static readonly int FlareId = Shader.PropertyToID("_Flare");

        private float _age;

        protected override void OnEnable()
        {
            base.OnEnable();
            _age = 0f;

            // Variety: some Corruptors are oblong (stretched black holes) at a random tilt.
            float stretch = Random.value < 0.45f ? Random.Range(1.15f, 1.45f) : 1f;
            SetBaseVisualScale(new Vector3(stretch, 1f, 1f));
            SetVisualRotation(Random.Range(0f, 360f));
        }

        protected override void Behave(float dt)
        {
            _age += dt;
            float grow = _config.CorruptGrowSeconds > 0f
                ? Mathf.Clamp01(_age / _config.CorruptGrowSeconds)
                : 1f;
            float radius = Mathf.Lerp(_config.CorruptStartRadius, _config.CorruptMaxRadius, grow);

            // Bass personality (spec8): bass temporarily swells the corruption (bounded, so bass can't
            // cause runaway coverage loss), and a bass ONSET produces a small forward lunge.
            MusicState s = Music;
            float bass = s != null ? s.Bass : 0f;
            radius *= 1f + bass * _config.BassCorruptRadius;

            Vector2 pos = transform.position;

            // Slow drift toward the smoke, slowing further as it entrenches and spreads.
            Vector2 dir = SmokeTargetWorld() - pos;
            if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
            dir += SeparationForce(_config.SeparationRadius, _config.SeparationStrength);
            if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
            float lunge = _config.BassLungeImpulse * BassOnsetEnv;
            _rb.linearVelocity = dir * (_config.Speed * SpeedMultiplier * (1f - 0.7f * grow) + lunge);

            // The expanding corruption: a growing (non-uniform) consume area.
            if (VisualizerField.Instance != null)
            {
                VisualizerField.Instance.Consume(pos, radius, _config.ConsumeStrengthPerSecond * dt);
            }
        }

        protected override void UpdateVisual(MusicState s, float dt)
        {
            // Smooth bass "thump": body swells with bass, harder on a bass onset (cosmetic).
            float bass = s != null ? s.Bass : 0f;
            float energy = s != null ? s.Energy : 0f;
            SetVisualScale(1f + bass * _config.BassPulseScale + BassOnsetEnv * _config.BassPulseScale * 0.6f);

            // Black hole: warm accretion disk/glow, brightening + thickening with bass; onset flare.
            Color c = Color.HSVToRGB(Mathf.Repeat(0.06f + energy * 0.03f, 1f), 0.9f, 1f);
            CurrentColor = c;

            var mpb = VisualBlock;
            mpb.SetColor(ColorId, c);
            mpb.SetFloat(Time0Id, Time.time);
            mpb.SetFloat(BassId, bass);
            mpb.SetFloat(FlareId, BassOnsetEnv);
            ApplyVisualBlock();
        }
    }
}
