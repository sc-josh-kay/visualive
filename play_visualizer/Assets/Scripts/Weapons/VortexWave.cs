using UnityEngine;

namespace PlayVisualizer.Weapons
{
    /// <summary>
    /// Weapon 6 — Vortex Wave (spec7 §8). The unusual one: instead of flinging projectiles it launches
    /// a slow wave that anchors into a temporary VORTEX which pulls, slows, and damages nearby enemies
    /// and swirls/distorts the visualizer. Bass → radius, and the vortex keeps reacting to the music
    /// for its whole life. Crowd control + visualizer manipulation — energy, never coverage.
    /// </summary>
    public class VortexWave : WeaponBase
    {
        [Header("Vortex Wave")]
        [SerializeField] private Vortex _vortexPrefab;
        [SerializeField] private float _fireRate = 0.6f;
        [SerializeField] private float _travelSpeed = 6f;
        [SerializeField] private float _travelTime = 0.6f;
        [Tooltip("Vortex radius at low/high bass.")]
        [SerializeField] private float _radiusMin = 2.5f;
        [SerializeField] private float _radiusMax = 4f;
        [SerializeField] private float _pullStrength = 4f;
        [Range(0f, 1f)] [SerializeField] private float _slowFactor = 0.2f;
        [SerializeField] private int _damagePerTick = 1;
        [SerializeField] private float _damageInterval = 0.25f;
        [SerializeField] private float _lifetime = 3.5f;
        [SerializeField] private float _pulseInterval = 0.3f;

        public override string DisplayName => "Vortex Wave";
        public override int Order => 5;

        public override void Tick(in WeaponContext ctx)
        {
            if (_vortexPrefab == null || !ctx.IsAiming || !ReadyToFire(_fireRate))
            {
                return;
            }

            float bass = ctx.Music != null ? ctx.Music.Bass : 0f;
            var p = new VortexParams
            {
                TravelSpeed = _travelSpeed,
                TravelTime = _travelTime,
                Radius = Mathf.Lerp(_radiusMin, _radiusMax, bass),
                PullStrength = _pullStrength,
                SlowFactor = _slowFactor,
                DamagePerTick = _damagePerTick,
                DamageInterval = _damageInterval,
                Lifetime = _lifetime,
                PulseInterval = _pulseInterval
            };

            Vortex vortex = Instantiate(_vortexPrefab, ctx.MuzzlePosition, Quaternion.identity);
            vortex.Init(ctx.AimDirection, p, EnergyColor(ctx.Music, 0.6f));
        }
    }
}
