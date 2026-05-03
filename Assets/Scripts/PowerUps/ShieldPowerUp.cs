using UnityEngine;

/// <summary>
/// Shield power-up: blocks next 2 damage instances (GDD §6.2).
/// Duration is effectively infinite until 2 hits are absorbed.
/// Visual: neon ring around player (enable/disable a child SpriteRenderer).
/// </summary>
public class ShieldPowerUp : IPowerUp
{
    public PowerUpType Type => PowerUpType.Shield;

    /// <summary>
    /// We use a very large duration since shield lasts until 2 hits are absorbed.
    /// The PowerUpSystem timer is cosmetic; actual deactivation happens in PlayerBrain when hits run out.
    /// </summary>
    public float Duration => 999f;

    const int ShieldHitCount = 2;

    PlayerBrain _player;

    public void Activate(PlayerBrain player)
    {
        _player = player;
        player.SetShield(ShieldHitCount);

        // Enable shield visual if it exists
        Transform shieldVisual = player.transform.Find("ShieldRing");
        if (shieldVisual != null)
            shieldVisual.gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        if (_player != null)
        {
            _player.ClearShield();

            // Disable shield visual
            Transform shieldVisual = _player.transform.Find("ShieldRing");
            if (shieldVisual != null)
                shieldVisual.gameObject.SetActive(false);
        }
    }
}
