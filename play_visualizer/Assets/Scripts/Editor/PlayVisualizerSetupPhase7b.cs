using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using PlayVisualizer.Audio;
using PlayVisualizer.Enemies;
using PlayVisualizer.Gameplay;
using PlayVisualizer.Visuals;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// One-click setup for the psychedelic feedback visualizer (Phase 7b, Drop A): Emitter
    /// layer, player seed (blob + trail), feedback camera, fullscreen display quad, and the
    /// FeedbackController. Removes the old radial background pulse and ambient particles.
    /// Re-runnable.
    ///
    /// Menu: PlayVisualizer → Build Feedback Visualizer (Phase 7b)
    /// </summary>
    public static class PlayVisualizerSetupPhase7b
    {
        private const string MatDir = "Assets/Materials";
        private const string ArtDir = "Assets/Art";
        private const string DisplayMatPath = "Assets/Materials/VisualizerDisplay.mat";
        private const string TrailMatPath = "Assets/Materials/SeedTrail.mat";

        [MenuItem("PlayVisualizer/Build Feedback Visualizer (Phase 7b)")]
        public static void BuildPhase7b()
        {
            Shader feedbackShader = Shader.Find("PlayVisualizer/Feedback");
            Shader kaleidoShader = Shader.Find("PlayVisualizer/Kaleidoscope");
            if (feedbackShader == null || kaleidoShader == null)
            {
                Debug.LogError("PlayVisualizer: shaders not found. Let Unity compile " +
                               "Assets/Shaders/*.shader first, then re-run.");
                return;
            }

            EnsureFolder("Assets", "Materials");
            int emitterLayer = AddLayer("Emitter");
            if (emitterLayer < 0)
            {
                return;
            }

            // Retire the old background pulse + ambient particles (kept: beat shockwave).
            DestroyIfExists("BackgroundPulse");
            DestroyIfExists("TrebleParticles");

            // Make the beat shockwave subtle (it now originates from the player).
            var visualizerConfig = AssetDatabase.LoadAssetAtPath<VisualizerConfig>(
                "Assets/ScriptableObjects/VisualizerConfig.asset");
            if (visualizerConfig != null)
            {
                visualizerConfig.ShockwaveColor = new Color(0.12f, 0.4f, 0.42f, 1f);
                EditorUtility.SetDirty(visualizerConfig);
            }

            Sprite radial = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/BackgroundRadial.png");

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("PlayVisualizer: no Main Camera found.");
                return;
            }
            // Main camera must NOT draw the raw emitter seed directly.
            mainCam.cullingMask &= ~(1 << emitterLayer);

            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("PlayVisualizer: no Player found — run earlier phases first.");
                return;
            }

            BuildPlayerSeed(player, emitterLayer, radial);

            // Enemy + kill seeding, and the test-mode toggle.
            DeathPop seedBurst = BuildSeedBurstPrefab(radial, emitterLayer);
            AddEnemySeeding(radial, emitterLayer, seedBurst);
            EnsureTestMode();

            Camera feedbackCam = BuildFeedbackCamera(mainCam, emitterLayer);
            Material kaleidoMat = CreateMaterial(DisplayMatPath, kaleidoShader, m =>
            {
                m.SetFloat("_Segments", 1f);
                m.SetFloat("_Brightness", 1.1f);
            });
            Renderer display = BuildDisplayQuad(kaleidoMat);

            // Controller
            DestroyIfExists("FeedbackVisualizer");
            var go = new GameObject("FeedbackVisualizer");
            var controller = go.AddComponent<FeedbackController>();
            AssignReference(controller, "_mainCamera", mainCam);
            AssignReference(controller, "_feedbackCamera", feedbackCam);
            AssignReference(controller, "_player", player.transform);
            AssignReference(controller, "_displayRenderer", display);
            AssignReference(controller, "_feedbackShader", feedbackShader);
            AssignReference(controller, "_kaleidoscopeShader", kaleidoShader);
            AssignReference(controller, "_analyzer", Object.FindFirstObjectByType<AudioAnalyzer>());

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Feedback visualizer (kaleidoscope + seeding + test mode) set up. " +
                      "Save the scene (Cmd+S) and press Play. T = invincible, M = music toggle, F1 = audio bars.");
        }

        // ---------------------------------------------------------------- pieces

        private static void BuildPlayerSeed(GameObject player, int emitterLayer, Sprite radial)
        {
            Transform existing = player.transform.Find("PlayerVisualSeed");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var seed = new GameObject("PlayerVisualSeed");
            seed.layer = emitterLayer;
            seed.transform.SetParent(player.transform, false);
            seed.transform.localPosition = Vector3.zero;
            seed.transform.localScale = new Vector3(1.0f, 1.0f, 1f);

            var sr = seed.AddComponent<SpriteRenderer>();
            sr.sprite = radial;
            sr.color = new Color(0.45f, 0.85f, 0.9f, 1f);
            sr.sortingOrder = 2;

            Material trailMat = CreateMaterial(TrailMatPath, Shader.Find("Sprites/Default"), null);
            var trail = seed.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMat;
            trail.time = 0.6f;
            trail.startWidth = 0.6f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.numCapVertices = 4;
            trail.sortingOrder = 1;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.4f, 0.8f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = grad;
        }

        private static DeathPop BuildSeedBurstPrefab(Sprite radial, int emitterLayer)
        {
            var go = new GameObject("SeedBurst");
            go.layer = emitterLayer;
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = radial;
            sr.color = Color.white;
            sr.sortingOrder = 3;

            var pop = go.AddComponent<DeathPop>();
            AssignFloat(pop, "_duration", 0.3f);
            AssignFloat(pop, "_endScaleMultiplier", 3f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Visuals/SeedBurst.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<DeathPop>();
        }

        private static void AddEnemySeeding(Sprite radial, int emitterLayer, DeathPop seedBurst)
        {
            const string path = "Assets/Prefabs/Enemies/Enemy.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            Transform existing = root.transform.Find("EnemySeed");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var seed = new GameObject("EnemySeed");
            seed.layer = emitterLayer;
            seed.transform.SetParent(root.transform, false);
            seed.transform.localScale = Vector3.one * 0.7f;
            var sr = seed.AddComponent<SpriteRenderer>();
            sr.sprite = radial;
            sr.color = new Color(1f, 0.45f, 0.15f, 0.5f);
            sr.sortingOrder = 2;

            // NOTE (gameplay MVP): the enemy '_seedBurstPrefab' field was removed — enemy death now
            // injects its explosion through VisualizerField.Paint, not the legacy feedback emitter.
            // This step only leaves the visual seed child for the old FeedbackController toggle.

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void EnsureTestMode()
        {
            if (Object.FindFirstObjectByType<TestMode>() != null)
            {
                return;
            }
            GameObject sys = GameObject.Find("GameSystems");
            if (sys == null)
            {
                sys = new GameObject("GameSystems");
            }
            sys.AddComponent<TestMode>();
        }

        private static Camera BuildFeedbackCamera(Camera mainCam, int emitterLayer)
        {
            DestroyIfExists("FeedbackCamera");
            var go = new GameObject("FeedbackCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = mainCam.orthographicSize;
            go.transform.SetPositionAndRotation(mainCam.transform.position, mainCam.transform.rotation);
            cam.cullingMask = 1 << emitterLayer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.depth = -10;
            cam.allowHDR = true;
            cam.allowMSAA = false;
            // targetTexture is assigned by the controller at runtime.
            return cam;
        }

        private static Renderer BuildDisplayQuad(Material material)
        {
            DestroyIfExists("VisualizerDisplay");
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "VisualizerDisplay";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.position = new Vector3(0f, 0f, 5f);
            var mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        // ---------------------------------------------------------------- util

        private static Material CreateMaterial(string path, Shader shader, System.Action<Material> configure)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            var mat = new Material(shader);
            configure?.Invoke(mat);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static int AddLayer(string name)
        {
            Object asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tagManager = new SerializedObject(asset);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == name)
                {
                    return i;
                }
            }
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty e = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(e.stringValue))
                {
                    e.stringValue = name;
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }
            Debug.LogError("PlayVisualizer: no free layer slot for 'Emitter'.");
            return -1;
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
