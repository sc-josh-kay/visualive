using UnityEngine;

namespace PlayVisualizer.Audio.Analysis
{
    /// <summary>
    /// Collapses the FFT spectrum into a set of log-spaced frequency bands, exposing both a
    /// stable RAW energy and an adaptive-normalized value per band. The raw values feed
    /// stable/structural features later; the normalized values are what a debug view or a
    /// visualizer typically wants.
    /// </summary>
    public class FrequencyBands
    {
        private readonly float[] _edgesHz;
        private readonly float _decay;
        private readonly float[] _raw;
        private readonly float[] _normalized;
        private readonly float[] _peak;

        public int Count { get; }
        public float[] Raw => _raw;
        public float[] Normalized => _normalized;

        public FrequencyBands(float[] edgesHz, float normalizeDecay)
        {
            _edgesHz = edgesHz;
            _decay = normalizeDecay;
            Count = Mathf.Max(0, edgesHz.Length - 1);
            _raw = new float[Count];
            _normalized = new float[Count];
            _peak = new float[Count];
        }

        public void Compute(float[] spectrum, float binHz, float dt)
        {
            for (int b = 0; b < Count; b++)
            {
                int lo = Mathf.Clamp(Mathf.FloorToInt(_edgesHz[b] / binHz), 0, spectrum.Length - 1);
                int hi = Mathf.Clamp(Mathf.CeilToInt(_edgesHz[b + 1] / binHz), 0, spectrum.Length - 1);

                float sum = 0f;
                for (int i = lo; i <= hi; i++)
                {
                    sum += spectrum[i];
                }
                _raw[b] = sum;

                // Adaptive per-band normalization (decaying peak follower).
                float peak = _peak[b];
                peak = sum > peak ? sum : Mathf.Max(sum, peak - _decay * peak * dt);
                _peak[b] = peak;
                _normalized[b] = peak > 1e-6f ? Mathf.Clamp01(sum / peak) : 0f;
            }
        }
    }
}
