using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using PlayVisualizer.Audio;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Phase V1 setup: builds the new VisualizerCore + its own display quad, running alongside the
    /// old FeedbackController (press V in play mode to toggle between them). Re-runnable.
    ///
    /// Menu: PlayVisualizer → Viz: Build Core (Phase V1)
    /// </summary>
    public static class PlayVisualizerVizPhaseV1
    {
        private const string DisplayMatPath = "Assets/Materials/VizDisplay.mat";

        [MenuItem("PlayVisualizer/Viz: Build Core (Phase V1)")]
        public static void BuildV1()
        {
            Shader smokeShader = Shader.Find("PlayVisualizer/SmokeField");
            Shader passthrough = Shader.Find("PlayVisualizer/Kaleidoscope");
            Shader fluidShader = Shader.Find("PlayVisualizer/FluidField");
            Shader filamentShader = Shader.Find("PlayVisualizer/Filament");
            Shader blendShader = Shader.Find("PlayVisualizer/Blend");
            if (smokeShader == null || passthrough == null || fluidShader == null ||
                filamentShader == null || blendShader == null)
            {
                Debug.LogError("PlayVisualizer: shaders not found — let Unity compile Assets/Shaders first, then re-run.");
                return;
            }

            Camera mainCam = Camera.main;
            GameObject player = GameObject.Find("Player");
            if (mainCam == null || player == null)
            {
                Debug.LogError("PlayVisualizer: need a Main Camera and a Player in the scene.");
                return;
            }

            // Display quad for the new system (starts hidden; old system shows by default).
            Material displayMat = CreateMaterial(DisplayMatPath, passthrough, m =>
            {
                m.SetFloat("_Segments", 1f);
                m.SetFloat("_Brightness", 1f);
            });
            Renderer display = BuildDisplayQuad("VizDisplay", displayMat);
            display.enabled = false;

            // Core.
            DestroyIfExists("VisualizerCore");
            var go = new GameObject("VisualizerCore");
            var core = go.AddComponent<VisualizerCore>();
            AssignReference(core, "_mainCamera", mainCam);
            AssignReference(core, "_player", player.transform);
            AssignReference(core, "_displayRenderer", display);
            AssignReference(core, "_analyzer", Object.FindFirstObjectByType<AudioAnalyzer>());
            AssignReference(core, "_smokeShader", smokeShader);
            AssignReference(core, "_kaleidoscopeShader", passthrough);
            AssignReference(core, "_fluidShader", fluidShader);
            AssignReference(core, "_filamentShader", filamentShader);
            AssignReference(core, "_blendShader", blendShader);
            AssignReference(core, "_oldSystem", Object.FindFirstObjectByType<FeedbackController>());

            GameObject oldDisplay = GameObject.Find("VisualizerDisplay");
            if (oldDisplay != null)
            {
                AssignReference(core, "_oldDisplay", oldDisplay.GetComponent<Renderer>());
            }

            GameObject feedbackCam = GameObject.Find("FeedbackCamera");
            if (feedbackCam != null)
            {
                AssignReference(core, "_oldFeedbackCamera", feedbackCam.GetComponent<Camera>());
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Viz Core (Phase V1) built. Save the scene (Cmd+S), press Play, " +
                      "then press V to toggle the new visualizer (B cycles patterns).");
        }

        private static Renderer BuildDisplayQuad(string name, Material material)
        {
            DestroyIfExists(name);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.position = new Vector3(0f, 0f, 5f);
            var mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        private static Material CreateMaterial(string path, Shader shader, System.Action<Material> configure)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) AssetDatabase.DeleteAsset(path);
            var mat = new Material(shader);
            configure?.Invoke(mat);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void AssignReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"PlayVisualizer: missing field '{field}' on {target.GetType().Name}."); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DestroyIfExists(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
