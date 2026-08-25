namespace PlayVisualizer.Audio
{
    /// <summary>
    /// The shared snapshot of the current musical moment (v2). Produced by the AudioAnalyzer and
    /// consumed (read-only) by gameplay/visual systems — which must never run their own FFT.
    ///
    /// Continuous vs. event signals are kept distinct: continuous features may be smoothed;
    /// event flags (Onset*/Beat) are one-frame and unsmoothed. Raw and processed values coexist
    /// so consumers can choose what they need.
    ///
    /// Fields are filled in progressively across the v2 build-out; unset ones stay at defaults.
    /// The original convenience fields (Bass/Mid/Treble/Energy/Beat) are retained for backward
    /// compatibility and are now derived from the richer band representation.
    /// </summary>
    public class MusicState
    {
        // --- Continuous spectral features ---
        public float Energy;            // overall intensity (a.k.a. OverallEnergy)
        public float Bass;              // enveloped (processed)
        public float Mid;
        public float Treble;

        public float BassInstant;       // pre-envelope normalized bass (raw, per §16)

        public float SpectralCentroid;  // 0..1 brightness           (Phase B)
        public float SpectralFlux;      // positive spectral change   (Phase B)
        public float SpectralContrast;  // optional                   (Phase B)

        // Rich frequency representation
        public float[] FrequencyBands;      // adaptive-normalized 0..1
        public float[] FrequencyBandsRaw;   // stable/raw energy

        // --- Onset features ---                                     (Phase C)
        public float OnsetStrength;
        public float BassOnsetStrength;
        public float MidOnsetStrength;
        public float TrebleOnsetStrength;

        public bool Onset;
        public bool BassOnset;
        public bool MidOnset;
        public bool TrebleOnset;

        // --- Rhythm ---                                            (Phase D)
        public float BPM;
        public float BeatPhase;         // 0..1 between beats
        public bool Beat;               // event
        public double LastBeatDspTime;  // audio-clock timestamp

        // --- Tonal ---                                             (Phase E)
        public float[] Chroma;          // 12 pitch classes
    }
}
