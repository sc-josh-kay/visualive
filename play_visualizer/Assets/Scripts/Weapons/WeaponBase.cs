using UnityEngine;
using PlayVisualizer.Audio;
using PlayVisualizer.Player;

namespace PlayVisualizer.Weapons
{
    /// <summary>Per-frame firing context handed to the active weapon by the WeaponController.</summary>
    public struct WeaponContext
    {
        public Vector2 AimDirection;
        public Vector3 MuzzlePosition;
        public bool IsAiming;
        public MusicState Music;
        public float Dt;
    }

    /// <summary>
    /// Base for every auto-fire weapon (spec7). Weapons differ in HOW they fling ENERGY and interact
    /// with the visualizer — never by firing input (there is no fire button) and never by painting
    /// coverage (only movement and enemy deaths do that). Each weapon owns its own fire timing.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected Projectile _projectilePrefab;

        protected float _nextFire;

        /// <summary>Display name for the debug weapon label.</summary>
        public abstract string DisplayName { get; }

        /// <summary>Cycle order (Pulse 0, Scatter 1, …) so switching is deterministic.</summary>
        public abstract int Order { get; }

        /// <summary>Called when this weapon becomes active — reset ramps/timers.</summary>
        public virtual void OnEquip() { _nextFire = 0f; }

        public virtual void OnUnequip() { }

        /// <summary>Advance and auto-fire. Called every frame by the controller while active.</summary>
        public abstract void Tick(in WeaponContext ctx);

        /// <summary>Simple shared fire-rate gate.</summary>
        protected bool ReadyToFire(float rate)
        {
            if (Time.time < _nextFire) return false;
            _nextFire = Time.time + 1f / Mathf.Max(0.01f, rate);
            return true;
        }

        /// <summary>Spawn one projectile from the muzzle along <paramref name="dir"/>.</summary>
        protected Projectile Spawn(in WeaponContext ctx, Vector2 dir, ProjectileSpec spec)
        {
            if (_projectilePrefab == null) return null;
            spec.Direction = dir;
            Projectile p = Instantiate(_projectilePrefab, ctx.MuzzlePosition, Quaternion.identity);
            p.Launch(spec);
            return p;
        }

        /// <summary>
        /// Bright energy color from the current musical moment: hue ← spectral brightness, value ←
        /// Energy (so louder passages fire brighter). Shared by all weapons (spec7 §9).
        /// </summary>
        protected static Color EnergyColor(MusicState s, float saturation = 0.7f)
        {
            float hue = s != null ? Mathf.Repeat(s.SpectralCentroid, 1f) : 0.6f;
            float val = s != null ? Mathf.Lerp(0.65f, 1f, s.Energy) : 1f;
            return Color.HSVToRGB(hue, saturation, val);
        }

        /// <summary>Rotate a 2D vector by <paramref name="degrees"/> (for spread/fan patterns).</summary>
        protected static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
