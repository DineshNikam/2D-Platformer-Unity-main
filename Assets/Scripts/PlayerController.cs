
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Controls { mobile,pc}

public class PlayerController : MonoBehaviour
{


    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float doubleJumpForce = 8f;
    public LayerMask groundLayer;
    public Transform groundCheck;

    [Tooltip("Feet overlap radius. Larger helps Tilemaps / CompositeCollider2D after chunk moves.")]
    [SerializeField] float groundCheckRadius = 0.12f;

    [Tooltip("Ground ray length (used with overlap — whichever hits first).")]
    [SerializeField] float groundRayLength = 0.28f;

    /// <summary>While vertical speed is below this, floor collision can restore jump charges (avoids resetting while moving up through a corner).</summary>
    [SerializeField] float jumpResetMaxUpVelocity = 0.35f;

    private Rigidbody2D rb;
    private bool isGroundedBool = false;
    private bool wasGroundedLastFrame;
    /// <summary>2 after a real landing / standing on floor; decrements per jump. Refill is driven by ground + velocity / floor contacts.</summary>
    private int jumpsRemaining;

    public Animator playeranim;

    bool _animHasRun;
    bool _animHasIsGrounded;

    public Controls controlmode;
   

    private float moveX;
    public bool isPaused = false;

    public ParticleSystem footsteps;
    private ParticleSystem.EmissionModule footEmissions;

    public ParticleSystem ImpactEffect;
    private bool wasonGround;


   // public GameObject projectile;
   // public Transform firePoint;

    public float fireRate = 0.5f; // Time between each shot
    private float nextFireTime = 0f; // Time of the next allowed shot




    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        footEmissions = footsteps.emission;

        if (controlmode == Controls.mobile)
        {
            UIManager.instance.EnableMobileControls();
        }

        CacheAnimatorBoolParams();
    }

    void CacheAnimatorBoolParams()
    {
        _animHasRun = false;
        _animHasIsGrounded = false;
        if (playeranim == null || playeranim.runtimeAnimatorController == null)
            return;

        for (int i = 0; i < playeranim.parameterCount; i++)
        {
            AnimatorControllerParameter p = playeranim.GetParameter(i);
            if (p.type != AnimatorControllerParameterType.Bool)
                continue;
            if (p.name == "run")
                _animHasRun = true;
            else if (p.name == "isGrounded")
                _animHasIsGrounded = true;
        }
    }

    private void Update()
    {
        isGroundedBool = IsGrounded();

        // Refill when we become grounded this frame (air → ground) — good for clean landings.
        if (isGroundedBool && !wasGroundedLastFrame)
            jumpsRemaining = 2;

        wasGroundedLastFrame = isGroundedBool;

        if (controlmode == Controls.pc)
        {
            moveX = Input.GetAxis("Horizontal");
            if (Input.GetButtonDown("Jump"))
                TryConsumeJump();
        }

        if (!isPaused)
        {
            // Calculate rotation angle based on mouse position
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 lookDirection = mousePosition - transform.position;
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

            // ... (your existing code for rotation)

            // Handle shooting
            if (controlmode == Controls.pc && Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + 1f / fireRate; // Set the next allowed fire time
            }
        }
        SetAnimations();

        if (moveX != 0)
        {
            FlipSprite(moveX);
        }

        //impactEffect

        if(!wasonGround && isGroundedBool)
        {
            ImpactEffect.gameObject.SetActive(true);
            ImpactEffect.Stop();
            ImpactEffect.transform.position = new Vector2(footsteps.transform.position.x,footsteps.transform.position.y-0.2f);
            ImpactEffect.Play();
        }

        wasonGround = isGroundedBool;

        
    }
    public void SetAnimations()
    {
        if (playeranim == null)
            return;

        if (_animHasRun)
        {
            if (moveX != 0 && isGroundedBool)
            {
                playeranim.SetBool("run", true);
                footEmissions.rateOverTime = 35f;
            }
            else
            {
                playeranim.SetBool("run", false);
                footEmissions.rateOverTime = 0f;
            }
        }
        else
        {
            footEmissions.rateOverTime = moveX != 0 && isGroundedBool ? 35f : 0f;
        }

        if (_animHasIsGrounded)
            playeranim.SetBool("isGrounded", isGroundedBool);
    }

    private void FlipSprite(float direction)
    {
        if (direction > 0)
        {
            // Moving right, flip sprite to the right
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (direction < 0)
        {
            // Moving left, flip sprite to the left
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }
    private void FixedUpdate()
    {
        // Player movement
        if (controlmode == Controls.pc)
        {
            moveX = Input.GetAxis("Horizontal");
        }
       


        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Zero out vertical velocity
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        if (playeranim != null)
            playeranim.SetTrigger("jump");
    }

    void TryConsumeJump()
    {
        if (jumpsRemaining <= 0)
            return;

        if (jumpsRemaining == 2)
            Jump(jumpForce);
        else
            Jump(doubleJumpForce);

        jumpsRemaining--;
    }

    /// <summary>Ray + overlap: more reliable with tilemaps and moving chunk colliders than a short ray alone.</summary>
    bool IsGrounded()
    {
        if (groundCheck == null)
            return false;

        Vector2 feet = groundCheck.position;
        if (Physics2D.OverlapCircle(feet, groundCheckRadius, groundLayer))
            return true;

        Vector2 rayOrigin = new Vector2(feet.x, feet.y - 0.05f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundRayLength, groundLayer);
        return hit.collider != null;
    }

    /// <summary>If the ray misses but we are standing on a floor collider (common after chunk recycling), restore jumps.</summary>
    void TryRefillJumpsFromFloorCollision(Collision2D collision)
    {
        if (!IsInGroundMask(collision.gameObject.layer))
            return;

        if (rb.linearVelocity.y > jumpResetMaxUpVelocity)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D c = collision.GetContact(i);
            if (c.normal.y > 0.45f)
            {
                jumpsRemaining = 2;
                return;
            }
        }
    }

    static bool IsInGroundMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    bool IsInGroundMask(int layer) => IsInGroundMask(layer, groundLayer);

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "killzone")
        {
            GameManager.instance.Death();
            return;
        }

        TryRefillJumpsFromFloorCollision(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("killzone"))
            return;
        TryRefillJumpsFromFloorCollision(collision);
    }



    //mobile;
    public void MobileMove(float value)
    {
        moveX = value;
    }
    public void MobileJump()
    {
        TryConsumeJump();
    }

    public void Shoot()
    {
        //GameObject fireBall = Instantiate(projectile, firePoint.position, Quaternion.identity);
        //fireBall.GetComponent<Rigidbody2D>().AddForce(firePoint.right * 500f);
    }

    public void MobileShoot()
    {
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / fireRate; // Set the next allowed fire time
        }
    }

}
