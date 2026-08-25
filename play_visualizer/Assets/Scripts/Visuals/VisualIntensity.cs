using UnityEngine;

namespace PlayVisualizer.Visuals
{
    /// <summary>
    /// A high-level 0..1 "how intense should the visuals be" signal with deliberate contrast:
    /// a gamma curve darkens quiet passages, a fast attack / slow release builds and eases, and a
    /// small floor keeps a faint ambient presence. Patterns use it to stay sparse/dark when the
    /// music is calm and bloom out when it's energetic (spec §9), rather than maxing everything.
    /// </summary>
    [System.Serializable]
    public class VisualIntensity
    {
        [Tooltip("Rise time (s) — how fast intensity builds on louder music.")]
        public float Attack = 0.4f;
        [Tooltip("Fall time (s) — how slowly intensity eases in quiet parts.")]
        public float Release = 1.5f;
        [Tooltip("Contrast curve; >1 darkens quiet passages.")]
        public float Gamma = 1.5f;
        [Tooltip("Minimum ambient intensity so it's never fully black.")]
        public float Floor = 0.06f;

        private float _value;

        public float Value => Mathf.Max(Floor, _value);

        public float Update(float energy, float dt)
        {
            float target = Mathf.Pow(Mathf.Clamp01(energy), Gamma);
            float tc = target > _value ? Attack : Release;
            _value += (target - _value) * (1f - Mathf.Exp(-dt / Mathf.Max(1e-4f, tc)));
            return Value;
        }
    }
}
