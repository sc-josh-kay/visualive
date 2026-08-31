using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayVisualizer.Enemies;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Phase 10 (spec10): adds the Dasher and Splitter enemies — builds their prefabs + configs +
    /// visuals — and rewrites the spawner's weighted table to include ALL enemy types (Color Eater,
    /// Corruptor, Swarm, Dasher, Splitter). Re-runnable. Requires Phase 3 AND Phase 6 first (it reuses
    /// their prefabs, DeathPop, CollisionBurst, and EnergyTrail material).
    ///
    /// Run order is now: Phase 3 → Phase 6 → Phase 10 → save the scene.
    ///
    /// Menu: PlayVisualizer → Build Dasher + Splitter (Phase 10)
    /// </summary>
    public static class PlayVisualizerSetupPhase10
    {
        private const string ConfigDir = "Assets/ScriptableObjects";
        private const string PrefabDir = "Assets/Prefabs/Enemies";

        [MenuItem("PlayVisualizer/Build Dasher + Splitter (Phase 10)")]
        public static void BuildPhase10()
        {
            var spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner == null)
            {
                Debug.LogError("PlayVisualizer: no EnemySpawner — run Phase 3 then Phase 6 first.");
                return;
            }

            EnemyBase colorEater = LoadComponent<EnemyBase>(PrefabDir + "/Enemy.prefab");
            EnemyBase corruptor = LoadComponent<EnemyBase>(PrefabDir + "/Corruptor.prefab");
            EnemyBase swarm = LoadComponent<EnemyBase>(PrefabDir + "/SwarmUnit.prefab");
            if (colorEater == null || corruptor == null || swarm == null)
            {
                Debug.LogError("PlayVisualizer: missing base enemy prefabs — run Phase 3 and Phase 6 first.");
                return;
            }

            var deathPop = LoadComponent<DeathPop>("Assets/Prefabs/Visuals/DeathPop.prefab");
            var burst = LoadComponent<CollisionBurst>("Assets/Prefabs/Visuals/CollisionBurst.prefab");
            Material lineMat = LoadOrCreateMaterial("Assets/Materials/EnergyTrail.mat", "PlayVisualizer/EnergyTrail");

            EnemyConfig dasherConfig = MakeConfig(ConfigDir + "/DasherConfig.asset", c =>
            {
                c.Speed = 3.5f; c.Health = 4; c.Scale = 1.1f; c.ScoreValue = 250;
                c.DeathPaintRadius = 1.6f; c.DeathPaintIntensity = 1.4f;
                c.PlayerChaseRadius = 0f; c.TargetJitter = 1.0f;
                c.SeparationRadius = 1.0f; c.SeparationStrength = 1.0f;
                c.HitConsumeRadius = 1.6f; c.HitConsumeStrength = 1f; c.HitBurstDuration = 0.35f;
                // Dasher (spec10):
                c.DashSpeed = 18f; c.ChargeDuration = 0.45f; c.DashDuration = 0.35f;
                c.RecoverDuration = 0.4f; c.DashCooldown = 1.5f;
                c.DashConsumeRadius = 0.55f; c.DashConsumeStrengthPerSecond = 20f;
                c.BassDashBoost = 0.6f; c.BassConsumeBoost = 0.5f;
            });

            EnemyBase dasher = BuildDasherPrefab(PrefabDir + "/Dasher.prefab", dasherConfig, lineMat, deathPop, burst);

            EnemyConfig splitterConfig = MakeConfig(ConfigDir + "/SplitterConfig.asset", c =>
            {
                c.Speed = 2.2f; c.Health = 2; c.Scale = 1.65f; c.ScoreValue = 120; // a tad > Corruptor (1.5)
                c.ConsumeRadius = 0.7f; c.ConsumeStrengthPerSecond = 12f;
                c.DeathPaintRadius = 1.4f; c.DeathPaintIntensity = 1.2f;
                c.PlayerChaseRadius = 0f; c.TargetJitter = 2.0f;
                c.SeparationRadius = 1.2f; c.SeparationStrength = 1.2f;
                c.HitConsumeRadius = 1.4f; c.HitConsumeStrength = 0.9f; c.HitBurstDuration = 0.3f;
                // Splitter (spec10):
                // Fragment absolute size ≈ Scale × FragmentScale (1.65 × 0.36 ≈ 0.59, unchanged).
                c.SplitCountMin = 2; c.SplitCountMax = 3; c.FragmentScale = 0.36f;
                c.SplitSpread = 1.0f; c.FragmentPopSpeed = 4f; c.FragmentCoastTime = 0.25f;
                c.SplitConsumeRadius = 1.4f; c.SplitConsumeStrength = 0.8f; c.ShatterBurstRadius = 2.2f;
            });

            EnemyBase splitter = BuildSplitterPrefab(PrefabDir + "/Splitter.prefab", splitterConfig, lineMat, deathPop, burst);

            WriteEntries(spawner, colorEater, corruptor, swarm, dasher, splitter);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Phase 10 complete (Dasher + Splitter + full spawn table). Save the scene (Cmd+S).");
        }

        private static EnemyBase BuildDasherPrefab(string path, EnemyConfig config, Material lineMat,
            DeathPop deathPop, CollisionBurst burst)
        {
            var go = new GameObject("Dasher");

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            // Geometry visual (LineRenderers) on a Visual child — like the Color Eater.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var viz = visual.AddComponent<DasherVisualizer>();
            if (lineMat != null) AssignReference(viz, "_lineMaterial", lineMat);

            var dasher = go.AddComponent<Dasher>();
            AssignReference(dasher, "_config", config);
            if (deathPop != null) AssignReference(dasher, "_deathPopPrefab", deathPop);
            if (burst != null) AssignReference(dasher, "_collisionBurstPrefab", burst);
            AssignReference(dasher, "_visualRoot", visual.transform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<EnemyBase>();
        }

        private static EnemyBase BuildSplitterPrefab(string path, EnemyConfig config, Material lineMat,
            DeathPop deathPop, CollisionBurst burst)
        {
            var go = new GameObject("Splitter");

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            // Fractured-ring visual (LineRenderer arcs) on a Visual child.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var viz = visual.AddComponent<SplitterVisualizer>();
            if (lineMat != null) AssignReference(viz, "_lineMaterial", lineMat);

            var splitter = go.AddComponent<Splitter>();
            AssignReference(splitter, "_config", config);
            if (deathPop != null) AssignReference(splitter, "_deathPopPrefab", deathPop);
            if (burst != null) AssignReference(splitter, "_collisionBurstPrefab", burst);
            AssignReference(splitter, "_visualRoot", visual.transform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            // Self-reference: the prefab spawns copies of itself as fragments on death.
            var prefabSplitter = prefab.GetComponent<Splitter>();
            AssignReference(prefabSplitter, "_fragmentPrefab", prefabSplitter);
            EditorUtility.SetDirty(prefab);
            return prefabSplitter;
        }

        // ----------------------------------------------------------------- spawner table

        private static void WriteEntries(EnemySpawner spawner, EnemyBase colorEater, EnemyBase corruptor,
            EnemyBase swarm, EnemyBase dasher, EnemyBase splitter)
        {
            var so = new SerializedObject(spawner);
            SerializedProperty arr = so.FindProperty("_entries");
            arr.arraySize = 5;
            SetEntry(arr.GetArrayElementAtIndex(0), colorEater, 1.0f, 1, 1, 0f, 0f,
                EnemySpawner.MusicChannel.Energy, 0.5f, 0);
            SetEntry(arr.GetArrayElementAtIndex(1), corruptor, 0.25f, 1, 1, 0f, 0.2f,
                EnemySpawner.MusicChannel.Bass, 0.85f, 4);
            SetEntry(arr.GetArrayElementAtIndex(2), swarm, 0.5f, 6, 10, 1.8f, 0.35f,
                EnemySpawner.MusicChannel.Treble, 0.85f, 0);
            // Dasher: unlocks mid-song, capped at 2 alive, energetic sections bring more.
            SetEntry(arr.GetArrayElementAtIndex(3), dasher, 0.3f, 1, 1, 0f, 0.4f,
                EnemySpawner.MusicChannel.Energy, 0.4f, 2);
            // Splitter: flux-driven; capped (splits bypass the cap, so keep base spawns low).
            SetEntry(arr.GetArrayElementAtIndex(4), splitter, 0.4f, 1, 1, 0f, 0.3f,
                EnemySpawner.MusicChannel.Flux, 0.7f, 4);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEntry(SerializedProperty el, EnemyBase prefab, float weight,
            int gMin, int gMax, float spread, float minProgress,
            EnemySpawner.MusicChannel channel, float influence, int maxAlive)
        {
            el.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
            el.FindPropertyRelative("Weight").floatValue = weight;
            el.FindPropertyRelative("GroupMin").intValue = gMin;
            el.FindPropertyRelative("GroupMax").intValue = gMax;
            el.FindPropertyRelative("GroupSpread").floatValue = spread;
            el.FindPropertyRelative("MinSongProgress").floatValue = minProgress;
            el.FindPropertyRelative("Channel").enumValueIndex = (int)channel;
            el.FindPropertyRelative("MusicInfluence").floatValue = influence;
            el.FindPropertyRelative("MaxAlive").intValue = maxAlive;
        }

        // ----------------------------------------------------------------- util

        private static EnemyConfig MakeConfig(string path, System.Action<EnemyConfig> setup)
        {
            EnemyConfig cfg = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
            bool created = false;
            if (cfg == null) { cfg = ScriptableObject.CreateInstance<EnemyConfig>(); created = true; }
            setup(cfg);
            if (created) AssetDatabase.CreateAsset(cfg, path);
            else EditorUtility.SetDirty(cfg);
            return cfg;
        }

        private static T LoadComponent<T>(string prefabPath) where T : Component
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return go != null ? go.GetComponent<T>() : null;
        }

        private static Material LoadOrCreateMaterial(string path, string shaderName)
        {
            Shader sh = Shader.Find(shaderName);
            if (sh == null)
            {
                Debug.LogError($"PlayVisualizer: shader '{shaderName}' not found — let Unity compile, then re-run.");
                return null;
            }
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            else if (m.shader != sh) m.shader = sh;
            return m;
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
