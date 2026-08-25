using UnityEngine;
using PlayVisualizer.Player;

namespace PlayVisualizer.Weapons
{
    /// <summary>
    /// Weapon 2 — Scatter (spec7 §4). Area / crowd-clearing: each auto-fire cycle throws a shotgun
    /// fan of several projectiles with lower damage each. Bass → projectile size, Energy → brightness,
    /// Beat → a stronger burst (extra pellets). Overlapping energy trails "splatter" the scene.
    /// (Energy only — no coverage; kills paint.)
    /// </summary>
    public class Scatter : WeaponBase
    {
        [Header("Scatter")]
        [SerializeField] private float _fireRate = 2.2f;
        [SerializeField] private int _count = 6;
        [SerializeField] private int _beatExtra = 2;
        [SerializeField] private float _spreadDegrees = 55f;
        [SerializeField] private float _projectileSpeed = 13f;
        [SerializeField] private float _projectileLifetime = 1.0f;
        [SerializeField] private int _damage = 1;
        [Tooltip("Projectile size at low/high bass.")]
        [SerializeField] private float _scaleMin = 0.7f;
        [SerializeField] private float _scaleMax = 1.3f;
        [SerializeField] private float _impactDistortion = 0.3f;

        public override string DisplayName => "Scatter";
        public override int Order => 1;

        public override void Tick(in WeaponContext ctx)
        {
            if (!ctx.IsAiming || !ReadyToFire(_fireRate))
            {
                return;
            }

            float bass = ctx.Music != null ? ctx.Music.Bass : 0f;
            bool beat = ctx.Music != null && ctx.Music.Beat;
            int count = Mathf.Max(1, _count + (beat ? _beatExtra : 0));
            float scale = Mathf.Lerp(_scaleMin, _scaleMax, bass);
            Color color = EnergyColor(ctx.Music);

            for (int i = 0; i < count; i++)
            {
                // Even fan across the spread (single pellet fires straight).
                float t = count > 1 ? (float)i / (count - 1) : 0.5f;
                float angle = Mathf.Lerp(-_spreadDegrees * 0.5f, _spreadDegrees * 0.5f, t);
                Vector2 dir = Rotate(ctx.AimDirection, angle);

                var spec = new ProjectileSpec
                {
                    Speed = _projectileSpeed,
                    Lifetime = _projectileLifetime,
                    Damage = _damage,
                    Color = color,
                    StartScale = scale,
                    ImpactDistortion = _impactDistortion
                };
                Spawn(ctx, dir, spec);
            }
        }
    }
}
