using UnityEngine;
using PlayVisualizer.Gameplay;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Player
{
    /// <summary>
    /// Turns player behaviour into how strongly the player paints the world, via the single
    /// <see cref="VisualizerField.PlayerPaintGain"/> channel (0..1). Two incentives, one gain:
    ///
    ///   * MOVEMENT-GATED: gain scales with speed, fading toward a small floor when still — so
    ///     standing put dims the world (and your score) but never fully kills it. Keep flying to
    ///     keep it glowing.
    ///   * HIT-DISRUPTION: an enemy clipping the player cuts the gain to ~0 and eases it back —
    ///     so contact briefly stops your brush. This is the *only* consequence of enemy contact
    ///     (no damage, no death, spec §15); it gives enemies avoid-pressure without a losing state.
    ///
    /// Gameplay decides the gain here; the visualizer just reads it. This class never touches
    /// shaders or RenderTextures.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerTrail : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;

        private Rigidbody2D _rb;
        private float _disrupt; // 1 right after a hit, decays to 0 over the recovery window

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        /// <summary>Called when an enemy clips the player: cut the trail, then recover.</summary>
        public void Disrupt()
        {
            _disrupt = 1f;
        }

        private void Update()
        {
            float floor = _config != null ? _config.TrailStillFloor : 0.15f;
            float fullFrac = _config != null ? Mathf.Max(0.05f, _config.TrailFullSpeedFraction) : 0.5f;
            float moveSpeed = _config != null ? _config.MoveSpeed : 8f;
            float recovery = _config != null ? Mathf.Max(0.01f, _config.HitDisruptRecovery) : 0.6f;

            // Movement gate: 0 speed → floor, (fullFrac * MoveSpeed) → 1.
            float speed = _rb != null ? _rb.linearVelocity.magnitude : 0f;
            float speedT = Mathf.Clamp01(speed / Mathf.Max(0.01f, moveSpeed * fullFrac));
            float moveGain = Mathf.Lerp(floor, 1f, speedT);

            // Hit disruption: decay the cut, ease the recovery so it feels like the brush catching.
            _disrupt = Mathf.Max(0f, _disrupt - Time.deltaTime / recovery);
            float disruptMul = Mathf.SmoothStep(0f, 1f, 1f - _disrupt);

            float gain = moveGain * disruptMul;

            // During Overdrive the movement trail steps back so it doesn't blow out on top of the
            // radial emission — it becomes the secondary paint source (spec9 §5).
            VisualizerMomentum momentum = VisualizerMomentum.Instance;
            if (momentum != null) gain *= momentum.TrailPaintScale;

            if (VisualizerField.Instance != null)
            {
                VisualizerField.Instance.PlayerPaintGain = gain;
            }
        }
    }
}
