using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using PlayVisualizer.Enemies;
using PlayVisualizer.Gameplay;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// One-click setup for Phase 4: HUD (score + health bar), Game Over panel with a working
    /// Restart button, the GameManager wiring, and a small arena-size bump. Re-runnable.
    ///
    /// Menu: PlayVisualizer → Build Game Loop UI (Phase 4)
    /// </summary>
    public static class PlayVisualizerSetupPhase4
    {
        private static readonly Color Cyan = new Color(0f, 1f, 1f, 1f);
        private static readonly Color Magenta = new Color(1f, 0.16f, 1f, 1f);
        private static Font _font;

        [MenuItem("PlayVisualizer/Build Game Loop UI (Phase 4)")]
        public static void BuildPhase4()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            DestroyIfExists("GameHUD");
            DestroyIfExists("GameManager");
            DestroyIfExists("EventSystem");

            EnsureEventSystem();

            // --- Canvas ---
            var canvasGo = new GameObject("GameHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // --- HUD: score (top-left) ---
            Text scoreText = CreateText(canvasGo.transform, "ScoreText", "SCORE  0", 36, Cyan,
                TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, -24), new Vector2(700, 60));

            // --- HUD: health bar (top-left, under score) ---
            var barBg = CreateImage(canvasGo.transform, "HealthBarBg", new Color(0.1f, 0.1f, 0.12f, 0.85f),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(30, -84), new Vector2(320, 26));
            var fillGo = new GameObject("HealthBarFill", typeof(Image));
            fillGo.transform.SetParent(barBg.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var healthFill = fillGo.GetComponent<Image>();
            healthFill.sprite = DefaultUISprite();
            healthFill.color = Magenta;
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFill.fillAmount = 1f;

            // --- Game Over panel ---
            var panel = CreateImage(canvasGo.transform, "GameOverPanel", new Color(0f, 0f, 0f, 0.72f),
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            // Named "GameOverText" for back-compat with existing scenes; the label is the no-death
            // results title (GameManager also relabels it at runtime).
            CreateText(panel.transform, "GameOverText", "SONG COMPLETE", 84, Magenta,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 120), new Vector2(900, 140));

            Text finalScore = CreateText(panel.transform, "FinalScoreText", "SCORE: 0", 44, Cyan,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 20), new Vector2(900, 80));

            Button restartBtn = CreateButton(panel.transform, "RestartButton", "RESTART",
                new Vector2(0, -90), new Vector2(280, 80));

            // --- GameManager + wiring ---
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();

            // No player death in the gameplay MVP: GameManager no longer tracks Health. The bar is
            // repurposed as the live coverage bar; the panel is the (future) results panel.
            AssignReference(gm, "_scoreText", scoreText);
            AssignReference(gm, "_coverageFill", healthFill);
            AssignReference(gm, "_resultsPanel", panel.gameObject);
            AssignReference(gm, "_finalScoreText", finalScore);

            // Persistent onClick → GameManager.Restart (survives play/edit).
            UnityEventTools.AddPersistentListener(restartBtn.onClick, gm.Restart);

            panel.gameObject.SetActive(false); // GameManager also hides it at runtime.

            // --- Arena bump ---
            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 11f;
            }
            var spawnerConfig = AssetDatabase.LoadAssetAtPath<SpawnerConfig>(
                "Assets/ScriptableObjects/SpawnerConfig.asset");
            if (spawnerConfig != null)
            {
                spawnerConfig.SpawnDistance = 20f;
                EditorUtility.SetDirty(spawnerConfig);
            }

            EnsureActiveSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Phase 4 setup complete. Save the scene (Cmd+S) and press Play.");
        }

        // ---------------------------------------------------------------- UI builders

        private static Text CreateText(Transform parent, string name, string content, int fontSize,
            Color color, TextAnchor alignment, Vector2 aMin, Vector2 aMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = _font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
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

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var btnImg = go.GetComponent<Image>();
            btnImg.sprite = DefaultUISprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            SetRect(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                anchoredPos, size);

            CreateText(go.transform, "Label", label, 32, Cyan, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            return go.GetComponent<Button>();
        }

        private static RectTransform SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        // ---------------------------------------------------------------- util

        // Restart reloads the scene via SceneManager, which requires the scene to be listed
        // in Build Settings — otherwise it errors at runtime. Add it if missing.
        private static void EnsureActiveSceneInBuildSettings()
        {
            string path = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("PlayVisualizer: active scene isn't saved yet; save it so Restart works.");
                return;
            }

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path))
            {
                return;
            }
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // Unity's built-in rounded UI sprite. Required for Image.Type.Filled/Sliced to render;
        // an Image with a null sprite ignores fillAmount and slicing.
        private static Sprite DefaultUISprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void DestroyIfExists(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
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
