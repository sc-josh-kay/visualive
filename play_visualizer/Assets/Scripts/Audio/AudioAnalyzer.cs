using UnityEngine;
using PlayVisualizer.Audio.Analysis;

namespace PlayVisualizer.Audio
{
    /// <summary>
    /// Orchestrates the music-analysis pipeline: pulls the FFT spectrum, computes log-spaced
    /// frequency bands, derives the convenience Bass/Mid/Treble/Energy signals, and (for now)
    /// the legacy beat. Publishes everything into <see cref="State"/>. It is the ONLY component
    /// that performs audio analysis, and it contains no gameplay logic.
    ///
    /// Phase A: bands + FFT config + derived features preserved. Spectral features, flux-based
    /// onsets, tempo/beat, and chroma are added in later phases via additional modules.
    /// </summary>
    [RequireComponent(typeof(LocalClipMusicProvider))]
    public class AudioAnalyzer : MonoBehaviour
    {
        [SerializeField] private MusicAnalysisConfig _config;

        public MusicState State { get; } = new MusicState();

        /// <summary>Cached instance so many small visuals (projectiles) can read State without a scene lookup.</summary>
        public static AudioAnalyzer Instance { get; private set; }

        private IMusicProvider _provider;
        private SpectrumProvider _spectrum;
        private FrequencyBands _bands;
        private SpectralFeatures _features;
        private EnvelopeFollower _bassEnv, _midEnv, _trebleEnv, _energyEnv;
        private OnsetDetector _onsetGlobal, _onsetBass, _onsetMid, _onsetTreble;
        private BeatTracker _beatTracker;
        private Chroma _chroma;

        // Derived convenience-band state (adaptive normalization), ported for compatibility.
        private float _bassPeak, _midPeak, _treblePeak, _energyPeak;

        // Resolved config values (fall back to sensible defaults if no asset is assigned).
        private float _bassMaxHz, _midMaxHz, _trebleMaxHz, _normalizeDecay;
        private float _attackSeconds, _releaseSeconds;
        private float _onsetSensitivity, _onsetRefractory;
        private int _onsetWindow;
        private float _minBPM, _maxBPM, _odfRate, _tempoInterval, _phaseCorrection, _tempoInertia;
        private float _chromaMinHz, _chromaMaxHz, _chromaSmoothing;

        private void Awake()
        {
            Instance = this;
            _provider = GetComponent<IMusicProvider>();
            ResolveConfig();

            int fftSize = _config != null ? _config.FftSize : 1024;
            FFTWindow window = _config != null ? _config.Window : FFTWindow.BlackmanHarris;
            float[] edges = _config != null && _config.BandCount > 0
                ? _config.BandEdgesHz
                : new[] { 20f, 40f, 80f, 160f, 320f, 640f, 1250f, 2500f, 5000f, 10000f, 20000f };

            _spectrum = new SpectrumProvider(_provider, fftSize, window);
            _bands = new FrequencyBands(edges, _normalizeDecay);
            _features = new SpectralFeatures(_spectrum.Size);
            _bassEnv = new EnvelopeFollower(_attackSeconds, _releaseSeconds);
            _midEnv = new EnvelopeFollower(_attackSeconds, _releaseSeconds);
            _trebleEnv = new EnvelopeFollower(_attackSeconds, _releaseSeconds);
            _energyEnv = new EnvelopeFollower(_attackSeconds, _releaseSeconds);
            _onsetGlobal = new OnsetDetector(_onsetWindow, _onsetSensitivity, _onsetRefractory);
            _onsetBass = new OnsetDetector(_onsetWindow, _onsetSensitivity, _onsetRefractory);
            _onsetMid = new OnsetDetector(_onsetWindow, _onsetSensitivity, _onsetRefractory);
            _onsetTreble = new OnsetDetector(_onsetWindow, _onsetSensitivity, _onsetRefractory);
            _beatTracker = new BeatTracker(_minBPM, _maxBPM, _odfRate, _tempoInterval, _phaseCorrection, _tempoInertia);
            _chroma = new Chroma(_spectrum.Size, _spectrum.BinHz, _chromaMinHz, _chromaMaxHz, _chromaSmoothing);

            State.FrequencyBands = new float[_bands.Count];
            State.FrequencyBandsRaw = new float[_bands.Count];
            State.Chroma = new float[12];
        }

        private void ResolveConfig()
        {
            _bassMaxHz = _config != null ? _config.BassMaxHz : 150f;
            _midMaxHz = _config != null ? _config.MidMaxHz : 2000f;
            _trebleMaxHz = _config != null ? _config.TrebleMaxHz : 8000f;
            _attackSeconds = _config != null ? _config.AttackSeconds : 0.03f;
            _releaseSeconds = _config != null ? _config.ReleaseSeconds : 0.25f;
            _normalizeDecay = _config != null ? _config.NormalizeDecay : 0.5f;
            _onsetSensitivity = _config != null ? _config.OnsetSensitivity : 1.5f;
            _onsetWindow = _config != null ? _config.OnsetWindow : 43;
            _onsetRefractory = _config != null ? _config.OnsetRefractorySeconds : 0.06f;
            _minBPM = _config != null ? _config.MinBPM : 70f;
            _maxBPM = _config != null ? _config.MaxBPM : 180f;
            _odfRate = _config != null ? _config.OdfRate : 90f;
            _tempoInterval = _config != null ? _config.TempoUpdateInterval : 0.3f;
            _phaseCorrection = _config != null ? _config.PhaseCorrection : 0.12f;
            _tempoInertia = _config != null ? _config.TempoInertia : 0.6f;
            _chromaMinHz = _config != null ? _config.ChromaMinHz : 55f;
            _chromaMaxHz = _config != null ? _config.ChromaMaxHz : 5000f;
            _chromaSmoothing = _config != null ? _config.ChromaSmoothing : 0.08f;
        }

