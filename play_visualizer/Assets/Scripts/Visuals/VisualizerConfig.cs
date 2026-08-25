using UnityEngine;

namespace PlayVisualizer.Visuals
{
    /// <summary>
    /// Tunable amounts for the audio-reactive visuals. Centralized so the whole look can be
    /// dialed in quickly (the spec expects lots of experimentation here).
    /// Create via: Assets → Create → PlayVisualizer → Visualizer Config.
    /// </summary>
    [CreateAssetMenu(fileName = "VisualizerConfig", menuName = "PlayVisualizer/Visualizer Config")]
    public class VisualizerConfig : ScriptableObject
    {
        [Header("Bass → Background pulse (world scale)")]
        public float BackgroundBaseScale = 28f;
        public float BackgroundPulseAmount = 8f;

        [Header("Energy → Bloom intensity")]
        public float BloomBase = 1f;
        public float BloomMax = 3.5f;

        [Header("Treble → Particle emission (per second)")]
        public float ParticleRateMin = 3f;
        public float ParticleRateMax = 80f;

        [Header("Beat → Shockwave")]
        public Color ShockwaveColor = new Color(0.3f, 1f, 1f, 1f);
    }
}
