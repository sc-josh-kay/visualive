using System;
using UnityEngine;

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// Simple hit-point pool for the player. Fires events so UI (Phase 4) and game-over
    /// logic can react without this component knowing about them.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private int _max = 100;

        public int Max => _max;
        public int Current { get; private set; }

        /// <summary>When true, damage is ignored (test mode).</summary>
        public bool Invincible { get; set; }

        /// <summary>(current, max) whenever health changes.</summary>
        public event Action<int, int> Changed;

        /// <summary>Fired once when health reaches zero.</summary>
        public event Action Died;

        private bool _dead;

        private void Awake()
        {
            Current = _max;
        }

        public void TakeDamage(int amount)
        {
            if (_dead || amount <= 0 || Invincible)
            {
                return;
            }

            Current = Mathf.Max(0, Current - amount);
            Changed?.Invoke(Current, _max);

            if (Current <= 0)
            {
                _dead = true;
                Died?.Invoke();
            }
        }

        /// <summary>Restore to full (used by restart in Phase 4).</summary>
        public void ResetHealth()
        {
            _dead = false;
            Current = _max;
            Changed?.Invoke(Current, _max);
        }
    }
}
