using UnityEngine;

namespace PlayVisualizer.Audio.Analysis
{
    /// <summary>
    /// Asymmetric (attack/release) one-pole smoother. Rises quickly toward a rising target and
    /// falls slowly when the target drops, so a signal reacts immediately to a musical event and
    /// then decays naturally — unlike a single symmetric Lerp which both delays and rounds hits.
    /// Reusable for any continuous signal (bass, mid, treble, energy, visual params).
    /// This is NOT a substitute for onset detection (that's a separate, event-based module).
    /// </summary>
    public class EnvelopeFollower
    {
        private float _attack;
        private float _release;

        public float Value { get; private set; }

        /// <param name="attackSeconds">Time constant while rising.</param>
        /// <param name="releaseSeconds">Time constant while falling.</param>
        public EnvelopeFollower(float attackSeconds, float releaseSeconds)
        {
            SetTimes(attackSeconds, releaseSeconds);
        }

        public void SetTimes(float attackSeconds, float releaseSeconds)
        {
            _attack = Mathf.Max(1e-4f, attackSeconds);
            _release = Mathf.Max(1e-4f, releaseSeconds);
        }

        public float Update(float target, float dt)
        {
            float tc = target > Value ? _attack : _release;
            float alpha = 1f - Mathf.Exp(-dt / tc); // frame-rate independent
            Value += (target - Value) * alpha;
            return Value;
        }
    }
}
