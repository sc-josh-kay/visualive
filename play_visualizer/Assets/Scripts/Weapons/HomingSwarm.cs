using UnityEngine;
using PlayVisualizer.Player;

namespace PlayVisualizer.Weapons
{
    /// <summary>
    /// Weapon 5 — Homing Swarm (spec7 §7). Multi-target / low-aim-pressure: each cycle launches a
    /// handful of small projectiles that fan out, then CURVE toward nearby enemies (visibly, not
    /// snapping) — painting a web of curved lines. Treble → projectile count, Mid → curvature,
    /// Beat → a launch burst, Energy → brightness. (Energy only — kills paint.)
    /// </summary>
    public class HomingSwarm : WeaponBase
    {
        [Header("Homing Swarm")]
        [SerializeField] private float _fireRate = 1.6f;
        [SerializeField] private int _count = 5;
        [SerializeField] private int _trebleExtra = 2;
        [SerializeField] private int _beatExtra = 2;
        [SerializeField] private int _maxCount = 10;
        [Tooltip("Initial fan spread before they curve toward enemies.")]
        [SerializeField] private float _spreadDegrees = 70f;
        [SerializeField] private float _projectileSpeed = 12f;
        [SerializeField] private float _projectileLifetime = 1.7f;
        [SerializeField] private int _damage = 1;
        [Tooltip("Homing curvature at low/high Mid.")]
        [SerializeField] private float _homingMin = 2.5f;
        [SerializeField] private float _homingMax = 6.5f;
        [SerializeField] private float _homingRange = 13f;
        [SerializeField] private float _startScale = 0.5f;
        [SerializeField] private float _impactDistortion = 0.25f;

        public override string DisplayName => "Homing Swarm";
        public override int Order => 4;

        public override void Tick(in WeaponContext ctx)
        {
            if (!ctx.IsAiming || !ReadyToFire(_fireRate))
            {
                return;
            }

            float treble = ctx.Music != null ? ctx.Music.Treble : 0f;
            float mid = ctx.Music != null ? ctx.Music.Mid : 0f;
            bool beat = ctx.Music != null && ctx.Music.Beat;

            int count = _count + Mathf.RoundToInt(treble * _trebleExtra) + (beat ? _beatExtra : 0);
            count = Mathf.Clamp(count, 1, _maxCount);
            float homing = Mathf.Lerp(_homingMin, _homingMax, mid);
            Color color = EnergyColor(ctx.Music);

            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (float)i / (count - 1) : 0.5f;
                float angle = Mathf.Lerp(-_spreadDegrees * 0.5f, _spreadDegrees * 0.5f, t);
                Vector2 dir = Rotate(ctx.AimDirection, angle);

                var spec = new ProjectileSpec
                {
                    Speed = _projectileSpeed,
                    Lifetime = _projectileLifetime,
                    Damage = _damage,
                    Color = color,
                    StartScale = _startScale,
                    HomingStrength = homing,
                    HomingRange = _homingRange,
                    ImpactDistortion = _impactDistortion
                };
                Spawn(ctx, dir, spec);
            }
        }
    }
}
