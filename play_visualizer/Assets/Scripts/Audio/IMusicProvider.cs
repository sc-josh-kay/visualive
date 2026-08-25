using UnityEngine;

namespace PlayVisualizer.Audio
{
    /// <summary>
    /// Abstraction over "where the music comes from and how we read its samples". The analyzer
    /// depends only on this, never on AudioClip or a specific source. The MVP has one
    /// implementation (LocalClipMusicProvider); future sources (files, streaming, generated)
    /// can implement the same contract without changing analysis or gameplay.
    /// </summary>
    public interface IMusicProvider
    {
        bool IsPlaying { get; }

        /// <summary>Output sample rate, used to map FFT bins to frequencies.</summary>
        int SampleRate { get; }

        /// <summary>Current playback position in seconds (0 if nothing is playing).</summary>
        float SongTime { get; }

        /// <summary>Length of the current track in seconds (0 if none).</summary>
        float SongLength { get; }

        void Play();
        void Stop();

        /// <summary>Fill <paramref name="samples"/> with FFT spectrum magnitudes.</summary>
        void GetSpectrumData(float[] samples, int channel, FFTWindow window);

        /// <summary>Fill <paramref name="samples"/> with raw waveform output.</summary>
        void GetOutputData(float[] samples, int channel);
    }
}
