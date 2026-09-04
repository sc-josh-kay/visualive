using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// App-wide startup config. Caps the frame rate (see below) and on iOS forces the audio
    /// session to "Playback" so music plays regardless of the phone's mute switch. No wiring.
    ///
    /// Frame rate: capped to a STEADY 30 to keep the device cool (heat ≈ frames rendered) and avoid
    /// the thermal governor's jarring 60↔30 oscillation. This is safe because the smoke feedback is
    /// now frame-rate-INDEPENDENT (KaleidoscopePattern's _DtScale), so 30 fps looks/plays identical
    /// to 60 — the only thing given up is motion smoothness / input latency, not gameplay balance.
    /// Set back to 60 for max smoothness on a cool device.
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
            Application.targetFrameRate = 30;
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
