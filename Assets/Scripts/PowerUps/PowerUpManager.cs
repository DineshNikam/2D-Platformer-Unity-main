using UnityEngine;

/// <summary>
/// Singleton that handles weighted random power-up selection and spawning (GDD §6.3).
/// Primary source: enemy death (called by EnemyBase.Die).
/// Weights: Health 35%, Shield 30%, DamageBoost 20%, Blast 15%.
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [Header("Pickup Prefab")]
    [Tooltip("Prefab with PowerUpPickup component and trigger collider.")]
    [SerializeField] GameObject powerUpPickupPrefab;

    [Header("Spawn Weights (GDD §6.3)")]
    [SerializeField] float healthWeight      = 35f;
    [SerializeField] float shieldWeight      = 30f;
    [SerializeField] float damageBoostWeight = 20f;
    [SerializeField] float blastWeight       = 15f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Spawn a random power-up pickup at the given world position.
    /// Called by enemies on death if random check passes.
    /// </summary>
    public void SpawnAt(Vector3 position)
    {
        if (powerUpPickupPrefab == null)
        {
            Debug.LogWarning("PowerUpManager: powerUpPickupPrefab not assigned!");
            return;
        }

        PowerUpType type = GetWeightedRandomType();

        GameObject pickupGO = Instantiate(powerUpPickupPrefab, position, Quaternion.identity);
        PowerUpPickup pickup = pickupGO.GetComponent<PowerUpPickup>();
        if (pickup != null)
        {
            pickup.SetType(type);
        }
    }

    /// <summary>Weighted random selection per GDD §6.3.</summary>
    PowerUpType GetWeightedRandomType()
    {
        float total = healthWeight + shieldWeight + damageBoostWeight + blastWeight;
        float roll = Random.Range(0f, total);

        if (roll < healthWeight)
            return PowerUpType.Health;
        roll -= healthWeight;

        if (roll < shieldWeight)
            return PowerUpType.Shield;
        roll -= shieldWeight;

        if (roll < damageBoostWeight)
            return PowerUpType.DamageBoost;

        return PowerUpType.BlastBullets;
    }
}
