using UnityEngine;

namespace PlayVisualizer.Audio.Analysis
{
    /// <summary>
    /// Owns the raw FFT magnitude spectrum for one analysis frame. Pulls from an IMusicProvider
    /// so it stays decoupled from the audio source, and exposes the bin→frequency mapping the
    /// rest of the pipeline needs. Plain class (not a MonoBehaviour) — owned by AudioAnalyzer.
    /// </summary>
    public class SpectrumProvider
    {
        private readonly IMusicProvider _provider;
        private readonly FFTWindow _window;
        private readonly float[] _spectrum;

        public float[] Spectrum => _spectrum;
        public int Size { get; }
        public float SampleRate { get; }

        /// <summary>Hz per FFT bin.</summary>
        public float BinHz => (SampleRate * 0.5f) / Size;

        public SpectrumProvider(IMusicProvider provider, int fftSize, FFTWindow window)
        {
            _provider = provider;
            _window = window;
            Size = Mathf.ClosestPowerOfTwo(Mathf.Clamp(fftSize, 64, 8192));
            _spectrum = new float[Size];
            SampleRate = provider != null ? provider.SampleRate : AudioSettings.outputSampleRate;
        }

        /// <summary>Refresh the spectrum for this frame (zeros when nothing is playing).</summary>
        public void Update()
        {
            if (_provider != null && _provider.IsPlaying)
            {
                _provider.GetSpectrumData(_spectrum, 0, _window);
            }
            else
            {
                System.Array.Clear(_spectrum, 0, Size);
            }
        }

        /// <summary>Bin index for a frequency, clamped to the valid range.</summary>
        public int BinForHz(float hz)
        {
            return Mathf.Clamp(Mathf.RoundToInt(hz / BinHz), 0, Size - 1);
        }
    }
}
