using UnityEngine;
using PlayVisualizer.Player;

namespace PlayVisualizer.Weapons
{
    /// <summary>
    /// Weapon 1 — Pulse Stream (spec7 §3). The baseline, precision weapon and the control/reference:
    /// a fast, single straight projectile per shot. Each projectile is a fine glowing energy filament
    /// (its trail), tinted by the music; on impact it briefly ripples the visualizer (energy, not
    /// coverage). "Drawing with a fine glowing pen."
    /// </summary>
    public class PulseStream : WeaponBase
    {
        [Header("Pulse Stream")]
        [SerializeField] private float _fireRate = 5f;
        [SerializeField] private float _projectileSpeed = 18f;
        [SerializeField] private float _projectileLifetime = 1.4f;
        [SerializeField] private int _damage = 1;
        [Tooltip("Transient ripple strength where a shot hits (energy interaction, no coverage).")]
        [SerializeField] private float _impactDistortion = 0.5f;

        public override string DisplayName => "Pulse Stream";
        public override int Order => 0;

        public override void Tick(in WeaponContext ctx)
        {
            if (!ctx.IsAiming || !ReadyToFire(_fireRate))
            {
                return;
            }

            var spec = new ProjectileSpec
            {
                Speed = _projectileSpeed,
                Lifetime = _projectileLifetime,
                Damage = _damage,
                Color = EnergyColor(ctx.Music),
                StartScale = 1f,
                ImpactDistortion = _impactDistortion
            };
            Spawn(ctx, ctx.AimDirection, spec);
        }
    }
}
