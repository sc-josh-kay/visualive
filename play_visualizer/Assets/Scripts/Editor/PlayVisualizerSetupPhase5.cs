using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// One-click setup for Phase 5: a MusicSystem object (AudioSource + provider + analyzer)
    /// plus the debug HUD, with the first track in Assets/Audio/Tracks assigned. Re-runnable.
    ///
    /// Menu: PlayVisualizer → Build Audio System (Phase 5)
    /// </summary>
    public static class PlayVisualizerSetupPhase5
    {
        [MenuItem("PlayVisualizer/Build Audio System (Phase 5)")]
        public static void BuildPhase5()
        {
            // Find the first audio clip in the Tracks folder (robust to exact filename).
            AudioClip clip = FindFirstTrack();
            if (clip == null)
            {
                Debug.LogError("PlayVisualizer: no AudioClip found in Assets/Audio/Tracks. " +
                               "Drop a track in there and re-run.");
                return;
            }

            if (Object.FindFirstObjectByType<AudioListener>() == null)
            {
                Debug.LogWarning("PlayVisualizer: no AudioListener in the scene — the Main Camera " +
                                 "normally has one. Spectrum data will be empty without it.");
            }

            GameObject existing = GameObject.Find("MusicSystem");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var go = new GameObject("MusicSystem");
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            go.AddComponent<LocalClipMusicProvider>();
            var analyzer = go.AddComponent<AudioAnalyzer>();
            var hud = go.AddComponent<MusicDebugHUD>();

            AssignReference(hud, "_analyzer", analyzer);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"PlayVisualizer: Phase 5 setup complete (track: {clip.name}). " +
                      "Save the scene (Cmd+S) and press Play.");
        }

        private static AudioClip FindFirstTrack()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Tracks" });
            if (guids.Length == 0)
            {
                return null;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void AssignReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"PlayVisualizer: could not find serialized field '{field}' on {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
