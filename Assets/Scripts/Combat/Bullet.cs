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

    [Header("Visuals")]
    [SerializeField] SpriteRenderer rootRenderer;
    [SerializeField] GameObject blastVisual;
    [SerializeField] float blastDuration = 0.35f;

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
        
        if (rootRenderer != null) rootRenderer.enabled = true;
        if (blastVisual != null) blastVisual.SetActive(false);
        
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (_isReturning)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0) ReturnToPool();
            return;
        }

        float frameDist = _speed * Time.deltaTime;
        Vector2 direction = Vector2.right * _direction;
        
        // Cast ahead to see if we hit something this frame
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.2f, direction, frameDist, groundMask | aoeDamageableMask);
        
        if (hit.collider != null && !hit.collider.CompareTag("Player"))
        {
            ProcessHit(hit.collider);
            return;
        }

        transform.Translate(Vector3.right * _direction * frameDist);

        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
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

        bool hitSomething = false;
        if (col.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(_damage);
            hitSomething = true;
        }
        else if (((1 << col.gameObject.layer) & groundMask.value) != 0)
        {
            hitSomething = true;
        }

        if (hitSomething)
        {
            _isReturning = true;
            _timer = blastDuration; // Repurpose timer for blast duration
            
            if (rootRenderer != null) rootRenderer.enabled = false;
            if (blastVisual != null) blastVisual.SetActive(true);

            if (_isBlast) DoAoE();
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
