using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayVisualizer.Audio;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// One-click setup for Phase 7: background pulse, treble particles, beat shockwave, and the
    /// VisualizerController that drives them from MusicState. Re-runnable.
    ///
    /// Menu: PlayVisualizer → Build Visualizer (Phase 7)
    /// </summary>
    public static class PlayVisualizerSetupPhase7
    {
        private const string ArtDir = "Assets/Art";
        private const string ConfigDir = "Assets/ScriptableObjects";
        private const string ShockwavePrefabPath = "Assets/Prefabs/Visuals/Shockwave.prefab";
        private const string ParticleMatPath = "Assets/Materials/TrebleParticle.mat";

        [MenuItem("PlayVisualizer/Build Visualizer (Phase 7)")]
        public static void BuildPhase7()
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Visuals");
            EnsureFolder("Assets", "ScriptableObjects");

            Sprite radial = CreateRadialSprite(ArtDir + "/BackgroundRadial.png", 256);
            Sprite ring = CreateRingSprite(ArtDir + "/ShockwaveRing.png", 256);
            Sprite circle = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/ProjectileCircle.png");

            VisualizerConfig config = LoadOrCreate<VisualizerConfig>(ConfigDir + "/VisualizerConfig.asset");

            Transform background = BuildBackground(radial, config);
            DeathPop shockwave = BuildShockwavePrefab(ring);
            ParticleSystem particles = BuildTrebleParticles(circle);

            // Controller
            DestroyIfExists("VisualizerController");
            var go = new GameObject("VisualizerController");
            var controller = go.AddComponent<VisualizerController>();
            AssignReference(controller, "_analyzer", Object.FindFirstObjectByType<AudioAnalyzer>());
            AssignReference(controller, "_config", config);
            AssignReference(controller, "_background", background);
            AssignReference(controller, "_trebleParticles", particles);
            AssignReference(controller, "_shockwavePrefab", shockwave);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Phase 7 setup complete. Save the scene (Cmd+S) and press Play.");
        }

        // ---------------------------------------------------------------- scene objects

        private static Transform BuildBackground(Sprite sprite, VisualizerConfig config)
        {
            DestroyIfExists("BackgroundPulse");
            var go = new GameObject("BackgroundPulse");
            go.transform.position = new Vector3(0f, 0f, 1f); // slightly behind the play plane
            float s = config.BackgroundBaseScale;
            go.transform.localScale = new Vector3(s, s, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.35f, 0.1f, 0.6f, 0.55f); // dim neon purple glow
            sr.sortingOrder = -100;
            return go.transform;
        }

        private static DeathPop BuildShockwavePrefab(Sprite ring)
        {
            var go = new GameObject("Shockwave");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ring;
            sr.sortingOrder = 4;

            var pop = go.AddComponent<DeathPop>();
            AssignFloat(pop, "_duration", 0.55f);
            AssignFloat(pop, "_endScaleMultiplier", 10f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ShockwavePrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<DeathPop>();
        }

        private static ParticleSystem BuildTrebleParticles(Sprite circle)
        {
            DestroyIfExists("TrebleParticles");
            var go = new GameObject("TrebleParticles");
            go.transform.position = Vector3.zero;

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = 2.5f;
            main.startSpeed = 0.6f;
            main.startSize = 0.14f;
            main.maxParticles = 500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.5f, 0.85f, 1f, 0.85f);

            var emission = ps.emission;
            emission.rateOverTime = 10f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(44f, 26f, 1f);

            // Twinkle: fade alpha in and out over lifetime.
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            // Material: Sprites/Default with the soft circle texture (renders reliably, glows with bloom).
            var mat = new Material(Shader.Find("Sprites/Default"));
            if (circle != null)
            {
                mat.mainTexture = circle.texture;
            }
            if (AssetDatabase.LoadAssetAtPath<Material>(ParticleMatPath) != null)
            {
                AssetDatabase.DeleteAsset(ParticleMatPath);
            }
            AssetDatabase.CreateAsset(mat, ParticleMatPath);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.sortingOrder = -50;

            return ps;
        }

        // ---------------------------------------------------------------- sprites

        private static Sprite CreateRadialSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) / 2f;
            float rad = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(1f - d / rad);
                    a = a * a; // softer falloff
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return WriteSprite(tex, path);
        }

        private static Sprite CreateRingSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) / 2f;
            float outer = size / 2f - 1f;
            float inner = outer * 0.6f;
            float rc = (inner + outer) / 2f;
            float halfW = (outer - inner) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - rc) / halfW);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return WriteSprite(tex, path);
        }

        private static Sprite WriteSprite(Texture2D tex, string path)
        {
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------------------------------------------------------------- util

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
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

        private static void AssignFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"PlayVisualizer: could not find serialized field '{field}' on {target.GetType().Name}.");
                return;
            }
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DestroyIfExists(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
