using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// The Dasher's "oscilloscope blade" look (spec10): a sharp hollow arrow core at the leading edge,
    /// a bright central axis streak, and 2–3 thin sine "waveform" traces trailing behind. Built once
    /// from LineRenderers (local space, additive EnergyTrail), then animated each frame. The Dasher's
    /// movement/state pushes dynamics in via <see cref="SetDynamics"/>:
    ///   * Charge  → traces compress + organize, everything brightens (energy compressing).
    ///   * Dash    → traces + axis stretch far behind, a bright streak (the slice).
    /// Bass grows the waveform amplitude/brightness; beat flashes it. Purple with pink undertones.
    ///
    /// Cosmetic only — the cut/consume and movement live on Dasher.cs. The whole child is oriented
    /// along the heading by Dasher via SetVisualRotation, so everything here is built pointing +X.
    /// </summary>
    public class DasherVisualizer : MonoBehaviour
    {
        public enum Phase { Roam, Charge, Dash, Recover }

        [SerializeField] private Material _lineMaterial; // additive vertex-color (EnergyTrail)

        [Header("Geometry")]
        [SerializeField] private int _wavePoints = 40;
        [SerializeField] private int _waveCount = 3;
        [SerializeField] private float _lineWidth = 0.05f;
        [SerializeField] private float _arrowLength = 0.42f;
        [SerializeField] private float _arrowHalfWidth = 0.22f;

        [Header("Waveform")]
        [Tooltip("Trailing length of the traces in roam / charge / dash. Kept well over the arrow " +
                 "head length (~0.42) so the blade always has a long trail (>=3x the head).")]
        [SerializeField] private float _lenRoam = 1.5f;
        [SerializeField] private float _lenCharge = 1.3f;
        [SerializeField] private float _lenDash = 2.8f;
        [SerializeField] private float _baseAmplitude = 0.11f;
        [SerializeField] private float _bassAmplitude = 0.16f;
        [SerializeField] private float _chargeAmplitude = 0.10f;
        [SerializeField] private float _freqRoam = 7f;
        [SerializeField] private float _freqCharge = 16f; // compressed / organized
        [SerializeField] private float _scrollSpeed = 9f;

        [Header("Color (purple → pink on dash)")]
        [SerializeField] private float _baseHue = 0.76f;   // purple at rest
        [SerializeField] private float _pinkHue = 0.92f;   // pink, reached during the dash
        [SerializeField] private float _idleBrightness = 0.9f;
        [SerializeField] private float _chargeBrightness = 2.2f;
        [SerializeField] private float _dashBrightness = 1.8f;

        private LineRenderer _arrow;
        private LineRenderer _axis;
        private LineRenderer[] _waves;
        private Vector3[] _wavePos;
        private float[] _wavePhase;

        // Pushed by Dasher each frame.
        private Phase _phase;
        private float _chargeT, _dashT;
        private Color _tint = Color.white;

        public void SetDynamics(Phase phase, float chargeT, float dashT, Color tint)
        {
            _phase = phase;
            _chargeT = Mathf.Clamp01(chargeT);
            _dashT = Mathf.Clamp01(dashT);
            _tint = tint;
        }

        private void Awake()
        {
            _wavePos = new Vector3[_wavePoints];
            _wavePhase = new float[Mathf.Max(1, _waveCount)];
            for (int i = 0; i < _wavePhase.Length; i++) _wavePhase[i] = Random.value * Mathf.PI * 2f;

            _arrow = MakeLine("Arrow", 3, _lineWidth * 1.1f);
            _axis = MakeLine("Axis", 2, _lineWidth * 0.7f);
            _waves = new LineRenderer[Mathf.Max(1, _waveCount)];
            for (int i = 0; i < _waves.Length; i++) _waves[i] = MakeLine("Wave" + i, _wavePoints, _lineWidth);
        }

        private LineRenderer MakeLine(string n, int points, float width)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.widthMultiplier = width;
            lr.positionCount = points;
            lr.sharedMaterial = _lineMaterial;
            lr.sortingOrder = 8;
            return lr;
        }

        private void Update()
        {
            MusicState s = AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.State : null;
            float bass = s != null ? Mathf.Clamp01(s.Bass) : 0f;
            float t = Time.time;

            // State blends: charge compresses toward the head, dash stretches behind.
            float trailLen = _lenRoam;
            float freq = _freqRoam;
            float bright = _idleBrightness;
            float amp = _baseAmplitude + bass * _bassAmplitude;

            if (_phase == Phase.Charge)
            {
                trailLen = Mathf.Lerp(_lenRoam, _lenCharge, _chargeT);
                freq = Mathf.Lerp(_freqRoam, _freqCharge, _chargeT);
                amp += _chargeAmplitude * _chargeT;
                bright = Mathf.Lerp(_idleBrightness, _chargeBrightness, _chargeT);
            }
            else if (_phase == Phase.Dash)
            {
                trailLen = Mathf.Lerp(_lenCharge, _lenDash, _dashT);
                bright = _dashBrightness;
            }

            // Purple at rest → shifts toward PINK during the DASH (a small warm-up during charge).
            float pinkMix = Mathf.Clamp01(0.25f * _chargeT + _dashT);
            float hue = Mathf.Lerp(_baseHue, _pinkHue, pinkMix);
            Color c = Color.HSVToRGB(Mathf.Repeat(hue, 1f), 0.85f, 1f) * (bright * Mathf.Max(0.2f, _tint.a));
            c.a = 1f;

            // Arrow head (hollow chevron pointing +X): upper wing → tip → lower wing.
            float headBright = 1f + 0.6f * _chargeT;
            _arrow.SetPosition(0, new Vector3(-_arrowLength * 0.25f, _arrowHalfWidth, 0f));
            _arrow.SetPosition(1, new Vector3(_arrowLength, 0f, 0f));
            _arrow.SetPosition(2, new Vector3(-_arrowLength * 0.25f, -_arrowHalfWidth, 0f));
            SetColor(_arrow, c * headBright);

            // Central axis streak, from just behind the tip trailing back (longer during dash).
            _axis.SetPosition(0, new Vector3(_arrowLength * 0.6f, 0f, 0f));
            _axis.SetPosition(1, new Vector3(-trailLen, 0f, 0f));
            SetColor(_axis, c * (_phase == Phase.Dash ? 1.4f : 1f));

            // Trailing sine traces, tapered at both ends, spread slightly in phase.
            for (int w = 0; w < _waves.Length; w++)
            {
                float phase = _wavePhase[w] + w * 1.7f;
                float sign = (w % 2 == 0) ? 1f : -1f;
                for (int p = 0; p < _wavePoints; p++)
                {
                    float f = p / (float)(_wavePoints - 1);         // 0 head → 1 tail
                    float x = Mathf.Lerp(_arrowLength * 0.4f, -trailLen, f);
                    float taper = Mathf.Sin(f * Mathf.PI);           // fade at head+tail
                    float y = sign * amp * taper * Mathf.Sin(x * freq + t * _scrollSpeed + phase);
                    _wavePos[p] = new Vector3(x, y, 0f);
                }
                _waves[w].SetPositions(_wavePos);
                SetColor(_waves[w], c);
            }
        }

        private static void SetColor(LineRenderer lr, Color c)
        {
            // Additive: brightness rides in the color; taper the tail alpha to fade the trace out.
            lr.startColor = c;
            lr.endColor = new Color(c.r, c.g, c.b, 0f);
        }
    }
}