        private void Update()
        {
            State.Beat = false;

            if (_provider == null || !_provider.IsPlaying)
            {
                return;
            }

            float dt = Time.deltaTime;
            _spectrum.Update();

            // --- Frequency bands (the rich representation) ---
            _bands.Compute(_spectrum.Spectrum, _spectrum.BinHz, dt);
            System.Array.Copy(_bands.Normalized, State.FrequencyBands, _bands.Count);
            System.Array.Copy(_bands.Raw, State.FrequencyBandsRaw, _bands.Count);

            // --- Derived convenience features (attack/release envelopes, raw kept alongside) ---
            float binHz = _spectrum.BinHz;
            float bassRaw = SumBand(0f, _bassMaxHz, binHz);
            float midRaw = SumBand(_bassMaxHz, _midMaxHz, binHz);
            float trebleRaw = SumBand(_midMaxHz, _trebleMaxHz, binHz);
            float energyRaw = bassRaw + midRaw + trebleRaw;

            float bassNorm = Normalize(bassRaw, ref _bassPeak, dt);
            State.BassInstant = bassNorm;                       // raw (pre-envelope)
            State.Bass = _bassEnv.Update(bassNorm, dt);         // processed (fast attack, slow release)
            State.Mid = _midEnv.Update(Normalize(midRaw, ref _midPeak, dt), dt);
            State.Treble = _trebleEnv.Update(Normalize(trebleRaw, ref _treblePeak, dt), dt);
            State.Energy = _energyEnv.Update(Normalize(energyRaw, ref _energyPeak, dt), dt);

            // --- Continuous spectral features ---
            _features.Compute(_spectrum.Spectrum, binHz, _spectrum.SampleRate, _normalizeDecay, dt,
                _bassMaxHz, _midMaxHz, _trebleMaxHz);
            State.SpectralCentroid = _features.Centroid01;
            State.SpectralFlux = _features.FluxNormalized;
            State.SpectralContrast = _features.ContrastNormalized;

            // --- Onset detection (flux-based, adaptive threshold, per audio clock) ---
            double dsp = AudioSettings.dspTime;
            _onsetGlobal.Process(_features.FluxRaw, dsp, _normalizeDecay, dt);
            _onsetBass.Process(_features.BassFluxRaw, dsp, _normalizeDecay, dt);
            _onsetMid.Process(_features.MidFluxRaw, dsp, _normalizeDecay, dt);
            _onsetTreble.Process(_features.TrebleFluxRaw, dsp, _normalizeDecay, dt);

            State.OnsetStrength = _onsetGlobal.Strength;
            State.BassOnsetStrength = _onsetBass.Strength;
            State.MidOnsetStrength = _onsetMid.Strength;
            State.TrebleOnsetStrength = _onsetTreble.Strength;
            State.Onset = _onsetGlobal.Onset;
            State.BassOnset = _onsetBass.Onset;
            State.MidOnset = _onsetMid.Onset;
            State.TrebleOnset = _onsetTreble.Onset;

            // --- Tempo + beat (from the onset signal) ---
            _beatTracker.Process(_onsetGlobal.Strength, _onsetGlobal.Onset, dsp, dt);
            State.Beat = _beatTracker.Beat;
            State.BPM = _beatTracker.BPM;
            State.BeatPhase = _beatTracker.Phase;
            State.LastBeatDspTime = _beatTracker.LastBeatDsp;

            // --- Tonal: chroma (pitch classes) ---
            _chroma.Compute(_spectrum.Spectrum, dt);
            System.Array.Copy(_chroma.Values, State.Chroma, 12);
        }

        private float SumBand(float loHz, float hiHz, float binHz)
        {
            float[] spectrum = _spectrum.Spectrum;
            int lo = Mathf.Clamp(Mathf.FloorToInt(loHz / binHz), 0, spectrum.Length - 1);
            int hi = Mathf.Clamp(Mathf.CeilToInt(hiHz / binHz), 0, spectrum.Length - 1);
            float sum = 0f;
            for (int i = lo; i <= hi; i++)
            {
                sum += spectrum[i];
            }
            return sum;
        }

        private float Normalize(float raw, ref float peak, float dt)
        {
            peak = raw > peak ? raw : Mathf.Max(raw, peak - _normalizeDecay * peak * dt);
            return peak > 1e-6f ? Mathf.Clamp01(raw / peak) : 0f;
        }
    }
}
