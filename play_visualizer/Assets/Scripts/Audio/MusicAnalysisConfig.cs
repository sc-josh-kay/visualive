using UnityEngine;

namespace PlayVisualizer.Audio
{
    /// <summary>
    /// Tunable configuration for the music analysis engine. Centralizes FFT settings, band
    /// boundaries, and processing constants so the DSP can be adjusted without code changes.
    /// Fields that later phases (onsets, beat, envelopes) will use are included now with sane
    /// defaults so the asset is stable across the build-out.
    /// Create via: Assets → Create → PlayVisualizer → Music Analysis Config.
    /// </summary>
    [CreateAssetMenu(fileName = "MusicAnalysisConfig", menuName = "PlayVisualizer/Music Analysis Config")]
    public class MusicAnalysisConfig : ScriptableObject
    {
        [Header("FFT")]
        [Tooltip("FFT sample count (power of two, >= 64). 1024 balances low-end resolution vs. " +
                 "transient responsiveness; try 2048 for finer bass at the cost of latency.")]
        public int FftSize = 1024;
        public FFTWindow Window = FFTWindow.BlackmanHarris;

        [Header("Frequency bands (log-spaced edges, Hz)")]
        [Tooltip("N+1 edges define N bands. Clamped to the FFT's actual resolution at runtime.")]
        public float[] BandEdgesHz =
        {
            20f, 40f, 80f, 160f, 320f, 640f, 1250f, 2500f, 5000f, 10000f, 20000f
        };

        [Header("Convenience bands (Hz) — derive Bass/Mid/Treble")]
        public float BassMaxHz = 150f;
        public float MidMaxHz = 2000f;
        public float TrebleMaxHz = 8000f;

        [Header("Continuous envelopes / normalization")]
        [Tooltip("Attack time (s) for continuous signals — fast, so hits register immediately.")]
        public float AttackSeconds = 0.03f;
        [Tooltip("Release time (s) — slower, so signals decay naturally after a hit.")]
        public float ReleaseSeconds = 0.25f;
        [Tooltip("Per-second decay of the adaptive normalization peak follower.")]
        public float NormalizeDecay = 0.5f;

        [Header("Onset detection")]
        [Tooltip("Threshold = local mean + sensitivity × local std of the flux. Higher = fewer onsets.")]
        public float OnsetSensitivity = 1.5f;
        [Tooltip("Frames of flux history used for the adaptive threshold (~0.7 s at 60 fps).")]
        public int OnsetWindow = 43;
        [Tooltip("Minimum seconds between onsets on the same channel.")]
        public float OnsetRefractorySeconds = 0.06f;

        [Header("Tempo / beat")]
        [Tooltip("Plausible tempo range for autocorrelation of the onset envelope.")]
        public float MinBPM = 70f;
        public float MaxBPM = 180f;
        [Tooltip("Sample rate (Hz) of the internal onset envelope used for tempo estimation.")]
        public float OdfRate = 90f;
        [Tooltip("How often (s) tempo is re-estimated; BPM is then slewed slowly toward it.")]
        public float TempoUpdateInterval = 0.3f;
        [Tooltip("How strongly detected onsets pull the beat phase into alignment (0..1).")]
        public float PhaseCorrection = 0.12f;
        [Tooltip("Bias toward the current tempo to stop BPM hopping between adjacent peaks. " +
                 "0 = none; higher = steadier (but slower to re-lock on a real tempo change).")]
        public float TempoInertia = 0.6f;

        [Header("Chroma (pitch classes)")]
        [Tooltip("Frequency range folded into the 12 pitch classes.")]
        public float ChromaMinHz = 55f;
        public float ChromaMaxHz = 5000f;
        [Tooltip("Chroma smoothing time constant (s) to reduce flicker.")]
        public float ChromaSmoothing = 0.08f;

        /// <summary>Number of frequency bands (edges - 1).</summary>
        public int BandCount => BandEdgesHz != null && BandEdgesHz.Length >= 2 ? BandEdgesHz.Length - 1 : 0;
    }
}
