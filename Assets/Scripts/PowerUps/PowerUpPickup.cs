using UnityEngine;

/// <summary>
/// World pickup item for power-ups (GDD §6.3).
/// Trigger collider — player walks into it to collect.
/// Type is set at spawn time by <see cref="PowerUpManager"/>.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PowerUpPickup : MonoBehaviour
{
    [SerializeField] PowerUpType powerUpType;

    [Header("Visual Tints (placeholder differentiation)")]
    [SerializeField] Color shieldColor      = Color.cyan;
    [SerializeField] Color healthColor      = Color.green;
    [SerializeField] Color damageBoostColor = new Color(1f, 0.5f, 0f); // orange
    [SerializeField] Color blastColor       = new Color(1f, 0.2f, 0.8f); // pink

    [Header("Auto-Destroy")]
    [Tooltip("Seconds before the pickup disappears if not collected.")]
    [SerializeField] float despawnTime = 10f;

    SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        ApplyVisualTint();
        Destroy(gameObject, despawnTime);
    }

    /// <summary>Called by PowerUpManager to set the type after instantiation.</summary>
    public void SetType(PowerUpType type)
    {
        powerUpType = type;
        ApplyVisualTint();
    }

    void ApplyVisualTint()
    {
        if (_sr == null) return;

        _sr.color = powerUpType switch
        {
            PowerUpType.Shield      => shieldColor,
            PowerUpType.Health      => healthColor,
            PowerUpType.DamageBoost => damageBoostColor,
            PowerUpType.BlastBullets => blastColor,
            _                       => Color.white,
        };
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        // Find the PowerUpSystem on the player (via PlayerBrain)
        PlayerBrain brain = collision.GetComponent<PlayerBrain>()
                         ?? collision.GetComponentInParent<PlayerBrain>();
        if (brain == null || brain.PowerUpSystem == null)
        {
            Debug.LogWarning("PowerUpPickup: Player has no PlayerBrain or PowerUpSystem.");
            return;
        }

        // GameFeel on pickup (GDD §4.3: 0.04s @ 0.2 scale)
        if (GameFeel.Instance != null)
            GameFeel.Instance.Freeze(0.04f, 0.2f);

        brain.PowerUpSystem.Activate(powerUpType);
        Destroy(gameObject);
    }
}
