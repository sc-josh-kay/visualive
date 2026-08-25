using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayVisualizer.Enemies;
using PlayVisualizer.Gameplay;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// One-click setup for Phase 3: enemy prefab, spawner, death-pop effect, and the
    /// player Health / ScoreManager wiring. Re-runnable.
    ///
    /// Menu: PlayVisualizer → Build Enemies + Spawner (Phase 3)
    /// </summary>
    public static class PlayVisualizerSetupPhase3
    {
        private const string ArtDir = "Assets/Art";
        private const string ConfigDir = "Assets/ScriptableObjects";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemies/Enemy.prefab";
        private const string DeathPopPrefabPath = "Assets/Prefabs/Visuals/DeathPop.prefab";

        private static readonly Color EnemyColor = new Color(1f, 0.35f, 0.08f, 1f); // orange-red

        [MenuItem("PlayVisualizer/Build Enemies + Spawner (Phase 3)")]
        public static void BuildPhase3()
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets", "ScriptableObjects");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Enemies");
            EnsureFolder("Assets/Prefabs", "Visuals");

            Sprite diamond = CreateDiamondSprite(ArtDir + "/EnemyDiamond.png", 128);
            Sprite circle = LoadOrGenerateCircle(ArtDir + "/ProjectileCircle.png", 64);

            EnemyConfig enemyConfig = LoadOrCreate<EnemyConfig>(ConfigDir + "/EnemyConfig.asset");
            SpawnerConfig spawnerConfig = LoadOrCreate<SpawnerConfig>(ConfigDir + "/SpawnerConfig.asset");

            GameObject deathPop = BuildDeathPopPrefab(circle);
            DeathPop deathPopComponent = deathPop.GetComponent<DeathPop>();

            CollisionBurst collisionBurst = BuildCollisionBurstPrefab();

            GameObject enemyPrefab = BuildEnemyPrefab(diamond, enemyConfig, deathPopComponent, collisionBurst);
            EnemyBase enemyComponent = enemyPrefab.GetComponent<ColorEater>();

            EnsurePlayerHealth();
            EnsureScoreManager();
            BuildSpawnerInScene(spawnerConfig, enemyComponent);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Phase 3 setup complete. Save the scene (Cmd+S) and press Play.");
        }

        // ---------------------------------------------------------------- prefabs

        private static GameObject BuildDeathPopPrefab(Sprite sprite)
        {
            var go = new GameObject("DeathPop");
            go.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 6;

            go.AddComponent<DeathPop>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, DeathPopPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static CollisionBurst BuildCollisionBurstPrefab()
        {
            Shader sh = Shader.Find("PlayVisualizer/CollisionBurst");
            if (sh == null)
            {
                Debug.LogError("PlayVisualizer: CollisionBurst shader not found — let Unity compile " +
                               "Assets/Shaders first, then re-run.");
                return null;
            }

            const string matPath = "Assets/Materials/CollisionBurst.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != sh)
            {
                mat.shader = sh;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "CollisionBurst";
            Object.DestroyImmediate(go.GetComponent<MeshCollider>());
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.AddComponent<CollisionBurst>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Visuals/CollisionBurst.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<CollisionBurst>();
        }

        private static GameObject BuildEnemyPrefab(Sprite sprite, EnemyConfig config, DeathPop deathPop,
            CollisionBurst collisionBurst)
        {
            var go = new GameObject("Enemy");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = EnemyColor;
            sr.sortingOrder = 8;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            var enemy = go.AddComponent<ColorEater>();
            AssignReference(enemy, "_config", config);
            AssignReference(enemy, "_deathPopPrefab", deathPop);
            if (collisionBurst != null) AssignReference(enemy, "_collisionBurstPrefab", collisionBurst);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, EnemyPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ---------------------------------------------------------------- scene

        private static void EnsurePlayerHealth()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogWarning("PlayVisualizer: no 'Player' found — run the Phase 2 setup first.");
                return;
            }
            if (player.GetComponent<Health>() == null)
            {
                player.AddComponent<Health>();
            }
        }

        private static void EnsureScoreManager()
        {
            if (Object.FindFirstObjectByType<ScoreManager>() != null)
            {
                return;
            }
            var go = new GameObject("GameSystems");
            go.AddComponent<ScoreManager>();
        }

        private static void BuildSpawnerInScene(SpawnerConfig config, EnemyBase enemyPrefab)
        {
            GameObject existing = GameObject.Find("EnemySpawner");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var go = new GameObject("EnemySpawner");
            var spawner = go.AddComponent<EnemySpawner>();

            AssignReference(spawner, "_config", config);
            AssignReference(spawner, "_enemyPrefab", enemyPrefab);

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                AssignReference(spawner, "_player", player.transform);
            }
        }

        // ---------------------------------------------------------------- sprites

        private static Sprite CreateDiamondSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) / 2f;
            float r = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = Mathf.Abs(x - c) + Mathf.Abs(y - c) <= r;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return WriteSprite(tex, path);
        }

        private static Sprite LoadOrGenerateCircle(string path, int size)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) / 2f;
            float radius = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    bool inside = dx * dx + dy * dy <= radius * radius;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
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

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
