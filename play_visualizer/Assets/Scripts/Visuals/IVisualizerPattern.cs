using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Visuals
{
    /// <summary>Shared context handed to every visualizer pattern.</summary>
    public class VisualizerContext
    {
        public Camera Camera;
        public Transform Player;

        /// <summary>
        /// Shared material (FieldSplat shader) the core owns and field-based patterns use to blit
        /// gameplay splats (enemy consume / death paint) into their persistent field. May be null
        /// if the shader is missing; patterns must guard against that.
        /// </summary>
        public Material SplatMaterial;
    }

    /// <summary>
    /// A swappable psychedelic visual style. Patterns read MusicState and render the current
    /// environment into a target RenderTexture; they may sample a shared trail field (Phase V2)
    /// and be distorted by ripples (Phase V3). Each pattern picks a small subset of features so it
    /// has its own personality. Patterns contain no gameplay logic.
    /// </summary>
    public interface IVisualizerPattern
    {
        string Name { get; }

        void Configure(VisualizerContext ctx);

        /// <summary>
        /// Advance internal state and push MusicState-derived params for this frame.
        /// <paramref name="paintGain"/> (0..1) is the gameplay-driven multiplier on the player's
        /// paint emission — movement-gated and cut on enemy hits — so gameplay can throttle how
        /// strongly the player paints without touching shaders. Patterns without a player-painted
        /// field may ignore it.
        /// </summary>
        void UpdatePattern(MusicState state, float intensity, float paintGain, float dt);

        /// <summary>
        /// Queue gameplay splats (enemy consume / death paint) to be applied to this pattern's
        /// persistent field during <see cref="Render"/>. No-op for patterns without a field.
        /// </summary>
        void InjectSplats(SplatData splats);

        /// <summary>
        /// Queue "black-hole" vacuums (Corruptors) that pull + swirl + eat the field in its advection.
        /// No-op for patterns without a persistent advected field.
        /// </summary>
        void InjectVacuums(VacuumData vacuums);

        /// <summary>
        /// Queue "turbulent tears" (aggregated Swarm zones) that displace + stretch + raggedly eat the
        /// field in its advection. No-op for patterns without a persistent advected field.
        /// </summary>
        void InjectTurbulence(TurbulenceData turbulence);

        /// <summary>Render the pattern into <paramref name="target"/>, optionally sampling the trail field.</summary>
        void Render(RenderTexture trailField, RenderTexture target);

        /// <summary>Apply the active ripples (Phase V3). No-op until then.</summary>
        void ApplyRipple(RippleData ripples);

        void Reset();
    }

    /// <summary>
    /// Gameplay splats handed to a field pattern each frame (fixed-capacity, only <see cref="Count"/>
    /// slots valid). Written by the visualizer core from <c>VisualizerField</c> requests; enemies
    /// never construct these directly. Positions/radii are already in viewport space.
    /// </summary>
    public class SplatData
    {
        // Must match MAX_SPLATS in FieldSplat.shader.
        public const int Max = 48;

        /// <summary>Per slot: (originU, originV, radiusV, strength).</summary>
        public Vector4[] Splats = new Vector4[Max];

        /// <summary>Per slot: (r, g, b, mode) — mode &gt; 0 = paint (additive), &lt; 0 = consume (darken).</summary>
        public Vector4[] Colors = new Vector4[Max];

        public int Count;
    }

    /// <summary>Active ripples handed to a pattern each frame (fixed-size, unused slots zeroed).</summary>
    public class RippleData
    {
        /// <summary>Per slot: (originU, originV, radius, strength). Length = RippleSystem.Max.</summary>
        public Vector4[] Ripples;
        public float Width;
    }

    /// <summary>
    /// Vacuum points (Corruptors) handed to a field pattern each frame. Positions/radii are in
    /// viewport space; written by the core from <c>VisualizerField</c> requests.
    /// </summary>
    public class VacuumData
    {
        // Must match VAC_MAX in SmokeField.shader.
        public const int Max = 6;

        /// <summary>Per slot: (originU, originV, radiusV, strength).</summary>
        public Vector4[] Vacuums = new Vector4[Max];

        /// <summary>Per slot: signed swirl direction/strength.</summary>
        public float[] Swirl = new float[Max];

        public int Count;
    }

    /// <summary>
    /// Aggregated Swarm turbulence zones handed to a field pattern each frame. Many swarm units are
    /// bucketed into at most <see cref="Max"/> zones by the core; positions/radii/flow are in viewport
    /// space. Must match TURB_MAX in SmokeField.shader.
    /// </summary>
    public class TurbulenceData
    {
        public const int Max = 6;

        /// <summary>Per slot: (originU, originV, radiusV, agitation).</summary>
        public Vector4[] Zones = new Vector4[Max];

        /// <summary>Per slot: (flowX, flowY, stretch01, seed) — normalized viewport-space flow dir.</summary>
        public Vector4[] Flow = new Vector4[Max];

        public int Count;
    }
}
