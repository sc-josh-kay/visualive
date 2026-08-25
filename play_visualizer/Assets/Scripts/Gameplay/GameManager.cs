using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif
using PlayVisualizer.Audio;
using PlayVisualizer.Enemies;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// Owns high-level game flow. There is deliberately NO player death / game-over (spec §15): the
    /// only consequence of poor play is a darker, lower-coverage world and a slower score. The run
    /// ends when the SONG ends (spec §16): spawning stops, a short outro plays, then a results
    /// summary (final score, peak/avg coverage, enemies destroyed, track, high score) is shown.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum GameState
        {
            Menu,
            Playing,
            Results
        }

        [Header("Flow")]
        [Tooltip("Start paused on the song-select screen until BeginGame() is called.")]
        [SerializeField] private bool _openMenuOnStart = true;

        [Header("HUD")]
        [SerializeField] private Text _scoreText;
        [Tooltip("Repurposed as the live COVERAGE bar (was the health bar in the survival build).")]
        [FormerlySerializedAs("_healthFill")]
        [SerializeField] private Image _coverageFill;

        [Header("Results")]
        [FormerlySerializedAs("_gameOverPanel")]
        [SerializeField] private GameObject _resultsPanel;
        [SerializeField] private Text _finalScoreText;

        [Header("Song end")]
        [SerializeField] private LocalClipMusicProvider _provider;
        [SerializeField] private EnemySpawner _spawner;
        [SerializeField] private MusicMapper _musicMapper;
        [Tooltip("Seconds of visual/audio tail after the song ends before the results appear.")]
        [SerializeField] private float _outroSeconds = 2.5f;

        public GameState State { get; private set; } = GameState.Playing;

        private bool _sawPlaying;   // the track was actually running (guards the end check)
        private bool _ending;       // song-end sequence has begun

        private void Start()
        {
            if (_openMenuOnStart)
            {
                State = GameState.Menu;
                Time.timeScale = 0f; // frozen until a song is chosen
            }
            else
            {
                State = GameState.Playing;
                Time.timeScale = 1f;
            }

            if (_resultsPanel != null) _resultsPanel.SetActive(false);

            if (_provider == null) _provider = FindFirstObjectByType<LocalClipMusicProvider>();
            if (_spawner == null) _spawner = FindFirstObjectByType<EnemySpawner>();
            if (_musicMapper == null) _musicMapper = FindFirstObjectByType<MusicMapper>();

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ScoreChanged += OnScoreChanged;
                OnScoreChanged(ScoreManager.Instance.Score);
            }
            else
            {
                OnScoreChanged(0);
            }
        }

        private void OnDestroy()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ScoreChanged -= OnScoreChanged;
            }
        }

        private void Update()
        {
            // Live coverage bar. Kept out of ScoreChanged (which is int-quantized) so it tracks
            // coverage smoothly, and only while playing.
            if (_coverageFill != null)
            {
                float coverage = State == GameState.Playing && VisualizerField.Instance != null
                    ? VisualizerField.Instance.Coverage
                    : _coverageFill.fillAmount;
                _coverageFill.fillAmount = coverage;
            }

            if (State == GameState.Playing)
            {
                CheckSongEnd();
#if UNITY_EDITOR
                // Dev shortcut: press K to end the song now and jump to the results screen.
                if (!_ending && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
                {
                    StartCoroutine(EndSongRoutine());
                }
#endif
            }
        }

        private void CheckSongEnd()
        {
            // While paused (timeScale 0) the AudioSource is paused too, so IsPlaying goes false —
            // don't mistake that for the song ending.
            if (_ending || Time.timeScale == 0f || _provider == null || _provider.SongLength <= 0.01f)
            {
                return;
            }

            if (_provider.IsPlaying) _sawPlaying = true;

            bool reachedEnd = _provider.SongTime >= _provider.SongLength - 0.2f;
            bool stopped = _sawPlaying && !_provider.IsPlaying;
            if (reachedEnd || stopped)
            {
                StartCoroutine(EndSongRoutine());
            }
        }

        private IEnumerator EndSongRoutine()
        {
            _ending = true;

            // Stop making new threats, but let the world (and the player's last paint) breathe.
            if (_spawner != null) _spawner.enabled = false;
            if (_musicMapper != null) _musicMapper.enabled = false;

            yield return new WaitForSeconds(Mathf.Max(0f, _outroSeconds));

            ShowResults();
        }

        private void ShowResults()
        {
            State = GameState.Results;

            if (_finalScoreText != null)
            {
                // Make the multi-line summary readable regardless of the panel's original layout.
                _finalScoreText.alignment = TextAnchor.MiddleCenter;
                _finalScoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
                _finalScoreText.verticalOverflow = VerticalWrapMode.Overflow;
                _finalScoreText.lineSpacing = 1.1f;
                _finalScoreText.text = BuildSummary();
            }
            if (_resultsPanel != null)
            {
                // The panel was built as a survival "GAME OVER" screen; relabel its title for the
                // no-death results screen (no panel rebuild needed).
                Transform title = _resultsPanel.transform.Find("GameOverText");
                if (title != null && title.TryGetComponent(out Text titleText))
                {
                    titleText.text = "SONG COMPLETE";
                }
                _resultsPanel.SetActive(true);
            }

            Time.timeScale = 0f;
        }

        private string BuildSummary()
        {
            ScoreManager sm = ScoreManager.Instance;
            int score = sm != null ? sm.Score : 0;
            float peak = sm != null ? sm.PeakCoverage : 0f;
            float avg = sm != null ? sm.AverageCoverage : 0f;
            int kills = sm != null ? sm.EnemiesDestroyed : 0;
            string song = _provider != null ? _provider.SongName : string.Empty;

            // Per-song high score.
            string key = "HiScore_" + song;
            int hi = PlayerPrefs.GetInt(key, 0);
            bool isNewHigh = score > hi;
            if (isNewHigh)
            {
                hi = score;
                PlayerPrefs.SetInt(key, hi);
                PlayerPrefs.Save();
            }

            string tail = isNewHigh ? "★ NEW HIGH SCORE ★" : $"High Score   {hi}";
            return
                $"FINAL SCORE\n{score}\n\n" +
                $"Peak Coverage       {peak * 100f:0}%\n" +
                $"Average Coverage    {avg * 100f:0}%\n" +
                $"Enemies Destroyed   {kills}\n" +
                $"Song   {song}\n\n" +
                tail;
        }

        private void OnScoreChanged(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"SCORE  {score}";
            }
        }

        /// <summary>Called by the start screen once a song is selected — unfreezes gameplay.</summary>
        public void BeginGame()
        {
            State = GameState.Playing;
            _sawPlaying = false;
            _ending = false;
            Time.timeScale = 1f;
        }

        /// <summary>Hooked to the Restart button. Reloads the scene for a clean reset.</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
