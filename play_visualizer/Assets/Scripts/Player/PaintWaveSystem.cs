using UnityEngine;
using PlayVisualizer.Audio;
using PlayVisualizer.Gameplay;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Player
{
    /// <summary>
    /// The discrete half of Overdrive painting (spec9 §6-8): on each qualifying beat/onset, spawn an
    /// expanding ring of paint centered on the player. The wavefront deposits paint into the visualizer
    /// field as it grows (via VisualizerField.PaintRing), then fades as it reaches its max radius; the
    /// deposited paint persists and advects like any painted content. Bass controls wave size and
    /// strength (§7); beat/onset controls timing (§8).
    ///
    /// Waves are a fixed POOL of value-type structs — no per-wave GameObjects, no per-frame allocation
    /// (§17). Multiple waves overlap. Inactive outside Overdrive.
    /// </summary>
    public class PaintWaveSystem : MonoBehaviour
    {
        private const int Pool = 6; // hard cap; config.MaxWaves clamps the active count within this.

        [SerializeField] private OverdriveConfig _config;

        /// <summary>Fired when a new wave is emitted, so the player VFX can pulse in sync (§12).</summary>
        public static event System.Action WaveEmitted;

        private struct Wave
        {
            public bool Active;
            public Vector2 Center;
            public float Radius;     // current expanding radius
            public float MaxRadius;
            public float Intensity;  // base per-second deposit rate
        }

        private readonly Wave[] _waves = new Wave[Pool];
        private float _lastSpawnTime = -999f;

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _config == null) return;

            VisualizerField field = VisualizerField.Instance;
            if (field == null) return;

            VisualizerMomentum momentum = VisualizerMomentum.Instance;
            bool overdrive = momentum != null && momentum.IsOverdrive;

            MusicState s = AudioAnalyzer.Instance != null ? AudioAnalyzer.Instance.State : null;

            // Spawn on beat/onset while in Overdrive (rate-limited so dense onsets don't spam the pool).
            if (overdrive && s != null && (s.Beat || s.BassOnset)
                && Time.time - _lastSpawnTime >= _config.WaveMinInterval)
            {
                TrySpawn(s);
            }

            // Advance + paint active waves (they keep expanding/fading even as Overdrive ends).
            for (int i = 0; i < Pool; i++)
            {
                if (!_waves[i].Active) continue;
                Wave w = _waves[i];
                w.Radius += _config.WaveSpeed * dt;
                if (w.Radius >= w.MaxRadius)
                {
                    _waves[i].Active = false;
                    continue;
                }

                // Fade the deposit as the ring approaches its max radius (dissipates into the smoke).
                float fade = 1f - w.Radius / Mathf.Max(0.001f, w.MaxRadius);
                float deposit = w.Intensity * fade * dt; // dt-scaled → frame-rate independent
                field.PaintRing(w.Center, w.Radius, deposit, s);
                _waves[i] = w;
            }
        }

        private void TrySpawn(MusicState s)
        {
            int maxActive = Mathf.Clamp(_config.MaxWaves, 1, Pool);
            int active = 0;
            int free = -1;
            for (int i = 0; i < Pool; i++)
            {
                if (_waves[i].Active) active++;
                else if (free < 0) free = i;
            }
            if (free < 0 || active >= maxActive) return; // pool full; short-lived waves free up quickly

            float bass = Mathf.Clamp01(s.Bass);
            _waves[free] = new Wave
            {
                Active = true,
                Center = transform.position,
                Radius = 0.2f,
                MaxRadius = _config.WaveMaxRadius * (1f + bass * _config.WaveBassRadiusBoost),
                Intensity = _config.WaveIntensity * (1f + bass * _config.WaveBassIntensityBoost)
            };
            _lastSpawnTime = Time.time;
            WaveEmitted?.Invoke();
        }
    }
}
