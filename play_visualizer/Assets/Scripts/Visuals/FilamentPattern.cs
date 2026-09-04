using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Visuals
{
    /// <summary>Live-tunable parameters for the filament pattern.</summary>
    [System.Serializable]
    public class FilamentPatternParams
    {
        public float Rays = 14f;
        public float LineFreq = 26f;
        public float Sharpness = 6f;
        public float Speed = 1.5f;
        public float Saturation = 0.8f;
        public float Vignette = 1.3f;
        public float Brightness = 1.0f;
    }

    /// <summary>
    /// Glowing filament / ray field emanating from the player, driven by treble and overall energy
    /// and gated by visual intensity so it stays sparse when the music is calm. Procedurally
    /// generated each frame (no persistent field) — a deliberately different personality from the
    /// field-based patterns.
    /// </summary>
    public class FilamentPattern : IVisualizerPattern
    {
        private readonly Material _mat;
        private readonly FilamentPatternParams _p;
        private Camera _cam;
        private Transform _player;

        public string Name => "Filament";

        public FilamentPattern(Shader shader, FilamentPatternParams p)
        {
            _mat = new Material(shader);
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

            _mat.SetVector("_Center", new Vector4(vp.x, vp.y, 0f, 0f));
            _mat.SetFloat("_Aspect", aspect);
            _mat.SetFloat("_Time0", Time.time);
            _mat.SetFloat("_Treble", s.Treble);
            _mat.SetFloat("_Energy", s.Energy);
            _mat.SetFloat("_Centroid", s.SpectralCentroid);
            _mat.SetFloat("_Intensity", intensity);
            _mat.SetFloat("_Rays", _p.Rays);
            _mat.SetFloat("_LineFreq", _p.LineFreq);
            _mat.SetFloat("_Sharp", _p.Sharpness);
            _mat.SetFloat("_Speed", _p.Speed);
            _mat.SetFloat("_Sat", _p.Saturation);
            _mat.SetFloat("_Vignette", _p.Vignette);
            _mat.SetFloat("_Brightness", _p.Brightness);
        }

        public void Render(RenderTexture trailField, RenderTexture target)
        {
            Graphics.Blit(Texture2D.blackTexture, target, _mat);
        }

        public void InjectSplats(SplatData splats) { }

        public void InjectVacuums(VacuumData vacuums) { }

        public void InjectTurbulence(TurbulenceData turbulence) { }

        public void ApplyRipple(RippleData ripples) { }

        public void Reset() { }
    }
}
