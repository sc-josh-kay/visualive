using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Visuals
{
    /// <summary>Live-tunable parameters for the smoke-ring pattern (edited on VisualizerCore).</summary>
    [System.Serializable]
    public class SmokePatternParams
    {
        [Header("Flow, drift & fade")]
        [Tooltip("Outward flow speed at low/high energy.")]
        public float FlowMin = 0.006f;
        public float FlowMax = 0.02f;
        [Tooltip("Rotational drift — makes smoke curl/twist as it rises.")]
        public float SwirlAmount = 0.004f;
        [Tooltip("Wavy turbulence that breaks smoke into shapes.")]
        public float WarpAmount = 0.004f;
        public float WarpFreq = 2f;
        public float WarpSpeed = 0.8f;
        [Tooltip("Persistence at low/high energy (higher = longer-lasting). ~0.99 ≈ 4-6 s.")]
        public float FadeMin = 0.989f;
        public float FadeMax = 0.993f;
        [Tooltip("Persistence when the player isn't painting (still / just hit). Low = the existing " +
                 "smoke actively dissipates, so stopping or getting hit visibly clears the world. " +
                 "~0.90 ≈ clears in a fraction of a second.")]
        public float DissipateFade = 0.90f;

        [Header("Emission")]
        [Tooltip("Continuous emission that paints the trail.")]
        public float BaseStrength = 0.03f;
        [Tooltip("Blob/ring radius at low/high bass.")]
        public float BlobRadiusMin = 0.03f;
        public float BlobRadiusMax = 0.07f;
        public float RingWidth = 0.03f;
        [Tooltip("Strength of the ring pop on bass onset / beat.")]
        public float PopStrength = 0.6f;
        [Tooltip("How fast a pop fades (per second).")]
        public float PopDecay = 5f;
        public float TrebleSparkle = 0.4f;

        [Header("Color")]
        public float HueSpeed = 0.3f;
        public float HueDrift = 0.03f;
        public float Saturation = 0.85f;

        [Header("Display")]
        [Tooltip("Kaleidoscope mirror count. 1 = off (pure smoke rings).")]
        public float Segments = 1f;
        public float Brightness = 0.6f;
        [Tooltip("Faint values below this crush to black. Keep low so smoke fades gently.")]
        public float BlackPoint = 0.015f;
    }

    /// <summary>
    /// The primary pattern: music-driven "psychedelic smoke rings" emanating from the player.
    /// A persistent field (RT) that the player continuously paints into; each frame the field is
    /// advected outward from the player and faded (both rates ← energy), a fresh ring/blob is
    /// emitted at the player (pops on bass onset / beat), and the result is shown — optionally
    /// folded into kaleidoscope symmetry (off by default). The field starts empty, so the screen
    /// fills in only where the player paints.
    /// </summary>
    public class KaleidoscopePattern : IVisualizerPattern
    {
        private readonly Material _smoke;
        private readonly Material _display;
        private readonly SmokePatternParams _p;

        private Camera _cam;
        private Transform _player;
        private Material _splatMat;
        private SplatData _splats;
        private VacuumData _vacuums;
        private RenderTexture _a, _b;
        private bool _ping;
        private int _w, _h;
        private float _pop;

        public string Name => "Smoke Rings";

        public KaleidoscopePattern(Shader smokeShader, Shader displayShader, SmokePatternParams p)
        {
            _smoke = new Material(smokeShader);
            _display = new Material(displayShader);
            _p = p;
        }

        public void Configure(VisualizerContext ctx)
        {
            _cam = ctx.Camera;
            _player = ctx.Player;
            _splatMat = ctx.SplatMaterial;
        }

        public void UpdatePattern(MusicState s, float intensity, float paintGain, float dt)
        {
            // Pop envelope from bass onset / beat.
            _pop = Mathf.Max(0f, _pop - _p.PopDecay * dt);
            if (s.BassOnset) _pop = Mathf.Max(_pop, Mathf.Max(0.4f, s.BassOnsetStrength));
            if (s.Beat) _pop = Mathf.Max(_pop, 1f);

            float aspect = _cam != null ? _cam.aspect : 1.777f;
            Vector3 vp = _cam != null && _player != null
                ? _cam.WorldToViewportPoint(_player.position)
                : new Vector3(0.5f, 0.5f, 0f);
            var center = new Vector4(vp.x, vp.y, 0f, 0f);

            float flow = Mathf.Lerp(_p.FlowMin, _p.FlowMax, s.Energy);
            // Persistence follows paint gain: painting (moving) keeps the smoke; not painting (still
            // or just hit) drops toward DissipateFade so the existing field actively clears. This is
            // what makes stillness AND hit-disruption VISIBLE — emission-scaling alone can't, because
            // the field otherwise lingers for seconds.
            float fade = Mathf.Lerp(_p.DissipateFade, Mathf.Lerp(_p.FadeMin, _p.FadeMax, s.Energy), paintGain);
            float blob = Mathf.Lerp(_p.BlobRadiusMin, _p.BlobRadiusMax, s.Bass);
            float hue = Mathf.Repeat(s.SpectralCentroid * 0.5f + Time.time * _p.HueDrift, 1f);
            Color emit = Color.HSVToRGB(hue, _p.Saturation, 1f);

            _smoke.SetVector("_Center", center);
            _smoke.SetFloat("_Aspect", aspect);
            _smoke.SetFloat("_Flow", flow);
            _smoke.SetFloat("_Swirl", _p.SwirlAmount);
            _smoke.SetFloat("_Warp", _p.WarpAmount);
            _smoke.SetFloat("_WarpFreq", _p.WarpFreq);
            _smoke.SetFloat("_WarpSpeed", _p.WarpSpeed);
            _smoke.SetFloat("_Fade", fade);
            _smoke.SetFloat("_Hue", _p.HueSpeed * (0.5f + s.Energy) * dt);
            _smoke.SetColor("_EmitColor", emit);
            _smoke.SetFloat("_BlobRadius", blob);
            _smoke.SetFloat("_RingRadius", blob);
            _smoke.SetFloat("_RingWidth", _p.RingWidth);
            // paintGain (0..1) is the gameplay throttle: it fades as the player stops moving and
            // cuts out briefly when an enemy clips the player, so a still/hit player paints little.
            _smoke.SetFloat("_BaseStrength", _p.BaseStrength * (0.4f + 0.6f * intensity) * paintGain);
            _smoke.SetFloat("_PopStrength", _pop * _p.PopStrength * paintGain);
            _smoke.SetFloat("_Treble", s.Treble * _p.TrebleSparkle);
            _smoke.SetFloat("_Time0", Time.time);

            _display.SetVector("_Center", center);
            _display.SetFloat("_Aspect", aspect);
            _display.SetFloat("_Segments", _p.Segments);
            _display.SetFloat("_Rotation", 0f);
            _display.SetFloat("_Hue", 0f);
            _display.SetFloat("_Brightness", _p.Brightness);
            _display.SetFloat("_BlackPoint", _p.BlackPoint);
        }

        public void InjectSplats(SplatData splats)
        {
            _splats = splats;
        }

        public void InjectVacuums(VacuumData vacuums)
        {
            _vacuums = vacuums;
        }

        public void Render(RenderTexture trailField, RenderTexture target)
        {
            EnsureFields(target.width, target.height);

            // Vacuums (Corruptors) modify the advection, so set them on the smoke material first.
            int vc = _vacuums != null ? _vacuums.Count : 0;
            _smoke.SetFloat("_VacCount", vc);
            if (vc > 0)
            {
                _smoke.SetVectorArray("_Vacuums", _vacuums.Vacuums);
                _smoke.SetFloatArray("_VacSwirl", _vacuums.Swirl);
            }

            RenderTexture src = _ping ? _b : _a;
            RenderTexture dst = _ping ? _a : _b;
            Graphics.Blit(src, dst, _smoke);   // advect + fade + emit

            // Apply gameplay splats (enemy consume / death paint) into the persistent field so they
            // linger and fade like painted content. Blit dst→src (src is now free) so the splatted
            // buffer becomes the latest field; leave dst untouched when there are nothing to apply.
            RenderTexture latest = dst;
            if (_splatMat != null && _splats != null && _splats.Count > 0)
            {
                float aspect = _cam != null ? _cam.aspect : 1.777f;
                _splatMat.SetFloat("_Aspect", aspect);
                _splatMat.SetFloat("_Count", _splats.Count);
                _splatMat.SetFloat("_Time0", Time.time);
                _splatMat.SetVectorArray("_Splats", _splats.Splats);
                _splatMat.SetVectorArray("_SplatColors", _splats.Colors);
                Graphics.Blit(dst, src, _splatMat);
                latest = src;
            }

            // Next frame's source must be the latest field.
            _ping = latest == _b;

            Graphics.Blit(latest, target, _display); // display (+ optional symmetry)
        }

        public void ApplyRipple(RippleData ripples)
        {
            if (ripples == null || ripples.Ripples == null || _display == null) return;
            _display.SetVectorArray("_Ripples", ripples.Ripples);
            _display.SetFloat("_RippleWidth", ripples.Width);
        }

        public void Reset()
        {
            if (_a != null) ClearRT(_a);
            if (_b != null) ClearRT(_b);
        }

        private void EnsureFields(int w, int h)
        {
            if (_a != null && w == _w && h == _h) return;
            _w = w;
            _h = h;
            if (_a != null) _a.Release();
            if (_b != null) _b.Release();
            _a = NewRT(w, h);
            _b = NewRT(w, h);
            ClearRT(_a);
            ClearRT(_b);
        }

        private static RenderTexture NewRT(int w, int h)
        {
            var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            rt.Create();
            return rt;
        }

        private static void ClearRT(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }
    }
}
