using UnityEngine;
using PlayVisualizer.Player;

namespace PlayVisualizer.Weapons
{
    /// <summary>
    /// Weapon 3 — Growth Stream (spec7 §5). Long-range / sustained fire: a continuous stream of
    /// projectiles that START SMALL and GROW as they travel, gaining damage with size (the projectile
    /// scales its own damage). Tactical tradeoff: keep enemies at range to let shots become powerful.
    /// Bass → growth rate + max size, Energy → brightness, Beat → a temporary growth pulse.
    /// "Throwing expanding drops of paint into the distance." (Energy only — kills paint.)
    /// </summary>
    public class GrowthStream : WeaponBase
    {
        [Header("Growth Stream")]
        [SerializeField] private float _fireRate = 6f;
        [SerializeField] private float _projectileSpeed = 11f;
        [SerializeField] private float _projectileLifetime = 1.7f;
        [SerializeField] private int _damage = 1;
        [SerializeField] private float _startScale = 0.35f;
        [Tooltip("Scale growth per second at low/high bass.")]
        [SerializeField] private float _growthMin = 1.2f;
        [SerializeField] private float _growthMax = 3.0f;
        [Tooltip("Maximum scale at low/high bass.")]
        [SerializeField] private float _maxScaleMin = 1.8f;
        [SerializeField] private float _maxScaleMax = 3.2f;
        [SerializeField] private float _beatGrowthMult = 1.6f;
        [SerializeField] private float _impactDistortion = 0.45f;

        public override string DisplayName => "Growth Stream";
        public override int Order => 2;

        public override void Tick(in WeaponContext ctx)
        {
            if (!ctx.IsAiming || !ReadyToFire(_fireRate))
            {
                return;
            }

            float bass = ctx.Music != null ? ctx.Music.Bass : 0f;
            bool beat = ctx.Music != null && ctx.Music.Beat;
            float growth = Mathf.Lerp(_growthMin, _growthMax, bass) * (beat ? _beatGrowthMult : 1f);
            float maxScale = Mathf.Lerp(_maxScaleMin, _maxScaleMax, bass);

            var spec = new ProjectileSpec
            {
                Speed = _projectileSpeed,
                Lifetime = _projectileLifetime,
                Damage = _damage,
                Color = EnergyColor(ctx.Music),
                StartScale = _startScale,
                GrowthPerSecond = growth,
                MaxScale = maxScale,
                ImpactDistortion = _impactDistortion
            };
            Spawn(ctx, ctx.AimDirection, spec);
        }
    }
}
