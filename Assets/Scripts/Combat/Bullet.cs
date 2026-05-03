using UnityEngine;

/// <summary>
/// Projectile fired by <see cref="ShootingSystem"/> (GDD §4.2).
/// Travels horizontally, hits first IDamageable, optional AoE blast, auto-returns to pool after lifetime.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Defaults — overridden by Init()")]
    [SerializeField] float defaultSpeed = 18f;
    [SerializeField] float defaultDamage = 1f;
    [SerializeField] float lifetime = 3f;

    [Tooltip("Layer mask for AoE overlap (blast mode). Should include enemy layers.")]
    [SerializeField] LayerMask aoeDamageableMask;

    [Tooltip("Layer mask for solid obstacles (ground/walls). Bullets destroy on contact.")]
    [SerializeField] LayerMask groundMask;

    [Tooltip("Radius of the AoE explosion when isBlast is true (GDD §6.2 Blast Bullets).")]
    [SerializeField] float aoeRadius = 2f;

    float _speed;
    float _damage;
    int _direction;    // +1 right, -1 left
    bool _isBlast;
    float _timer;

    BulletPool _pool;
    bool _isReturning;

    /// <summary>Called by <see cref="BulletPool"/> on creation to wire the return path.</summary>
    public void SetPool(BulletPool pool) => _pool = pool;

    /// <summary>
    /// Configure and launch the bullet. Called by <see cref="ShootingSystem"/> each shot.
    /// </summary>

    public void Init(int direction, float damage, bool isBlast, float speed = 0f)
    {
        _direction = direction;
        _damage    = damage;
        _isBlast   = isBlast;
        _speed     = speed > 0f ? speed : defaultSpeed;
        _timer     = lifetime; // Reset timer BEFORE activation
        _isReturning = false;
        
        gameObject.SetActive(true);

        System.IO.File.AppendAllText("BulletDebugLog.txt", 
            $"[{System.DateTime.Now:HH:mm:ss.fff}] Bullet Init: dir={_direction}, speed={_speed}, damage={_damage}, blast={isBlast}, pos={transform.position}\n");
    }

    void Update()
    {
        if (_isReturning) return;

        float frameDist = _speed * Time.deltaTime;
        Vector2 direction = Vector2.right * _direction;
        
        // Cast ahead to see if we hit something this frame
        // Using 0.2f radius (slightly smaller than collider) for the cast to be safe
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.2f, direction, frameDist, groundMask | aoeDamageableMask);
        
        // Only process the hit if it's NOT the player
        if (hit.collider != null && !hit.collider.CompareTag("Player"))
        {
            // Process hit logic immediately
            ProcessHit(hit.collider);
            return;
        }

        transform.Translate(Vector3.right * _direction * frameDist);

        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            System.IO.File.AppendAllText("BulletDebugLog.txt", 
                $"[{System.DateTime.Now:HH:mm:ss.fff}] Bullet Lifetime Expired at {transform.position}\n");
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        ProcessHit(col);
    }

    private void ProcessHit(Collider2D col)
    {
        if (_isReturning) return;
        if (col.CompareTag("Player")) return;

        Vector3 hitPos = transform.position;
        string hitInfo = $"[{System.DateTime.Now:HH:mm:ss.fff}] Bullet Hit: {col.name} (Layer {col.gameObject.layer}) at {hitPos}";
        
        if (col.TryGetComponent<IDamageable>(out var target))
        {
            _isReturning = true;
            target.TakeDamage(_damage);

            if (_isBlast) DoAoE();
            ReturnToPool();
        }
        else if (((1 << col.gameObject.layer) & groundMask.value) != 0)
        {
            _isReturning = true;
            if (_isBlast) DoAoE();
            ReturnToPool();
        }
    }

    /// <summary>AoE damage in a circle around the hit point (GDD §6.2 Blast Bullets).</summary>
    void DoAoE()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius, aoeDamageableMask);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var aoeTarget))
                aoeTarget.TakeDamage(_damage);
        }
    }

    public void ReturnToPool()
    {
        if (_pool != null)
            _pool.Return(this);
        else
            gameObject.SetActive(false);
    }

    void OnDisable()
    {
        _timer = 0f;
    }
}
