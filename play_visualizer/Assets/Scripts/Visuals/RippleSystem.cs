using UnityEngine;

namespace PlayVisualizer.Visuals
{
    /// <summary>Live-tunable ripple parameters (edited on VisualizerCore).</summary>
    [System.Serializable]
    public class RippleParams
    {
        [Tooltip("Outward travel speed (viewport units/sec).")]
        public float Speed = 0.5f;
        [Tooltip("Ring thickness (viewport units).")]
        public float Width = 0.06f;
        [Tooltip("Seconds a ripple lives.")]
        public float Lifetime = 1.4f;
        [Tooltip("Max UV displacement per unit strength.")]
        public float StrengthScale = 0.04f;
        [Tooltip("Minimum seconds between spawns.")]
        public float Refractory = 0.07f;
        public bool SpawnOnBassOnset = true;
        public bool SpawnOnBeat = true;
    }

    /// <summary>
    /// A small pool of outward-travelling radial ripples that distort the visualizer (never the
    /// player or gameplay). Triggered by bass onset / beat; each ripple grows a ring of radial
    /// displacement that fades over its lifetime. Packed into shader uniforms for the pattern.
    /// </summary>
    public class RippleSystem
    {
        public const int Max = 8;

        private struct Ripple
        {
            public Vector2 Origin;
            public float Age;
            public float Strength0;
            public bool Active;
        }

        private readonly Ripple[] _ripples = new Ripple[Max];
        private readonly Vector4[] _packed = new Vector4[Max];
        private readonly RippleData _data = new RippleData();
        private RippleParams _p = new RippleParams();
        private double _lastSpawn = -10.0;

        public void Configure(RippleParams p)
        {
            _p = p;
        }

        public void Spawn(Vector2 originUV, float strength, double dspTime)
        {
            if (dspTime - _lastSpawn < _p.Refractory) return;
            int slot = FreeSlot();
            _ripples[slot] = new Ripple { Origin = originUV, Age = 0f, Strength0 = strength, Active = true };
            _lastSpawn = dspTime;
        }

        public void Update(float dt)
        {
            for (int i = 0; i < Max; i++)
            {
                if (!_ripples[i].Active) continue;
                _ripples[i].Age += dt;
                if (_ripples[i].Age > _p.Lifetime) _ripples[i].Active = false;
            }
        }

        public RippleData Pack()
        {
            for (int i = 0; i < Max; i++)
            {
                if (_ripples[i].Active)
                {
                    float radius = _ripples[i].Age * _p.Speed;
                    float fade = 1f - _ripples[i].Age / _p.Lifetime;
                    float strength = _ripples[i].Strength0 * _p.StrengthScale * Mathf.Clamp01(fade);
                    _packed[i] = new Vector4(_ripples[i].Origin.x, _ripples[i].Origin.y, radius, strength);
                }
                else
                {
                    _packed[i] = Vector4.zero;
                }
            }
            _data.Ripples = _packed;
            _data.Width = _p.Width;
            return _data;
        }

        private int FreeSlot()
        {
            for (int i = 0; i < Max; i++)
            {
                if (!_ripples[i].Active) return i;
            }
            // Reuse the oldest.
            int oldest = 0;
            float maxAge = -1f;
            for (int i = 0; i < Max; i++)
            {
                if (_ripples[i].Age > maxAge) { maxAge = _ripples[i].Age; oldest = i; }
            }
            return oldest;
        }
    }
}
