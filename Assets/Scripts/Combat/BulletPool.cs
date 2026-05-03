using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Object pool for <see cref="Bullet"/> instances using Unity 6 built-in ObjectPool (GDD §10.4).
/// Pool size: 20 bullets. At 4/sec with 3s lifetime ≈ 12 concurrent max; 20 gives headroom.
/// </summary>
public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [SerializeField] Bullet bulletPrefab;
    [Tooltip("Number of bullets to pre-instantiate on Awake.")]
    [SerializeField] int prewarmCount = 20;

    ObjectPool<Bullet> _pool;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _pool = new ObjectPool<Bullet>(
            createFunc:      CreateBullet,
            actionOnGet:     OnGetBullet,
            actionOnRelease: OnReleaseBullet,
            actionOnDestroy: OnDestroyBullet,
            collectionCheck: false,
            defaultCapacity: prewarmCount,
            maxSize:         prewarmCount * 2
        );

        // Prewarm
        var temp = new Bullet[prewarmCount];
        for (int i = 0; i < prewarmCount; i++)
            temp[i] = _pool.Get();
        for (int i = 0; i < prewarmCount; i++)
            _pool.Release(temp[i]);
    }

    public Bullet Get() => _pool.Get();

    public void Return(Bullet bullet)
    {
        if (bullet == null) return;
        _pool.Release(bullet);
    }

    Bullet CreateBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletPool: bulletPrefab is not assigned!", this);
            return null;
        }
        Bullet b = Instantiate(bulletPrefab, transform);
        b.SetPool(this);
        b.gameObject.SetActive(false);
        return b;
    }

    void OnGetBullet(Bullet b)
    {
        // We no longer call SetActive(true) here.
        // ShootingSystem calls bullet.Init() which handles activation after setup.
    }

    void OnReleaseBullet(Bullet b)
    {
        if (b != null)
            b.gameObject.SetActive(false);
    }

    void OnDestroyBullet(Bullet b)
    {
        if (b != null)
            Destroy(b.gameObject);
    }
}
