using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using PlayVisualizer.Audio;
using PlayVisualizer.Gameplay;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// Builds the "VISUALIVE" start screen: a SongLibrary populated from Assets/Audio/Tracks, a
    /// full-screen menu Canvas with a button per track, and the StartScreenController wiring.
    /// Also stops the music provider from auto-playing and puts the GameManager into menu-on-start.
    /// Re-runnable.
    ///
    /// Menu: PlayVisualizer → Build Start Screen (Visualive)
    /// </summary>
    public static class PlayVisualizerStartScreen
    {
        private const string LibraryPath = "Assets/ScriptableObjects/SongLibrary.asset";

        [MenuItem("PlayVisualizer/Build Start Screen (Visualive)")]
        public static void BuildStartScreen()
        {
            SongLibrary library = PopulateLibrary();
            EnsureEventSystem();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            DestroyIfExists("StartScreen");
            var canvasGo = new GameObject("StartScreen", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above the HUD
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Dark full-screen panel.
            Image panel = MakeImage(canvasGo.transform, "Panel", uiSprite, new Color(0.02f, 0.02f, 0.06f, 0.96f));
            Stretch(panel.rectTransform);

            // Title + subtitle.
            MakeText(panel.transform, "Title", "VISUALIVE", font, 96, new Color(0.5f, 1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1200, 140));
            MakeText(panel.transform, "Subtitle", "select a track", font, 34, new Color(0.8f, 0.8f, 0.9f),
                new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(900, 60));

            // Scrollable song list (accommodates any number of tracks; drag to scroll on touch).
            var scrollGo = new GameObject("SongScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.anchoredPosition = new Vector2(0f, -70f);
            scrollRt.sizeDelta = new Vector2(760f, 620f);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            Stretch(viewportRt);
            viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.004f); // near-invisible; enables the mask

            // Content = the vertical song list (buttons cloned into it at runtime).
            var listGo = new GameObject("SongList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listGo.transform.SetParent(viewportGo.transform, false);
            var listRect = listGo.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 1f);
            listRect.anchorMax = new Vector2(0.5f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = new Vector2(700f, 0f);
            var vlg = listGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.padding = new RectOffset(0, 0, 6, 6);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            var fitter = listGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = listRect;

            // Template button (cloned at runtime per track).
            Button template = MakeButton(listRect, "SongButtonTemplate", uiSprite, font);
            template.gameObject.SetActive(false);

            // Controller.
            var sc = canvasGo.AddComponent<StartScreenController>();
            AssignReference(sc, "_library", library);
            AssignReference(sc, "_provider", Object.FindFirstObjectByType<LocalClipMusicProvider>());
            AssignReference(sc, "_panel", panel.gameObject);
            AssignReference(sc, "_songListContainer", listRect);
            AssignReference(sc, "_songButtonTemplate", template);

            // Provider shouldn't auto-play; GameManager should open the menu on start.
            var provider = Object.FindFirstObjectByType<LocalClipMusicProvider>();
            if (provider != null) AssignBool(provider, "_playOnStart", false);
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null) AssignBool(gm, "_openMenuOnStart", true);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            int count = library != null && library.Tracks != null ? library.Tracks.Length : 0;
            Debug.Log($"PlayVisualizer: Start screen built with {count} track(s). Save the scene (Cmd+S) and press Play.");
        }

        private static SongLibrary PopulateLibrary()
        {
            var lib = AssetDatabase.LoadAssetAtPath<SongLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<SongLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Tracks" });
            var clips = new List<AudioClip>();
            foreach (string g in guids)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g));
                if (clip != null) clips.Add(clip);
            }
            clips.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            lib.Tracks = clips.ToArray();
            EditorUtility.SetDirty(lib);
            return lib;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
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
            Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, anchor.y);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, Sprite sprite, Font font)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.12f, 0.12f, 0.2f, 1f);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 58f;
            le.preferredHeight = 58f;

            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text = "Song";
            t.font = font;
            t.fontSize = 30;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            Stretch(t.rectTransform);

            return go.GetComponent<Button>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ---------------------------------------------------------------- util

        private static void AssignReference(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"PlayVisualizer: missing field '{field}' on {target.GetType().Name}."); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"PlayVisualizer: missing field '{field}' on {target.GetType().Name}."); return; }
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DestroyIfExists(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
