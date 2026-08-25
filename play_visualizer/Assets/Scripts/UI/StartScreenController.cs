using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// The "VISUALIVE" start screen. On load it shows a song menu (one button per track in the
    /// SongLibrary) while the game is frozen. Selecting a song starts its playback and unfreezes
    /// the game via GameManager.BeginGame().
    /// </summary>
    public class StartScreenController : MonoBehaviour
    {
        [SerializeField] private SongLibrary _library;
        [SerializeField] private LocalClipMusicProvider _provider;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _songListContainer;
        [SerializeField] private Button _songButtonTemplate;

        private GameManager _gameManager;

        /// <summary>Set before a scene reload to auto-start this song (Restart), skipping the menu.</summary>
        public static AudioClip PendingSong;

        private void Awake()
        {
            _gameManager = FindFirstObjectByType<GameManager>();
            if (_provider == null) _provider = FindFirstObjectByType<LocalClipMusicProvider>();
            BuildSongButtons();
            // Don't flash the menu when we're about to auto-restart a song.
            if (_panel != null) _panel.SetActive(PendingSong == null);
        }

        private void Start()
        {
            if (PendingSong != null)
            {
                AudioClip clip = PendingSong;
                PendingSong = null;
                StartCoroutine(AutoStart(clip));
            }
        }

        // Wait one frame so GameManager.Start (which opens the menu) has run, then override it.
        private IEnumerator AutoStart(AudioClip clip)
        {
            yield return null;
            StartGame(clip);
        }

        private void BuildSongButtons()
        {
            if (_library == null || _library.Tracks == null || _songButtonTemplate == null || _songListContainer == null)
            {
                return;
            }

            _songButtonTemplate.gameObject.SetActive(false);

            foreach (AudioClip clip in _library.Tracks)
            {
                if (clip == null) continue;

                Button b = Instantiate(_songButtonTemplate, _songListContainer);
                Text label = b.GetComponentInChildren<Text>();
                if (label != null) label.text = clip.name;

                AudioClip captured = clip;
                b.onClick.AddListener(() => StartGame(captured));
                b.gameObject.SetActive(true);
            }
        }

        private void StartGame(AudioClip clip)
        {
            if (_provider != null) _provider.PlayClip(clip);
            if (_panel != null) _panel.SetActive(false);

            if (_gameManager != null) _gameManager.BeginGame();
            else Time.timeScale = 1f;
        }
    }
}
