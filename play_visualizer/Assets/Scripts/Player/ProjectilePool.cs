using System.Collections.Generic;
using UnityEngine;

namespace PlayVisualizer.Player
{
    /// <summary>
    /// Lightweight per-prefab object pool for projectiles (perf). Auto-fire otherwise
    /// Instantiate/Destroys many projectiles per second → GC churn + frame hitches on mobile; pooling
    /// reuses inactive instances instead. Lives as a scene object so a scene reload (Restart) resets
    /// it cleanly. Auto-created on first use — no wiring.
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        private static ProjectilePool _instance;

        public static ProjectilePool Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ProjectilePool");
                    _instance = go.AddComponent<ProjectilePool>();
                }
                return _instance;
            }
        }

        private readonly Dictionary<Projectile, Stack<Projectile>> _pools =
            new Dictionary<Projectile, Stack<Projectile>>();

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>Acquire a projectile for <paramref name="prefab"/> at a position (pooled or fresh).</summary>
        public Projectile Get(Projectile prefab, Vector3 position)
        {
            if (prefab == null) return null;
            Stack<Projectile> stack = StackFor(prefab);

            Projectile p = null;
            while (stack.Count > 0 && p == null) p = stack.Pop(); // skip any destroyed entries
            if (p == null)
            {
                p = Instantiate(prefab);
                p.SetPool(this, prefab);
            }

            p.transform.SetParent(null, false);
            p.transform.SetPositionAndRotation(position, Quaternion.identity);
            p.gameObject.SetActive(true);
            return p;
        }

        /// <summary>Return a projectile to its prefab's pool (called by the projectile itself).</summary>
        public void Release(Projectile p, Projectile prefab)
        {
            if (p == null) return;
            p.gameObject.SetActive(false);
            p.transform.SetParent(transform, false);
            if (prefab != null) StackFor(prefab).Push(p);
        }

        private Stack<Projectile> StackFor(Projectile prefab)
        {
            if (!_pools.TryGetValue(prefab, out Stack<Projectile> stack))
            {
                stack = new Stack<Projectile>();
                _pools[prefab] = stack;
            }
            return stack;
        }
    }
}
