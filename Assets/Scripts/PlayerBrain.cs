using System;
using UnityEngine;

/// <summary>
/// Central player orchestrator (GDD §10.2).
/// Lives on the same GameObject as <see cref="PlayerController"/>.
/// Owns references to all player sub-systems and exposes events for external listeners
/// (GameManager, UIManager, CameraSystem, GameFeel).
/// </summary>
public class PlayerBrain : MonoBehaviour
{
    [Header("Sub-System References")]
    [Tooltip("Auto-assigned if on same GO or children.")]
    [SerializeField] PlayerController controller;

    [Tooltip("Auto-assigned if on same GO.")]
    [SerializeField] Health health;

    [Tooltip("Auto-assigned if on same GO or children.")]
    [SerializeField] ShootingSystem shootingSystem;

    [Tooltip("Auto-assigned if on same GO or children.")]
    [SerializeField] PowerUpSystem powerUpSystem;

    [Header("Player Setup")]
    [Tooltip("Child transform at the barrel tip where bullets spawn.")]
    [SerializeField] Transform gunPoint;

    [Header("Audio (GDD §12.1)")]
    [SerializeField] private AudioClip hurtSFX;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip shieldAbsorbSFX;

    // ─── Public API ────────────────────────────────────────────────

    /// <summary>+1 (right) or -1 (left), driven by the controller's sprite flip.</summary>
    public int FacingDirection => controller != null ? controller.FacingDirection : 1;

    /// <summary>World-space point where bullets originate.</summary>
    public Transform GunPoint => gunPoint;

    /// <summary>Reference to the Health component for external reads.</summary>
    public Health Health => health;

    /// <summary>Reference to the ShootingSystem for power-up modifications.</summary>
    public ShootingSystem ShootingSystem => shootingSystem;

    /// <summary>Reference to the PowerUpSystem for pickup activation.</summary>
    public PowerUpSystem PowerUpSystem => powerUpSystem;

    // ─── Events (GDD §10.3) ───────────────────────────────────────

    /// <summary>Fired when the player dies (HP <= 0).</summary>
    public event Action OnDied;

    /// <summary>Fired when the player takes damage. Arg = damage amount.</summary>
    public event Action<float> OnDamaged;

    // ─── Shield (set by ShieldPowerUp) ────────────────────────────

    int _shieldHitsRemaining;

    /// <summary>Whether a shield is currently absorbing hits.</summary>
    public bool HasShield => _shieldHitsRemaining > 0;

    /// <summary>Called by ShieldPowerUp to grant hit-absorbing shield.</summary>
    public void SetShield(int hits) => _shieldHitsRemaining = Mathf.Max(0, hits);

    /// <summary>Called by ShieldPowerUp to remove shield on deactivation.</summary>
    public void ClearShield() => _shieldHitsRemaining = 0;

    // ─── Unity Lifecycle ──────────────────────────────────────────

    void Awake()
    {
        // Auto-resolve references if not assigned in Inspector
        if (controller == null)  controller    = GetComponent<PlayerController>();
        if (health == null)      health        = GetComponent<Health>();
        if (shootingSystem == null) shootingSystem = GetComponentInChildren<ShootingSystem>();
        if (powerUpSystem == null)  powerUpSystem  = GetComponentInChildren<PowerUpSystem>();
    }

    void Start()
    {
        // Wire shooting system
        if (shootingSystem != null)
            shootingSystem.Init(this);

        // Wire power-up system
        if (powerUpSystem != null)
            powerUpSystem.Init(this);

        // Subscribe to Health events
        if (health != null)
        {
            health.Damaged += HandleDamaged;
            health.Died    += HandleDied;
        }
        else
        {
            Debug.LogWarning("PlayerBrain: No Health component found!", this);
        }
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= HandleDamaged;
            health.Died    -= HandleDied;
        }
    }

    // ─── Damage Pipeline ──────────────────────────────────────────

    /// <summary>
    /// External entry point for applying damage to the player.
    /// Shield absorbs before Health takes the hit.
    /// </summary>
    public void ApplyDamage(float amount)
    {
        Debug.Log($"[PlayerBrain] ApplyDamage called with amount {amount}");

        if (health == null)
        {
            Debug.LogWarning("[PlayerBrain] Cannot apply damage because Health component is null!");
            return;
        }
        
        if (!health.IsAlive)
        {
            Debug.Log("[PlayerBrain] Cannot apply damage because player is already dead (!health.IsAlive)");
            return;
        }

        // Shield absorbs the hit entirely
        if (_shieldHitsRemaining > 0)
        {
            _shieldHitsRemaining--;
            Debug.Log($"[PlayerBrain] Shield absorbed the hit! Shields remaining: {_shieldHitsRemaining}");
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(shieldAbsorbSFX);
            return;
        }

        Debug.Log($"[PlayerBrain] Sending {amount} damage to Health component...");
        health.TakeDamage(amount);
    }

    // ─── Event Handlers ───────────────────────────────────────────

    void HandleDamaged(float amount, float remaining)
    {
        OnDamaged?.Invoke(amount);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(hurtSFX);

        // Trigger game feel
        if (GameFeel.Instance != null)
        {
            GameFeel.Instance.Freeze(0.2f, 0f);     // GDD §4.3: Player hit
            GameFeel.Instance.Shake(0.3f, 0.15f);
        }
    }

    void HandleDied()
    {
        OnDied?.Invoke();

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(deathSFX);

        // Trigger game feel
        if (GameFeel.Instance != null)
        {
            GameFeel.Instance.Freeze(0.5f, 0f);     // GDD §4.3: Player death
            GameFeel.Instance.Shake(0.6f, 0.4f);
        }
    }
}
