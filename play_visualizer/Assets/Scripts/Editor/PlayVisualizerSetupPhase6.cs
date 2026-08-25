using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayVisualizer.Enemies;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Phase 6: adds the Corruptor and Swarm enemy types and wires the spawner's weighted multi-type
    /// table (Color Eater + Corruptor + Swarm groups). Re-runnable. Requires the Phase 3 setup first
    /// (it reuses the Color Eater prefab, DeathPop, and CollisionBurst).
    ///
    /// Menu: PlayVisualizer → Build Corruptor + Swarm (Phase 6)
    /// </summary>
    public static class PlayVisualizerSetupPhase6
    {
        private const string ArtDir = "Assets/Art";
        private const string ConfigDir = "Assets/ScriptableObjects";
        private const string PrefabDir = "Assets/Prefabs/Enemies";

        private static readonly Color CorruptorColor = new Color(0.6f, 0.15f, 0.95f, 1f); // violet
        private static readonly Color SwarmColor = new Color(0.2f, 0.95f, 0.7f, 1f);      // cyan-green

        [MenuItem("PlayVisualizer/Build Corruptor + Swarm (Phase 6)")]
        public static void BuildPhase6()
        {
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner == null)
            {
                Debug.LogError("PlayVisualizer: no EnemySpawner in scene — run the Phase 3 setup first.");
                return;
            }

            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets", "ScriptableObjects");
            EnsureFolder("Assets/Prefabs", "Enemies");

            Sprite diamond = LoadOrGenerate(ArtDir + "/EnemyDiamond.png", diamondShape: true);
            Sprite circle = LoadOrGenerate(ArtDir + "/ProjectileCircle.png", diamondShape: false);

            var deathPop = LoadComponent<DeathPop>("Assets/Prefabs/Visuals/DeathPop.prefab");
            var burst = LoadComponent<CollisionBurst>("Assets/Prefabs/Visuals/CollisionBurst.prefab");

            EnemyConfig corruptorConfig = MakeConfig(ConfigDir + "/CorruptorConfig.asset", c =>
            {
                c.Speed = 1.2f; c.Health = 10; c.Scale = 1.5f; c.ScoreValue = 400;
                c.ConsumeRadius = 1.0f; c.ConsumeStrengthPerSecond = 6f;
                c.DeathPaintRadius = 3.5f; c.DeathPaintIntensity = 2.5f;
                c.PlayerChaseRadius = 0f; c.TargetJitter = 1.5f;
                c.SeparationRadius = 2.0f; c.SeparationStrength = 1.0f;
                c.HitConsumeRadius = 2.5f; c.HitConsumeStrength = 1f; c.HitBurstDuration = 0.5f;
                c.CorruptStartRadius = 0.8f; c.CorruptMaxRadius = 4.5f; c.CorruptGrowSeconds = 9f;
            });

            EnemyConfig swarmConfig = MakeConfig(ConfigDir + "/SwarmConfig.asset", c =>
            {
                c.Speed = 5f; c.Health = 1; c.Scale = 0.35f; c.ScoreValue = 20;
                c.ConsumeRadius = 0.5f; c.ConsumeStrengthPerSecond = 2.5f;
                c.DeathPaintRadius = 0.7f; c.DeathPaintIntensity = 0.9f;
                c.PlayerChaseRadius = 2f; c.TargetJitter = 3.5f;
                c.SeparationRadius = 0.5f; c.SeparationStrength = 1.2f;
                c.HitConsumeRadius = 0.9f; c.HitConsumeStrength = 0.8f; c.HitBurstDuration = 0.25f;
                c.SwarmCohesionRadius = 3.5f; c.SwarmCohesionStrength = 0.7f;
            });

            EnemyBase corruptor = BuildEnemyPrefab("Corruptor", PrefabDir + "/Corruptor.prefab",
                typeof(Corruptor), diamond, CorruptorColor, corruptorConfig, deathPop, burst);
            EnemyBase swarm = BuildEnemyPrefab("SwarmUnit", PrefabDir + "/SwarmUnit.prefab",
                typeof(SwarmUnit), circle, SwarmColor, swarmConfig, deathPop, burst);

            EnemyBase colorEater = LoadComponent<EnemyBase>("Assets/Prefabs/Enemies/Enemy.prefab");
            if (colorEater == null)
            {
                Debug.LogError("PlayVisualizer: Color Eater prefab missing — run Phase 3 first.");
                return;
            }

            // Give swarms room to be a cloud.
            SpawnerConfig spawnerConfig = AssetDatabase.LoadAssetAtPath<SpawnerConfig>(ConfigDir + "/SpawnerConfig.asset");
            if (spawnerConfig != null)
            {
                spawnerConfig.MaxEnemies = Mathf.Max(spawnerConfig.MaxEnemies, 40);
                EditorUtility.SetDirty(spawnerConfig);
            }

            WriteEntries(spawner, colorEater, corruptor, swarm);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Phase 6 complete (Corruptor + Swarm + weighted spawner). " +
                      "Save the scene (Cmd+S) and press Play.");
        }

        // ----------------------------------------------------------------- spawner table

        private static void WriteEntries(EnemySpawner spawner, EnemyBase colorEater, EnemyBase corruptor, EnemyBase swarm)
        {
            var so = new SerializedObject(spawner);
            SerializedProperty arr = so.FindProperty("_entries");
            arr.arraySize = 3;
            // prefab, weight, groupMin, groupMax, spread, minSongProgress (tougher types unlock later)
            SetEntry(arr.GetArrayElementAtIndex(0), colorEater, 1.0f, 1, 1, 0f, 0f);
            SetEntry(arr.GetArrayElementAtIndex(1), corruptor, 0.25f, 1, 1, 0f, 0.2f);
            SetEntry(arr.GetArrayElementAtIndex(2), swarm, 0.5f, 6, 10, 1.8f, 0.35f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEntry(SerializedProperty el, EnemyBase prefab, float weight,
            int gMin, int gMax, float spread, float minProgress)
        {
            el.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
            el.FindPropertyRelative("Weight").floatValue = weight;
            el.FindPropertyRelative("GroupMin").intValue = gMin;
            el.FindPropertyRelative("GroupMax").intValue = gMax;
            el.FindPropertyRelative("GroupSpread").floatValue = spread;
            el.FindPropertyRelative("MinSongProgress").floatValue = minProgress;
        }

        // ----------------------------------------------------------------- prefabs / configs

        private static EnemyBase BuildEnemyPrefab(string name, string path, System.Type component,
            Sprite sprite, Color color, EnemyConfig config, DeathPop deathPop, CollisionBurst burst)
        {
            var go = new GameObject(name);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 8;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            var enemy = (EnemyBase)go.AddComponent(component);
            AssignReference(enemy, "_config", config);
            if (deathPop != null) AssignReference(enemy, "_deathPopPrefab", deathPop);
            if (burst != null) AssignReference(enemy, "_collisionBurstPrefab", burst);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<EnemyBase>();
        }

        private static EnemyConfig MakeConfig(string path, System.Action<EnemyConfig> setup)
        {
            EnemyConfig cfg = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
            bool created = false;
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<EnemyConfig>();
                created = true;
            }
            setup(cfg);
            if (created) AssetDatabase.CreateAsset(cfg, path);
            else EditorUtility.SetDirty(cfg);
            return cfg;
        }

        // ----------------------------------------------------------------- util

        private static T LoadComponent<T>(string prefabPath) where T : Component
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return go != null ? go.GetComponent<T>() : null;
        }

        private static Sprite LoadOrGenerate(string path, bool diamondShape)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) / 2f;
            float r = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = diamondShape
                        ? Mathf.Abs(x - c) + Mathf.Abs(y - c) <= r
                        : (x - c) * (x - c) + (y - c) * (y - c) <= r * r;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void AssignReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"PlayVisualizer: no serialized field '{field}' on {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
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
