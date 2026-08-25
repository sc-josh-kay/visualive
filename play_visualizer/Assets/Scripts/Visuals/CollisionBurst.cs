using UnityEngine;

namespace PlayVisualizer.Visuals
{
    /// <summary>
    /// The transient effect when an enemy reaches the player: an enemy-colored radial spark burst
    /// (CollisionBurst shader) that expands its outline, while the smoke inside that outline is
    /// scooped to black via VisualizerField.Consume. The sparks are a pure overlay — they never
    /// paint the field — so once the burst fades, only the black hole remains ("only blackness left
    /// inside the outline"). A self-contained, short-lived object, spawned by the enemy on impact.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class CollisionBurst : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private Material _mat;
        private Vector2 _center;
        private float _radius;
        private float _duration;
        private float _consumeStrength;
        private float _t;
        private bool _playing;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// Start the burst at this object's position, tinted to <paramref name="color"/>, expanding
        /// to <paramref name="worldRadius"/> over <paramref name="duration"/> seconds and eating the
        /// smoke inside to black at <paramref name="consumeStrength"/>.
        /// </summary>
        public void Play(Color color, float worldRadius, float duration, float consumeStrength)
        {
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            _mat = _renderer.material; // instance, so per-burst color/progress don't clash

            _center = transform.position;
            _radius = Mathf.Max(0.05f, worldRadius);
            _duration = Mathf.Max(0.05f, duration);
            _consumeStrength = Mathf.Max(0f, consumeStrength);

            // Quad spans the full diameter so the shader's centered UVs map to the world radius.
            transform.localScale = new Vector3(_radius * 2f, _radius * 2f, 1f);

            color.a = 1f;
            _mat.SetColor("_Color", color);
            _mat.SetFloat("_Progress", 0f);

            // Per-burst randomness so no two explosions are identical (alive / firework feel).
            _mat.SetFloat("_Seed", Random.value * 97f);
            _mat.SetFloat("_Sparks", Random.Range(16f, 28f));
            _mat.SetFloat("_Curl", Random.Range(0.5f, 1.3f));

            _t = 0f;
            _playing = true;
        }

        private void Update()
        {
            if (!_playing) return;

            float dt = Time.deltaTime;
            _t += dt;
            float p = Mathf.Clamp01(_t / _duration);

            if (_mat != null) _mat.SetFloat("_Progress", p);

            // Grow the black hole in step with the expanding outline. Repeated soft consumes over the
            // burst blacken the interior; the growing radius leaves a softer edge.
            if (VisualizerField.Instance != null)
            {
                float r = _radius * Mathf.Clamp01(p * 1.1f);
                float strength = _consumeStrength * (dt / _duration) * 5f;
                VisualizerField.Instance.Consume(_center, Mathf.Max(0.05f, r), strength);
            }

            if (_t >= _duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
