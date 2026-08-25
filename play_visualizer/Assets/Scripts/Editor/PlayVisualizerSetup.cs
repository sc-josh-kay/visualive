using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayVisualizer.Player;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// One-click scene/asset setup helpers, so the tedious wiring is deterministic and
    /// version-controlled instead of hand-clicked. Look/feel values (colors, bloom, tuning)
    /// are left deliberately simple here — those are meant to be tweaked by hand afterward.
    ///
    /// Menu: PlayVisualizer → Build Player + Projectile (Phase 2)
    /// </summary>
    public static class PlayVisualizerSetup
    {
        private const string ArtDir = "Assets/Art";
        private const string ConfigDir = "Assets/ScriptableObjects";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectiles/Projectile.prefab";

        private static readonly Color PlayerColor = new Color(0f, 1f, 1f, 1f);      // cyan
        private static readonly Color ProjectileColor = new Color(1f, 0.16f, 1f, 1f); // magenta

        [MenuItem("PlayVisualizer/Build Player + Projectile (Phase 2)")]
        public static void BuildPhase2()
        {
            // Remove any leftover in-scene Projectile instance (e.g. a manual one) so the
            // arena doesn't ship with a static bullet sitting in it.
            GameObject strayProjectile = GameObject.Find("Projectile");
            if (strayProjectile != null)
            {
                Object.DestroyImmediate(strayProjectile);
            }

            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets", "ScriptableObjects");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Projectiles");

            // --- Sprites (generated so we don't depend on built-in sprite assets) ---
            Sprite triangleSprite = CreateTriangleSprite(ArtDir + "/PlayerTriangle.png", 128);
            Sprite circleSprite = CreateCircleSprite(ArtDir + "/ProjectileCircle.png", 64);

            // --- PlayerConfig asset ---
            PlayerConfig config = LoadOrCreateConfig(ConfigDir + "/PlayerConfig.asset");

            // --- Projectile prefab ---
            GameObject projectilePrefab = BuildProjectilePrefab(circleSprite);
            Projectile projectileComponent = projectilePrefab.GetComponent<Projectile>();

            // --- Player in the currently open scene ---
            BuildPlayerInScene(triangleSprite, config, projectileComponent);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Phase 2 setup complete. Save the scene (Cmd+S) and press Play.");
        }

        // ---------------------------------------------------------------- assets

        private static PlayerConfig LoadOrCreateConfig(string path)
        {
            PlayerConfig config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PlayerConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            return config;
        }

        private static GameObject BuildProjectilePrefab(Sprite sprite)
        {
            var go = new GameObject("Projectile");
            go.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = ProjectileColor;
            sr.sortingOrder = 5;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            go.AddComponent<Projectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ProjectilePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void BuildPlayerInScene(Sprite sprite, PlayerConfig config, Projectile projectilePrefab)
        {
            // Replace any previous Player so re-running is safe.
            GameObject existing = GameObject.Find("Player");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var go = new GameObject("Player");
            go.transform.position = Vector3.zero;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = PlayerColor;
            sr.sortingOrder = 10;

            // Adding the controller pulls in Rigidbody2D + PlayerInputReader via RequireComponent.
            var controller = go.AddComponent<PlayerController>();

            var rb = go.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            // Assign private [SerializeField] references via SerializedObject. The weapon system
            // (WeaponController + weapons) is added by the weapons setup, not here.
            AssignReference(controller, "_config", config);
            _ = projectilePrefab; // built for the weapons setup to reference

            Selection.activeGameObject = go;
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

        // ---------------------------------------------------------------- sprites

        private static Sprite CreateTriangleSprite(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int r = 0; r < size; r++)      // r = 0 at bottom, size-1 at top (apex)
            {
                float halfWidth = (size / 2f) * (1f - (float)r / (size - 1));
                float cx = size / 2f;
                for (int x = 0; x < size; x++)
                {
                    bool inside = x >= cx - halfWidth && x <= cx + halfWidth;
                    tex.SetPixel(x, r, inside ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return WriteSprite(tex, path);
        }

        private static Sprite CreateCircleSprite(string path, int size)
        {
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

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
