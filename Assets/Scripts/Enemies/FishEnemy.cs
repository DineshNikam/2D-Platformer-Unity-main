using UnityEngine;

/// <summary>
/// Fish Enemy (GDD §5.2). Flies in formation and leader shoots arc projectiles.
/// </summary>
public class FishEnemy : EnemyBase
{
    [Header("Formation")]
    public FishEnemy leader;
    public Vector2 formationOffset;
    public bool isLeader;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float sineFrequency = 2f;
    public float sineAmplitude = 0.5f;

    [Header("Combat")]
    public float fireInterval = 3f;
    public GameObject projectilePrefab;
    
    private float fireTimer;
    private float startY;
    private Transform playerTransform;

    protected override void Awake()
    {
        base.Awake();
        startY = transform.position.y;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        fireTimer = fireInterval;
        
        PlayerBrain pb = FindFirstObjectByType<PlayerBrain>();
        if (pb != null)
        {
            playerTransform = pb.transform;
        }
    }

    private void Update()
    {
        Move();

        if (isLeader)
        {
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                Fire();
                fireTimer = fireInterval;
            }
        }
    }

    private void Move()
    {
        if (isLeader || leader == null)
        {
            // Move leftwards and oscillate
            float newX = transform.position.x - moveSpeed * Time.deltaTime;
            float newY = startY + Mathf.Sin(Time.time * sineFrequency) * sineAmplitude;
            transform.position = new Vector3(newX, newY, transform.position.z);
        }
        else
        {
            // Follow leader with offset
            if (leader != null)
            {
                Vector3 targetPos = leader.transform.position + (Vector3)formationOffset;
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
            }
        }
    }

    private void Fire()
    {
        if (projectilePrefab != null && playerTransform != null)
        {
            GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            FishProjectile fp = proj.GetComponent<FishProjectile>();
            if (fp != null)
            {
                // Simple arc toward player
                Vector2 direction = (playerTransform.position - transform.position).normalized;
                fp.Initialize(direction);
            }
        }
    }
}
