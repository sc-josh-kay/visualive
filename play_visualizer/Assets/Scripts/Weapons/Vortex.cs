using System.Collections.Generic;
using UnityEngine;
using PlayVisualizer.Audio;
using PlayVisualizer.Enemies;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Weapons
{
    /// <summary>Launch parameters for a <see cref="Vortex"/>.</summary>
    public struct VortexParams
    {
        public float TravelSpeed, TravelTime;
        public float Radius, PullStrength, SlowFactor;
        public int DamagePerTick;
        public float DamageInterval;
        public float Lifetime, PulseInterval;
    }

    /// <summary>
    /// The Vortex Wave's effect (spec7 §8). Phase 1: a slow wave drifts out along the aim. Phase 2: it
    /// anchors as a vortex that PULLS + slows + damages nearby enemies (enemies sample the live
    /// <see cref="Active"/> registry) and DISTORTS the visualizer with rotating swirl + ripple pulses
    /// — energy, never coverage. It reacts to the music (Bass → radius, Energy → intensity, Beat →
    /// expansion) and disappears after its lifetime.
    /// </summary>
    public class Vortex : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _swirl; // child quad with the VortexSwirl material

        /// <summary>Live vortices currently pulling — sampled by enemies for the pull force.</summary>
        public static readonly List<Vortex> Active = new List<Vortex>();

        private VortexParams _p;
        private Vector2 _travelDir;
        private Color _color;
        private AudioAnalyzer _analyzer;
        private Material _mat;

        private bool _pulling;
        private float _t;
        private float _damageTimer;
        private float _pulseTimer;

        public Vector2 Center => transform.position;
        public bool IsPulling => _pulling;
        public float CurrentRadius { get; private set; }
        public float PullStrength => _p.PullStrength;
        public float SlowFactor => _p.SlowFactor;

        public void Init(Vector2 dir, VortexParams p, Color color)
        {
            _travelDir = dir.normalized;
            _p = p;
            _color = color;
            CurrentRadius = p.Radius;
        }

        private void Awake()
        {
            _analyzer = FindFirstObjectByType<AudioAnalyzer>();
            if (_swirl != null) _mat = _swirl.material;
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            MusicState s = _analyzer != null ? _analyzer.State : null;

            if (!_pulling)
            {
                transform.position += (Vector3)(_travelDir * (_p.TravelSpeed * dt));
                _t += dt;
                if (_swirl != null) _swirl.transform.localScale = Vector3.one * 1.2f; // small traveling wave
                if (_mat != null)
                {
                    _mat.SetColor("_Color", _color);
                    _mat.SetFloat("_Progress", 0.5f); // mid-life so the wave is visible while it travels
                    _mat.SetFloat("_Strength", 0.6f);
                    _mat.SetFloat("_Time0", Time.time);
                }
                if (_t >= _p.TravelTime)
                {
                    _pulling = true;
                    _t = 0f;
                    Active.Add(this);
                }
                return;
            }

            _t += dt;
            if (_t >= _p.Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float bass = s != null ? s.Bass : 0f;
            float energy = s != null ? s.Energy : 0f;
            bool beat = s != null && s.Beat;

            CurrentRadius = _p.Radius * (0.8f + 0.4f * bass);

            // Damage enemies inside on a tick.
            _damageTimer -= dt;
            if (_damageTimer <= 0f)
            {
                _damageTimer = _p.DamageInterval;
                EnemyBase.ApplyRadialDamage(Center, CurrentRadius, _p.DamagePerTick);
            }

            // Distort the visualizer: periodic ripple pulses, stronger on the beat (energy, no coverage).
            _pulseTimer -= dt;
            if (_pulseTimer <= 0f || beat)
            {
                _pulseTimer = _p.PulseInterval;
                if (VisualizerField.Instance != null)
                {
                    VisualizerField.Instance.Distort(Center, (0.6f + energy) * (beat ? 1.6f : 1f));
                }
            }

            // Rotating swirl overlay, scaled to the current radius and pulsing with the music.
            if (_swirl != null) _swirl.transform.localScale = new Vector3(CurrentRadius * 2f, CurrentRadius * 2f, 1f);
            if (_mat != null)
            {
                _mat.SetColor("_Color", _color);
                _mat.SetFloat("_Progress", Mathf.Clamp01(_t / _p.Lifetime));
                _mat.SetFloat("_Strength", 0.6f + 0.8f * energy + (beat ? 0.5f : 0f));
                _mat.SetFloat("_Time0", Time.time);
            }
        }
    }
}
