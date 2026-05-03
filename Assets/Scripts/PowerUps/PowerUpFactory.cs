/// <summary>
/// Factory for creating IPowerUp instances from a PowerUpType enum (GDD §6.4).
/// </summary>
public static class PowerUpFactory
{
    public static IPowerUp Create(PowerUpType type)
    {
        return type switch
        {
            PowerUpType.Shield      => new ShieldPowerUp(),
            PowerUpType.Health      => new HealthPowerUp(),
            PowerUpType.DamageBoost => new DamageBoostPowerUp(),
            PowerUpType.BlastBullets => new BlastBulletsPowerUp(),
            _                       => new HealthPowerUp(), // fallback
        };
    }
}
