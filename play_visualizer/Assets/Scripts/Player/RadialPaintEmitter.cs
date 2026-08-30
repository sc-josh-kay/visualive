using UnityEngine;
using PlayVisualizer.Audio;
using PlayVisualizer.Gameplay;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Player
{
    /// <summary>
    /// During Overdrive (spec9 §5) the player becomes a music-powered paint emitter: a modest paint
    /// disc is deposited AROUND the player every frame — omnidirectional fill that works even while
    /// standing still, which is the readable difference from the movement-gated trail. Deliberately a
    /// distinct mechanism (an explicit VisualizerField.Paint), NOT a boosted PlayerPaintGain (§4).
    /// Bass scales how much paint it pushes into the world (§7). Inactive outside Overdrive.
    /// </summary>
    public class RadialPaintEmitter : MonoBehaviour
    {
        [SerializeField] private OverdriveConfig _config;

        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _config == null) return;

            VisualizerMomentum momentum = VisualizerMomentum.Instance;
            if (momentum == null || !momentum.IsOverdrive) return;

            VisualizerField field = VisualizerField.Instance;
            if (field == null) return;

            MusicState s = AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.State : null;
            float bass = s != null ? Mathf.Clamp01(s.Bass) : 0f;
            float bassBoost = 1f + bass * _config.RadialBassBoost;

            // Fade the deposit as the ship speeds up: a moving ship lays fresh discs across a wide
            // swath, so full intensity everywhere blows out. Standing still → full; at/above the
            // reference speed → reduced by RadialSpeedFalloff (spec9 §5). 0 falloff = constant.
            float speedFactor = 1f;
            if (_config.RadialSpeedFalloff > 0f && _rb != null && _config.RadialSpeedReference > 0.01f)
            {
                float speedT = Mathf.Clamp01(_rb.linearVelocity.magnitude / _config.RadialSpeedReference);
                speedFactor = Mathf.Lerp(1f, 1f - _config.RadialSpeedFalloff, speedT);
            }

            float radius = _config.RadialRadius * bassBoost;
            // Intensity is a per-frame rate, so scale by dt for frame-rate independence (the config
            // value is tuned as "intensity per second" of continuous emission).
            float intensity = _config.RadialIntensity * bassBoost * speedFactor * dt;

            field.Paint(transform.position, radius, intensity, s);
        }
    }
}
