using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlayVisualizer.Player;
using PlayVisualizer.Visuals;
using PlayVisualizer.Weapons;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Weapon system setup (spec7). Replaces the old single PlayerShooter with the auto-fire
    /// WeaponController + weapon components, and gives the projectile prefab an additive energy
    /// TrailRenderer (the glowing "energy" filament). Re-runnable. Requires a Player in the scene.
    ///
    /// Menu: PlayVisualizer → Build Weapon System (spec7)
    /// </summary>
    public static class PlayVisualizerSetupWeapons
    {
        private const string ProjectilePath = "Assets/Prefabs/Projectiles/Projectile.prefab";
        private const string VortexPath = "Assets/Prefabs/Projectiles/Vortex.prefab";
        private const string ImpactBloomPath = "Assets/Prefabs/Visuals/ImpactBloom.prefab";
        private const string TrailMatPath = "Assets/Materials/EnergyTrail.mat";
        private const string SwirlMatPath = "Assets/Materials/VortexSwirl.mat";
        private const string SpriteMatPath = "Assets/Materials/EnergySprite.mat";
        private const string SoftGlowPath = "Assets/Art/SoftGlow.png";

        [MenuItem("PlayVisualizer/Build Weapon System (spec7)")]
        public static void BuildWeapons()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("PlayVisualizer: no 'Player' in scene — run the base setup first.");
                return;
            }

            Material trailMat = LoadOrCreateMaterial(TrailMatPath, "PlayVisualizer/EnergyTrail");
            Material spriteMat = LoadOrCreateMaterial(SpriteMatPath, "PlayVisualizer/EnergySprite");
            Sprite softGlow = LoadOrGenerateSoftGlow(SoftGlowPath);
            if (spriteMat != null && softGlow != null)
            {
                spriteMat.mainTexture = softGlow.texture; // ensure the glow renders even if the sprite path doesn't bind _MainTex
                EditorUtility.SetDirty(spriteMat);
            }
            DeathPop impactBloom = BuildImpactBloomPrefab(softGlow, spriteMat);
            Projectile projectile = EnhanceProjectilePrefab(trailMat, spriteMat, softGlow, impactBloom);
            if (projectile == null) return;

            Material swirlMat = LoadOrCreateMaterial(SwirlMatPath, "PlayVisualizer/VortexSwirl");
            Vortex vortexPrefab = swirlMat != null ? BuildVortexPrefab(swirlMat) : null;

            // Remove the old shooter (now a missing script after the refactor).
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player);

            var controller = player.GetComponent<WeaponController>();
            if (controller == null) controller = player.AddComponent<WeaponController>();

            // Rebuild the weapon set fresh each run so tuned C# defaults always apply.
            foreach (WeaponBase existing in player.GetComponents<WeaponBase>())
            {
                Object.DestroyImmediate(existing);
            }
            AddWeapon<PulseStream>(player, projectile);
            AddWeapon<Scatter>(player, projectile);
            AddWeapon<GrowthStream>(player, projectile);
            AddWeapon<RapidStream>(player, projectile);
            AddWeapon<HomingSwarm>(player, projectile);

            var vortexWave = player.AddComponent<VortexWave>();
            if (vortexPrefab != null) AssignReference(vortexWave, "_vortexPrefab", vortexPrefab);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: weapon system built (Pulse / Scatter / Growth / Rapid / Homing / Vortex). " +
                      "Save the scene (Cmd+S) and press Play. Auto-fires while aiming; Space cycles weapons.");
        }

        private static void AddWeapon<T>(GameObject player, Projectile projectile) where T : WeaponBase
        {
            T weapon = player.AddComponent<T>();
            AssignReference(weapon, "_projectilePrefab", projectile);
        }

        private static Vortex BuildVortexPrefab(Material swirlMat)
        {
            var go = new GameObject("Vortex");
            var vortex = go.AddComponent<Vortex>();

            var swirl = GameObject.CreatePrimitive(PrimitiveType.Quad);
            swirl.name = "Swirl";
            Object.DestroyImmediate(swirl.GetComponent<MeshCollider>());
            swirl.transform.SetParent(go.transform, false);
            swirl.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            var mr = swirl.GetComponent<MeshRenderer>();
            mr.sharedMaterial = swirlMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 7;
            AssignReference(vortex, "_swirl", mr);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, VortexPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Vortex>();
        }

        private static Projectile EnhanceProjectilePrefab(Material trailMat, Material spriteMat,
            Sprite softGlow, DeathPop impactBloom)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ProjectilePath);
            if (root == null)
            {
                Debug.LogError($"PlayVisualizer: projectile prefab not found at {ProjectilePath} — run the base setup first.");
                return null;
            }

            // Physics stays on the root (straight, predictable). Strip old root visuals.
            var rootSprite = root.GetComponent<SpriteRenderer>();
            if (rootSprite != null) Object.DestroyImmediate(rootSprite);
            var rootTrail = root.GetComponent<TrailRenderer>();
            if (rootTrail != null) Object.DestroyImmediate(rootTrail);
            var col = root.GetComponent<CircleCollider2D>();
            if (col != null) col.radius = 0.22f;

            var oldVisual = root.transform.Find("Visual");
            if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);

            // Visual child: the "alive" look (wobbles/pulses), never affects flight.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var pv = visual.AddComponent<ProjectileVisual>();

            var trail = visual.AddComponent<TrailRenderer>();
            trail.time = 0.18f;                 // short-lived → energy, not paint
            trail.startWidth = 0.22f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.02f;    // smooth enough to show the gentle wobble
            trail.numCapVertices = 4;
            trail.autodestruct = false;
            trail.emitting = true;
            trail.generateLightingData = false;
            trail.sharedMaterial = trailMat;
            trail.sortingOrder = 9;

            SpriteRenderer halo = MakeGlowSprite("Halo", visual.transform, softGlow, spriteMat, 0.6f, 8);
            SpriteRenderer core = MakeGlowSprite("Core", visual.transform, softGlow, spriteMat, 0.3f, 9);

            AssignReference(pv, "_core", core);
            AssignReference(pv, "_halo", halo);
            AssignReference(pv, "_trail", trail);

            var proj = root.GetComponent<Projectile>();
            AssignReference(proj, "_visual", pv);
            if (impactBloom != null) AssignReference(proj, "_impactBloomPrefab", impactBloom);

            PrefabUtility.SaveAsPrefabAsset(root, ProjectilePath);
            PrefabUtility.UnloadPrefabContents(root);

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);
            return asset != null ? asset.GetComponent<Projectile>() : null;
        }

        private static SpriteRenderer MakeGlowSprite(string name, Transform parent, Sprite sprite,
            Material mat, float scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            return sr;
        }

        private static DeathPop BuildImpactBloomPrefab(Sprite softGlow, Material spriteMat)
        {
            var go = new GameObject("ImpactBloom");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = softGlow;
            sr.sharedMaterial = spriteMat;
            sr.sortingOrder = 9;
            go.AddComponent<DeathPop>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ImpactBloomPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<DeathPop>();
        }

        private static Sprite LoadOrGenerateSoftGlow(string path)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) / 2f;
            float maxR = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c) / maxR;
                    float dy = (y - c) / maxR;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);       // 0 center, 1 at edge
                    float a = Mathf.Clamp01(Mathf.Exp(-d * d * 5f)); // soft radial glow
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
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

        private static Material LoadOrCreateMaterial(string path, string shaderName)
        {
            Shader sh = Shader.Find(shaderName);
            if (sh == null)
            {
                Debug.LogError($"PlayVisualizer: shader '{shaderName}' not found — let Unity compile shaders, then re-run.");
                return null;
            }
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != sh)
            {
                m.shader = sh;
            }
            return m;
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
    }
}
