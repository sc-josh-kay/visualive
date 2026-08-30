using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PlayVisualizer.Gameplay;
using PlayVisualizer.Player;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// One-click setup for spec9 — Visualizer Momentum & Overdrive. Re-runnable. Builds/wires:
    ///   * OverdriveConfig.asset (created with defaults if missing),
    ///   * a VisualizerMomentum controller object (state machine),
    ///   * RadialPaintEmitter + PaintWaveSystem + PlayerOverdriveVFX on the Player (+ a glow child),
    ///   * a Momentum HUD bar under the coverage bar, wired to GameManager.
    ///
    /// Requires the scene from earlier phases (Player, GameHUD, GameManager). Independent of the
    /// Phase 3 / Phase 6 enemy setups, so run order relative to those doesn't matter.
    ///
    /// Menu: PlayVisualizer → Build Overdrive System (spec9)
    /// </summary>
    public static class PlayVisualizerSetupPhase9
    {
        private const string ConfigPath = "Assets/ScriptableObjects/OverdriveConfig.asset";
        private static readonly Color Cyan = new Color(0f, 0.85f, 1f, 1f);
        private static Font _font;

        [MenuItem("PlayVisualizer/Build Overdrive System (spec9)")]
        public static void BuildPhase9()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            OverdriveConfig config = LoadOrCreateConfig();

            // --- VisualizerMomentum controller ---
            GameObject controllerGo = GameObject.Find("VisualizerMomentum");
            if (controllerGo == null) controllerGo = new GameObject("VisualizerMomentum");
            var momentum = controllerGo.GetComponent<VisualizerMomentum>();
            if (momentum == null) momentum = controllerGo.AddComponent<VisualizerMomentum>();
            AssignReference(momentum, "_config", config);

            // --- Player components ---
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("PlayVisualizer: no 'Player' in the scene — run the base/Phase setups first.");
                return;
            }

            var radial = player.GetComponent<RadialPaintEmitter>();
            if (radial == null) radial = player.AddComponent<RadialPaintEmitter>();
            AssignReference(radial, "_config", config);

            var waves = player.GetComponent<PaintWaveSystem>();
            if (waves == null) waves = player.AddComponent<PaintWaveSystem>();
            AssignReference(waves, "_config", config);

            var vfx = player.GetComponent<PlayerOverdriveVFX>();
            if (vfx == null) vfx = player.AddComponent<PlayerOverdriveVFX>();
            AssignReference(vfx, "_config", config);

            // The ship's own SpriteRenderer (on the Player root) — tinted neon rainbow during Overdrive.
            var shipRenderer = player.GetComponent<SpriteRenderer>();
            if (shipRenderer != null) AssignReference(vfx, "_shipRenderer", shipRenderer);
            else Debug.LogWarning("PlayVisualizer: Player has no SpriteRenderer — skipping rainbow ship tint.");

            // Glow child (additive SoftGlow), rebuilt fresh each run.
            Transform existingGlow = player.transform.Find("OverdriveGlow");
            if (existingGlow != null) Object.DestroyImmediate(existingGlow.gameObject);
            SpriteRenderer glow = BuildGlowChild(player.transform);
            AssignReference(vfx, "_glow", glow);

            // --- HUD: Momentum bar under the coverage bar ---
            BuildMomentumHud();

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Overdrive (spec9) setup complete. Save the scene (Cmd+S) and press Play.");
        }

        // ---------------------------------------------------------------- config

        private static OverdriveConfig LoadOrCreateConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<OverdriveConfig>(ConfigPath);
            if (cfg == null)
            {
                Directory.CreateDirectory("Assets/ScriptableObjects");
                cfg = ScriptableObject.CreateInstance<OverdriveConfig>();
                AssetDatabase.CreateAsset(cfg, ConfigPath);
            }
            return cfg;
        }

        // ---------------------------------------------------------------- glow

        private static SpriteRenderer BuildGlowChild(Transform parent)
        {
            var go = new GameObject("OverdriveGlow", typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.05f); // just behind the ship
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = LoadOrGenerateSoftGlow("Assets/Art/SoftGlow.png");
            sr.sharedMaterial = LoadOrCreateMaterial("Assets/Materials/EnergySprite.mat",
                "PlayVisualizer/EnergySprite");
            sr.color = new Color(0f, 0.85f, 1f, 0f); // starts invisible; the component drives it
            sr.sortingOrder = -5;                    // behind the ship silhouette
            sr.enabled = false;
            return sr;
        }

        // ---------------------------------------------------------------- HUD

        private static void BuildMomentumHud()
        {
            GameObject hud = GameObject.Find("GameHUD");
            if (hud == null)
            {
                Debug.LogWarning("PlayVisualizer: no 'GameHUD' — run 'Build Game Loop UI (Phase 4)' first; " +
                                 "skipping the Momentum bar for now.");
                return;
            }

            // Rebuild fresh each run.
            DestroyChild(hud.transform, "MomentumLabel");
            DestroyChild(hud.transform, "MomentumBarBg");

            Text label = CreateText(hud.transform, "MomentumLabel", "MOMENTUM", 22, Cyan,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, -120), new Vector2(320, 26));

            var barBg = CreateImage(hud.transform, "MomentumBarBg", new Color(0.1f, 0.1f, 0.12f, 0.85f),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, -150), new Vector2(320, 22));

            var fillGo = new GameObject("MomentumBarFill", typeof(Image));
            fillGo.transform.SetParent(barBg.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = DefaultUISprite();
            fill.color = Cyan;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;

            GameObject gmGo = GameObject.Find("GameManager");
            if (gmGo != null)
            {
                var gm = gmGo.GetComponent<GameManager>();
                if (gm != null)
                {
                    AssignReference(gm, "_momentumFill", fill);
                    AssignReference(gm, "_momentumLabel", label);
                }
            }
        }

        // ---------------------------------------------------------------- UI builders (local copies)

        private static Text CreateText(Transform parent, string name, string content, int fontSize,
            Color color, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = _font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.LowerLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(go, aMin, aMax, pivot, anchoredPos, size);
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Color color,
            Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = DefaultUISprite();
            img.type = Image.Type.Sliced;
            img.color = color;
            SetRect(go, aMin, aMax, pivot, anchoredPos, size);
            return img;
        }

        private static void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static Sprite DefaultUISprite() =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // ---------------------------------------------------------------- asset helpers

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

        private static Sprite LoadOrGenerateSoftGlow(string path)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) / 2f;
            float maxR = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c) / maxR, dy = (y - c) / maxR;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Exp(-d * d * 5f))));
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

        private static void DestroyChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
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
