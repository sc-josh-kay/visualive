using UnityEngine;

namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// Tunable ranges/curves for how MusicState drives gameplay. Centralizing these here (and
    /// applying them only in the MusicMapper) is the key architectural move: mappings can grow
    /// far more sophisticated later without any gameplay system knowing about music.
    /// Create via: Assets → Create → PlayVisualizer → Music Mapping Config.
    /// </summary>
    [CreateAssetMenu(fileName = "MusicMappingConfig", menuName = "PlayVisualizer/Music Mapping Config")]
    public class MusicMappingConfig : ScriptableObject
    {
        [Header("Warmup")]
        [Tooltip("Seconds over which spawning ramps 0→full at the start of a run, so the " +
                 "opening is always calm regardless of the (relative) energy reading.")]
        public float WarmupSeconds = 8f;

        [Header("Song-time intensity (spec §13)")]
        [Tooltip("Overall enemy intensity across the song (x = 0..1 progress → y = 0..1). Scales " +
                 "BOTH the spawn rate and the on-screen enemy cap, so the song starts calm with few " +
                 "enemies and builds as it plays.")]
        public AnimationCurve IntensityOverSong = new AnimationCurve(
            new Keyframe(0f, 0.15f), new Keyframe(0.5f, 0.55f), new Keyframe(1f, 1f));

        [Tooltip("On-screen enemy cap at the START of the song (intensity 0).")]
        public int StartMaxEnemies = 6;

        [Tooltip("On-screen enemy cap at full intensity (song climax).")]
        public int FullMaxEnemies = 40;

        [Header("Music-reactive composition (spec8)")]
        [Tooltip("Seconds of smoothing on the spawn-composition channel levels, so composition " +
                 "shifts by section rather than frame-to-frame.")]
        public float SpawnCompositionSmoothing = 1.5f;
        [Tooltip("Scales raw spectral flux into a 0..1 level for composition/formation weighting.")]
        public float FluxScale = 4f;

        [Header("Energy → Spawn Rate (enemies/sec)")]
        public float SpawnRateMin = 0.2f;
        public float SpawnRateMax = 2f;
        [Tooltip("Shapes how energy (x, 0..1) maps to the min..max range (y, 0..1).")]
        public AnimationCurve EnergyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Bass → Enemy Speed (multiplier)")]
        public float SpeedMultMin = 0.8f;
        public float SpeedMultMax = 1.8f;

        [Header("Beat → Spawn Burst")]
        [Tooltip("Enemies spawned on a beat (capped by MaxEnemies).")]
        public int BeatBurstCount = 1;
        [Tooltip("Minimum seconds between beat bursts, so fast beats don't flood the arena.")]
        public float BeatBurstMinInterval = 0.7f;
    }
}
