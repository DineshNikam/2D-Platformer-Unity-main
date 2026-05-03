using UnityEngine;

/// <summary>
/// Auto-fire shooting system (GDD §4.1).
/// Fires bullets automatically on a timer in the player's facing direction.
/// No player input required — this is the core design decision of Neon Runner.
/// </summary>
public class ShootingSystem : MonoBehaviour
{
    [Header("Fire Settings (GDD §4.1)")]
    [Tooltip("Seconds between shots. 0.25 = 4 shots/sec.")]
    [SerializeField] float fireRate = 0.25f;

    [Tooltip("Base damage per bullet. Doubled by DamageBoost power-up.")]
    [SerializeField] float baseDamage = 1f;

    [Tooltip("Bullet travel speed in units/sec.")]
    [SerializeField] float bulletSpeed = 18f;

    [Header("Audio (GDD §12.1)")]
    [SerializeField] private AudioClip shootSFX;

    PlayerBrain _player;
    float _timer;
    float _damageMultiplier = 1f;
    bool _isBlastMode;

    /// <summary>Wire to the PlayerBrain that owns this system.</summary>
    public void Init(PlayerBrain player)
    {
        _player = player;
    }

    void Update()
    {
        if (_player == null) return;
        
        // Don't fire if the player is dead
        if (_player.Health != null && !_player.Health.IsAlive) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            Fire();
            _timer = fireRate;
        }
    }

    void Fire()
    {
        if (BulletPool.Instance == null)
        {
            Debug.LogWarning("ShootingSystem: BulletPool.Instance is null. Cannot fire.");
            return;
        }

        Transform gunPoint = _player.GunPoint;
        if (gunPoint == null)
        {
            Debug.LogWarning("ShootingSystem: GunPoint is null on PlayerBrain.");
            return;
        }

        Bullet bullet = BulletPool.Instance.Get();
        if (bullet == null) return;

        bullet.transform.position = gunPoint.position;
        bullet.transform.rotation = Quaternion.identity;

        // Flip bullet sprite to match direction
        float scaleX = Mathf.Abs(bullet.transform.localScale.x) * _player.FacingDirection;
        Vector3 ls = bullet.transform.localScale;
        bullet.transform.localScale = new Vector3(scaleX, ls.y, ls.z);

        bullet.Init(
            direction: _player.FacingDirection,
            damage:    baseDamage * _damageMultiplier,
            isBlast:   _isBlastMode,
            speed:     bulletSpeed
        );

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(shootSFX);
    }

    /// <summary>Called by DamageBoost power-up (GDD §6.2). Pass 1f to reset.</summary>
    public void SetDamageMultiplier(float mult)
    {
        _damageMultiplier = Mathf.Max(0.1f, mult);
    }

    /// <summary>Called by BlastBullets power-up (GDD §6.2). Bullets do AoE on hit.</summary>
    public void SetBlastMode(bool on)
    {
        _isBlastMode = on;
    }
}
