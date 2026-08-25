using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Visuals
{
    /// <summary>
    /// Coordinator for the new layered visualizer (built fresh, alongside the old FeedbackController).
    /// Owns the render target + display quad, the set of swappable patterns, and — later — the trail
    /// and ripple systems and cross-fade transitions. Reads MusicState; no gameplay logic.
    ///
    /// Phase V1: one pattern (Kaleidoscope), rendered to an RT and shown on its own quad. Press V to
    /// toggle between the old effect and this one; B cycles patterns (a no-op with one pattern).
    /// </summary>
    public class VisualizerCore : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Transform _player;
        [SerializeField] private Renderer _displayRenderer;
        [SerializeField] private AudioAnalyzer _analyzer;
        [Tooltip("SmokeField shader — the persistent field pass.")]
        [SerializeField] private Shader _smokeShader;
        [Tooltip("Kaleidoscope shader — used as the display/symmetry pass.")]
        [SerializeField] private Shader _kaleidoscopeShader;

        [Header("Smoke pattern (live-tunable)")]
        [SerializeField] private SmokePatternParams _smokeParams = new SmokePatternParams();

        [Header("Ripples (live-tunable)")]
        [SerializeField] private RippleParams _rippleParams = new RippleParams();

        [Header("Additional patterns")]
        [SerializeField] private Shader _fluidShader;
        [SerializeField] private Shader _filamentShader;
        [SerializeField] private Shader _blendShader;
        [SerializeField] private FluidPatternParams _fluidParams = new FluidPatternParams();
        [SerializeField] private FilamentPatternParams _filamentParams = new FilamentPatternParams();

        [Header("Intensity / transitions")]
        [SerializeField] private VisualIntensity _intensity = new VisualIntensity();
        [SerializeField] private float _fadeDuration = 1.2f;

        [Header("Gameplay field (Phase 1-2)")]
        [Tooltip("FieldSplat shader — applies enemy consume / death paint into the field. " +
                 "Auto-found by name if unset.")]
        [SerializeField] private Shader _fieldSplatShader;
        [SerializeField] private CoverageParams _coverageParams = new CoverageParams();

        [Header("Old system (for the toggle)")]
        [SerializeField] private FeedbackController _oldSystem;
        [SerializeField] private Renderer _oldDisplay;
        [Tooltip("The old feedback camera — must be disabled with the old system so it doesn't " +
                 "render to the backbuffer and conflict with the Main Camera in URP.")]
        [SerializeField] private Camera _oldFeedbackCamera;

        [Header("Config")]
        [Range(0.25f, 1f)] [SerializeField] private float _resScale = 0.5f;
        [SerializeField] private float _displayZ = 5f;
        [SerializeField] private bool _useNew = true; // new layered visualizer is now the default

        private readonly List<IVisualizerPattern> _patterns = new List<IVisualizerPattern>();
        private readonly RippleSystem _ripples = new RippleSystem();
        private readonly CoverageSampler _coverage = new CoverageSampler();
        private readonly List<FieldSplat> _splatBuffer = new List<FieldSplat>();
        private readonly List<DistortRequest> _distortBuffer = new List<DistortRequest>();
        private readonly SplatData _splatData = new SplatData();
        private Material _splatMat;
        private int _active;
        private Material _displayMat;
        private Material _blendMat;
        private RenderTexture _target, _targetA, _targetB;
        private int _w, _h;
        private bool _fading;
        private int _from, _to;
        private float _fadeT;

        private void Awake()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_analyzer == null) _analyzer = FindFirstObjectByType<AudioAnalyzer>();
            if (_displayRenderer != null) _displayMat = _displayRenderer.material;

            // Gameplay field seam: ensure the service exists and build the shared splat material
            // that field patterns use to stamp enemy consume / death paint into their field.
            VisualizerField.Ensure();
            if (_fieldSplatShader == null) _fieldSplatShader = Shader.Find("PlayVisualizer/FieldSplat");
            _splatMat = _fieldSplatShader != null ? new Material(_fieldSplatShader) : null;
            if (_splatMat == null)
            {
                // In a device build this means the shader was stripped (no serialized/asset reference).
                // Assign _fieldSplatShader in the scene or add it to Always Included Shaders.
                Debug.LogWarning("VisualizerCore: FieldSplat shader missing — enemy consume & death " +
                                 "paint will not render (shader stripped from the build?).");
            }

            var ctx = new VisualizerContext
            {
                Camera = _mainCamera,
                Player = _player,
                SplatMaterial = _splatMat
            };
            _patterns.Add(new KaleidoscopePattern(_smokeShader, _kaleidoscopeShader, _smokeParams));
            _patterns.Add(new FluidSwirlPattern(_fluidShader, _kaleidoscopeShader, _fluidParams));
            _patterns.Add(new FilamentPattern(_filamentShader, _filamentParams));
            foreach (var p in _patterns) p.Configure(ctx);

            _ripples.Configure(_rippleParams);
            _coverage.Configure(_coverageParams);
            _blendMat = new Material(_blendShader);

            EnsureTarget();
            ApplyToggle();
        }

        private void Update()
        {
            HandleKeys();

            if (!_useNew || _patterns.Count == 0)
            {
                return;
            }

            EnsureTarget();
            FitDisplayQuad();

            MusicState s = _analyzer != null ? _analyzer.State : new MusicState();
            float dt = Time.deltaTime;
            float intensity = _intensity.Update(s.Energy, dt);

            // Ripples: advance, spawn on onset/beat at the player.
            _ripples.Update(dt);
            double dsp = AudioSettings.dspTime;
            Vector3 vp = _mainCamera != null && _player != null
                ? _mainCamera.WorldToViewportPoint(_player.position)
                : new Vector3(0.5f, 0.5f, 0f);
            var origin = new Vector2(vp.x, vp.y);
            if (_rippleParams.SpawnOnBassOnset && s.BassOnset)
                _ripples.Spawn(origin, Mathf.Max(0.4f, s.BassOnsetStrength), dsp);
            if (_rippleParams.SpawnOnBeat && s.Beat)
                _ripples.Spawn(origin, 1f, dsp);

            // Weapon "energy": drain queued transient distortions (impacts, vortices) into ripples.
            VisualizerField vfield = VisualizerField.Instance;
            if (vfield != null && _mainCamera != null)
            {
                vfield.DrainDistorts(_distortBuffer);
                for (int i = 0; i < _distortBuffer.Count; i++)
                {
                    Vector3 dvp = _mainCamera.WorldToViewportPoint(_distortBuffer[i].WorldPos);
                    _ripples.Spawn(new Vector2(dvp.x, dvp.y), _distortBuffer[i].Strength, dsp);
                }
            }

            RippleData ripples = _ripples.Pack();

            // Gameplay: pull queued enemy/player splats and the player's paint-gain throttle.
            BuildSplats();
            float paintGain = VisualizerField.Instance != null
                ? Mathf.Clamp01(VisualizerField.Instance.PlayerPaintGain)
                : 1f;

            if (_fading)
            {
                // Only the incoming pattern receives splats, so they aren't double-applied.
                RenderPattern(_from, s, intensity, paintGain, dt, ripples, null, _targetA);
                RenderPattern(_to, s, intensity, paintGain, dt, ripples, _splatData, _targetB);

                _fadeT += dt / Mathf.Max(0.01f, _fadeDuration);
                _blendMat.SetTexture("_TexB", _targetB);
                _blendMat.SetFloat("_T", Mathf.SmoothStep(0f, 1f, _fadeT));
                Graphics.Blit(_targetA, _target, _blendMat);

                if (_fadeT >= 1f) { _fading = false; _active = _to; }
            }
            else
            {
                RenderPattern(_active, s, intensity, paintGain, dt, ripples, _splatData, _target);
            }

            if (_displayMat != null)
            {
                _displayMat.SetTexture("_MainTex", _target);
                _displayMat.SetFloat("_Segments", 1f);   // passthrough
                _displayMat.SetFloat("_Brightness", 1f);
            }

            // Coverage: measure the displayed field and publish it for scoring/targeting.
            _coverage.Update(_target);
            VisualizerField vf = VisualizerField.Instance;
            if (vf != null)
            {
                vf.Coverage = _coverage.Coverage;

                // Publish the painted-mass hot point (viewport → world) for enemy targeting.
                if (_coverage.HasContent && _mainCamera != null)
                {
                    Vector2 uv = _coverage.HotUV;
                    float depth = _mainCamera.orthographic
                        ? Mathf.Abs(_displayZ - _mainCamera.transform.position.z)
                        : Mathf.Abs(_mainCamera.transform.position.z);
                    Vector3 world = _mainCamera.ViewportToWorldPoint(new Vector3(uv.x, uv.y, depth));
                    vf.SetHotPoint(new Vector2(world.x, world.y));
                }
                else
                {
                    vf.ClearHotPoint();
                }
            }
        }

        /// <summary>Drain gameplay splat requests and convert them to viewport space for the shader.</summary>
        private void BuildSplats()
        {
            _splatData.Count = 0;
            VisualizerField field = VisualizerField.Instance;
            if (field == null || _mainCamera == null)
            {
                return;
            }

            field.DrainInto(_splatBuffer);
            if (_splatBuffer.Count == 0)
            {
                return;
            }

            // Viewport radius is world radius over the orthographic view height (the field's
            // height-normalized metric; the shader re-applies aspect on x).
            float orthoH = _mainCamera.orthographic ? _mainCamera.orthographicSize * 2f : 1f;
            int n = Mathf.Min(_splatBuffer.Count, SplatData.Max);
            for (int i = 0; i < n; i++)
            {
                FieldSplat sp = _splatBuffer[i];
                Vector3 vp = _mainCamera.WorldToViewportPoint(sp.WorldPos);
                float radiusV = orthoH > 0f ? sp.WorldRadius / orthoH : 0.05f;
                _splatData.Splats[i] = new Vector4(vp.x, vp.y, radiusV, sp.Strength);
                float mode = sp.Mode == SplatMode.Paint ? 1f : -1f;
                _splatData.Colors[i] = new Vector4(sp.Color.r, sp.Color.g, sp.Color.b, mode);
            }
            _splatData.Count = n;
        }

        private void HandleKeys()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.vKey.wasPressedThisFrame)
            {
                _useNew = !_useNew;
                ApplyToggle();
            }
            if (Keyboard.current.bKey.wasPressedThisFrame && _patterns.Count > 1 && !_fading)
            {
                _from = _active;
                _to = (_active + 1) % _patterns.Count;
                _fadeT = 0f;
                _fading = true;
            }
        }

        private void RenderPattern(int index, MusicState s, float intensity, float paintGain, float dt,
            RippleData ripples, SplatData splats, RenderTexture target)
        {
            IVisualizerPattern p = _patterns[index];
            p.UpdatePattern(s, intensity, paintGain, dt);
            p.InjectSplats(splats);
            p.ApplyRipple(ripples);
            p.Render(null, target);
        }

        private void ApplyToggle()
        {
            if (_oldSystem != null) _oldSystem.enabled = !_useNew;
            if (_oldDisplay != null) _oldDisplay.enabled = !_useNew;
            if (_oldFeedbackCamera != null) _oldFeedbackCamera.enabled = !_useNew;
            if (_displayRenderer != null) _displayRenderer.enabled = _useNew;
        }

        private void FitDisplayQuad()
        {
            if (_displayRenderer == null || _mainCamera == null || !_mainCamera.orthographic) return;
            float h = _mainCamera.orthographicSize * 2f;
            float w = h * _mainCamera.aspect;
            Transform t = _displayRenderer.transform;
            Vector3 camPos = _mainCamera.transform.position;
            t.position = new Vector3(camPos.x, camPos.y, _displayZ);
            t.rotation = _mainCamera.transform.rotation;
            t.localScale = new Vector3(w, h, 1f);
        }

        private void EnsureTarget()
        {
            int w = Mathf.Max(16, Mathf.RoundToInt(Screen.width * _resScale));
            int h = Mathf.Max(16, Mathf.RoundToInt(Screen.height * _resScale));
            if (_target != null && w == _w && h == _h) return;

            _w = w;
            _h = h;
            ReleaseTargets();
            _target = NewTarget(w, h);
            _targetA = NewTarget(w, h);
            _targetB = NewTarget(w, h);
        }

        private static RenderTexture NewTarget(int w, int h)
        {
            var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            rt.Create();
            return rt;
        }

        private void ReleaseTargets()
        {
            if (_target != null) { _target.Release(); _target = null; }
            if (_targetA != null) { _targetA.Release(); _targetA = null; }
            if (_targetB != null) { _targetB.Release(); _targetB = null; }
        }

        private void OnDisable()
        {
            ReleaseTargets();
            _coverage.Release();
        }
    }
}
