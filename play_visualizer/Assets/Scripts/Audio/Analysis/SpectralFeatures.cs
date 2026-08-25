using UnityEngine;

namespace PlayVisualizer.Audio.Analysis
{
    /// <summary>
    /// Continuous, frame-to-frame spectral descriptors:
    ///   • Centroid  — spectral "brightness" (where the energy's balance point is), 0..1 log-mapped.
    ///   • Flux      — positive spectral change vs. the previous frame (basis for onsets, Phase C).
    ///   • Contrast  — rough peakiness (peak-vs-mean) across log chunks; best-effort per the spec.
    /// Raw flux/contrast are adaptively normalized to 0..1 for convenient consumption; the raw
    /// flux is also exposed for the onset detector to threshold itself.
    /// </summary>
    public class SpectralFeatures
    {
        private readonly float[] _prev;
        private readonly int _size;
        private float _fluxPeak;
        private float _contrastPeak;

        public float Centroid01 { get; private set; }
        public float FluxRaw { get; private set; }
        public float FluxNormalized { get; private set; }
        public float ContrastNormalized { get; private set; }

        // Positive spectral flux confined to each convenience band (feeds per-band onsets).
        public float BassFluxRaw { get; private set; }
        public float MidFluxRaw { get; private set; }
        public float TrebleFluxRaw { get; private set; }

        private const int ContrastGroups = 6;

        public SpectralFeatures(int size)
        {
            _size = size;
            _prev = new float[size];
        }

        public void Compute(float[] spectrum, float binHz, float sampleRate, float decay, float dt,
                            float bassMaxHz, float midMaxHz, float trebleMaxHz)
        {
            // --- Centroid (brightness) ---
            float num = 0f, den = 0f;
            for (int i = 1; i < _size; i++)
            {
                float mag = spectrum[i];
                num += (i * binHz) * mag;
                den += mag;
            }
            float centroidHz = den > 1e-6f ? num / den : 0f;
            float minHz = 50f;
            float maxHz = sampleRate * 0.5f;
            float c = Mathf.Clamp(centroidHz, minHz, maxHz);
            Centroid01 = (Mathf.Log(c) - Mathf.Log(minHz)) / (Mathf.Log(maxHz) - Mathf.Log(minHz));

            // --- Flux (sum of positive changes), split into bands in the same pass ---
            float flux = 0f, bassFlux = 0f, midFlux = 0f, trebleFlux = 0f;
            for (int i = 0; i < _size; i++)
            {
                float d = spectrum[i] - _prev[i];
                _prev[i] = spectrum[i];
                if (d <= 0f) continue;

                flux += d;
                float hz = i * binHz;
                if (hz < bassMaxHz) bassFlux += d;
                else if (hz < midMaxHz) midFlux += d;
                else if (hz < trebleMaxHz) trebleFlux += d;
            }
            FluxRaw = flux;
            BassFluxRaw = bassFlux;
            MidFluxRaw = midFlux;
            TrebleFluxRaw = trebleFlux;
            _fluxPeak = flux > _fluxPeak ? flux : Mathf.Max(flux, _fluxPeak - decay * _fluxPeak * dt);
            FluxNormalized = _fluxPeak > 1e-6f ? Mathf.Clamp01(flux / _fluxPeak) : 0f;

            // --- Contrast (peak vs mean across log chunks) ---
            float contrast = 0f;
            int start = 1;
            for (int g = 0; g < ContrastGroups; g++)
            {
                // log-spaced chunk boundaries across the usable bins
                int lo = Mathf.Max(1, Mathf.RoundToInt(Mathf.Pow((float)g / ContrastGroups, 2f) * (_size - 1)));
                int hi = Mathf.Max(lo + 1, Mathf.RoundToInt(Mathf.Pow((float)(g + 1) / ContrastGroups, 2f) * (_size - 1)));
                hi = Mathf.Min(hi, _size - 1);

                float peak = 0f, sum = 0f;
                int count = 0;
                for (int i = lo; i <= hi; i++)
                {
                    float m = spectrum[i];
                    if (m > peak) peak = m;
                    sum += m;
                    count++;
                }
                float mean = count > 0 ? sum / count : 0f;
                contrast += peak - mean;
                start = hi;
            }
            contrast /= ContrastGroups;
            _contrastPeak = contrast > _contrastPeak ? contrast : Mathf.Max(contrast, _contrastPeak - decay * _contrastPeak * dt);
            ContrastNormalized = _contrastPeak > 1e-6f ? Mathf.Clamp01(contrast / _contrastPeak) : 0f;
        }
    }
}
