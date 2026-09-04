using UnityEngine;

namespace PlayVisualizer.Visuals
{
    /// <summary>
    /// Shared render-texture format for the persistent visualizer feedback loop. The feedback field is
    /// read+written across several full-screen passes every frame, so its bandwidth dominates mobile
    /// cost. RGB111110Float keeps HDR range (needed for the bloom-driven look) at HALF the bandwidth of
    /// ARGBHalf, and the field never uses alpha (the smoke shader writes 1.0). Falls back to ARGBHalf
    /// on the rare device that can't render RGB111110Float.
    /// </summary>
    internal static class VisualFormat
    {
        public static readonly RenderTextureFormat Feedback =
            SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float)
                ? RenderTextureFormat.RGB111110Float
                : RenderTextureFormat.ARGBHalf;
    }
}
