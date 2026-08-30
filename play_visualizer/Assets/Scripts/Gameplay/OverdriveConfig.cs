using UnityEngine;

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// All tunables for the Visualizer Momentum → Overdrive system (spec9). Kept in one config asset
    /// so momentum/overdrive/wave/emission behavior can be tuned without touching code.
    /// Create via: Assets → Create → PlayVisualizer → Overdrive Config.
    /// </summary>
    [CreateAssetMenu(fileName = "OverdriveConfig", menuName = "PlayVisualizer/Overdrive Config")]
    public class OverdriveConfig : ScriptableObject
    {
        [Header("Momentum (spec9 §1)")]
        [Tooltip("Coverage below this fraction drains momentum.")]
        [Range(0f, 1f)] public float DrainBelowCoverage = 0.5f;
        [Tooltip("Coverage at or above this fraction fills momentum (between the two = neutral/flat). " +
                 "Keep this close to the drain threshold for a small dead band so the bar visibly " +
                 "tracks coverage; widen the gap for a more forgiving 'hold' zone.")]
        [Range(0f, 1f)] public float FillAboveCoverage = 0.6f;
        [Tooltip("MAX momentum drained per second — reached at 0% coverage. Scales down toward 0 as " +
                 "coverage approaches the drain threshold (so near the threshold it drains slowly).")]
        public float DrainPerSecond = 14f;
        [Tooltip("MAX momentum gained per second — reached at 100% coverage. Scales down toward 0 as " +
                 "coverage approaches the fill threshold (so near the threshold it fills slowly).")]
        public float GainPerSecond = 18f;
        [Tooltip("Shapes how the drain/fill rate ramps with distance from the threshold. 1 = linear; " +
                 ">1 = extra slow near the threshold and steeper far away; <1 = flatter/more uniform.")]
        [Range(0.25f, 4f)] public float RateRampPower = 1f;
        [Tooltip("EMA smoothing time (seconds) applied to coverage for momentum, so GPU-readback " +
                 "jitter doesn't destabilize it. Larger = smoother/slower to respond.")]
        public float CoverageSmoothing = 0.25f;

        [Header("Overdrive (spec9 §3)")]
        [Tooltip("How long Overdrive lasts once triggered.")]
        public float OverdriveDuration = 8f;
        [Tooltip("Lockout after Overdrive ends before it can trigger again, even at full momentum. " +
                 "Prevents instantly re-triggering while the screen is still full from the last one.")]
        public float OverdriveCooldown = 10f;

        [Header("Overdrive — normal painting balance (spec9 §5)")]
        [Tooltip("Multiplier on the MOVEMENT trail's paint gain while Overdrive is active — the trail " +
                 "becomes the secondary paint source so it doesn't blow out on top of the radial " +
                 "emission. 1 = trail unchanged, 0 = trail fully suppressed during Overdrive.")]
        [Range(0f, 1f)] public float OverdriveTrailScale = 0.5f;
        [Tooltip("How much the continuous radial deposit is reduced as the ship SPEEDS UP (a moving " +
                 "ship smears fresh discs over a wide area, so full intensity everywhere is too much). " +
                 "0 = no speed falloff (constant), 1 = radial fully fades out at reference speed.")]
        [Range(0f, 1f)] public float RadialSpeedFalloff = 0f;
        [Tooltip("Ship speed (world units/sec) at which RadialSpeedFalloff reaches full effect. " +
                 "Around the player's max move speed is a good starting point.")]
        public float RadialSpeedReference = 8f;

        [Header("Overdrive multipliers (spec9 §14)")]
        [Tooltip("Score generation multiplier during Overdrive (stacks with combo).")]
        public float ScoreMultiplier = 2f;
        [Tooltip("Enemy-death paint multiplier during Overdrive (stacks with combo).")]
        public float DeathPaintMultiplier = 1.5f;

        [Header("Radial emission (spec9 §5) — continuous paint around the player")]
        [Tooltip("Base world radius of the continuous paint disc emitted at the player each frame.")]
        public float RadialRadius = 2.2f;
        [Tooltip("Paint intensity PER SECOND of the continuous radial emission (dt-scaled internally). " +
                 "Higher = fills faster; too high instantly whites out around the player.")]
        public float RadialIntensity = 0.35f;
        [Tooltip("Extra radial radius/intensity at full bass, as a fraction (0.5 = up to +50%).")]
        [Range(0f, 1.5f)] public float RadialBassBoost = 0.6f;

        [Header("Paint waves (spec9 §6-8) — expanding ripples on beat/onset")]
        [Tooltip("Max simultaneous waves (pooled; no allocation).")]
        [Range(1, 6)] public int MaxWaves = 6;
        [Tooltip("Minimum seconds between wave emissions (rate-limit so dense onsets don't spam).")]
        public float WaveMinInterval = 0.18f;
        [Tooltip("Wave expansion speed (world units/sec).")]
        public float WaveSpeed = 9f;
        [Tooltip("Base maximum world radius a wave expands to before fading.")]
        public float WaveMaxRadius = 5f;
        [Tooltip("Extra max radius at full bass, as a fraction (spec9 §7 — bass = wave size).")]
        [Range(0f, 2f)] public float WaveBassRadiusBoost = 0.8f;
        [Tooltip("Base per-frame paint intensity deposited along the wavefront.")]
        public float WaveIntensity = 0.5f;
        [Tooltip("Extra wave intensity at full bass, as a fraction (spec9 §7 — bass = wave strength).")]
        [Range(0f, 2f)] public float WaveBassIntensityBoost = 1f;

        [Header("Player VFX (spec9 §11-12)")]
        [Tooltip("Base world scale of the Overdrive glow around the player.")]
        public float GlowScale = 2.4f;
        [Tooltip("Base glow brightness (alpha).")]
        [Range(0f, 1f)] public float GlowBrightness = 0.5f;
        [Tooltip("Extra glow brightness at full bass, as a fraction.")]
        [Range(0f, 2f)] public float GlowBassBoost = 0.6f;
        [Tooltip("How fast the beat/wave glow pulse decays (per second).")]
        public float GlowPulseDecay = 4f;
        [Tooltip("Neon-rainbow ship tint speed during Overdrive (hue cycles per second, Mario " +
                 "star-power). Higher = faster flickering rainbow.")]
        public float ShipRainbowSpeed = 1.5f;
    }
}
