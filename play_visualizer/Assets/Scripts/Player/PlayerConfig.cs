using UnityEngine;

namespace PlayVisualizer.Player
{
    /// <summary>
    /// Data-driven tuning values for the player. A ScriptableObject so values can be
    /// edited in the inspector, swapped, and (later) driven by the music system without
    /// touching player code.
    /// Create via: Assets → Create → PlayVisualizer → Player Config.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "PlayVisualizer/Player Config")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Units per second at full stick/key deflection.")]
        public float MoveSpeed = 8f;

        [Header("Shooting")]
        [Tooltip("Shots per second while the fire button is held.")]
        public float FireRate = 8f;

        [Tooltip("Projectile travel speed in units per second.")]
        public float ProjectileSpeed = 16f;

        [Tooltip("Seconds before a projectile despawns on its own.")]
        public float ProjectileLifetime = 1.5f;

        [Tooltip("Damage each projectile deals to an enemy.")]
        public int ProjectileDamage = 1;

        [Header("Trail / paint expression")]
        [Tooltip("Paint gain when the player is standing still. The trail fades toward this floor " +
                 "(never fully off) so staying put lets the world go mostly dark but not black. 0..1.")]
        [Range(0f, 1f)] public float TrailStillFloor = 0.04f;

        [Tooltip("Fraction of MoveSpeed at which the trail paints at full strength. Lower = the " +
                 "trail 'lights up' with only a little movement.")]
        [Range(0.05f, 1f)] public float TrailFullSpeedFraction = 0.5f;

        [Tooltip("Seconds for the trail to recover after an enemy clips the player. During recovery " +
                 "the trail is cut AND the existing smoke dissipates, so getting hit visibly clears " +
                 "your painting for a second or two.")]
        public float HitDisruptRecovery = 1.2f;
    }
}
