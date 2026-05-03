using UnityEngine;

/// <summary>
/// Health power-up: instant restore 30–50% of max HP (GDD §6.2).
/// Cannot overheal past max. Instant effect (Duration = 0).
/// </summary>
public class HealthPowerUp : IPowerUp
{
    public PowerUpType Type => PowerUpType.Health;
    public float Duration => 0f; // Instant

    public void Activate(PlayerBrain player)
    {
        if (player == null || player.Health == null) return;

        float maxHp = player.Health.MaxHp;
        float restorePercent = Random.Range(0.3f, 0.5f); // 30–50%
        float healAmount = maxHp * restorePercent;

        player.Health.Heal(healAmount);
    }

    public void Deactivate()
    {
        // Instant effect — nothing to clean up
    }
}
