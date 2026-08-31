using UnityEngine;
using PlayVisualizer.Audio;
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
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int Time0Id = Shader.PropertyToID("_Time0");
        private static readonly int FlutterId = Shader.PropertyToID("_Flutter");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int ArmsId = Shader.PropertyToID("_Arms");

        // Per-unit visual identity, rolled once so no two units look identical (spec10 "waveform
        // particles"): a warm base hue (orange→yellow→red, with an occasional deep purple), a shape
        // seed (squiggle phase / orientation), and an arm count.
        private float _baseHue;
        private float _seed;
        private float _arms;

        protected override void Awake()
        {
            base.Awake();
            _baseHue = Random.value < 0.85f
                ? Random.Range(0.00f, 0.11f)   // red → orange → yellow (dominant)
                : Random.Range(0.76f, 0.80f);  // occasional deep purple
            _seed = Random.value;
            _arms = Random.Range(3, 7);         // 3..6 tendrils
        }

        protected override void Behave(float dt)
        {
            Vector2 pos = transform.position;

            // Seek the smoke, or the player if they stray into the cloud.
            Vector2 seek = DistanceToPlayer(pos) < _config.PlayerChaseRadius && _target != null
                ? (Vector2)_target.position - pos
                : SmokeTargetWorld() - pos;
            if (seek.sqrMagnitude > 1e-6f) seek.Normalize();

            // Treble/Flux personality (spec8): high frequencies make the swarm "buzz" — more per-unit
            // perturbation and weaker cohesion (frantic scattering). Targeting is preserved; the
            // perturbation is bounded so units stay readable.
            MusicState s = Music;
            float treble = s != null ? s.Treble : 0f;
            float flux = s != null ? Mathf.Clamp01(s.SpectralFlux) : 0f;
            float agitation = Mathf.Clamp01(treble * _config.TrebleJitter + flux * _config.FluxAgitation);
            float cohesion = _config.SwarmCohesionStrength * (1f - treble * _config.TrebleCohesionLoss);

            Vector2 sep = SeparationForce(_config.SeparationRadius, _config.SeparationStrength);
            Vector2 coh = CohesionForce(_config.SwarmCohesionRadius, cohesion);
            Vector2 perturb = Random.insideUnitCircle * agitation;

            Vector2 dir = seek + sep + coh + perturb;
            if (dir.sqrMagnitude > 1e-6f) dir.Normalize();
            _rb.linearVelocity = dir * (_config.Speed * SpeedMultiplier);

            if (VisualizerField.Instance != null)
            {
                // Per-unit ragged bite — the unit's individual presence. Shreds faster with treble/flux.
                VisualizerField.Instance.Consume(
                    pos, _config.ConsumeRadius,
                    _config.ConsumeStrengthPerSecond * (1f + agitation * _config.AgitationConsumeBoost) * dt);

                // Turbulent tear: displace + stretch + raggedly eat the smoke along the unit's motion.
                // Emitted per unit; the core aggregates nearby units into a few bounded turbulence
                // zones, so this scales to the whole swarm without a per-unit shader array.
                VisualizerField.Instance.Turbulence(
                    pos, _config.TurbulenceRadius, agitation, dir);
            }
        }

        protected override void UpdateVisual(MusicState s, float dt)
        {
            // High-frequency vibration: small rapid position jitter (+ subtle scale flutter). Cosmetic.
            float treble = s != null ? s.Treble : 0f;
            float flux = s != null ? Mathf.Clamp01(s.SpectralFlux) : 0f;
            float agitation = Mathf.Clamp01(treble * _config.TrebleJitter + flux * _config.FluxAgitation);
            SetVisualOffset(Random.insideUnitCircle * (0.06f * agitation));
            SetVisualScale(1f + treble * 0.08f);

            // Warm "waveform particle": fixed per-unit hue (orange/yellow/red, some deep purple),
            // brightening slightly with treble. The tendril agitation is driven by _Flutter in-shader.
            Color c = Color.HSVToRGB(_baseHue, 0.85f, Mathf.Lerp(0.85f, 1f, treble));
            CurrentColor = c;

            var mpb = VisualBlock;
            mpb.SetColor(ColorId, c);
            mpb.SetFloat(Time0Id, Time.time);
            mpb.SetFloat(FlutterId, agitation);
            mpb.SetFloat(SeedId, _seed);
            mpb.SetFloat(ArmsId, _arms);
            ApplyVisualBlock();
        }
    }
}
