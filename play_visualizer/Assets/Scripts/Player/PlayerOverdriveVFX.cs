using UnityEngine;
using PlayVisualizer.Audio;
using PlayVisualizer.Gameplay;

namespace PlayVisualizer.Player
{
    /// <summary>
    /// The player's unmistakable Overdrive visual state (spec9 §11-12): a soft glowing halo around the
    /// ship that brightens with bass and PULSES on each paint-wave emission, PLUS the ship itself
    /// cycling through a neon rainbow (Mario star-power style) so the charged state is unmistakable.
    /// Additive glow behind the ship — it never obscures the silhouette. Both ease out of Overdrive
    /// and the ship's original color is restored.
    ///
    /// Drives a child glow SpriteRenderer (additive SoftGlow) + tints the ship SpriteRenderer; no new
    /// shader. Cosmetic only.
    /// </summary>
    public class PlayerOverdriveVFX : MonoBehaviour
    {
        [SerializeField] private OverdriveConfig _config;
        [Tooltip("Child SpriteRenderer for the additive glow halo. Assigned by the Phase 9 setup.")]
        [SerializeField] private SpriteRenderer _glow;
        [Tooltip("The player ship's SpriteRenderer — tinted to a neon rainbow during Overdrive. " +
                 "Assigned by the Phase 9 setup.")]
        [SerializeField] private SpriteRenderer _shipRenderer;

        private float _pulse;    // beat/wave pulse, decays
        private float _envelope; // eased 0..1 presence of the glow (in/out)
        private Color _originalShipColor = Color.white;
        private bool _shipTinted;

        private void OnEnable() => PaintWaveSystem.WaveEmitted += OnWaveEmitted;

        private void OnDisable()
        {
            PaintWaveSystem.WaveEmitted -= OnWaveEmitted;
            RestoreShipColor(); // don't leave the ship stuck rainbow if disabled mid-Overdrive
        }

        private void OnWaveEmitted() => _pulse = 1f;

        private void Awake()
        {
            if (_glow != null) _glow.enabled = false;
            // Fallback: the ship SpriteRenderer lives on this same (Player root) object; the glow is a
            // child, so GetComponent here returns the ship, not the glow. Lets it work without a
            // Phase 9 re-run if the reference wasn't assigned in the editor.
            if (_shipRenderer == null) _shipRenderer = GetComponent<SpriteRenderer>();
            if (_shipRenderer != null) _originalShipColor = _shipRenderer.color;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_config == null || dt <= 0f) return;

            VisualizerMomentum momentum = VisualizerMomentum.Instance;
            bool overdrive = momentum != null && momentum.IsOverdrive;

            // Ease presence in/out; also fold in a brief charge as momentum nears full (anticipation).
            float target = overdrive ? 1f : 0f;
            _envelope = Mathf.MoveTowards(_envelope, target, dt * 3f);
            _pulse = Mathf.Max(0f, _pulse - _config.GlowPulseDecay * dt);

            UpdateShipTint();

            if (_glow == null) return;
            if (_envelope <= 0.001f && !overdrive)
            {
                if (_glow.enabled) _glow.enabled = false;
                return;
            }
            if (!_glow.enabled) _glow.enabled = true;

            MusicState s = AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.State : null;
            float bass = s != null ? Mathf.Clamp01(s.Bass) : 0f;

            // Brightness: base × bass boost + pulse, all gated by the ease envelope.
            float bright = _config.GlowBrightness * (1f + bass * _config.GlowBassBoost) + _pulse * 0.6f;
            float alpha = Mathf.Clamp01(bright) * _envelope;

            // Energetic tint from the current musical moment (cohesive with the paint waves), kept
            // bright so it reads as "charged" rather than a flat color.
            float hue = s != null ? Mathf.Repeat(s.SpectralCentroid, 1f) : 0.5f;
            Color c = Color.HSVToRGB(hue, 0.5f, 1f);
            c.a = alpha;
            _glow.color = c;

            // Scale: bass swell + pulse punch, so the halo breathes with the music and pops on waves.
            float scale = _config.GlowScale * (1f + 0.15f * bass + 0.35f * _pulse) * Mathf.Lerp(0.6f, 1f, _envelope);
            _glow.transform.localScale = new Vector3(scale, scale, 1f);
            // Slow rotation for a living, non-static halo.
            _glow.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 40f);
        }

        // Neon rainbow ship tint (Mario star-power): cycle hue while charged, eased by the envelope so
        // it fades into and out of the ship's normal color. Restored once the envelope reaches 0.
        private void UpdateShipTint()
        {
            if (_shipRenderer == null) return;

            if (_envelope > 0.001f)
            {
                float hue = Mathf.Repeat(Time.time * _config.ShipRainbowSpeed, 1f);
                Color rainbow = Color.HSVToRGB(hue, 0.9f, 1f);
                rainbow.a = _originalShipColor.a; // keep the ship's own opacity
                _shipRenderer.color = Color.Lerp(_originalShipColor, rainbow, _envelope);
                _shipTinted = true;
            }
            else if (_shipTinted)
            {
                RestoreShipColor();
            }
        }

        private void RestoreShipColor()
        {
            if (_shipTinted && _shipRenderer != null) _shipRenderer.color = _originalShipColor;
            _shipTinted = false;
        }
    }
}
