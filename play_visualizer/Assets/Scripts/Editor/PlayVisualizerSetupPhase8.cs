using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayVisualizer.Player;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Gameplay-MVP wiring (spec6). Phase 4: no player death — the survival Health/contact-damage
    /// model is replaced by non-lethal paint-trail disruption. This step adds the PlayerTrail
    /// component (movement-gating + hit-disruption) to the Player and points it at the PlayerConfig.
    ///
    /// Everything else in Phases 1–4 auto-wires at runtime (VisualizerField is created by the
    /// visualizer core, the FieldSplat shader is found by name, scoring tunables live on the
    /// ScoreManager), so this is the only manual step. Re-runnable.
    ///
    /// Menu: PlayVisualizer → Gameplay MVP: Wire Player Trail (Phase 4)
    /// </summary>
    public static class PlayVisualizerSetupPhase8
    {
        private const string PlayerConfigPath = "Assets/ScriptableObjects/PlayerConfig.asset";

        [MenuItem("PlayVisualizer/Gameplay MVP: Wire Player Trail (Phase 4)")]
        public static void WirePlayerTrail()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogWarning("PlayVisualizer: no 'Player' found — run the earlier setup phases first.");
                return;
            }

            PlayerTrail trail = player.GetComponent<PlayerTrail>();
            if (trail == null)
            {
                trail = player.AddComponent<PlayerTrail>();
            }

            PlayerConfig config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(PlayerConfigPath);
            if (config != null)
            {
                AssignReference(trail, "_config", config);
            }
            else
            {
                Debug.LogWarning($"PlayVisualizer: no PlayerConfig at {PlayerConfigPath}; " +
                                 "PlayerTrail will run with built-in defaults.");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Phase 4 wiring complete (PlayerTrail on Player). " +
                      "Save the scene (Cmd+S) and press Play.");
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
