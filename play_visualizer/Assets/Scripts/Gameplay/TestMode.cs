using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// Developer test mode. Press T to toggle player invincibility so the visuals/audio can be
    /// observed without having to survive. Purely a debugging aid.
    /// </summary>
    public class TestMode : MonoBehaviour
    {
        [SerializeField] private Health _playerHealth;

        private bool _invincible;

        private void Awake()
        {
            if (_playerHealth == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    _playerHealth = player.GetComponent<Health>();
                }
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                _invincible = !_invincible;
                if (_playerHealth != null)
                {
                    _playerHealth.Invincible = _invincible;
                }
            }
        }

#if UNITY_EDITOR
        // Editor-only debug overlay: compiled out of device builds so OnGUI never dispatches there.
        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            style.normal.textColor = _invincible ? new Color(1f, 0.85f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(20, 370, 520, 24),
                _invincible ? "TEST MODE (invincible): ON  (T to toggle)" : "TEST MODE (invincible): OFF  (T to toggle)",
                style);
        }
#endif
    }
}
