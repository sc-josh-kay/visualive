using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// The Splitter's "fractured waveform" look (spec10): several THIN, DISCONNECTED luminous arcs
    /// around a dark center — a waveform ring that has broken apart. Shares DNA with the Color Eater
    /// (arc waveforms) but broken, with gaps, so it reads as pieces struggling to stay together.
    /// SPECTRAL FLUX drives visual instability (NOT the AI): more jitter, relative rotation, separation
    /// and waveform amplitude as the music changes — subtle at rest, agitated during transitions.
    /// Light green-teal, tinting toward deep purple at high flux. Built from LineRenderers (local
    /// space, additive EnergyTrail); cosmetic only (the split/consume live on Splitter.cs).
    /// </summary>
    public class SplitterVisualizer : MonoBehaviour
    {
        [SerializeField] private Material _lineMaterial;

        [Header("Geometry")]
        [SerializeField] private int _arcCount = 5;
        [SerializeField] private int _pointsPerArc = 8;
        [SerializeField] private float _lineWidth = 0.03f;
        [Tooltip("Geometry rebuild rate (Hz). Line meshes rebuild at most this often (perf) — the " +
                 "wobble is subtle so 24 Hz is imperceptible while cutting mesh rebuilds vs every frame.")]
        [SerializeField] private float _updateHz = 24f;
        [SerializeField] private float _radius = 0.4f;
        [Tooltip("Angular width of each arc (radians). Smaller = bigger gaps between fragments.")]
        [SerializeField] private float _arcSpan = 0.7f;

        [Header("Flux instability")]
        [SerializeField] private float _idleAmp = 0.02f;
        [SerializeField] private float _fluxAmp = 0.12f;
        [SerializeField] private float _idleJitter = 0.004f;
        [SerializeField] private float _fluxJitter = 0.06f;
        [SerializeField] private float _fluxSeparation = 0.16f;   // radius growth with flux
        [SerializeField] private float _rotSpeed = 12f;           // base slow relative rotation (deg/s)
        [SerializeField] private float _fluxRotSpeed = 70f;
        [SerializeField] private float _fluxSmoothing = 6f;

        [Header("Color (teal → deep purple at high flux)")]
        [SerializeField] private float _tealHue = 0.46f;
        [SerializeField] private float _purpleHue = 0.78f;
        [SerializeField] private float _brightness = 1.4f;
        [SerializeField] private float _waveSpeed = 5f;

        private LineRenderer[] _arcs;
        private Vector3[] _pts;
        private float[] _arcPhase;
        private float[] _arcRot;      // per-arc angular offset (relative rotation drift)
        private float[] _arcJitterSeed;
        private float _flux;
        private float _accum;

        private void Awake()
        {
            int n = Mathf.Max(1, _arcCount);
            _arcs = new LineRenderer[n];
            _pts = new Vector3[_pointsPerArc];
            _arcPhase = new float[n];
            _arcRot = new float[n];
            _arcJitterSeed = new float[n];
            for (int i = 0; i < n; i++)
            {
                _arcPhase[i] = Random.value * Mathf.PI * 2f;
                _arcJitterSeed[i] = Random.value * 100f;
                _arcs[i] = MakeArc("Arc" + i);
            }
        }

        private LineRenderer MakeArc(string n)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.alignment = LineAlignment.View;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.widthMultiplier = _lineWidth;
            lr.positionCount = _pointsPerArc;
            lr.sharedMaterial = _lineMaterial;
            lr.sortingOrder = 8;
            return lr;
        }

        private void Update()
        {
            // Throttle the (mesh-rebuilding) geometry update — bounds per-Splitter cost, which matters
            // because Splitters multiply. Use the real elapsed time as the step so motion is unchanged.
            _accum += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, _updateHz);
            if (_accum < interval) return;
            float step = _accum;
            _accum = 0f;

            MusicState s = AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.State : null;
            float fluxRaw = s != null ? Mathf.Clamp01(s.SpectralFlux) : 0f;
            _flux = Mathf.Lerp(_flux, fluxRaw, 1f - Mathf.Exp(-_fluxSmoothing * step));

            float t = Time.time;
            float amp = Mathf.Lerp(_idleAmp, _idleAmp + _fluxAmp, _flux);
            float jitter = Mathf.Lerp(_idleJitter, _idleJitter + _fluxJitter, _flux);
            float radius = _radius + _fluxSeparation * _flux;

            // Teal at rest → tint toward deep purple as flux rises.
            Color col = Color.HSVToRGB(Mathf.Lerp(_tealHue, _purpleHue, _flux * 0.5f), 0.8f, 1f) * _brightness;
            col.a = 1f;

            int n = _arcs.Length;
            for (int i = 0; i < n; i++)
            {
                // Each fragment drifts in angle (relative rotation), faster with flux, alternating dir.
                float dir = (i % 2 == 0) ? 1f : -1f;
                _arcRot[i] += dir * (_rotSpeed + _fluxRotSpeed * _flux) * Mathf.Deg2Rad * step;
                float baseAngle = i * (Mathf.PI * 2f / n) + _arcRot[i];

                // Per-fragment jitter (the "struggling to stay together" wobble) — cheap seeded sines
                // instead of PerlinNoise, since this runs per arc per Splitter.
                float seed = _arcJitterSeed[i];
                float jx = jitter * Mathf.Sin(t * 3.1f + seed);
                float jy = jitter * Mathf.Cos(t * 2.7f + seed * 1.3f);

                for (int p = 0; p < _pointsPerArc; p++)
                {
                    float f = p / (float)(_pointsPerArc - 1);            // 0..1 across the arc
                    float a = baseAngle + Mathf.Lerp(-_arcSpan * 0.5f, _arcSpan * 0.5f, f);
                    float taper = Mathf.Sin(f * Mathf.PI);              // fade waveform at arc ends
                    float rr = radius + amp * taper * Mathf.Sin(f * 9f + t * _waveSpeed + _arcPhase[i]);
                    _pts[p] = new Vector3(Mathf.Cos(a) * rr + jx, Mathf.Sin(a) * rr + jy, 0f);
                }
                _arcs[i].SetPositions(_pts);
                _arcs[i].startColor = col;
                _arcs[i].endColor = col;
            }
        }
    }
}
