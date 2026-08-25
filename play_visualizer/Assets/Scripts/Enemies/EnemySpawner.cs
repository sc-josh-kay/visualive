using System.Collections.Generic;
using UnityEngine;

namespace PlayVisualizer.Enemies
{
    /// <summary>
    /// Spawns enemies on a timer at a distance from the player, capped by MaxEnemies.
    ///
    /// Supports multiple enemy types via a weighted table (<see cref="_entries"/>): each timer tick
    /// picks a type by weight and spawns a group of 1..N (so Swarms arrive as a cloud, singles as
    /// singles). If no entries are configured it falls back to the single legacy <see cref="_enemyPrefab"/>.
    ///
    /// The spawner is deliberately separate from enemy behaviour, and does not read audio itself —
    /// the music mapper drives <see cref="SpawnRate"/> and calls <see cref="SpawnBurst"/>.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct SpawnEntry
        {
            public EnemyBase Prefab;
            [Min(0f)] public float Weight;
            [Tooltip("Units spawned together (a Swarm uses a range like 6..10; singles use 1..1).")]
            [Min(1)] public int GroupMin;
            [Min(1)] public int GroupMax;
            [Tooltip("World radius the group is scattered within.")]
            public float GroupSpread;
            [Tooltip("Song progress (0..1) this type unlocks at — so tougher/denser types appear " +
                     "later in the song.")]
            [Range(0f, 1f)] public float MinSongProgress;
        }

        [SerializeField] private SpawnerConfig _config;
        [Tooltip("Weighted enemy-type table. If empty, the legacy single prefab is used.")]
        [SerializeField] private SpawnEntry[] _entries;
        [Tooltip("Legacy single-type fallback (Color Eater) when no entries are set.")]
        [SerializeField] private EnemyBase _enemyPrefab;
        [SerializeField] private Transform _player;

        private readonly List<EnemyBase> _alive = new List<EnemyBase>();
        private float _spawnRate;
        private float _timer;

        /// <summary>Enemies per second. Seam for the music mapper; clamped to >= 0.</summary>
        public float SpawnRate
        {
            get => _spawnRate;
            set => _spawnRate = Mathf.Max(0f, value);
        }

        /// <summary>The config's baseline spawn rate, so the mapper can revert when disabled.</summary>
        public float BaselineSpawnRate => _config != null ? _config.SpawnRate : 1f;

        /// <summary>
        /// Runtime on-screen enemy cap, driven by the song-time director. -1 = use the config cap.
        /// Always clamped to the config cap so it can only ever reduce, never exceed.
        /// </summary>
        public int ActiveMaxEnemies { get; set; } = -1;

        /// <summary>Song progress 0..1, set by the director. Gates which enemy types can spawn.</summary>
        public float SongProgress { get; set; } = 1f;

        private int EffectiveMax
        {
            get
            {
                int cap = _config != null ? _config.MaxEnemies : 0;
                return ActiveMaxEnemies >= 0 ? Mathf.Min(ActiveMaxEnemies, cap) : cap;
            }
        }

        private void Awake()
        {
            if (_config != null)
            {
                _spawnRate = _config.SpawnRate;
            }
            if (_player == null)
            {
                GameObject found = GameObject.Find("Player");
                if (found != null)
                {
                    _player = found.transform;
                }
            }
        }

        private void Update()
        {
            if (_config == null || _player == null)
            {
                return;
            }

            _alive.RemoveAll(e => e == null);
            if (_alive.Count >= EffectiveMax)
            {
                return;
            }

            _timer += Time.deltaTime;
            float interval = _spawnRate > 0f ? 1f / _spawnRate : float.MaxValue;
            if (_timer >= interval)
            {
                _timer = 0f;
                SpawnOne();
            }
        }

        /// <summary>
        /// Spawn several enemies at once (e.g. on a beat), respecting the max-enemy cap. Each is a
        /// single weighted pick (not a group), so fast beats don't dump whole swarms.
        /// Called by the music mapper — the spawner does not read audio itself.
        /// </summary>
        public void SpawnBurst(int count)
        {
            if (_config == null || _player == null)
            {
                return;
            }

            _alive.RemoveAll(e => e == null);
            for (int i = 0; i < count && _alive.Count < EffectiveMax; i++)
            {
                EnemyBase prefab = PickPrefab();
                if (prefab != null) SpawnAt(prefab, RandomSpawnPos());
            }
        }

        /// <summary>One timer tick: pick a type and spawn its group (a cloud for Swarms).</summary>
        private void SpawnOne()
        {
            if (!TryPickEntry(out SpawnEntry entry))
            {
                if (_enemyPrefab != null) SpawnAt(_enemyPrefab, RandomSpawnPos());
                return;
            }

            int count = Random.Range(Mathf.Max(1, entry.GroupMin), Mathf.Max(1, entry.GroupMax) + 1);
            Vector2 center = RandomSpawnPos();
            for (int i = 0; i < count && _alive.Count < EffectiveMax; i++)
            {
                Vector2 pos = entry.GroupSpread > 0f
                    ? center + Random.insideUnitCircle * entry.GroupSpread
                    : center;
                SpawnAt(entry.Prefab, pos);
            }
        }

        private void SpawnAt(EnemyBase prefab, Vector2 pos)
        {
            EnemyBase enemy = Instantiate(prefab, pos, Quaternion.identity);
            enemy.SetTarget(_player);
            _alive.Add(enemy);
        }

        private Vector2 RandomSpawnPos()
        {
            float angle = Random.value * Mathf.PI * 2f;
            float dist = _config.SpawnDistance +
                         Random.Range(-_config.SpawnDistanceJitter, _config.SpawnDistanceJitter);
            return (Vector2)_player.position +
                   new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
        }

        /// <summary>Weighted-random entry with a valid prefab; false if none configured.</summary>
        private bool TryPickEntry(out SpawnEntry chosen)
        {
            chosen = default;
            if (_entries == null || _entries.Length == 0) return false;

            float total = 0f;
            for (int i = 0; i < _entries.Length; i++)
            {
                if (IsEligible(_entries[i])) total += _entries[i].Weight;
            }
            if (total <= 0f) return false;

            float r = Random.value * total;
            for (int i = 0; i < _entries.Length; i++)
            {
                if (!IsEligible(_entries[i])) continue;
                r -= _entries[i].Weight;
                if (r <= 0f) { chosen = _entries[i]; return true; }
            }
            // Fallback to the last eligible entry (floating-point guard).
            for (int i = _entries.Length - 1; i >= 0; i--)
            {
                if (IsEligible(_entries[i])) { chosen = _entries[i]; return true; }
            }
            return false;
        }

        /// <summary>A single weighted prefab (for bursts), falling back to the legacy prefab.</summary>
        private EnemyBase PickPrefab()
        {
            return TryPickEntry(out SpawnEntry e) ? e.Prefab : _enemyPrefab;
        }

        private bool IsEligible(SpawnEntry e)
        {
            return e.Prefab != null && e.Weight > 0f && SongProgress >= e.MinSongProgress;
        }
    }
}
