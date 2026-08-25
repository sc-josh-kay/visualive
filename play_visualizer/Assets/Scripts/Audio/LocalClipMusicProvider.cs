using UnityEngine;

namespace PlayVisualizer.Audio
{
    /// <summary>
    /// The MVP music source: plays a local AudioClip through an AudioSource and exposes its
    /// sample data via IMusicProvider. This is the only class that knows about AudioClip.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class LocalClipMusicProvider : MonoBehaviour, IMusicProvider
    {
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _loop = true;

        private AudioSource _source;

        public bool IsPlaying => _source != null && _source.isPlaying;
        public int SampleRate => AudioSettings.outputSampleRate;
        public float SongTime => _source != null && _source.clip != null ? _source.time : 0f;
        public float SongLength => _source != null && _source.clip != null ? _source.clip.length : 0f;

        /// <summary>Name of the current track (for the results summary).</summary>
        public string SongName => _source != null && _source.clip != null ? _source.clip.name : string.Empty;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.loop = _loop;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D: full volume regardless of position
        }

        private void Start()
        {
            if (_playOnStart)
            {
                Play();
            }
        }

        public void Play()
        {
            if (_source != null && _source.clip != null)
            {
                _source.Play();
            }
        }

        /// <summary>Assign a clip and start it (used by the start-screen song selection).</summary>
        public void PlayClip(AudioClip clip)
        {
            if (_source == null) return;
            _source.clip = clip;
            _source.loop = false; // a gameplay run plays the track once, so the song can END (Phase 8)
            _source.Play();
        }

        public void Stop()
        {
            if (_source != null)
            {
                _source.Stop();
            }
        }

        /// <summary>The clip currently loaded (used to replay the same song on Restart).</summary>
        public AudioClip CurrentClip => _source != null ? _source.clip : null;

        /// <summary>Pause playback (music must stop with the game, since it ignores timeScale).</summary>
        public void Pause() => _source?.Pause();

        /// <summary>Resume paused playback.</summary>
        public void Resume() => _source?.UnPause();

        public void GetSpectrumData(float[] samples, int channel, FFTWindow window)
        {
            _source.GetSpectrumData(samples, channel, window);
        }

        public void GetOutputData(float[] samples, int channel)
        {
            _source.GetOutputData(samples, channel);
        }
    }
}
