namespace PlayVisualizer.Gameplay
{
    /// <summary>
    /// Anything a projectile can damage. Keeps the projectile decoupled from the concrete
    /// Enemy type, and lets future destructible things opt in without changing the shooter.
    /// Deliberately NOT implemented by the player's Health, so player bullets can never
    /// damage the player.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(int amount);
    }
}
