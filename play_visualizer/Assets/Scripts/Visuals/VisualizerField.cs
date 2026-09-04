using System.Collections.Generic;
using UnityEngine;
using PlayVisualizer.Audio;

namespace PlayVisualizer.Visuals
{
    /// <summary>How a queued splat affects the field.</summary>
    public enum SplatMode { Paint, Consume, PaintRing }

    /// <summary>A transient distortion request (world space) — a ripple, no coverage.</summary>
    public struct DistortRequest
    {
        public Vector2 WorldPos;
        public float Strength;
    }

    /// <summary>A "black-hole" vacuum: pull + swirl the smoke inward at a point, then eat it.</summary>
    public struct VacuumRequest
    {
        public Vector2 WorldPos;
        public float WorldRadius;
        public float Strength;  // 0..1 pull/eat intensity
        public float Swirl;     // signed swirl direction/strength
    }

    /// <summary>
    /// A "turbulent tear": a Swarm unit churning through the smoke — noisy directional displacement +
    /// stretch + ragged consume. Many units are aggregated by the core into a few bounded zones.
    /// </summary>
    public struct TurbulenceRequest
    {
        public Vector2 WorldPos;
        public float WorldRadius;
        public float Agitation;   // 0..1 treble/flux-driven shred intensity
        public Vector2 FlowDir;   // world-space direction the unit is moving (need not be normalized)
    }

    /// <summary>A single gameplay request to modify the visualizer field (world space).</summary>
    public struct FieldSplat
    {
        public Vector2 WorldPos;
        public float WorldRadius;
        public Color Color;   // only meaningful for Paint
        public float Strength;
        public SplatMode Mode;
    }

    /// <summary>
    /// The clean seam between GAMEPLAY and the VISUALIZER (spec §9). Enemies and the player talk
    /// to the visual field only through this thin channel — they never touch RenderTextures or
    /// shaders. It mirrors the existing EnemyModulation/ScoreManager singleton-service pattern:
    ///
    ///   * <see cref="Consume"/> — an enemy eats coverage into black at a point.
    ///   * <see cref="Paint"/>   — an enemy death (or other event) injects a color burst.
    ///   * <see cref="Coverage"/> — how much of the play area currently has visualizer content.
    ///   * <see cref="PlayerPaintGain"/> — movement-gated / hit-disrupted multiplier on the
    ///     player's trail emission.
    ///
    /// VisualizerCore owns the actual field: it drains queued splats each frame, converts them to
    /// viewport space, and writes back Coverage. This class holds no rendering state, so it stays
    /// safe for gameplay code to depend on.
    /// </summary>
    public class VisualizerField : MonoBehaviour
    {
        public static VisualizerField Instance { get; private set; }

        /// <summary>
        /// Current visualizer coverage, 0..1 (current, not historical). Written by VisualizerCore's
        /// coverage sampler; read by scoring and by enemy targeting.
        /// </summary>
        public float Coverage { get; set; }

        /// <summary>
        /// Multiplier (0..1) on the player's paint emission. 1 = full. Movement gating and enemy-hit
        /// disruption drive this down (see PlayerTrail); the smoke pattern reads it via the core.
        /// Defaults to 1 so the visualizer paints normally before any gameplay writes it.
        /// </summary>
        public float PlayerPaintGain = 1f;

        /// <summary>
        /// Overdrive intensity (0..1), written by gameplay (VisualizerMomentum) and read by the
        /// visualizer to lightly amplify its own effects (spec9 §13). Same write-by-gameplay /
        /// read-by-visualizer channel as <see cref="PlayerPaintGain"/> — gameplay never touches
        /// shaders. 0 = normal play.
        /// </summary>
        public float OverdriveIntensity = 0f;

        /// <summary>World position of the densest painted region — the point enemies seek (spec §4).</summary>
        public Vector2 HotWorldPos { get; private set; }

        /// <summary>True when there is painted content worth targeting; else enemies fall back.</summary>
        public bool HasHotPoint { get; private set; }

        private readonly List<FieldSplat> _pending = new List<FieldSplat>();
        private readonly List<DistortRequest> _distorts = new List<DistortRequest>();
        private readonly List<VacuumRequest> _vacuums = new List<VacuumRequest>();
        private readonly List<TurbulenceRequest> _turbulence = new List<TurbulenceRequest>();

        /// <summary>
        /// A Corruptor "black hole": pull + swirl the smoke field inward at this point and eat it
        /// (like water down a drain). Radius/strength grow with bass. Drained by the core into the
        /// smoke pattern's advection. Reduces coverage (the eat), replacing the plain consume.
        /// </summary>
        public void Vacuum(Vector2 worldPos, float worldRadius, float strength, float swirl)
        {
            if (strength <= 0f || worldRadius <= 0f) return;
            _vacuums.Add(new VacuumRequest
            {
                WorldPos = worldPos,
                WorldRadius = worldRadius,
                Strength = Mathf.Clamp01(strength),
                Swirl = swirl
            });
        }

        /// <summary>Hand queued vacuums to the core and clear them. Called once per frame.</summary>
        public void DrainVacuums(List<VacuumRequest> dest)
        {
            dest.Clear();
            dest.AddRange(_vacuums);
            _vacuums.Clear();
        }

