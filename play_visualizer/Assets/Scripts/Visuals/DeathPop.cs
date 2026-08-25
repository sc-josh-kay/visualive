using UnityEngine;

namespace PlayVisualizer.Visuals
{
    /// <summary>
    /// Minimal, dependency-free destruction feedback: a sprite that briefly expands and fades,
    /// then removes itself. A placeholder for the richer particle effects coming in Phase 7.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DeathPop : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.25f;
        [SerializeField] private float _endScaleMultiplier = 2.5f;

        private SpriteRenderer _sprite;
        private Vector3 _startScale;
        private Color _startColor;
        private float _t;
        private bool _playing;

        private void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
        }

        /// <summary>Start the pop, tinted to match whatever was destroyed.</summary>
        public void Play(Color color)
        {
            _startColor = color;
            _startColor.a = 1f;
            _sprite.color = _startColor;
            _startScale = transform.localScale;
            _t = 0f;
            _playing = true;
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            _t += Time.deltaTime;
            float u = _duration > 0f ? _t / _duration : 1f;
            if (u >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.localScale = Vector3.Lerp(_startScale, _startScale * _endScaleMultiplier, u);
            Color c = _startColor;
            c.a = Mathf.Lerp(1f, 0f, u);
            _sprite.color = c;
        }
    }
}
