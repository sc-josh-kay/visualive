using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// The Corruptor's crisp overlay on top of the swirling black-hole glow: a horizontal AUDIO
    /// WAVEFORM across the middle driven by the frequency spectrum + bass, and drifting speckles that
    /// BURST on bass onsets. Palette = teal/purple/pink. Reads MusicState only. Lives on the Visual
    /// child. The waveform stays put; the spiral (shader) and speckles do the rotating/swirling.
    /// </summary>
    public class CorruptorVisualizer : MonoBehaviour
    {
        [SerializeField] private Material _lineMaterial;     // additive vertex-color × _Tint (EnergyTrail)
        [SerializeField] private Material _speckleMaterial;  // additive glow sprite (EnergySprite)

        [Header("Palette")]
        [SerializeField] private Color _teal = new Color(0.1f, 0.95f, 1f);
        [SerializeField] private Color _purple = new Color(0.6f, 0.3f, 1f);
        [SerializeField] private Color _pink = new Color(1f, 0.25f, 0.8f);
        [Tooltip("Speckle-emit radius (also the visible cloud size).")]
        [SerializeField] private float _outerRadius = 0.5f;

        [Header("Waveform")]
        [SerializeField] private int _wavePoints = 48;
        [SerializeField] private float _waveHalfWidth = 0.6f;
        [SerializeField] private float _waveAmp = 0.14f;
        [SerializeField] private float _waveWidth = 0.02f;
        [SerializeField] private float _waveBaseBright = 0.55f;
        [SerializeField] private float _waveBassBright = 1.3f;

        [Header("Speckles")]
        [SerializeField] private float _speckleRate = 14f;
        [SerializeField] private int _speckleBurst = 26;
        [SerializeField] private float _speckleSpeed = 1.1f;
        [SerializeField] private float _speckleLife = 1.2f;
        [SerializeField] private float _speckleSize = 0.09f;

        private static readonly int TintId = Shader.PropertyToID("_Tint");

        private LineRenderer _wave;
        private ParticleSystem _speckles;
        private ParticleSystem.EmissionModule _emission;
        private Vector3[] _wavePos;
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _wave = MakeWaveform();
            _wavePos = new Vector3[_wavePoints];
            _speckles = MakeSpeckles();
            if (_speckles != null) _emission = _speckles.emission;
        }

        private LineRenderer MakeWaveform()
        {
            var go = new GameObject("Waveform");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = false;
            lr.positionCount = _wavePoints;
            lr.widthMultiplier = _waveWidth;
            lr.numCornerVertices = 2;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sortingOrder = 10;
            lr.sharedMaterial = _lineMaterial;
            return lr;
        }

        private ParticleSystem MakeSpeckles()
        {
            var go = new GameObject("Speckles");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var main = ps.main;
            main.startLifetime = _speckleLife;
            main.startSpeed = _speckleSpeed;
            main.startSize = _speckleSize;
            main.maxParticles = 220;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor = new ParticleSystem.MinMaxGradient(PaletteGradient())
            {
                mode = ParticleSystemGradientMode.RandomColor
            };

            var emission = ps.emission;
            emission.rateOverTime = _speckleRate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = _outerRadius;
            shape.radiusThickness = 0.15f;
            shape.arc = 360f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalZ = 0.6f; // gentle swirl as they drift off

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(fade);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = _speckleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 9;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Play();
            return ps;
        }

        private Gradient PaletteGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(_teal, 0f),
                    new GradientColorKey(_purple, 0.5f),
                    new GradientColorKey(_pink, 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private void Update()
        {
            if (_wave == null) return;

            MusicState s = AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.State : null;
            float bass = s != null ? s.Bass : 0f;
            float treble = s != null ? s.Treble : 0f;
            float[] bands = s != null ? s.FrequencyBands : null;
            int bandCount = bands != null ? bands.Length : 0;
            bool bassOnset = s != null && s.BassOnset;
            float time = Time.time;

            float bright = _waveBaseBright + bass * _waveBassBright;

            // Horizontal audio waveform: the spectrum plotted across x, height pulsing with bass.
            float amp = _waveAmp * (0.3f + bass * 1.7f);
            for (int j = 0; j < _wavePoints; j++)
            {
                float t = _wavePoints > 1 ? (float)j / (_wavePoints - 1) : 0.5f;
                float x = Mathf.Lerp(-_waveHalfWidth, _waveHalfWidth, t);
                float band = bandCount > 0 ? SampleBand(bands, t * (bandCount - 1)) : 0f;
                float fine = treble * 0.25f * Mathf.Sin(t * 30f + time * 12f);
                float y = (band + fine) * amp * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI)); // taper ends
                _wavePos[j] = new Vector3(x, y, 0f);
            }
            _wave.SetPositions(_wavePos);
            SetTint(_wave, _purple * bright);

            // Speckles drift continuously (scaled by bass) and burst on a bass onset.
            _emission.rateOverTime = _speckleRate * (0.3f + bass);
            if (bassOnset && _speckles != null) _speckles.Emit(_speckleBurst);
        }

        private void SetTint(LineRenderer lr, Color color)
        {
            lr.GetPropertyBlock(_mpb);
            _mpb.SetColor(TintId, color);
            lr.SetPropertyBlock(_mpb);
        }

        private static float SampleBand(float[] bands, float idx)
        {
            int i0 = Mathf.Clamp(Mathf.FloorToInt(idx), 0, bands.Length - 1);
            int i1 = Mathf.Clamp(i0 + 1, 0, bands.Length - 1);
            return Mathf.Lerp(bands[i0], bands[i1], idx - i0);
        }
    }
}
