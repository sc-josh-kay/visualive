using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayVisualizer.Player
{
    /// <summary>
    /// A floating on-screen thumbstick. Its RectTransform is a full touch zone (e.g. the left or
    /// right half of the screen); on touch-down the stick appears under the thumb and re-centers
    /// each touch, so the player never has to find a fixed spot. Outputs a normalized [-1,1]
    /// vector, read by TouchControls and fed into PlayerInputReader.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _base;   // ring shown at the touch point
        [SerializeField] private RectTransform _handle; // knob
        [Tooltip("Max thumb travel from center, in canvas units.")]
        [SerializeField] private float _radius = 140f;

        private RectTransform _zone;
        private Vector2 _center;

        public Vector2 Value { get; private set; }
        public bool Active { get; private set; }

        private void Awake()
        {
            _zone = GetComponent<RectTransform>();
            Hide();
        }

        public void OnPointerDown(PointerEventData e)
        {
            Active = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_zone, e.position, e.pressEventCamera, out _center);
            if (_base != null) { _base.anchoredPosition = _center; _base.gameObject.SetActive(true); }
            if (_handle != null) { _handle.anchoredPosition = _center; _handle.gameObject.SetActive(true); }
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Active) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_zone, e.position, e.pressEventCamera, out Vector2 local);
            Vector2 delta = Vector2.ClampMagnitude(local - _center, _radius);
            if (_handle != null) _handle.anchoredPosition = _center + delta;
            Value = _radius > 0f ? delta / _radius : Vector2.zero;
        }

        public void OnPointerUp(PointerEventData e)
        {
            Active = false;
            Value = Vector2.zero;
            Hide();
        }

        private void Hide()
        {
            if (_base != null) _base.gameObject.SetActive(false);
            if (_handle != null) _handle.gameObject.SetActive(false);
        }
    }
}
