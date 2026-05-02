using System;
using UnityEngine;

/// <summary>
/// Shared HP for player and enemies. Implements IDamageable for bullet / hazard hits.
/// Optional invincibility window uses realtime so hit-stop (timeScale) does not stall i-frames.
/// </summary>
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHp = 5f;
    /// <summary>Seconds of invulnerability after taking damage (0 for typical enemies).</summary>
    [SerializeField] float invincibilityDurationAfterHit = 1.2f;

    float _currentHp;
    float _invincibleUntilRealtime;

    /// <summary>Damage dealt, remaining HP after the hit.</summary>
    public event Action<float, float> Damaged;
    public event Action Died;

    public float MaxHp => maxHp;
    public float CurrentHp => _currentHp;
    public bool IsAlive => _currentHp > 0f;
    public bool IsInvincible => Time.realtimeSinceStartup < _invincibleUntilRealtime;

    void Awake()
    {
        _currentHp = maxHp;
    }

    public void SetMaxHp(float value, bool fillToMax = true)
    {
        maxHp = Mathf.Max(1f, value);
        if (fillToMax)
            _currentHp = maxHp;
        else
            _currentHp = Mathf.Min(_currentHp, maxHp);
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;
        _currentHp = Mathf.Min(maxHp, _currentHp + amount);
    }

    public void RestoreFullHp()
    {
        _currentHp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;
        if (invincibilityDurationAfterHit > 0f && IsInvincible)
            return;

        _currentHp -= amount;
        if (invincibilityDurationAfterHit > 0f)
            _invincibleUntilRealtime = Time.realtimeSinceStartup + invincibilityDurationAfterHit;

        Damaged?.Invoke(amount, Mathf.Max(0f, _currentHp));

        if (_currentHp <= 0f)
        {
            _currentHp = 0f;
            Died?.Invoke();
        }
    }
}
