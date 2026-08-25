using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayVisualizer.Audio;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Phase G setup: builds the player circle (persistent ring + spark system) driven by the
    /// v2 analysis features, and disables the legacy bass-threshold shockwave. Re-runnable.
    ///
    /// Menu: PlayVisualizer → Analysis: Build Player Circle (Phase G)
    /// </summary>
    public static class PlayVisualizerAnalysisPhaseG
    {
        private const string RingSpritePath = "Assets/Art/ShockwaveRing.png";
        private const string CircleSpritePath = "Assets/Art/ProjectileCircle.png";
        private const string SparkMatPath = "Assets/Materials/SparkParticle.mat";

        [MenuItem("PlayVisualizer/Analysis: Build Player Circle (Phase G)")]
        public static void BuildPhaseG()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("PlayVisualizer: no Player found.");
                return;
            }

            Sprite ring = AssetDatabase.LoadAssetAtPath<Sprite>(RingSpritePath);
            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);

            // Fresh PlayerCircle child.
            Transform existing = player.transform.Find("PlayerCircle");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var circleGo = new GameObject("PlayerCircle");
            circleGo.transform.SetParent(player.transform, false);
            circleGo.transform.localPosition = Vector3.zero;
            var sr = circleGo.AddComponent<SpriteRenderer>();
            sr.sprite = ring;
            sr.sortingOrder = 9; // behind the player triangle (10)
            var pc = circleGo.AddComponent<PlayerCircle>();

            // Spark particle system.
            ParticleSystem sparks = BuildSparks(circleGo.transform, circle);

            AssignReference(pc, "_analyzer", Object.FindFirstObjectByType<AudioAnalyzer>());
            AssignReference(pc, "_ring", sr);
            AssignReference(pc, "_sparks", sparks);

            // Disable the legacy shockwave — the circle replaces it.
            var vc = Object.FindFirstObjectByType<VisualizerController>();
            if (vc != null) AssignBool(vc, "_spawnShockwave", false);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Phase G player circle built. Save the scene (Cmd+S) and press Play.");
        }

        private static ParticleSystem BuildSparks(Transform parent, Sprite circle)
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 3f;
            main.startSize = 0.12f;
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.7f, 0.9f, 1f, 1f);

            var emission = ps.emission;
            emission.rateOverTime = 0f; // manual Emit() only

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.6f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var mat = CreateMaterial(SparkMatPath, Shader.Find("Sprites/Default"), circle);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.sortingOrder = 12; // above the player

            return ps;
        }

        private static Material CreateMaterial(string path, Shader shader, Sprite tex)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            var mat = new Material(shader);
            if (tex != null) mat.mainTexture = tex.texture;
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

        private static void AssignBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"PlayVisualizer: missing field '{field}' on {target.GetType().Name}."); return; }
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
