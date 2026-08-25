using UnityEngine;

namespace PlayVisualizer.Audio.Analysis
{
    /// <summary>
    /// Folds the FFT magnitude spectrum into 12 pitch classes (C, C#, … B) by mapping each bin
    /// to its nearest musical pitch and summing across octaves. Values are max-normalized (the
    /// strongest class ≈ 1) and lightly smoothed. This is a coarse tonal descriptor, not source
    /// separation — good enough to drive color/geometry later, per the spec.
    /// </summary>
    public class Chroma
    {
        private readonly int[] _pitchClass;
        private readonly bool[] _use;
        private readonly int _size;
        private readonly float _smoothing;
        private readonly float[] _raw = new float[12];
        private readonly float[] _values = new float[12];

        public float[] Values => _values;

        public Chroma(int size, float binHz, float minHz, float maxHz, float smoothing)
        {
            _size = size;
            _smoothing = Mathf.Max(1e-3f, smoothing);
            _pitchClass = new int[size];
            _use = new bool[size];

            for (int i = 1; i < size; i++)
            {
                float f = i * binHz;
                if (f < minHz || f > maxHz)
                {
                    _use[i] = false;
                    continue;
                }
                float midi = 69f + 12f * Mathf.Log(f / 440f, 2f);
                int pc = Mathf.RoundToInt(midi) % 12;
                if (pc < 0) pc += 12;
                _pitchClass[i] = pc;
                _use[i] = true;
            }
        }

        public void Compute(float[] spectrum, float dt)
        {
            for (int i = 0; i < 12; i++) _raw[i] = 0f;

            for (int i = 1; i < _size; i++)
            {
                if (_use[i]) _raw[_pitchClass[i]] += spectrum[i];
            }

            float max = 0f;
            for (int i = 0; i < 12; i++)
            {
                if (_raw[i] > max) max = _raw[i];
            }

            float alpha = 1f - Mathf.Exp(-dt / _smoothing);
            for (int i = 0; i < 12; i++)
            {
                float target = max > 1e-6f ? _raw[i] / max : 0f;
                _values[i] += (target - _values[i]) * alpha;
            }
        }
    }
}
