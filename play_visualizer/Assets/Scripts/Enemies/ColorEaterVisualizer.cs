using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// The Color Eater's living visual (spec: "the music wrapped around a circle"). It builds 3–5 thin
    /// LineRenderer rings ONCE, then each frame deforms a short waveform ARC on each ring from that
    /// ring's assigned frequency band, rotates/offsets/wobbles it, and fades it between a nearly-
    /// invisible idle state and a bright active state. Reads MusicState only — no FFT, no gameplay.
    /// Kept separate from ColorEater (which sets <see cref="Consuming"/>).
    /// </summary>
    public class ColorEaterVisualizer : MonoBehaviour
    {
        [SerializeField] private Material _lineMaterial;   // additive, vertex-color × _Tint (EnergyTrail)
        [Tooltip("Optional dim, music-colored center fill so the enemy stays visible against dark areas.")]
        [SerializeField] private SpriteRenderer _center;

        [Header("Geometry")]
        [SerializeField] private int _pointsPerRing = 40;
        [SerializeField] private float _lineWidth = 0.022f;
        [SerializeField] private float _radiusMin = 0.28f;
        [SerializeField] private float _radiusMax = 0.5f;

        [Header("Response")]
        [Tooltip("Radial waveform displacement at idle / active.")]
        [SerializeField] private float _idleAmp = 0.02f;
        [SerializeField] private float _activeAmp = 0.22f;
        [Tooltip("Ring brightness at idle / active.")]
        [SerializeField] private float _idleBrightness = 0.28f;
        [SerializeField] private float _activeBrightness = 1.7f;
        [Tooltip("How much the consuming state and bass lift a ring's activity.")]
        [SerializeField] private float _consumeBoost = 0.15f;
        [SerializeField] private float _bassActivity = 0.15f;
        [SerializeField] private float _activitySmoothing = 8f;

        [Header("Center fill (readability)")]
        [SerializeField] private float _centerBrightness = 0.3f;
        [SerializeField] private float _centerBassPulse = 0.25f;

        [Header("Color")]
        [Tooltip("How far the rings may drift from the center hue (fraction of the color wheel). " +
                 "0.13 ≈ ±47°, e.g. a purple center → rings between red and blue.")]
        [SerializeField] private float _hueRange = 0.13f;

        /// <summary>The Color Eater's fixed base color, chosen from the music at spawn (for death paint).</summary>
        public Color BaseColor => Color.HSVToRGB(_baseHue, 0.9f, 1f);

        /// <summary>0..1 how strongly this Color Eater is currently eating (set by ColorEater).</summary>
        public float Consuming { get; set; }

        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private const float TwoPi = Mathf.PI * 2f;

        private class Ring
        {
            public LineRenderer Lr;
            public float Radius;
            public Vector2 Center;
            public float WobbleAmount, WobbleSpeed, WobblePhase;
            public float RotFreq;       // oscillation frequency (rad/sec)
            public float RotAmp;        // oscillation amplitude (rad) — bounded so arcs stay in-sector
            public float RotPhase;
            public float ArcDir;        // world angle the arc points at
            public float ArcLen;        // radians
            public float LoFrac, HiFrac; // frequency-band range (0..1 across the spectrum)
            public float HueOffset;
            public float WavePeaks, WaveSpeed, WavePhase;
            public float Activity;      // smoothed
        }

        private Ring[] _rings;
        private Vector3[] _pos;
        private MaterialPropertyBlock _mpb;
        private float _baseHue;   // fixed at spawn from the music

        private void Awake()
        {
            Build(Random.Range(int.MinValue, int.MaxValue));
        }

        private void Build(int seed)
        {
            var rng = new System.Random(seed);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            int ringCount = 3 + rng.Next(0, 3); // 3..5
            _rings = new Ring[ringCount];
            _pos = new Vector3[_pointsPerRing];
            _mpb = new MaterialPropertyBlock();

            // Base hue chosen ONCE from the music at spawn (then fixed for this enemy's lifetime).
            MusicState spawnMusic = AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.State : null;
            float spawnHue = spawnMusic != null ? spawnMusic.SpectralCentroid : (float)rng.NextDouble();
            _baseHue = Mathf.Repeat(spawnHue + Range(-0.04f, 0.04f), 1f);

            // Distribute each ring's waveform arc into its own evenly-spaced sector (with small jitter)
            // so the active/deforming parts are spread around the perimeter, never stacking.
            float arcSlice = TwoPi / ringCount;
            float arcOffset = Range(0f, TwoPi);

            for (int i = 0; i < ringCount; i++)
            {
                float f = ringCount > 1 ? (float)i / (ringCount - 1) : 0f;
                var ring = new Ring
                {
                    Radius = Mathf.Lerp(_radiusMin, _radiusMax, f) + Range(-0.02f, 0.02f),
                    Center = new Vector2(Range(-0.025f, 0.025f), Range(-0.025f, 0.025f)),
                    WobbleAmount = Range(0.006f, 0.016f),
                    WobbleSpeed = Range(0.4f, 1.4f),
                    WobblePhase = Range(0f, TwoPi),
                    RotFreq = Range(0.3f, 0.9f),
                    RotAmp = Range(7f, 12f) * Mathf.Deg2Rad,
                    RotPhase = Range(0f, TwoPi),
                    ArcDir = arcOffset + i * arcSlice + Range(-arcSlice * 0.1f, arcSlice * 0.1f),
                    // Cap the arc to fit inside its sector (with jitter) so neighbouring arcs keep a gap.
                    ArcLen = Mathf.Min(Range(25f, 70f) * Mathf.Deg2Rad, arcSlice * 0.6f),
                    LoFrac = (float)i / ringCount,       // ring 0 = bass … ring N-1 = treble
                    HiFrac = (float)(i + 1) / ringCount,
                    // Static per-ring hue bias, spread WITHIN the restricted range around the center.
                    HueOffset = (ringCount > 1 ? Mathf.Lerp(-1f, 1f, (float)i / (ringCount - 1)) : 0f)
                                * _hueRange * 0.55f + Range(-0.015f, 0.015f),
                    WavePeaks = Mathf.Round(Range(2f, 5f)),
                    WaveSpeed = Range(6f, 14f),
                    WavePhase = Range(0f, TwoPi),
                };

                var go = new GameObject("Ring" + i);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.positionCount = _pointsPerRing;
                lr.widthMultiplier = _lineWidth;
                lr.numCapVertices = 0;
                lr.numCornerVertices = 2;
                lr.textureMode = LineTextureMode.Stretch;
                lr.alignment = LineAlignment.View;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.sortingOrder = 8;
                lr.sharedMaterial = _lineMaterial;
                lr.colorGradient = BuildArcGradient(ring.ArcLen); // dim baseline + one bright arc
                ring.Lr = lr;

                _rings[i] = ring;
            }
        }

        // A fixed alpha profile along the loop: low baseline everywhere, bright plateau over the arc
        // (centered at time 0.5, since point index N/2 sits at the arc center).
        private static Gradient BuildArcGradient(float arcLen)
        {
            const float baseline = 0.22f;
            float half = Mathf.Clamp(arcLen / (2f * TwoPi), 0.03f, 0.4f); // time half-width
            const float ramp = 0.03f;

            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(baseline, 0f),
                    new GradientAlphaKey(baseline, Mathf.Clamp01(0.5f - half - ramp)),
                    new GradientAlphaKey(1f, Mathf.Clamp01(0.5f - half)),
                    new GradientAlphaKey(1f, Mathf.Clamp01(0.5f + half)),
                    new GradientAlphaKey(baseline, Mathf.Clamp01(0.5f + half + ramp)),
                    new GradientAlphaKey(baseline, 1f),
                });
            return g;
        }

        private void Update()
        {
            if (_rings == null) return;

            MusicState s = AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.State : null;
            float bass = s != null ? s.Bass : 0f;
            float treble = s != null ? s.Treble : 0f;
            float flux = s != null ? Mathf.Clamp01(s.SpectralFlux) : 0f;
            float[] bands = s != null ? s.FrequencyBands : null;
            int bandCount = bands != null ? bands.Length : 0;

            float time = Time.time;
            float k = 1f - Mathf.Exp(-Time.deltaTime * _activitySmoothing);

            for (int rIdx = 0; rIdx < _rings.Length; rIdx++)
            {
                Ring ring = _rings[rIdx];

                float loIdx = ring.LoFrac * Mathf.Max(0, bandCount - 1);
                float hiIdx = ring.HiFrac * Mathf.Max(0, bandCount - 1);
                float rawAct = bandCount > 0 ? BandAverage(bands, loIdx, hiIdx) : 0f;
                ring.Activity = Mathf.Lerp(ring.Activity, rawAct, k);

                float act = Mathf.Clamp01(ring.Activity + Consuming * _consumeBoost + bass * _bassActivity);
                float amp = Mathf.Lerp(_idleAmp, _activeAmp, act);
                // Bounded sway (not continuous drift) so each ring's arc stays in its own sector.
                float rot = ring.RotAmp * Mathf.Sin(time * ring.RotFreq + ring.RotPhase);
                Vector2 wobble = ring.Center + new Vector2(
                    Mathf.Sin(time * ring.WobbleSpeed + ring.WobblePhase),
                    Mathf.Cos(time * ring.WobbleSpeed * 1.1f + ring.WobblePhase)) * ring.WobbleAmount;

                float halfArc = ring.ArcLen * 0.5f;
                for (int i = 0; i < _pointsPerRing; i++)
                {
                    float rel = (float)i / _pointsPerRing - 0.5f;   // -0.5..0.5
                    float baseA = ring.ArcDir + rel * TwoPi;
                    float dFromArc = Mathf.Abs(rel * TwoPi);

                    float disp = 0f;
                    if (dFromArc < halfArc && bandCount > 0)
                    {
                        float f = Mathf.Clamp01((rel * TwoPi + halfArc) / ring.ArcLen); // 0..1 across arc
                        float bandVal = InterpBand(bands, Mathf.Lerp(loIdx, hiIdx, f));
                        float fine = (treble * 0.5f + flux * 0.5f)
                                   * Mathf.Sin(f * ring.WavePeaks * TwoPi + time * ring.WaveSpeed + ring.WavePhase);
                        float taper = Mathf.Sin(f * Mathf.PI); // blend into the baseline at arc ends
                        disp = amp * (bandVal + fine) * taper;
                    }

                    float rr = ring.Radius + disp;
                    float a = baseA + rot;
                    Vector2 p = wobble + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr;
                    _pos[i] = new Vector3(p.x, p.y, 0f);
                }

                ring.Lr.SetPositions(_pos);

                // Ring hue = the fixed center hue + a static per-ring bias + a music shift, all kept
                // within the restricted range (so colors stay near the center, never the whole wheel).
                float musicShift = (ring.Activity - 0.5f) * _hueRange * 0.7f;
                float hue = Mathf.Repeat(_baseHue + ring.HueOffset + musicShift, 1f);
                Color col = Color.HSVToRGB(hue, 0.9f, 1f);
                float bright = Mathf.Lerp(_idleBrightness, _activeBrightness, act);
                _mpb.SetColor(TintId, col * bright);
                ring.Lr.SetPropertyBlock(_mpb);
            }

            // Dim center fill (fixed hue chosen at spawn — only its brightness pulses with bass).
            if (_center != null)
            {
                Color cc = Color.HSVToRGB(_baseHue, 0.8f, 1f);
                cc.a = _centerBrightness + bass * _centerBassPulse;
                _center.color = cc;
            }
        }

        private static float BandAverage(float[] bands, float loIdx, float hiIdx)
        {
            int lo = Mathf.Clamp(Mathf.FloorToInt(loIdx), 0, bands.Length - 1);
            int hi = Mathf.Clamp(Mathf.CeilToInt(hiIdx), 0, bands.Length - 1);
            float sum = 0f; int n = 0;
            for (int i = lo; i <= hi; i++) { sum += bands[i]; n++; }
            return n > 0 ? sum / n : 0f;
        }

        private static float InterpBand(float[] bands, float idx)
        {
            int i0 = Mathf.Clamp(Mathf.FloorToInt(idx), 0, bands.Length - 1);
            int i1 = Mathf.Clamp(i0 + 1, 0, bands.Length - 1);
            return Mathf.Lerp(bands[i0], bands[i1], idx - i0);
        }
    }
}
