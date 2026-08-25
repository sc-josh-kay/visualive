using UnityEngine;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// Shared, plain runtime parameters that enemies read each frame (currently a speed
    /// multiplier). The music mapper WRITES these; enemies READ them. Neither the enemy nor
    /// this class knows anything about audio — it's just a generic modulation channel, which
    /// keeps the audio→gameplay coupling in one place (the mapper).
    /// </summary>
    public class EnemyModulation : MonoBehaviour
    {
        public static EnemyModulation Instance { get; private set; }

        [Tooltip("Multiplies enemy movement speed. 1 = unmodified.")]
        public float SpeedMultiplier = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
