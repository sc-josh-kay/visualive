using UnityEngine;
using UnityEngine.InputSystem;
using PlayVisualizer.Audio;
using PlayVisualizer.Enemies;

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// The one place music influences gameplay. Reads MusicState and pushes values into the
    /// spawner and enemy-modulation channel via a tunable MusicMappingConfig. Gameplay systems
    /// stay audio-agnostic; all coupling lives here, ready to grow into a richer director later.
    ///
    /// Press M at runtime to toggle music-driven mode on/off — the direct A/B that proves the
    /// music is actually changing the game.
    /// </summary>
    public class MusicMapper : MonoBehaviour
    {
        [SerializeField] private AudioAnalyzer _analyzer;
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private EnemyModulation _enemyModulation;
        [SerializeField] private LocalClipMusicProvider _provider;
        [SerializeField] private MusicMappingConfig _config;

        [SerializeField] private bool _musicDriven = true;

        private float _lastBurstTime;
        private float _intensity; // song-time intensity, exposed on the HUD readout

        private void Awake()
        {
            if (_analyzer == null) _analyzer = FindFirstObjectByType<AudioAnalyzer>();
            if (_spawner == null) _spawner = FindFirstObjectByType<EnemySpawner>();
            if (_enemyModulation == null) _enemyModulation = FindFirstObjectByType<EnemyModulation>();
            if (_provider == null) _provider = FindFirstObjectByType<LocalClipMusicProvider>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                _musicDriven = !_musicDriven;
                if (!_musicDriven)
                {
                    RevertToBaseline();
                }
            }

            if (!_musicDriven || _analyzer == null || _config == null)
            {
                return;
            }

            MusicState s = _analyzer.State;

            // Warmup ramp: 0→1 over the first WarmupSeconds of the run, so the opening is
            // always calm even though the (relative) energy reading is high immediately.
            float warmup = _config.WarmupSeconds > 0f
                ? Mathf.Clamp01(Time.timeSinceLevelLoad / _config.WarmupSeconds)
                : 1f;

            // Song-time intensity (spec §13): ramps enemy activity across the song. Progress comes
            // from the track position; scales both the spawn rate and the on-screen enemy cap so the
            // song starts calm with few enemies and builds — the core density control.
            float progress = _provider != null && _provider.SongLength > 0.01f
                ? Mathf.Clamp01(_provider.SongTime / _provider.SongLength)
                : 0f;
            _intensity = Mathf.Clamp01(_config.IntensityOverSong.Evaluate(progress));

            // Mapping 1: Energy → Spawn Rate, scaled by warmup AND song intensity.
            if (_spawner != null)
            {
                float t = Mathf.Clamp01(_config.EnergyCurve.Evaluate(s.Energy));
                _spawner.SpawnRate =
                    Mathf.Lerp(_config.SpawnRateMin, _config.SpawnRateMax, t) * warmup * _intensity;

                // Enemy cap grows with intensity, and gate which types can appear by song progress.
                _spawner.ActiveMaxEnemies =
                    Mathf.RoundToInt(Mathf.Lerp(_config.StartMaxEnemies, _config.FullMaxEnemies, _intensity));
                _spawner.SongProgress = progress;
            }

            // Mapping 2: Bass → Enemy Speed (applied live to all enemies via the shared channel)
            if (_enemyModulation != null)
            {
                _enemyModulation.SpeedMultiplier =
                    Mathf.Lerp(_config.SpeedMultMin, _config.SpeedMultMax, s.Bass);
            }

            // Mapping 3: Beat → Spawn Burst — gated by intensity and a minimum interval so fast
            // beats don't flood the arena.
            if (s.Beat && _spawner != null &&
                _intensity > 0.3f &&
                Time.time - _lastBurstTime >= _config.BeatBurstMinInterval)
            {
                _spawner.SpawnBurst(_config.BeatBurstCount);
                _lastBurstTime = Time.time;
            }
        }

        private void RevertToBaseline()
        {
            if (_spawner != null)
            {
                _spawner.SpawnRate = _spawner.BaselineSpawnRate;
                _spawner.ActiveMaxEnemies = -1; // back to the config cap
                _spawner.SongProgress = 1f;     // all enemy types eligible
            }
            if (_enemyModulation != null)
            {
                _enemyModulation.SpeedMultiplier = 1f;
            }
        }

        private void OnGUI()
        {
            if (Application.isMobilePlatform) return; // debug overlay: editor/desktop only
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            style.normal.textColor = _musicDriven ? new Color(0.3f, 1f, 0.6f) : new Color(1f, 0.5f, 0.5f);
            string label = _musicDriven
                ? $"MUSIC-DRIVEN: ON  (M)   ·   INTENSITY {_intensity:0.00}"
                : "MUSIC-DRIVEN: OFF  (M to toggle)";
            GUI.Label(new Rect(20, 340, 520, 24), label, style);
        }
    }
}
