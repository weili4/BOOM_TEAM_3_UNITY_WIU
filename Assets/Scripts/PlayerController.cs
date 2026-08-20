using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;

    [Header("GROUND CHECK")]
    public bool isGrounded = false;
    private bool wasGrounded = false;
    public Transform groundChecker;
    public Vector2 groundCheckSize = new Vector2(0.2f, 0.04f);
    public LayerMask groundLayer;

    [Header("MOVEMENT AND JUMP")]
    private Vector2 moveInput;
    public float moveSpeed = 4.0f;
    private bool jumpPressed = false;
    private bool jumpReleased = false;
    public float jumpHeight = 8.0f;

    [Header("DOUBLE JUMP SETTINGS")]
    public int maxJumps = 2;
    private int currentJumps = 0;

    [Header("DOUBLE JUMP SHOCKWAVE ATTACK")]
    [SerializeField] private bool enableDoubleJumpAttack = true;
    [SerializeField] private float doubleJumpAttackRadius = 2.5f;
    [SerializeField] private int doubleJumpDamage = 25;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject doubleJumpVFXPrefab;

    [Header("PLAYER MOVEMENT AUDIO CLIPS")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip doubleJumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float footstepInterval = 0.35f;
    private float footstepTimer = 0f;

    [Header("JUMP FEEL ADJUSTMENTS")]
    public float coyoteTime = 0.1f;
    private float coyoteTimer = 0f;
    public float jumpBufferTime = 0.1f;
    private float jumpBufferTimer = 0f;
    public float fallMultiplier = 2.5f;
    public float maxFallSpeed = 15f;

    [Header("LADDER SETTINGS")]
    public LayerMask ladderLayer;
    public float climbSpeed = 3.0f;
    public float ladderSnapSpeed = 15.0f;
    private bool isOnLadderTrigger = false;
    private bool isClimbing = false;
    private float originalGravity;
    private Collider2D currentLadderCollider;

    public bool isRaging = false;
    public bool attackedPressed = false;

    private float knockbackTimer = 0f;
    public void ApplyKnockback(Vector2 knockbackVector, float duration = 0.2f)
    {
        knockbackTimer = duration;
        body.linearVelocity = knockbackVector;
    }
    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        originalGravity = body.gravityScale;

        // DISABLE SOLID PHYSICS COLLISION BETWEEN PLAYER AND ENEMIES
        int pLayer = LayerMask.NameToLayer("Player");
        int eLayer = LayerMask.NameToLayer("Enemy");
        if (pLayer != -1 && eLayer != -1)
        {
            // stop player and enemies from physically pushing each other
            Physics2D.IgnoreLayerCollision(pLayer, eLayer, true);
        }
    }

    void Update()
    {
        // check if player is touching the ground
        isGrounded = Physics2D.OverlapBox(groundChecker.position, groundCheckSize, 0f, groundLayer);
        animator.SetBool("IsGrounded", isGrounded);

        if (isGrounded && !wasGrounded) // play landing sound only when player just landed
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(landSound, transform.position, 0.5f);
        }
        wasGrounded = isGrounded;

        moveInput = InputSystem.actions["Move"].ReadValue<Vector2>();
        jumpPressed = InputSystem.actions["Jump"].WasPressedThisFrame();
        jumpReleased = InputSystem.actions["Jump"].WasReleasedThisFrame();

        if (isGrounded && Mathf.Abs(moveInput.x) > 0.1f && !isClimbing)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(footstepSound, transform.position, 0.3f);
                footstepTimer = footstepInterval;
            }
        }

        if (isOnLadderTrigger && isClimbing && Mathf.Abs(moveInput.x) > 0.1f && Mathf.Abs(moveInput.y) <= 0.1f)
        {
            ExitLadder();
        }

        if (isGrounded && body.linearVelocityY <= 0.05f) // reset jumps and coyote time when player is on ground
        {
            currentJumps = 0;
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;

            if (coyoteTimer <= 0f && currentJumps == 0) // use one jump when coyote time finish
            {
                currentJumps = 1;
            }
        }

        if (jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        bool canJump = (currentJumps < maxJumps);

        if (jumpBufferTimer > 0f && canJump)
        {
            // if on a ladder, dismount immediately when jumping
            if (isClimbing)
            {
                ExitLadder();
            }

            body.linearVelocityY = jumpHeight;
            currentJumps++;
            jumpBufferTimer = 0f;

            if (currentJumps == 1)
            {
                animator.SetBool("IsJumping", true);
                animator.SetBool("IsFalling", false);

                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(jumpSound, transform.position);
            }
            else if (currentJumps >= 2)
            {
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsFalling", false);
                animator.SetTrigger("DoubleJump");

                if (enableDoubleJumpAttack)
                {
                    PerformDoubleJumpAttack();
                }
            }
        }

        if (jumpReleased && body.linearVelocityY > 0)
        {
            body.linearVelocityY *= 0.5f;
        }

        attackedPressed = InputSystem.actions["Attack"].WasPressedThisFrame();

        if (attackedPressed && isGrounded && !isClimbing)
        {
            animator.SetTrigger("IsAttacking");
        }
    }

    private void PerformDoubleJumpAttack()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(doubleJumpSound, transform.position);

        if (doubleJumpVFXPrefab != null)
        {
            Instantiate(doubleJumpVFXPrefab, transform.position, Quaternion.identity);
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, doubleJumpAttackRadius, enemyLayer);
        foreach (Collider2D col in hitEnemies)
        {
            if (col.TryGetComponent<Damageable>(out Damageable enemy))
            {
                enemy.TakeDamage(doubleJumpDamage);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & ladderLayer) != 0)
        {
            isOnLadderTrigger = true;
            currentLadderCollider = collision;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & ladderLayer) != 0)
        {
            ExitLadder();
        }
    }

    private void StartClimbing()
    {
        isClimbing = true;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;

        // reset jump counter so player has full jumps refreshed from ladder
        currentJumps = 0;
        coyoteTimer = coyoteTime;

        if (currentLadderCollider != null)
        {
            float centerX = currentLadderCollider.bounds.center.x;
            transform.position = new Vector2(centerX, transform.position.y);
        }

        if (animator != null)
        {
            animator.SetBool("IsOnLadder", true);
            animator.SetBool("IsFalling", false);
            animator.SetBool("IsJumping", false);
        }
    }

    private void ExitLadder()
    {
        isOnLadderTrigger = false;
        isClimbing = false;
        currentLadderCollider = null;
        body.gravityScale = originalGravity > 0f ? originalGravity : 2.5f;

        // refresh jumps upon leaving ladder
        currentJumps = 0;
        coyoteTimer = coyoteTime;

        if (animator != null)
        {
            animator.SetBool("IsOnLadder", false);
            animator.SetBool("IsMoving", false);
        }
    }

    void FixedUpdate()
    {
        // 1. while in knockback, do not overwrite horizontal velocity with walking input
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
        }
        else
        {
            // normal movement handling
            if (isOnLadderTrigger && currentLadderCollider != null)
            {
                if (Mathf.Abs(moveInput.y) > 0.1f)
                {
                    if (!isClimbing) StartClimbing();
                }

                if (isClimbing)
                {
                    body.linearVelocityX = 0;
                    body.linearVelocityY = moveInput.y * climbSpeed;

                    float centerX = currentLadderCollider.bounds.center.x;
                    float newX = Mathf.MoveTowards(transform.position.x, centerX, ladderSnapSpeed * Time.fixedDeltaTime);
                    transform.position = new Vector2(newX, transform.position.y);

                    animator.SetBool("IsMoving", Mathf.Abs(moveInput.y) > 0.1f);
                    return;
                }
            }

            body.gravityScale = originalGravity > 0f ? originalGravity : 2.5f;

            float speedToUse = isGrounded ? moveSpeed : Mathf.Max(moveSpeed, 5.0f);
            body.linearVelocityX = moveInput.x * speedToUse;
            animator.SetBool("IsMoving", (Mathf.Abs(moveInput.x) > 0f));

            if (moveInput.x < 0) transform.localScale = new Vector3(-2, 2, 2);
            else if (moveInput.x > 0) transform.localScale = new Vector3(2, 2, 2);
        }

        if (!isGrounded && body.linearVelocityY < 0)
        {
            animator.SetBool("IsFalling", true);
            animator.SetBool("IsJumping", false);
        }
        else
        {
            animator.SetBool("IsFalling", false);
        }

        if (body.linearVelocityY < 0)
        {
            body.linearVelocityY += Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }

        if (body.linearVelocityY < -maxFallSpeed)
        {
            body.linearVelocityY = -maxFallSpeed;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, doubleJumpAttackRadius);
    }
}