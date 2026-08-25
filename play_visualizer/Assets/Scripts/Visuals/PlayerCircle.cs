using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Visuals
{
    /// <summary>
    /// The Phase G demonstration: a persistent ring around the player that combines several
    /// independent musical features, proving the analyzer drives continuous values, transient
    /// events, rhythm, and tonal character — not just "bigger when louder".
    ///
    ///   Bass energy      → baseline radius (continuous breathing)
    ///   Bass onset       → sharp pulse (transient)
    ///   Beat             → stronger synchronized pulse (rhythm)
    ///   Spectral bright. → hue (tonal/spectral character)
    ///   Overall energy   → intensity (value + alpha)
    ///   Treble onset     → spark burst (secondary transient)
    ///
    /// This component OWNS the mappings; the analyzer only exposes features.
    /// </summary>
    public class PlayerCircle : MonoBehaviour
    {
        [SerializeField] private AudioAnalyzer _analyzer;
        [SerializeField] private SpriteRenderer _ring;
        [SerializeField] private ParticleSystem _sparks;

        [Header("Size — bass energy → radius, onsets/beat → pulse")]
        [SerializeField] private float _baseSize = 1.2f;
        [SerializeField] private float _bassSize = 0.8f;
        [SerializeField] private float _bassOnsetPulse = 0.7f;
        [SerializeField] private float _beatPulse = 1.1f;
        [SerializeField] private float _pulseDecay = 5f;

        [Header("Color — brightness → hue, energy → intensity")]
        [Tooltip("Hue (0..1) at low spectral brightness.")]
        [SerializeField] private float _hueDark = 0.55f;
        [Tooltip("Hue (0..1) at high spectral brightness.")]
        [SerializeField] private float _hueBright = 0.02f;
        [SerializeField] private float _saturation = 0.85f;
        [SerializeField] private float _minValue = 0.5f, _maxValue = 1f;
        [SerializeField] private float _minAlpha = 0.35f, _maxAlpha = 0.95f;

        [Header("Sparks — treble onset")]
        [SerializeField] private int _sparkCount = 10;

        private float _pulse;

        private void Awake()
        {
            if (_analyzer == null) _analyzer = FindFirstObjectByType<AudioAnalyzer>();
            if (_ring == null) _ring = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (_analyzer == null)
            {
                return;
            }

            MusicState s = _analyzer.State;

            // Pulse envelope: fast rise on transients, steady decay.
            _pulse = Mathf.Max(0f, _pulse - _pulseDecay * Time.deltaTime);
            if (s.BassOnset) _pulse = Mathf.Max(_pulse, _bassOnsetPulse * Mathf.Max(0.4f, s.BassOnsetStrength));
            if (s.Beat) _pulse = Mathf.Max(_pulse, _beatPulse);

            float size = _baseSize + s.Bass * _bassSize + _pulse;
            transform.localScale = new Vector3(size, size, 1f);

            // Brightness → hue (shortest path around the wheel), energy → value + alpha.
            float hue = Mathf.Repeat(Mathf.LerpAngle(_hueDark * 360f, _hueBright * 360f, s.SpectralCentroid) / 360f, 1f);
            float value = Mathf.Lerp(_minValue, _maxValue, s.Energy);
            float alpha = Mathf.Lerp(_minAlpha, _maxAlpha, s.Energy);
            Color c = Color.HSVToRGB(hue, _saturation, value);
            c.a = alpha;
            if (_ring != null) _ring.color = c;

            // Treble onset → spark burst.
            if (s.TrebleOnset && _sparks != null)
            {
                _sparks.Emit(_sparkCount);
            }
        }
    }
}
