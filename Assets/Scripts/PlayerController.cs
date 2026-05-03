
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

    [Header("Coyote Time & Jump Buffer (GDD §3.3)")]
    [Tooltip("Forgiveness window (seconds) after walking off an edge where jump is still allowed.")]
    [SerializeField] float coyoteTime = 0.12f;

    [Tooltip("Pre-input window (seconds): if jump is pressed this long before landing, it fires on contact.")]
    [SerializeField] float jumpBufferTime = 0.15f;

    private Rigidbody2D rb;
    private bool isGroundedBool = false;
    private bool wasGroundedLastFrame;
    /// <summary>2 after a real landing / standing on floor; decrements per jump. Refill is driven by ground + velocity / floor contacts.</summary>
    private int jumpsRemaining;

    float _coyoteTimer;
    float _jumpBufferTimer;

    public Animator playeranim;

    [Header("Audio")]
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip doubleJumpSFX;
    [SerializeField] private AudioClip landSFX;

    bool _animHasRun;
    bool _animHasIsGrounded;
    bool _animHasDie;

    public Controls controlmode;
   

    private float moveX;
    public bool isPaused = false;

    public ParticleSystem footsteps;
    private ParticleSystem.EmissionModule footEmissions;

    public ParticleSystem ImpactEffect;
    private bool wasonGround;

    // ─── Facing Direction (GDD §3.4) ────────────────────────────
    /// <summary>+1 (right) or -1 (left). Driven by last non-zero horizontal input / sprite flip.</summary>
    public int FacingDirection => transform.localScale.x >= 0 ? 1 : -1;
    void Awake()
    {
 #if UNITY_EDITOR
 controlmode = Controls.pc;
 #else
 controlmode = Controls.mobile;
 #endif
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        footEmissions = footsteps.emission;

        bool grounded = IsGrounded();
        wasonGround = grounded;
        wasGroundedLastFrame = grounded;

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
            if (p.name == "run" && p.type == AnimatorControllerParameterType.Bool) _animHasRun = true;
            if (p.name == "isGrounded" && p.type == AnimatorControllerParameterType.Bool) _animHasIsGrounded = true;
            if (p.name == "die" && p.type == AnimatorControllerParameterType.Trigger) _animHasDie = true;
        }
    }

    private void Update()
    {
        if (isPaused) return;

        isGroundedBool = IsGrounded();
        
        if (isGroundedBool && rb.linearVelocity.y <= jumpResetMaxUpVelocity)
        {
            jumpsRemaining = 2;
            _coyoteTimer = coyoteTime;
        }
        else
        {
            _coyoteTimer -= Time.deltaTime;
        }

        if (isGroundedBool && !wasonGround)
        {
            if (ImpactEffect != null)
                ImpactEffect.Play();
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayLandSFX(landSFX);
        }

        wasonGround = isGroundedBool;

        if (moveX != 0)
        {
            if (_animHasRun)
                playeranim.SetBool("run", true);

            FlipSprite(moveX);
            footEmissions.rateOverTime = 35f;
        }
        else
        {
            if (_animHasRun)
                playeranim.SetBool("run", false);

            footEmissions.rateOverTime = 0f;
        }

        if (controlmode == Controls.pc)
        {
            if (Input.GetButtonDown("Jump"))
            {
                _jumpBufferTimer = jumpBufferTime;
            }
        }

        _jumpBufferTimer -= Time.deltaTime;

        if (_jumpBufferTimer > 0f)
        {
            if (TryConsumeJump())
            {
                _jumpBufferTimer = 0f;
            }
        }

        wasGroundedLastFrame = isGroundedBool;

        if (_animHasIsGrounded)
            playeranim.SetBool("isGrounded", isGroundedBool);
    }

    private void FlipSprite(float direction)
    {
        if (direction > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (direction < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    private void FixedUpdate()
    {
        if (isPaused)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        if (controlmode == Controls.pc)
        {
            moveX = Input.GetAxis("Horizontal");
        }
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
    }

    /// <summary>Disables movement input and stops current horizontal movement.</summary>
    public void DisableInput()
    {
        System.IO.File.AppendAllText("DeathDebugLog.txt", $"[{System.DateTime.Now:HH:mm:ss.fff}] DisableInput() called. Current isPaused: {isPaused}\n");
        isPaused = true;
        moveX = 0;
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        footEmissions.rateOverTime = 0f;

        if (playeranim != null)
        {
            System.IO.File.AppendAllText("DeathDebugLog.txt", $"[{System.DateTime.Now:HH:mm:ss.fff}] Triggering 'die'. _animHasDie: {_animHasDie}, playeranim.enabled: {playeranim.enabled}\n");
            if (_animHasDie)
            {
                playeranim.SetTrigger("die");
            }
            else
            {
                System.IO.File.AppendAllText("DeathDebugLog.txt", $"[{System.DateTime.Now:HH:mm:ss.fff}] WARNING: _animHasDie is FALSE!\n");
            }
        }
        else
        {
            System.IO.File.AppendAllText("DeathDebugLog.txt", $"[{System.DateTime.Now:HH:mm:ss.fff}] ERROR: playeranim is NULL!\n");
        }
    }

    private void Jump(float force, bool isDoubleJump = false)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        if (playeranim != null)
            playeranim.SetTrigger("jump");
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayJumpSFX(isDoubleJump, jumpSFX, doubleJumpSFX);
    }

    bool TryConsumeJump()
    {
        if (jumpsRemaining == 2 && !isGroundedBool && _coyoteTimer > 0f)
        {
            Jump(jumpForce);
            jumpsRemaining = 1;
            _coyoteTimer = 0f;
            return true;
        }

        if (isGroundedBool)
        {
            Jump(jumpForce);
            jumpsRemaining = 1;
            _coyoteTimer = 0f;
            return true;
        }

        if (jumpsRemaining > 0)
        {
            Jump(doubleJumpForce, true);
            jumpsRemaining--;
            _coyoteTimer = 0f;
            return true;
        }

        return false;
    }

    bool IsGrounded()
    {
        if (groundCheck == null)
            return false;

        Vector2 feet = groundCheck.position;
        Collider2D[] cols = Physics2D.OverlapBoxAll(feet, new Vector2(groundCheckRadius * 2, 0.05f), 0f, groundLayer);
        foreach (var c in cols)
        {
            if (c.gameObject != gameObject) return true;
        }

        Vector2 rayOrigin = new Vector2(feet.x, feet.y - 0.05f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, Vector2.down, groundRayLength, groundLayer);
        foreach (var h in hits)
        {
            if (h.collider != null && h.collider.gameObject != gameObject) return true;
        }
        
        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "killzone")
        {
            GameManager.instance.Death();
        }
    }

    public void MobileMove(float value)
    {
        moveX = value;
    }
    public void MobileJump()
    {
        _jumpBufferTimer = jumpBufferTime;
    }

    [System.Obsolete("Use ShootingSystem for auto-fire.")]
    public void Shoot() { }

    [System.Obsolete("Use ShootingSystem for auto-fire.")]
    public void MobileShoot() { }
}
