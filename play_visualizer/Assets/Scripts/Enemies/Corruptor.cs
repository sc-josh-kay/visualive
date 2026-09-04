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
        private static readonly int ColorAId = Shader.PropertyToID("_ColorA");
        private static readonly int ColorBId = Shader.PropertyToID("_ColorB");
        private static readonly int Time0Id = Shader.PropertyToID("_Time0");
        private static readonly int BassId = Shader.PropertyToID("_Bass");
        private static readonly int FlareId = Shader.PropertyToID("_Flare");
        private static readonly int SpinId = Shader.PropertyToID("_Spin");

        // Palette (teal / pink) — matches the CorruptorVisualizer.
        private static readonly Color Teal = new Color(0.1f, 0.95f, 1f);
        private static readonly Color Pink = new Color(1f, 0.25f, 0.8f);

        [Tooltip("How fast the inner spiral pattern rotates (the waveform itself stays fixed).")]
        [SerializeField] private float _spiralSpin = 1.6f;

        private float _baseRot;
        private float _swirl;   // signed drain-swirl direction/strength (per enemy)
        private float _age;

        protected override void OnEnable()
        {
            base.OnEnable();
            _age = 0f;

            // Variety: some Corruptors are oblong (stretched black holes) at a random starting tilt;
            // the whole thing then slowly swirls (see UpdateVisual). Each also drains in its own dir.
            float stretch = Random.value < 0.45f ? Random.Range(1.15f, 1.45f) : 1f;
            SetBaseVisualScale(new Vector3(stretch, 1f, 1f));
            _baseRot = Random.Range(0f, 360f);
            SetVisualRotation(_baseRot);
            _swirl = (Random.value < 0.5f ? -1f : 1f) * Random.Range(0.7f, 1.2f);
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

            // Black-hole VACUUM: pull + swirl the smoke inward and eat it (replaces plain consume).
            // The suck radius grows with bass and pulses wider on a bass onset; strength bass-boosted.
            if (VisualizerField.Instance != null)
            {
                float suckRadius = radius * (1f + BassOnsetEnv * 0.5f);
                float strength = Mathf.Clamp01(0.5f + bass * 0.5f);
                VisualizerField.Instance.Vacuum(pos, suckRadius, strength, _swirl);
            }
        }

        protected override void UpdateVisual(MusicState s, float dt)
        {
            // Bass "thump" only — the design keeps its fixed oblong tilt (waveform doesn't rotate);
            // the INNER SPIRAL rotates instead, driven in the shader by _Spin.
            float bass = s != null ? s.Bass : 0f;
            SetVisualScale(1f + bass * _config.BassPulseScale + BassOnsetEnv * _config.BassPulseScale * 0.6f);

            // Swirling black-hole glow: teal↔pink palette, brightening with bass, flaring on onset.
            CurrentColor = Pink;
            var mpb = VisualBlock;
            mpb.SetColor(ColorAId, Teal);
            mpb.SetColor(ColorBId, Pink);
            mpb.SetFloat(Time0Id, Time.time);
            mpb.SetFloat(BassId, bass);
            mpb.SetFloat(FlareId, BassOnsetEnv);
            mpb.SetFloat(SpinId, _spiralSpin);
            ApplyVisualBlock();
        }
    }
}
