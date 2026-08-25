using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// Buttons on the song-complete results screen: Restart replays the same song, Choose Song
    /// returns to the song-select menu. Lives on the results panel; wires itself when the panel
    /// is first shown.
    /// </summary>
    public class ResultsMenu : MonoBehaviour
    {
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _chooseButton;
        [SerializeField] private LocalClipMusicProvider _provider;

        private void Awake()
        {
            if (_provider == null) _provider = FindFirstObjectByType<LocalClipMusicProvider>();
            if (_restartButton != null) _restartButton.onClick.AddListener(Restart);
            if (_chooseButton != null) _chooseButton.onClick.AddListener(ChooseSong);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            StartScreenController.PendingSong = _provider != null ? _provider.CurrentClip : null;
            Reload();
        }

        public void ChooseSong()
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
