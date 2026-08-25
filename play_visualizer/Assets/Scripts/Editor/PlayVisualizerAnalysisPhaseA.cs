using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Phase A setup for the analysis engine v2: creates the MusicAnalysisConfig asset and
    /// assigns it to the AudioAnalyzer on the MusicSystem. Re-runnable.
    ///
    /// Menu: PlayVisualizer → Analysis: Build Config (Phase A)
    /// </summary>
    public static class PlayVisualizerAnalysisPhaseA
    {
        private const string ConfigPath = "Assets/ScriptableObjects/MusicAnalysisConfig.asset";

        [MenuItem("PlayVisualizer/Analysis: Build Config (Phase A)")]
        public static void BuildPhaseA()
        {
            var config = AssetDatabase.LoadAssetAtPath<MusicAnalysisConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<MusicAnalysisConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            AudioAnalyzer analyzer = Object.FindFirstObjectByType<AudioAnalyzer>();
            if (analyzer == null)
            {
                Debug.LogError("PlayVisualizer: no AudioAnalyzer in the scene (run the Phase 5 audio setup first).");
                return;
            }

            var so = new SerializedObject(analyzer);
            SerializedProperty prop = so.FindProperty("_config");
            if (prop != null)
            {
                prop.objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Analysis Phase A config created and assigned. " +
                      "Save the scene (Cmd+S), press Play, and F1 to see the frequency bands.");
        }
    }
}
