/// <summary>
/// Damage Boost power-up: bullet damage ×2 for 5 seconds (GDD §6.2).
/// Modifies ShootingSystem.SetDamageMultiplier.
/// </summary>
public class DamageBoostPowerUp : IPowerUp
{
    public PowerUpType Type => PowerUpType.DamageBoost;
    public float Duration => 5f;

    PlayerBrain _player;

    public void Activate(PlayerBrain player)
    {
        _player = player;
        if (player.ShootingSystem != null)
            player.ShootingSystem.SetDamageMultiplier(2f);
    }

    public void Deactivate()
    {
        if (_player != null && _player.ShootingSystem != null)
            _player.ShootingSystem.SetDamageMultiplier(1f);
    }
}
