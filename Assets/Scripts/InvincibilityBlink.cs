using UnityEngine;

/// <summary>
/// Toggles the player's SpriteRenderer on/off during invincibility frames (GDD P4-11).
/// Blinks every 0.1s while <see cref="Health.IsInvincible"/> is true.
/// Ensures renderer is re-enabled when invincibility ends.
/// </summary>
public class InvincibilityBlink : MonoBehaviour
{
    [Tooltip("The SpriteRenderer to blink. Auto-finds in children if not set.")]
    [SerializeField] SpriteRenderer spriteRenderer;

    [Tooltip("How fast to toggle (seconds per blink cycle).")]
    [SerializeField] float blinkInterval = 0.1f;

    Health _health;
    float _blinkTimer;
    bool _wasInvincible;

    void Start()
    {
        _health = GetComponent<Health>() ?? GetComponentInParent<Health>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (_health == null)
            Debug.LogWarning("InvincibilityBlink: No Health component found.", this);
    }

    void Update()
    {
        if (_health == null || spriteRenderer == null) return;

        if (_health.IsInvincible)
        {
            _blinkTimer -= Time.deltaTime;
            if (_blinkTimer <= 0f)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                _blinkTimer = blinkInterval;
            }
            _wasInvincible = true;
        }
        else if (_wasInvincible)
        {
            // Invincibility just ended — ensure renderer is enabled
            spriteRenderer.enabled = true;
            _wasInvincible = false;
            _blinkTimer = 0f;
        }
    }

    void OnDisable()
    {
        // Safety: always re-enable on disable
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }
}
