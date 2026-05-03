/// <summary>
/// Blast Bullets power-up: each bullet deals AoE explosion on hit for 8 seconds (GDD §6.2).
/// 2 unit radius, hits all enemies. Modifies ShootingSystem.SetBlastMode.
/// </summary>
public class BlastBulletsPowerUp : IPowerUp
{
    public PowerUpType Type => PowerUpType.BlastBullets;
    public float Duration => 8f;

    PlayerBrain _player;

    public void Activate(PlayerBrain player)
    {
        _player = player;
        if (player.ShootingSystem != null)
            player.ShootingSystem.SetBlastMode(true);
    }

    public void Deactivate()
    {
        if (_player != null && _player.ShootingSystem != null)
            _player.ShootingSystem.SetBlastMode(false);
    }
}
