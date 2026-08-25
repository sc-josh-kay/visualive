using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Player
{
    /// <summary>
    /// The "alive" look for a projectile (spec7 organic pass). Lives on a VISUAL CHILD of the
    /// projectile (the physics root flies straight), so none of this affects trajectory, hits, or
    /// fire cadence — it's purely cosmetic:
    ///
    ///   * WOBBLE — a small per-shot lateral sine offset perpendicular to travel, so each shot's tail
    ///     bends a little differently (bounded + smooth; never reads as the shot curving off-target).
    ///   * PULSE — the head breathes subtly with Bass (and a tiny Beat blip), Energy lifts brightness.
    ///   * SHIMMER — a faint Treble-driven hue drift so the color feels alive, not flat.
    ///
    /// The wobble is deliberately independent of the music (per-shot randomness), so the two kinds of
    /// life don't compound into jitter.
    /// </summary>
    public class ProjectileVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _core;
        [SerializeField] private SpriteRenderer _halo;
        [SerializeField] private TrailRenderer _trail;

        [Header("Wobble (per-shot, cosmetic)")]
        [SerializeField] private float _amplitude = 0.08f;
        [SerializeField] private float _freqMin = 5f;
        [SerializeField] private float _freqMax = 10f;

        [Header("Music pulse (subtle)")]
        [SerializeField] private float _bassPulse = 0.18f;
        [SerializeField] private float _beatPulse = 0.12f;
        [SerializeField] private float _energyBrightness = 0.35f;
        [Tooltip("Treble → hue drift amount (kept small).")]
        [SerializeField] private float _hueShimmer = 0.05f;
        [SerializeField] private float _shimmerSpeed = 6f;
        [Tooltip("Halo brightness relative to the core.")]
        [SerializeField] private float _haloDim = 0.55f;

        private Rigidbody2D _rootBody;
        private float _phase, _freq, _amp;
        private float _baseHue, _baseSat, _baseVal;
        private Vector2 _lastDir = Vector2.right;

        /// <summary>Called by the projectile at launch: physics root (for travel direction) + color.</summary>
        public void Configure(Rigidbody2D rootBody, Color color)
        {
            _rootBody = rootBody;

            // Per-shot wobble seed — fixed for this shot, different each shot.
            _phase = Random.value * Mathf.PI * 2f;
            _freq = Random.Range(_freqMin, _freqMax);
            _amp = _amplitude * Random.Range(0.7f, 1.2f);

            Color.RGBToHSV(color, out _baseHue, out _baseSat, out _baseVal);
            ApplyColor(color);

            if (_trail != null) _trail.Clear();
        }

        private void LateUpdate()
        {
            MusicState s = AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.State : null;
            float bass = s != null ? s.Bass : 0f;
            float energy = s != null ? s.Energy : 0f;
            float treble = s != null ? s.Treble : 0f;
            bool beat = s != null && s.Beat;

            // Wobble perpendicular to the current travel direction (local = world; root isn't rotated).
            if (_rootBody != null && _rootBody.linearVelocity.sqrMagnitude > 0.0001f)
            {
                _lastDir = _rootBody.linearVelocity.normalized;
            }
            Vector2 perp = new Vector2(-_lastDir.y, _lastDir.x);
            float offset = _amp * Mathf.Sin(Time.time * _freq + _phase);
            transform.localPosition = (Vector3)(perp * offset);

            // Subtle head pulse (bass + tiny beat blip).
            float pulse = 1f + bass * _bassPulse + (beat ? _beatPulse : 0f);
            transform.localScale = Vector3.one * pulse;

            // Brightness (energy) + faint hue shimmer (treble).
            float hue = Mathf.Repeat(_baseHue + _hueShimmer * treble * Mathf.Sin(Time.time * _shimmerSpeed + _phase), 1f);
            float val = Mathf.Clamp01(_baseVal * (1f + energy * _energyBrightness));
            Color c = Color.HSVToRGB(hue, _baseSat, val);
            ApplyColor(c);
        }

        private void ApplyColor(Color c)
        {
            if (_core != null) _core.color = c;
            if (_halo != null) _halo.color = new Color(c.r, c.g, c.b, _haloDim);
            if (_trail != null)
            {
                _trail.startColor = c;
                _trail.endColor = new Color(c.r, c.g, c.b, 0f);
            }
        }
    }
}
