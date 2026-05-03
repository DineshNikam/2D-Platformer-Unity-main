using UnityEngine;

/// <summary>
/// Crab Enemy (GDD §5.2). Patrols and drops mines.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CrabEnemy : EnemyBase
{
    public float patrolSpeed = 3f;
    public float mineDropInterval = 2.5f;
    public GameObject minePrefab;
    
    [Header("Edge/Wall Detection")]
    public Transform groundDetection;
    public Transform wallDetection;
    public float detectionRayLength = 1f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator anim;
    private float mineDropTimer;
    private bool movingRight = true;

    private enum State { Patrol, PlaceMine, Dead }
    private State currentState = State.Patrol;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        mineDropTimer = mineDropInterval;
        currentState = State.Patrol;
        if (anim != null) anim.Play("patrol_crab");
    }

    protected override void Die()
    {
        if (currentState == State.Dead) return;
        currentState = State.Dead;
        
        CancelInvoke(); // Stop pending DropMine calls

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false; // Stop physics
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;

        if (anim != null) 
        {
            anim.enabled = true;
            anim.Play("death_crab", 0, 0f); // Force play from start
        }

        if (GameFeel.Instance != null) GameFeel.Instance.Freeze(0.08f, 0f);
        if (Random.value < powerUpDropChance && PowerUpManager.Instance != null) PowerUpManager.Instance.SpawnAt(transform.position);
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddKill(maxHP);

        // Destroy after animation
        Destroy(gameObject, 2f);
    }

    private void Update()
    {
        if (currentState == State.Dead) return;

        if (currentState == State.Patrol)
        {
            mineDropTimer -= Time.deltaTime;
            if (mineDropTimer <= 0f)
            {
                currentState = State.PlaceMine;
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Stop moving
                if (anim != null) anim.Play("Attack_crab");
                Invoke(nameof(DropMine), 0.3f); // 0.3s wind-up
            }
        }
    }

    private void FixedUpdate()
    {
        if (currentState == State.Dead) return;

        // Debug.Log("FixedUpdate running, state: " + currentState);
        if (currentState == State.Patrol)
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        // Move
        rb.linearVelocity = new Vector2((movingRight ? 1 : -1) * patrolSpeed, rb.linearVelocity.y);

        // Do not check for edges/walls if we are currently falling or jumping
        if (Mathf.Abs(rb.linearVelocity.y) > 0.05f) return;

        // Check edge and wall
        if (groundDetection != null && wallDetection != null)
        {
            RaycastHit2D groundInfo = Physics2D.Raycast(groundDetection.position, Vector2.down, detectionRayLength, groundLayer);
            RaycastHit2D wallInfo = Physics2D.Raycast(wallDetection.position, movingRight ? Vector2.right : Vector2.left, detectionRayLength, groundLayer);

            if (groundInfo.collider == null || wallInfo.collider != null)
            {
                Flip();
            }
        }
    }

    private void Flip()
    {
        movingRight = !movingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    private void DropMine()
    {
        if (currentState == State.Dead) return;

        if (minePrefab != null)
        {
            Vector3 spawnPos = transform.position;
            var boxCol = GetComponent<BoxCollider2D>();
            if (boxCol != null)
            {
                spawnPos.y -= (boxCol.size.y / 2f) * Mathf.Abs(transform.localScale.y);
            }
            Instantiate(minePrefab, spawnPos, Quaternion.identity);
        }
        
        mineDropTimer = mineDropInterval;
        currentState = State.Patrol;
        if (anim != null) anim.Play("patrol_crab");
    }
}
