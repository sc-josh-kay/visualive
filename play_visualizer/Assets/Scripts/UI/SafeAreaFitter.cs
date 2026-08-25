using UnityEngine;

namespace PlayVisualizer.UI
{
    /// <summary>
    /// Anchors this RectTransform to the device safe area (inside the notch / home indicator), so
    /// its children stay clear of cutouts. Put UI content under an object with this component;
    /// full-screen backgrounds should stay OUTSIDE it so they still cover the whole screen.
    /// Updates if the safe area changes (rotation). No-op on platforms with a full-screen safe area.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect _applied;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (_applied != Screen.safeArea)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rt == null || Screen.width <= 0 || Screen.height <= 0) return;

            Rect sa = Screen.safeArea;
            _applied = sa;

            Vector2 min = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            Vector2 max = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
