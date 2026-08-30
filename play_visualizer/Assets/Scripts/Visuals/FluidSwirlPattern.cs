using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Visuals
{
    /// <summary>Live-tunable parameters for the fluid-swirl pattern.</summary>
    [System.Serializable]
    public class FluidPatternParams
    {
        [Header("Swirl / warp (mid & flux driven)")]
        public float SwirlMin = 0.001f;
        public float SwirlMax = 0.008f;
        public float WarpMin = 0.001f;
        public float WarpMax = 0.006f;
        public float WarpFreq = 2f;
        public float WarpSpeed = 1f;

        [Header("Fade / emission")]
        public float FadeMin = 0.93f;
        public float FadeMax = 0.98f;
        public float BaseStrength = 0.1f;
        public float BlobRadius = 0.05f;

        [Header("Color / display")]
        public float HueSpeed = 0.25f;
        public float HueDrift = 0.02f;
        public float Saturation = 0.8f;
        public float Segments = 1f;
        public float Brightness = 0.6f;
    }

    /// <summary>
    /// Organic, flowing pattern: a persistent field the player paints into, advected along a
    /// swirling vector field (tangential rotation + wavy warp). Driven by mid/flux (swirl and warp
    /// amount) with treble adding emission detail. Shares the trail/paint model with the smoke
    /// pattern but reads different features and moves very differently.
    /// </summary>
    public class FluidSwirlPattern : IVisualizerPattern
    {
        private readonly Material _fluid;
        private readonly Material _display;
        private readonly FluidPatternParams _p;

        private Camera _cam;
        private Transform _player;
        private RenderTexture _a, _b;
        private bool _ping;
        private int _w, _h;

        public string Name => "Fluid Swirl";

        public FluidSwirlPattern(Shader fluidShader, Shader displayShader, FluidPatternParams p)
        {
            _fluid = new Material(fluidShader);
            _display = new Material(displayShader);
            _p = p;
        }

        public void Configure(VisualizerContext ctx)
        {
            _cam = ctx.Camera;
            _player = ctx.Player;
        }

        public void UpdatePattern(MusicState s, float intensity, float paintGain, float dt)
        {
            float aspect = _cam != null ? _cam.aspect : 1.777f;
            Vector3 vp = _cam != null && _player != null
                ? _cam.WorldToViewportPoint(_player.position)
                : new Vector3(0.5f, 0.5f, 0f);
            var center = new Vector4(vp.x, vp.y, 0f, 0f);

            float swirl = Mathf.Lerp(_p.SwirlMin, _p.SwirlMax, s.Mid);
            float warp = Mathf.Lerp(_p.WarpMin, _p.WarpMax, Mathf.Max(s.Mid, s.SpectralFlux));
            float fade = Mathf.Lerp(_p.FadeMin, _p.FadeMax, s.Energy);
            float hue = Mathf.Repeat(s.SpectralCentroid * 0.5f + Time.time * _p.HueDrift, 1f);
            Color emit = Color.HSVToRGB(hue, _p.Saturation, 1f);

            _fluid.SetVector("_Center", center);
            _fluid.SetFloat("_Aspect", aspect);
            _fluid.SetFloat("_Swirl", swirl);
            _fluid.SetFloat("_Warp", warp);
            _fluid.SetFloat("_Freq", _p.WarpFreq);
            _fluid.SetFloat("_Speed", _p.WarpSpeed);
            _fluid.SetFloat("_Fade", fade);
            _fluid.SetFloat("_Hue", _p.HueSpeed * (0.5f + s.Energy) * dt);
            _fluid.SetColor("_EmitColor", emit);
            _fluid.SetFloat("_BlobRadius", _p.BlobRadius);
            _fluid.SetFloat("_BaseStrength", _p.BaseStrength * (0.2f + 0.8f * intensity) * paintGain);
            _fluid.SetFloat("_Treble", s.Treble);
            _fluid.SetFloat("_Time0", Time.time);

            _display.SetVector("_Center", center);
            _display.SetFloat("_Aspect", aspect);
            _display.SetFloat("_Segments", _p.Segments);
            _display.SetFloat("_Rotation", 0f);
            _display.SetFloat("_Hue", 0f);
            _display.SetFloat("_Brightness", _p.Brightness);
        }

        public void Render(RenderTexture trailField, RenderTexture target)
        {
            EnsureFields(target.width, target.height);
            RenderTexture src = _ping ? _b : _a;
            RenderTexture dst = _ping ? _a : _b;
            Graphics.Blit(src, dst, _fluid);
            _ping = !_ping;
            Graphics.Blit(dst, target, _display);
        }

        public void InjectSplats(SplatData splats) { }

        public void InjectVacuums(VacuumData vacuums) { }

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
            _w = w; _h = h;
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
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
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