        /// <summary>
        /// A Swarm unit "turbulent tear": it displaces, stretches, and raggedly consumes the smoke as
        /// it moves through it. Emitted per unit; the core aggregates nearby units into a few bounded
        /// turbulence zones (the advection array is capped), so this scales to the whole swarm.
        /// Agitation (treble/flux) grows the shred; reduces coverage (the eat).
        /// </summary>
        public void Turbulence(Vector2 worldPos, float worldRadius, float agitation, Vector2 flowDir)
        {
            if (worldRadius <= 0f) return;
            _turbulence.Add(new TurbulenceRequest
            {
                WorldPos = worldPos,
                WorldRadius = worldRadius,
                Agitation = Mathf.Clamp01(agitation),
                FlowDir = flowDir
            });
        }

        /// <summary>Hand queued turbulence requests to the core and clear them. Called once per frame.</summary>
        public void DrainTurbulence(List<TurbulenceRequest> dest)
        {
            dest.Clear();
            dest.AddRange(_turbulence);
            _turbulence.Clear();
        }

        /// <summary>
        /// Transiently DISTORT the visualizer at a world point (a ripple), WITHOUT adding coverage —
        /// this is the "energy" language: weapon fire/impacts ripple the field but never paint it.
        /// Drained by the core into the RippleSystem.
        /// </summary>
        public void Distort(Vector2 worldPos, float strength)
        {
            if (strength <= 0f) return;
            _distorts.Add(new DistortRequest { WorldPos = worldPos, Strength = strength });
        }

        /// <summary>Hand queued distortions to the core and clear them. Called once per frame.</summary>
        public void DrainDistorts(List<DistortRequest> dest)
        {
            dest.Clear();
            dest.AddRange(_distorts);
            _distorts.Clear();
        }

        /// <summary>Set by the visualizer core from the coverage sampler each frame.</summary>
        public void SetHotPoint(Vector2 worldPos)
        {
            HotWorldPos = worldPos;
            HasHotPoint = true;
        }

        /// <summary>Cleared by the core when there is no painted content.</summary>
        public void ClearHotPoint()
        {
            HasHotPoint = false;
        }

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

        /// <summary>Ensure a VisualizerField exists (auto-created by the core if not scene-wired).</summary>
        public static VisualizerField Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("VisualizerField");
                go.AddComponent<VisualizerField>();
            }
            return Instance;
        }

        /// <summary>Enemy eats coverage into black at a world point.</summary>
        public void Consume(Vector2 worldPos, float worldRadius, float strength)
        {
            if (strength <= 0f || worldRadius <= 0f) return;
            _pending.Add(new FieldSplat
            {
                WorldPos = worldPos,
                WorldRadius = worldRadius,
                Color = Color.black,
                Strength = Mathf.Clamp01(strength),
                Mode = SplatMode.Consume
            });
        }

        /// <summary>
        /// Inject a color burst (e.g. an enemy death explosion). The color inherits the current
        /// musical moment so combat reads as part of the visualizer (spec §8, §14). The MusicState
        /// is sampled now (not retained), since it is a shared, mutating snapshot.
        /// </summary>
        public void Paint(Vector2 worldPos, float worldRadius, float intensity, MusicState state)
        {
            if (intensity <= 0f || worldRadius <= 0f) return;
            _pending.Add(new FieldSplat
            {
                WorldPos = worldPos,
                WorldRadius = worldRadius,
                Color = MusicColor.From(state),
                Strength = Mathf.Max(0f, intensity),
                Mode = SplatMode.Paint
            });
        }

        /// <summary>
        /// Deposit paint along an expanding RING (annulus) rather than a filled disc — the wavefront
        /// of an Overdrive paint wave (spec9 §6). <paramref name="worldRadius"/> is the ring's current
        /// radius; the band width is a shader constant (fraction of radius). Persists/advects/fades in
        /// the field like any painted content. Color inherits the current musical moment.
        /// </summary>
        public void PaintRing(Vector2 worldPos, float worldRadius, float intensity, MusicState state)
        {
            if (intensity <= 0f || worldRadius <= 0f) return;
            _pending.Add(new FieldSplat
            {
                WorldPos = worldPos,
                WorldRadius = worldRadius,
                Color = MusicColor.From(state),
                Strength = Mathf.Max(0f, intensity),
                Mode = SplatMode.PaintRing
            });
        }

        /// <summary>
        /// Hand the queued splats to the core and clear the queue. Called once per frame by
        /// VisualizerCore; gameplay never calls this.
        /// </summary>
        public void DrainInto(List<FieldSplat> dest)
        {
            dest.Clear();
            dest.AddRange(_pending);
            _pending.Clear();
        }
    }

    /// <summary>Maps a MusicState snapshot to a paint color (brightness/centroid → hue).</summary>
    public static class MusicColor
    {
        public static Color From(MusicState s)
        {
            if (s == null) return Color.white;
            float hue = Mathf.Repeat(s.SpectralCentroid, 1f);
            float sat = 0.85f;
            float val = 1f;
            return Color.HSVToRGB(hue, sat, val);
        }
    }
}
