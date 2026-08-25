using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// App-wide startup config. Caps the frame rate to 60 so the per-frame feedback visuals
    /// (smoke advect/fade/emit) run at the rate they were tuned for, and on iOS forces the audio
    /// session to "Playback" so music plays regardless of the phone's mute switch. No wiring.
    /// </summary>
    public static class AppBootstrap
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void _SetAudioSessionPlayback();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ConfigureAudio()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _SetAudioSessionPlayback();
#endif
        }
    }
}
