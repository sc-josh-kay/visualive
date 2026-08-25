using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PlayVisualizer.Audio;
using PlayVisualizer.Gameplay;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Lays out the song-complete results screen for readability and adds Restart / Choose-Song
    /// buttons (wired via ResultsMenu). Reads the panel + summary text from GameManager. Re-runnable.
    ///
    /// Menu: PlayVisualizer → iOS: Build Results Menu
    /// </summary>
    public static class PlayVisualizerResultsMenu
    {
        [MenuItem("PlayVisualizer/iOS: Build Results Menu")]
        public static void Build()
        {
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null) { Debug.LogError("PlayVisualizer: no GameManager in scene."); return; }

            var gmSo = new SerializedObject(gm);
            var panel = gmSo.FindProperty("_resultsPanel").objectReferenceValue as GameObject;
            var finalText = gmSo.FindProperty("_finalScoreText").objectReferenceValue as Text;
            if (panel == null) { Debug.LogError("PlayVisualizer: GameManager._resultsPanel is not assigned."); return; }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // Readable full-screen dark backdrop.
            var panelImg = panel.GetComponent<Image>();
            if (panelImg == null) panelImg = panel.AddComponent<Image>();
            panelImg.sprite = uiSprite; panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.03f, 0.03f, 0.07f, 0.96f);
            Stretch(panel.GetComponent<RectTransform>());

            // Title.
            Transform titleT = panel.transform.Find("GameOverText");
            if (titleT != null && titleT.TryGetComponent(out Text title))
            {
                title.font = font; title.fontSize = 76; title.color = new Color(0.5f, 1f, 1f);
                title.alignment = TextAnchor.MiddleCenter;
                title.horizontalOverflow = HorizontalWrapMode.Overflow;
                title.verticalOverflow = VerticalWrapMode.Overflow;
                PlaceCenter(title.rectTransform, new Vector2(0f, 350f), new Vector2(1200, 120));
            }

            // Summary (GameManager fills the text/alignment at runtime; we just size/position it).
            if (finalText != null)
            {
                finalText.font = font; finalText.fontSize = 32; finalText.color = Color.white;
                finalText.lineSpacing = 1.2f;
                PlaceCenter(finalText.rectTransform, new Vector2(0f, 20f), new Vector2(1100, 470));
            }

            // Buttons.
            var provider = Object.FindFirstObjectByType<LocalClipMusicProvider>();
            Button restart = MakeButton(panel.transform, "ResultsRestartButton", "RESTART", font, uiSprite, new Vector2(-210f, -380f));
            Button choose = MakeButton(panel.transform, "ResultsChooseButton", "CHOOSE SONG", font, uiSprite, new Vector2(210f, -380f));

            var menu = panel.GetComponent<ResultsMenu>();
            if (menu == null) menu = panel.AddComponent<ResultsMenu>();
            AssignRef(menu, "_restartButton", restart);
            AssignRef(menu, "_chooseButton", choose);
            AssignRef(menu, "_provider", provider);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Results menu built (Restart / Choose Song).");
        }

        private static Button MakeButton(Transform parent, string name, string label, Font font, Sprite uiSprite, Vector2 pos)
        {
            DestroyChild(parent, name);
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = uiSprite; img.type = Image.Type.Sliced; img.color = new Color(0.15f, 0.16f, 0.25f, 1f);
            PlaceCenter(img.rectTransform, pos, new Vector2(380f, 96f));

            var textGo = new GameObject("Label", typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text = label; t.font = font; t.fontSize = 34; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            Stretch(t.rectTransform);
            return go.GetComponent<Button>();
        }

        private static void PlaceCenter(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        private static void AssignRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"PlayVisualizer: missing field '{field}' on {target.GetType().Name}."); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
