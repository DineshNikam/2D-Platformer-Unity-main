/// <summary>
/// Contract for all power-up effects (GDD §6.1).
/// Only one IPowerUp can be active at a time; collecting a new one replaces the old.
/// </summary>
public interface IPowerUp
{
    PowerUpType Type { get; }

    /// <summary>Duration in seconds. 0 or negative = instant effect (e.g. Health).</summary>
    float Duration { get; }

    /// <summary>Apply the effect to the player. Called once on pickup.</summary>
    void Activate(PlayerBrain player);

    /// <summary>Remove / clean up the effect. Called when duration expires or a new power-up replaces this one.</summary>
    void Deactivate();
}
