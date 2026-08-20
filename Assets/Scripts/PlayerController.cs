using System.Collections.Generic;
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

    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector2 dashDirection;
    private float dashSpeed = 16.0f;
    private int currentDashDamage = 25;
    private float currentDashKnockback = 6.0f;
    private float dashHitRadius = 1.0f;
    private LayerMask dashEnemyLayer;
    private HashSet<Damageable> enemiesHitThisDash = new HashSet<Damageable>();
    private GhostTrail ghostTrail;
    public bool IsDashing => isDashing;

    private bool isGravityInverted = false;
    public bool IsGravityInverted => isGravityInverted;

    private float currentFacingDirection = 1f;

    public void SetGravityInverted(bool inverted)
    {
        isGravityInverted = inverted;

        float baseGrav = originalGravity > 0f ? originalGravity : 2.5f;
        body.gravityScale = isGravityInverted ? -baseGrav : baseGrav;

        // refresh jumps upon flipping gravity
        currentJumps = 0;
        coyoteTimer = coyoteTime;
    }

    public void ApplyKnockback(Vector2 knockbackVector, float duration = 0.2f)
    {
        // ignore all knockback forces while upside down on inverted gravity
        if (isGravityInverted) return;

        knockbackTimer = duration;
        body.linearVelocity = knockbackVector;
    }

    public void PerformDash(
        Vector2 direction,
        float speed = 18.0f,
        float duration = 0.16f,
        int damage = 25,
        float enemyKnockback = 6.0f,
        float hitRadius = 1.0f,
        LayerMask enemyLayer = default,
        GameObject vfxPrefab = null)
    {
        isDashing = true;
        dashTimer = duration;
        dashSpeed = speed;
        currentDashDamage = damage;
        currentDashKnockback = enemyKnockback;
        dashHitRadius = hitRadius;
        dashEnemyLayer = enemyLayer;

        // reset list so each enemy is only hit once per dash
        enemiesHitThisDash.Clear();

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = new Vector2(Mathf.Sign(transform.localScale.x), 0f);
        }

        dashDirection = direction.normalized;

        // 1. spawn ghost trail afterimages
        if (ghostTrail == null) ghostTrail = GetComponent<GhostTrail>();
        if (ghostTrail == null) ghostTrail = gameObject.AddComponent<GhostTrail>();
        ghostTrail.StartTrail(duration);

        // 2. spawn front vfx (meteorite / wind cone) facing dash direction
        if (vfxPrefab != null)
        {
            float angle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            Vector3 spawnPos = transform.position + (Vector3)(dashDirection * 0.5f);

            GameObject vfxObj = Instantiate(vfxPrefab, spawnPos, rot, transform);
            Destroy(vfxObj, duration + 0.1f);
        }

        if (animator != null)
        {
            animator.SetTrigger("IsAttacking");
        }
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
        // 1. ground check (checks floor when normal, ceiling when inverted)
        float checkDist = groundChecker != null ? Mathf.Abs(groundChecker.localPosition.y) : 0.8f;
        Vector2 checkPos = (Vector2)transform.position + (isGravityInverted ? Vector2.up : Vector2.down) * checkDist;

        isGrounded = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);
        animator.SetBool("IsGrounded", isGrounded);

        if (isGrounded && !wasGrounded)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(landSound, transform.position, 0.5f);
        }
        wasGrounded = isGrounded;

        moveInput = InputSystem.actions["Move"].ReadValue<Vector2>();
        jumpPressed = InputSystem.actions["Jump"].WasPressedThisFrame();
        jumpReleased = InputSystem.actions["Jump"].WasReleasedThisFrame();

        // 2. jump reset
        bool isMovingTowardsSurface = isGravityInverted ? body.linearVelocityY >= -0.1f : body.linearVelocityY <= 0.1f;

        if (isGrounded && isMovingTowardsSurface)
        {
            currentJumps = 0;
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
            if (coyoteTimer <= 0f && currentJumps == 0)
            {
                currentJumps = 1;
            }
        }

        if (jumpPressed) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        bool canJump = (currentJumps < maxJumps);

        // 3. jump execution (pushes away from ceiling when inverted)
        if (jumpBufferTimer > 0f && canJump && !isClimbing)
        {
            body.linearVelocityY = isGravityInverted ? -jumpHeight : jumpHeight;
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

        if (jumpReleased)
        {
            if (!isGravityInverted && body.linearVelocityY > 0) body.linearVelocityY *= 0.5f;
            else if (isGravityInverted && body.linearVelocityY < 0) body.linearVelocityY *= 0.5f;
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
        if (isDashing) return;
        if (knockbackTimer > 0f) return;

        float baseGrav = originalGravity > 0f ? originalGravity : 2.5f;
        body.gravityScale = isGravityInverted ? -baseGrav : baseGrav;

        // screen-relative controls (pressing D always moves right on screen)
        float moveX = isGravityInverted ? -moveInput.x : moveInput.x;
        float speedToUse = isGrounded ? moveSpeed : Mathf.Max(moveSpeed, 5.0f);
        body.linearVelocityX = moveX * speedToUse;

        animator.SetBool("IsMoving", (Mathf.Abs(moveInput.x) > 0f));

        // update facing direction only when there is actual movement input
        if (Mathf.Abs(moveInput.x) > 0.05f)
        {
            currentFacingDirection = Mathf.Sign(moveInput.x);
        }

        // apply screen-relative facing direction without reading raw localscale
        float visualDirX = isGravityInverted ? -currentFacingDirection : currentFacingDirection;
        float scaleX = visualDirX * 2f;
        float scaleY = isGravityInverted ? -2f : 2f;

        transform.localScale = new Vector3(scaleX, scaleY, 2f);

        // fall animation logic
        if (!isGrounded)
        {
            bool isFalling = isGravityInverted ? (body.linearVelocityY > 0.1f) : (body.linearVelocityY < -0.1f);
            animator.SetBool("IsFalling", isFalling);
            animator.SetBool("IsJumping", !isFalling);
        }
        else
        {
            animator.SetBool("IsFalling", false);
            animator.SetBool("IsJumping", false);
        }

        // fall multiplier
        if (!isGravityInverted)
        {
            if (body.linearVelocityY < 0)
                body.linearVelocityY += Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else
        {
            if (body.linearVelocityY > 0)
                body.linearVelocityY -= Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void CheckDashEnemyCollisions()
    {
        // detect all enemies in dash hitbox radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, dashHitRadius, dashEnemyLayer);

        foreach (var col in hits)
        {
            if (col.CompareTag("Player") || col.CompareTag("Ally")) continue;

            if (col.TryGetComponent<Damageable>(out var enemyHealth))
            {
                if (!enemiesHitThisDash.Contains(enemyHealth))
                {
                    enemiesHitThisDash.Add(enemyHealth);

                    // damage enemy and knock them in dash travel direction
                    enemyHealth.TakeDamage(currentDashDamage, dashDirection, currentDashKnockback);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        float checkDist = groundChecker != null ? Mathf.Abs(groundChecker.localPosition.y) : 0.8f;
        Vector2 checkPos = (Vector2)transform.position + (isGravityInverted ? Vector2.up : Vector2.down) * checkDist;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(checkPos, groundCheckSize);
    }
}