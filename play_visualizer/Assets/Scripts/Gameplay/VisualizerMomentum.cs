using UnityEngine;
using PlayVisualizer.Visuals;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// Owns the Visualizer Momentum → Overdrive state machine (spec9). Momentum (0..100) is driven by
    /// smoothed visualizer coverage: it drains below a low threshold, is neutral in a mid band, and
    /// fills above a high threshold. At 100 it triggers OVERDRIVE — a timed state that resets momentum
    /// and turns the player into a music-powered paint emitter (the RadialPaintEmitter / PaintWaveSystem
    /// do the actual painting; this class only owns the state and the reward multipliers).
    ///
    /// This is explicitly NOT health: reaching zero never damages the player, ends the song, or shows
    /// game-over. The only consequence of low coverage is losing access to the Overdrive reward.
    ///
    /// A lightweight singleton (like ScoreManager) so UI, scoring, and enemies can read it without
    /// wiring. It reads coverage from VisualizerField and writes back OverdriveIntensity (for the
    /// visualizer's own §13 amplification); it never touches shaders or RenderTextures.
    /// </summary>
    public class VisualizerMomentum : MonoBehaviour
    {
        public static VisualizerMomentum Instance { get; private set; }

        [SerializeField] private OverdriveConfig _config;

        /// <summary>Current momentum, 0..100.</summary>
        public float Momentum { get; private set; }

        /// <summary>Momentum as 0..1 (for the HUD bar).</summary>
        public float Momentum01 => Momentum * 0.01f;

        public bool IsOverdrive { get; private set; }

        /// <summary>Overdrive time remaining as 0..1 (1 = just started), for the HUD.</summary>
        public float OverdriveRemaining01 { get; private set; }

        /// <summary>Smoothed [0..1] envelope of Overdrive state for the visualizer's §13 amplification.</summary>
        public float OverdriveIntensity01 { get; private set; }

        /// <summary>Score multiplier from Overdrive (1 when inactive). Stacks with the combo.</summary>
        public float ScoreMultiplier => IsOverdrive && _config != null ? Mathf.Max(1f, _config.ScoreMultiplier) : 1f;

        /// <summary>Enemy-death paint multiplier from Overdrive (1 when inactive). Stacks with the combo.</summary>
        public float DeathPaintMultiplier => IsOverdrive && _config != null ? Mathf.Max(1f, _config.DeathPaintMultiplier) : 1f;

        /// <summary>
        /// Multiplier applied to the player's MOVEMENT trail while Overdrive is active (1 when inactive),
        /// so the trail steps back to a secondary paint source under the radial emission (spec9 §5).
        /// Read by PlayerTrail.
        /// </summary>
        public float TrailPaintScale => IsOverdrive && _config != null ? Mathf.Clamp01(_config.OverdriveTrailScale) : 1f;

        /// <summary>True during the post-Overdrive lockout (can't re-trigger yet).</summary>
        public bool OnCooldown => _cooldownTimer > 0f;

        private float _smoothedCoverage;
        private float _overdriveTimer;
        private float _cooldownTimer;
        private bool _hasCoverageSample;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            // Leave the visualizer in a clean (non-overdrive) state.
            if (VisualizerField.Instance != null) VisualizerField.Instance.OverdriveIntensity = 0f;
        }

        private void Update()
        {
            // Time.deltaTime is 0 while paused / in menu (timeScale 0), so momentum only moves during play.
            float dt = Time.deltaTime;
            if (dt <= 0f || _config == null) return;

#if UNITY_EDITOR
            // Dev shortcut: press O to jump straight into Overdrive (test the radial paint / waves / VFX
            // without having to grind coverage up to 100% momentum first).
            if (!IsOverdrive && Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame)
            {
                EnterOverdrive();
            }
#endif

            VisualizerField field = VisualizerField.Instance;
            float coverage = field != null ? Mathf.Clamp01(field.Coverage) : 0f;

            // Lightly smooth coverage (EMA) so GPU-readback jitter doesn't destabilize momentum (§1).
            if (!_hasCoverageSample) { _smoothedCoverage = coverage; _hasCoverageSample = true; }
            float k = _config.CoverageSmoothing > 0.0001f
                ? 1f - Mathf.Exp(-dt / _config.CoverageSmoothing)
                : 1f;
            _smoothedCoverage = Mathf.Lerp(_smoothedCoverage, coverage, k);

            // Momentum response to coverage bands (§1). The rate ramps with DISTANCE from the
            // threshold: near the threshold it drains/fills slowly, far from it (toward 0% / 100%
            // coverage) it drains/fills near the configured max rate. RateRampPower shapes the curve.
            float delta;
            if (_smoothedCoverage < _config.DrainBelowCoverage)
            {
                float t = (_config.DrainBelowCoverage - _smoothedCoverage)
                          / Mathf.Max(1e-4f, _config.DrainBelowCoverage);
                t = Mathf.Pow(Mathf.Clamp01(t), _config.RateRampPower);
                delta = -_config.DrainPerSecond * t;
            }
            else if (_smoothedCoverage >= _config.FillAboveCoverage)
            {
                float t = (_smoothedCoverage - _config.FillAboveCoverage)
                          / Mathf.Max(1e-4f, 1f - _config.FillAboveCoverage);
                t = Mathf.Pow(Mathf.Clamp01(t), _config.RateRampPower);
                delta = _config.GainPerSecond * t;
            }
            else delta = 0f;

            if (IsOverdrive)
            {
                // Momentum does NOT charge while Overdrive is active — it holds at 0 and only starts
                // building again once Overdrive completes (design override of spec §3's "build during").
                Momentum = 0f;
                _overdriveTimer -= dt;
                OverdriveRemaining01 = _config.OverdriveDuration > 0.0001f
                    ? Mathf.Clamp01(_overdriveTimer / _config.OverdriveDuration)
                    : 0f;
                if (_overdriveTimer <= 0f) ExitOverdrive();
            }
            else
            {
                // Build (or drain) from 0 after Overdrive ends.
                Momentum = Mathf.Clamp(Momentum + delta * dt, 0f, 100f);
                // Post-Overdrive lockout: momentum can refill but won't trigger again until the
                // cooldown elapses — stops instant re-triggering while the screen is still full.
                if (_cooldownTimer > 0f) _cooldownTimer = Mathf.Max(0f, _cooldownTimer - dt);
                if (Momentum >= 100f && _cooldownTimer <= 0f) EnterOverdrive();
            }

            // Smooth the intensity envelope for §13 (ease in/out so amplification doesn't pop).
            float targetIntensity = IsOverdrive ? 1f : 0f;
            OverdriveIntensity01 = Mathf.MoveTowards(OverdriveIntensity01, targetIntensity, dt * 4f);
            if (field != null) field.OverdriveIntensity = OverdriveIntensity01;
        }

        private void EnterOverdrive()
        {
            IsOverdrive = true;
            _overdriveTimer = _config.OverdriveDuration;
            OverdriveRemaining01 = 1f;
            Momentum = 0f; // reset; the player immediately starts building toward the next one (§3).
        }

        private void ExitOverdrive()
        {
            IsOverdrive = false;
            _overdriveTimer = 0f;
            OverdriveRemaining01 = 0f;
            _cooldownTimer = _config != null ? _config.OverdriveCooldown : 0f;
        }

        /// <summary>Clear momentum/overdrive at the start of a run.</summary>
        public void ResetMomentum()
        {
            Momentum = 0f;
            IsOverdrive = false;
            _overdriveTimer = 0f;
            _cooldownTimer = 0f;
            OverdriveRemaining01 = 0f;
            OverdriveIntensity01 = 0f;
            _hasCoverageSample = false;
            if (VisualizerField.Instance != null) VisualizerField.Instance.OverdriveIntensity = 0f;
        }
    }
}
