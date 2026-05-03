using System;
using UnityEngine;

/// <summary>
/// Manages the single active power-up slot (GDD §6.1 / §6.4).
/// Only one IPowerUp can be active at a time; collecting a new one replaces the old immediately.
/// </summary>
public class PowerUpSystem : MonoBehaviour
{
    IPowerUp _activePowerUp;
    PlayerBrain _player;
    float _timer;

    // ─── Events (GDD §10.3) ───────────────────────────────────────
    public event Action<PowerUpType> OnActivated;
    public event Action OnDeactivated;

    /// <summary>Currently active power-up type, or null if none.</summary>
    public IPowerUp ActivePowerUp => _activePowerUp;

    /// <summary>Called by PlayerBrain on Start.</summary>
    public void Init(PlayerBrain player)
    {
        _player = player;
    }

    void Update()
    {
        if (_activePowerUp == null) return;

        // Instant power-ups (duration <= 0) don't need a timer
        if (_activePowerUp.Duration <= 0f) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            Deactivate();
        }
    }

    /// <summary>
    /// Activate a power-up by type. If one is already active, it is deactivated first (GDD §6.1).
    /// </summary>
    public void Activate(PowerUpType type)
    {
        if (_player == null)
        {
            Debug.LogWarning("PowerUpSystem: No PlayerBrain assigned.");
            return;
        }

        // Deactivate existing
        if (_activePowerUp != null)
            DeactivateInternal();

        // Create and activate the new power-up
        _activePowerUp = PowerUpFactory.Create(type);
        _activePowerUp.Activate(_player);

        _timer = _activePowerUp.Duration;

        OnActivated?.Invoke(type);

        // Score bonus for collecting a power-up
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddPowerUpBonus();

        // Instant effects auto-deactivate
        if (_activePowerUp.Duration <= 0f)
        {
            DeactivateInternal();
        }
    }

    /// <summary>Force-deactivate the current power-up.</summary>
    public void Deactivate()
    {
        if (_activePowerUp == null) return;
        DeactivateInternal();
    }

    void DeactivateInternal()
    {
        _activePowerUp?.Deactivate();
        _activePowerUp = null;
        _timer = 0f;
        OnDeactivated?.Invoke();
    }

    /// <summary>Remaining duration of the active power-up (for UI timer bar).</summary>
    public float GetRemainingTime() => _timer;

    /// <summary>Normalized 0..1 remaining (for UI fill).</summary>
    public float GetRemainingNormalized()
    {
        if (_activePowerUp == null || _activePowerUp.Duration <= 0f)
            return 0f;
        return Mathf.Clamp01(_timer / _activePowerUp.Duration);
    }
}
