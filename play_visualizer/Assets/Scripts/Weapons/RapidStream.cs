using UnityEngine;
using PlayVisualizer.Player;

namespace PlayVisualizer.Weapons
{
    /// <summary>
    /// Weapon 4 — Rapid Stream (spec7 §6). Volume / aggressive sustained fire: the fire rate RAMPS UP
    /// the longer the player keeps aiming, and decays when they stop (no fire button — the aiming
    /// state is the trigger). Energy → the maximum rate, Beat → a momentary rate burst, Bass → impact
    /// size. "Increasing the brush-stroke frequency until the scene saturates." (Energy only.)
    /// </summary>
    public class RapidStream : WeaponBase
    {
        [Header("Rapid Stream")]
        [SerializeField] private float _baseRate = 4f;
        [Tooltip("Max fire rate at low/high Energy (the ramp climbs toward this).")]
        [SerializeField] private float _maxRateLow = 10f;
        [SerializeField] private float _maxRateHigh = 18f;
        [Tooltip("Ramp climb / decay per second (0..1 over these seconds).")]
        [SerializeField] private float _rampUpPerSec = 0.45f;
        [SerializeField] private float _rampDownPerSec = 1.2f;
        [SerializeField] private float _beatRateMult = 1.5f;
        [SerializeField] private float _projectileSpeed = 18f;
        [SerializeField] private float _projectileLifetime = 1.2f;
        [SerializeField] private int _damage = 1;
        [SerializeField] private float _impactBase = 0.25f;

        private float _ramp; // 0..1

        public override string DisplayName => "Rapid Stream";
        public override int Order => 3;

        public override void OnEquip()
        {
            base.OnEquip();
            _ramp = 0f; // each time you switch to it, the ramp restarts
        }

        public override void Tick(in WeaponContext ctx)
        {
            float dt = ctx.Dt;
            if (!ctx.IsAiming)
            {
                _ramp = Mathf.Max(0f, _ramp - _rampDownPerSec * dt);
                return;
            }

            _ramp = Mathf.Min(1f, _ramp + _rampUpPerSec * dt);

            float energy = ctx.Music != null ? ctx.Music.Energy : 0f;
            float bass = ctx.Music != null ? ctx.Music.Bass : 0f;
            bool beat = ctx.Music != null && ctx.Music.Beat;

            float maxRate = Mathf.Lerp(_maxRateLow, _maxRateHigh, energy);
            float rate = Mathf.Lerp(_baseRate, maxRate, _ramp);
            if (beat) rate *= _beatRateMult;

            if (!ReadyToFire(rate)) return;

            var spec = new ProjectileSpec
            {
                Speed = _projectileSpeed,
                Lifetime = _projectileLifetime,
                Damage = _damage,
                Color = EnergyColor(ctx.Music),
                StartScale = 1f,
                ImpactDistortion = _impactBase + 0.4f * bass
            };
            Spawn(ctx, ctx.AimDirection, spec);
        }
    }
}
