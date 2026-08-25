using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PlayVisualizer.Audio;
using PlayVisualizer.Player;

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// Touch pause flow: when the player lifts BOTH thumbs off the sticks during play (after a
    /// short grace period), the game pauses (freezes gameplay AND the music) and shows Resume /
    /// Restart / Quit. Restart replays the same song; Quit returns to the song-select screen.
    /// Auto-pause re-arms only after a stick is grabbed again, so resuming doesn't instantly re-pause.
    /// Touch platforms only (or TouchControls.ForceInEditor).
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private VirtualJoystick _moveStick;
        [SerializeField] private VirtualJoystick _aimStick;
        [SerializeField] private TouchControls _touch;
        [SerializeField] private LocalClipMusicProvider _provider;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private float _graceSeconds = 0.4f;

        private bool _paused;
        private bool _armed;
        private float _releaseTimer;

        private void Awake()
        {
            if (_touch == null) _touch = FindFirstObjectByType<TouchControls>();
            if (_provider == null) _provider = FindFirstObjectByType<LocalClipMusicProvider>();
            if (_gameManager == null) _gameManager = FindFirstObjectByType<GameManager>();

            if (_pausePanel != null) _pausePanel.SetActive(false);
            if (_resumeButton != null) _resumeButton.onClick.AddListener(Resume);
            if (_restartButton != null) _restartButton.onClick.AddListener(Restart);
            if (_quitButton != null) _quitButton.onClick.AddListener(QuitToSongs);
        }

        private void Update()
        {
            if (_touch == null || !_touch.TouchMode || _paused) return;
            if (_gameManager == null || _gameManager.State != GameManager.GameState.Playing) return;

            bool anyStick = (_moveStick != null && _moveStick.Active) || (_aimStick != null && _aimStick.Active);
            if (anyStick)
            {
                _armed = true;
                _releaseTimer = 0f;
            }
            else if (_armed)
            {
                _releaseTimer += Time.unscaledDeltaTime;
                if (_releaseTimer >= _graceSeconds) Pause();
            }
        }

        private void Pause()
        {
            _paused = true;
            Time.timeScale = 0f;
            if (_provider != null) _provider.Pause();
            if (_pausePanel != null) _pausePanel.SetActive(true);
        }

        public void Resume()
        {
            _paused = false;
            _armed = false; // require re-grabbing a stick before it can auto-pause again
            _releaseTimer = 0f;
            if (_pausePanel != null) _pausePanel.SetActive(false);
            if (_provider != null) _provider.Resume();
            Time.timeScale = 1f;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            StartScreenController.PendingSong = _provider != null ? _provider.CurrentClip : null;
            Reload();
        }

        public void QuitToSongs()
        {
            Time.timeScale = 1f;
            StartScreenController.PendingSong = null; // show the song menu
            Reload();
        }

        private static void Reload()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
