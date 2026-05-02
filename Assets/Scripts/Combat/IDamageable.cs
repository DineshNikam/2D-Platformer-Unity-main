/// <summary>
/// Anything bullets, mines, or hazards can hit implements this (player, enemies, destructibles).
/// </summary>
public interface IDamageable
{
    /// <summary>Apply damage after team / invincibility rules are evaluated by the receiver.</summary>
    void TakeDamage(float amount);
}
