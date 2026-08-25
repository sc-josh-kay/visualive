using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Visuals
{
    /// <summary>
    /// Drives the audio-reactive visuals from MusicState: background pulse (bass), bloom
    /// (energy), particle intensity (treble), and a shockwave on each beat. The visual
    /// counterpart to the gameplay MusicMapper — it reads MusicState and never runs its own
    /// analysis. Reacts continuously, independent of the gameplay music toggle.
    /// </summary>
    public class VisualizerController : MonoBehaviour
    {
        [SerializeField] private AudioAnalyzer _analyzer;
        [SerializeField] private VisualizerConfig _config;

        [Header("Scene refs")]
        [SerializeField] private Transform _background;
        [SerializeField] private ParticleSystem _trebleParticles;
        [SerializeField] private DeathPop _shockwavePrefab;
        [SerializeField] private Transform _player;
        [SerializeField] private Volume _volume;

        [Header("Bass-synced pulse")]
        [Tooltip("Legacy shockwave rings. Superseded by PlayerCircle in Phase G; off by default now.")]
        [SerializeField] private bool _spawnShockwave = false;
        [Tooltip("Bass level (0..1) the pulse ring fires at, on the rising edge.")]
        [SerializeField] private float _bassHitThreshold = 0.55f;
        [Tooltip("Minimum seconds between pulse rings.")]
        [SerializeField] private float _bassHitRefractory = 0.14f;

        private Bloom _bloom;
        private ParticleSystem.EmissionModule _emission;
        private bool _hasEmission;
        private float _prevBass;
        private float _lastBassHit = -10f;

        private void Awake()
        {
            if (_analyzer == null) _analyzer = FindFirstObjectByType<AudioAnalyzer>();
            if (_volume == null) _volume = FindFirstObjectByType<Volume>();
            if (_player == null)
            {
                GameObject p = GameObject.Find("Player");
                if (p != null) _player = p.transform;
            }

            // .profile (not .sharedProfile) gives a runtime clone, so we don't dirty the asset.
            if (_volume != null && _volume.profile != null)
            {
                _volume.profile.TryGet(out _bloom);
            }

            if (_trebleParticles != null)
            {
                _emission = _trebleParticles.emission;
                _hasEmission = true;
            }
        }

        private void Update()
        {
            if (_analyzer == null || _config == null)
            {
                return;
            }

            MusicState s = _analyzer.State;

            // Bass → background pulse
            if (_background != null)
            {
                float scale = _config.BackgroundBaseScale + s.Bass * _config.BackgroundPulseAmount;
                _background.localScale = new Vector3(scale, scale, 1f);
            }

            // Energy → bloom intensity
            if (_bloom != null)
            {
                _bloom.intensity.value = Mathf.Lerp(_config.BloomBase, _config.BloomMax, s.Energy);
            }

            // Treble → particle emission rate
            if (_hasEmission)
            {
                _emission.rateOverTime =
                    Mathf.Lerp(_config.ParticleRateMin, _config.ParticleRateMax, s.Treble);
            }

            // Bass hit → pulse ring at the ship. Fire on the rising edge of bass past a
            // threshold (the kick), with a short refractory, so it locks to the bassline
            // rather than the analyzer's generic beat flag.
            if (_spawnShockwave && _shockwavePrefab != null &&
                s.Bass >= _bassHitThreshold && _prevBass < _bassHitThreshold &&
                Time.time - _lastBassHit >= _bassHitRefractory)
            {
                Vector3 origin = _player != null ? _player.position : Vector3.zero;
                DeathPop shockwave = Instantiate(_shockwavePrefab, origin, Quaternion.identity);
                shockwave.Play(_config.ShockwaveColor);
                _lastBassHit = Time.time;
            }
            _prevBass = s.Bass;
        }
    }
}
