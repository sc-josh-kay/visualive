using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using PlayVisualizer.Audio;
using PlayVisualizer.Gameplay;
using PlayVisualizer.Player;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Builds the touch pause menu (Resume / Restart / Quit to Songs) and wires the PauseController
    /// to the thumbsticks, music provider and GameManager. Re-runnable.
    ///
    /// Menu: PlayVisualizer → iOS: Build Pause Menu
    /// </summary>
    public static class PlayVisualizerPauseMenu
    {
        [MenuItem("PlayVisualizer/iOS: Build Pause Menu")]
        public static void Build()
        {
            EnsureEventSystem();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            DestroyIfExists("PauseMenu");

            var canvasGo = new GameObject("PauseMenu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // above the touch sticks (5), below the song menu (100)
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Dimming panel (blocks touches under it) — toggled by PauseController.
            var panel = MakeImage(canvasGo.transform, "PausePanel", uiSprite, new Color(0.02f, 0.02f, 0.06f, 0.72f));
            Stretch(panel.rectTransform);

            MakeText(panel.transform, "PausedTitle", "PAUSED", font, 84, new Color(0.5f, 1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(900, 120));

            Button resume = MakeButton(panel.transform, "ResumeButton", "RESUME", font, uiSprite, new Vector2(0f, 70f));
            Button restart = MakeButton(panel.transform, "RestartButton", "RESTART", font, uiSprite, new Vector2(0f, -30f));
            Button quit = MakeButton(panel.transform, "QuitButton", "QUIT TO SONGS", font, uiSprite, new Vector2(0f, -130f));

            var pc = canvasGo.AddComponent<PauseController>();
            FindSticks(out VirtualJoystick move, out VirtualJoystick aim);
            AssignRef(pc, "_moveStick", move);
            AssignRef(pc, "_aimStick", aim);
            AssignRef(pc, "_touch", Object.FindFirstObjectByType<TouchControls>());
            AssignRef(pc, "_provider", Object.FindFirstObjectByType<LocalClipMusicProvider>());
            AssignRef(pc, "_gameManager", Object.FindFirstObjectByType<GameManager>());
            AssignRef(pc, "_pausePanel", panel.gameObject);
            AssignRef(pc, "_resumeButton", resume);
            AssignRef(pc, "_restartButton", restart);
            AssignRef(pc, "_quitButton", quit);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Pause menu built. Lift both thumbs during play to pause " +
                      "(Resume / Restart / Quit to Songs).");
        }

        private static void FindSticks(out VirtualJoystick move, out VirtualJoystick aim)
        {
            move = null; aim = null;
            foreach (var js in Object.FindObjectsByType<VirtualJoystick>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (js.gameObject.name == "MoveStick") move = js;
                else if (js.gameObject.name == "AimStick") aim = js;
            }
        }

        // ---------------------------------------------------------------- UI helpers

        private static Image MakeImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        private static Text MakeText(Transform parent, string name, string text, Font font, int size, Color color,
            Vector2 anchor, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text; t.font = font; t.fontSize = size; t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, string label, Font font, Sprite uiSprite, Vector2 pos)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = uiSprite; img.type = Image.Type.Sliced; img.color = new Color(0.15f, 0.16f, 0.25f, 1f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(560f, 84f);
            MakeText(go.transform, "Label", label, font, 34, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 84));
            return go.GetComponent<Button>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void AssignRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"PlayVisualizer: missing field '{field}' on {target.GetType().Name}."); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DestroyIfExists(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
