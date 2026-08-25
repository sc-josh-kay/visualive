using UnityEngine;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// Spawning parameters, kept separate from enemy behaviour (spec requirement). The music
    /// mapper (Phase 6) will drive the spawner's runtime SpawnRate from musical energy; this
    /// asset provides the baseline.
    /// Create via: Assets → Create → PlayVisualizer → Spawner Config.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnerConfig", menuName = "PlayVisualizer/Spawner Config")]
    public class SpawnerConfig : ScriptableObject
    {
        [Tooltip("Baseline enemies spawned per second.")]
        public float SpawnRate = 1f;

        [Tooltip("Hard cap on simultaneously alive enemies.")]
        public int MaxEnemies = 20;

        [Tooltip("Radius from the player at which enemies appear (keep off-screen).")]
        public float SpawnDistance = 11f;

        [Tooltip("Random +/- variation added to the spawn distance.")]
        public float SpawnDistanceJitter = 2f;
    }
}
