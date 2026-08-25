using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using PlayVisualizer.Player;
using PlayVisualizer.Weapons;

namespace PlayVisualizer.EditorTools
{
    /// <summary>
    /// iOS port M2: builds the on-screen twin thumbsticks (floating, left = move / right = aim) and
    /// the TouchControls manager (double-tap → cycle weapon), wired to the player. The touch UI
    /// only activates on a touch device (or when TouchControls._forceInEditor is on). Re-runnable.
    ///
    /// Menu: PlayVisualizer → iOS: Build Touch Controls (M2)
    /// </summary>
    public static class PlayVisualizerTouchM2
    {
        [MenuItem("PlayVisualizer/iOS: Build Touch Controls (M2)")]
        public static void Build()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("PlayVisualizer: no Player in scene.");
                return;
            }
            var input = player.GetComponent<PlayerInputReader>();
            var weapons = player.GetComponent<WeaponController>();

            EnsureEventSystem();
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            DestroyIfExists("TouchControlsCanvas");
            DestroyIfExists("TouchControlsManager");

            // Canvas (below the menu at 100, above the game).
            var canvasGo = new GameObject("TouchControlsCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            VirtualJoystick move = BuildStick(canvasGo.transform, "MoveStick", knob, true);
            VirtualJoystick aim = BuildStick(canvasGo.transform, "AimStick", knob, false);

            // Manager (separate, always-active object so it can toggle the canvas).
            var mgrGo = new GameObject("TouchControlsManager");
            var mgr = mgrGo.AddComponent<TouchControls>();
            AssignRef(mgr, "_moveStick", move);
            AssignRef(mgr, "_aimStick", aim);
            AssignRef(mgr, "_input", input);
            AssignRef(mgr, "_weapons", weapons);
            AssignRef(mgr, "_touchCanvas", canvasGo);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("PlayVisualizer: Touch controls (M2) built. On device the sticks auto-enable; " +
                      "in the editor, tick TouchControlsManager → Force In Editor to test with the mouse.");
        }

        private static VirtualJoystick BuildStick(Transform parent, string name, Sprite knob, bool leftHalf)
        {
            // Full-half touch zone (transparent, raycastable).
            var zoneGo = new GameObject(name, typeof(Image), typeof(VirtualJoystick));
            zoneGo.transform.SetParent(parent, false);
            var zoneRt = zoneGo.GetComponent<RectTransform>();
            zoneRt.anchorMin = new Vector2(leftHalf ? 0f : 0.5f, 0f);
            zoneRt.anchorMax = new Vector2(leftHalf ? 0.5f : 1f, 1f);
            zoneRt.offsetMin = Vector2.zero;
            zoneRt.offsetMax = Vector2.zero;
            var zoneImg = zoneGo.GetComponent<Image>();
            zoneImg.color = new Color(0f, 0f, 0f, 0f); // invisible but raycastable
            zoneImg.raycastTarget = true;

            RectTransform baseRt = BuildKnob(zoneRt, "Base", knob, 300f, new Color(1f, 1f, 1f, 0.18f));
            RectTransform handleRt = BuildKnob(zoneRt, "Handle", knob, 140f, new Color(1f, 1f, 1f, 0.45f));

            var js = zoneGo.GetComponent<VirtualJoystick>();
            AssignRef(js, "_base", baseRt);
            AssignRef(js, "_handle", handleRt);
            return js;
        }

        private static RectTransform BuildKnob(RectTransform parent, string name, Sprite knob, float size, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = knob;
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Debug.Log("PlayVisualizer: created an EventSystem (Input System UI module).");
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
