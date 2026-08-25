using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;

    [Header("movement and multipliers (abilities can tweak these)")]
    public float moveSpeed = 4.0f;
    public float moveSpeedMultiplier = 1.0f;       // e.g. flurry sets to 0.5, overload sets to 2.0
    public float jumpHeight = 8.0f;
    public int maxJumps = 2;
    private int currentJumps = 0;

    [Header("gravity orientation and multipliers")]
    public Vector2 gravityDirection = Vector2.down; // set to Vector2.up for inverted gravity
    public float gravityScaleMultiplier = 1.0f;     // set to 0.3 for gliding
    public float fallMultiplier = 2.5f;
    public float maxFallSpeed = 15f;

    [Header("ground check settings")]
    public bool isGrounded = false;
    private bool wasGrounded = false;
    public Transform groundChecker;
    public Vector2 groundCheckSize = new Vector2(0.2f, 0.04f);
    public LayerMask groundLayer;

    [Header("ladder settings")]
    public LayerMask ladderLayer;
    public float climbSpeed = 3.0f;
    public float ladderSnapSpeed = 15.0f;
    private bool isOnLadderTrigger = false;
    private bool isClimbing = false;
    private float originalGravity;
    private Collider2D currentLadderCollider;
    private float ladderDismountCooldown = 0f;

    [Header("audio clips")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip doubleJumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float footstepInterval = 0.35f;
    private float footstepTimer = 0f;

    // jump feel adjustments
    public float coyoteTime = 0.1f;
    private float coyoteTimer = 0f;
    public float jumpBufferTime = 0.1f;
    private float jumpBufferTimer = 0f;

    // forced velocity state (used generically by dash, knockback, hook pull, etc)
    private bool isVelocityOverridden = false;
    private float velocityOverrideTimer = 0f;
    private Vector2 overriddenVelocity = Vector2.zero;
    private bool overrideZeroGravity = false;

    private Vector2 moveInput;
    private bool jumpPressed = false;
    private bool jumpReleased = false;
    private float currentFacingDirection = 1f;

    // backwards compatibility helpers for teammates
    public bool isRaging = false;
    public bool IsGravityInverted => gravityDirection.y > 0;
    public void SetGravityInverted(bool inv) => gravityDirection = inv ? Vector2.up : Vector2.down;

    public bool IsClimbing => isClimbing;

    public bool IsVelocityOverridden => isVelocityOverridden;

    public bool isInputLocked = false;

    // abilities call this to temporarily force movement without hardcoding dash/knockback states here
    public void SetForcedVelocity(Vector2 velocity, float duration, bool zeroGravity = false)
    {
        isVelocityOverridden = true;
        velocityOverrideTimer = duration;
        overriddenVelocity = velocity;
        overrideZeroGravity = zeroGravity;
        body.linearVelocity = velocity;
    }

    public void ClearForcedVelocity()
    {
        isVelocityOverridden = false;
        velocityOverrideTimer = 0f;
        body.gravityScale = originalGravity > 0f ? originalGravity : 2.5f;

        // reminder: cut residual upward velocity so dashing diagonally or up does not rocket the player into the sky
        if (overrideZeroGravity)
        {
            if (gravityDirection.y < 0 && body.linearVelocityY > 0f)
            {
                body.linearVelocityY *= 0.15f; // cut upward momentum cleanly
            }
            else if (gravityDirection.y > 0 && body.linearVelocityY < 0f)
            {
                body.linearVelocityY *= 0.15f; // for inverted gravity
            }
        }
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        originalGravity = body.gravityScale;

        // stop player and enemies from pushing each other
        int pLayer = LayerMask.NameToLayer("Player");
        int eLayer = LayerMask.NameToLayer("Enemy");
        if (pLayer != -1 && eLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(pLayer, eLayer, true);
        }
    }

    void Update()
    {
        // 1. ground check
        float checkDist = groundChecker != null ? Mathf.Abs(groundChecker.localPosition.y) : 0.8f;
        Vector2 checkPos = (Vector2)transform.position + gravityDirection * checkDist;

        isGrounded = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);
        if (animator != null) animator.SetBool("IsGrounded", isGrounded);

        if (isGrounded && !wasGrounded)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(landSound, transform.position, 0.5f);
        }
        wasGrounded = isGrounded;

        // master lock: blocks input during cutscenes or while shooting/charging
        bool isLocked = isInputLocked || (DialogueManager.Instance != null && DialogueManager.Instance.IsCinematicActive);

        if (isLocked)
        {
            moveInput = Vector2.zero;
            jumpPressed = false;
            jumpReleased = false;
            jumpBufferTimer = 0f;
        }
        else
        {
            moveInput = InputSystem.actions != null && InputSystem.actions["Move"] != null ? InputSystem.actions["Move"].ReadValue<Vector2>() : Vector2.zero;
            jumpPressed = InputSystem.actions != null && InputSystem.actions["Jump"] != null && InputSystem.actions["Jump"].WasPressedThisFrame();
            jumpReleased = InputSystem.actions != null && InputSystem.actions["Jump"] != null && InputSystem.actions["Jump"].WasReleasedThisFrame();
        }

        // count down ladder dismount cooldown
        if (ladderDismountCooldown > 0f)
        {
            ladderDismountCooldown -= Time.deltaTime;
        }

        // 1. prioritize dismounting when pressing left or right (A / D)
        if (isClimbing && Mathf.Abs(moveInput.x) > 0.2f)
        {
            ExitLadder();
            ladderDismountCooldown = 0.25f; // prevent W/S from immediately re-grabbing ladder
        }

        // footsteps
        if (isGrounded && Mathf.Abs(moveInput.x) > 0.1f && !isClimbing && !isLocked)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(footstepSound, transform.position, 0.3f);
                footstepTimer = footstepInterval;
            }
        }

        // jump reset
        bool isMovingTowardsFloor = gravityDirection.y < 0 ? body.linearVelocityY <= 0.1f : body.linearVelocityY >= -0.1f;

        if (isGrounded && isMovingTowardsFloor)
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

        if (jumpPressed && !isLocked) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        bool canJump = (currentJumps < maxJumps);

        // 2. jump execution (allows leaping directly off the ladder)
        if (jumpBufferTimer > 0f && canJump && !isLocked)
        {
            if (isClimbing)
            {
                ExitLadder();
                ladderDismountCooldown = 0.25f;
            }

            body.linearVelocityY = -gravityDirection.y * jumpHeight;
            currentJumps++;
            jumpBufferTimer = 0f;

            if (currentJumps == 1)
            {
                if (animator != null) { animator.SetBool("IsJumping", true); animator.SetBool("IsFalling", false); }
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(jumpSound, transform.position);
            }
            else if (currentJumps >= 2)
            {
                if (animator != null) { animator.SetBool("IsJumping", false); animator.SetBool("IsFalling", false); animator.SetTrigger("DoubleJump"); }
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(doubleJumpSound, transform.position);
            }
        }

        if (jumpReleased)
        {
            if (gravityDirection.y < 0 && body.linearVelocityY > 0) body.linearVelocityY *= 0.5f;
            else if (gravityDirection.y > 0 && body.linearVelocityY < 0) body.linearVelocityY *= 0.5f;
        }
    }

    void FixedUpdate()
    {
        if (isVelocityOverridden)
        {
            body.gravityScale = overrideZeroGravity ? 0f : (originalGravity * gravityScaleMultiplier);
            body.linearVelocity = overriddenVelocity;

            velocityOverrideTimer -= Time.fixedDeltaTime;
            if (velocityOverrideTimer <= 0f)
            {
                ClearForcedVelocity();
            }
            return;
        }

        // 2. ladder climbing logic
        if (isOnLadderTrigger && currentLadderCollider != null)
        {
            // only start climbing if player is moving vertically AND NOT pushing horizontal dismount
            if (ladderDismountCooldown <= 0f && Mathf.Abs(moveInput.y) > 0.1f && Mathf.Abs(moveInput.x) < 0.2f && !isClimbing)
            {
                StartClimbing();
            }

            if (isClimbing)
            {
                body.linearVelocityX = 0;
                body.linearVelocityY = moveInput.y * climbSpeed;

                float centerX = currentLadderCollider.bounds.center.x;
                float newX = Mathf.MoveTowards(transform.position.x, centerX, ladderSnapSpeed * Time.fixedDeltaTime);
                transform.position = new Vector2(newX, transform.position.y);

                if (animator != null)
                {
                    animator.SetBool("IsOnLadder", true);
                    animator.SetBool("IsMoving", Mathf.Abs(moveInput.y) > 0.1f);
                }
                return;
            }
        }

        float baseGrav = originalGravity > 0f ? originalGravity : 2.5f;
        body.gravityScale = (gravityDirection.y < 0 ? baseGrav : -baseGrav) * gravityScaleMultiplier;

        bool isPlayingSummonAnim = animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("Summon Gun Attack");
        bool isLocked = isInputLocked || isPlayingSummonAnim || (DialogueManager.Instance != null && DialogueManager.Instance.IsCinematicActive);

        // freeze horizontal velocity completely while locked
        if (isLocked)
        {
            body.linearVelocityX = 0f;
            if (animator != null) animator.SetBool("IsMoving", false);
        }
        else
        {
            float moveDirX = gravityDirection.y < 0 ? moveInput.x : -moveInput.x;
            float finalSpeed = (isGrounded ? moveSpeed : Mathf.Max(moveSpeed, 5.0f)) * moveSpeedMultiplier;
            body.linearVelocityX = moveDirX * finalSpeed;

            if (animator != null) animator.SetBool("IsMoving", Mathf.Abs(moveInput.x) > 0f);

            if (Mathf.Abs(moveInput.x) > 0.05f)
            {
                currentFacingDirection = Mathf.Sign(moveInput.x);
            }
        }


        float visualDirX = gravityDirection.y < 0 ? currentFacingDirection : -currentFacingDirection;
        float scaleY = gravityDirection.y < 0 ? 2f : -2f;
        transform.localScale = new Vector3(visualDirX * 2f, scaleY, 2f);

        // fall animation
        if (!isGrounded && animator != null)
        {
            bool isFalling = gravityDirection.y < 0 ? (body.linearVelocityY < -0.1f) : (body.linearVelocityY > 0.1f);
            animator.SetBool("IsFalling", isFalling);
            animator.SetBool("IsJumping", !isFalling);
        }
        else if (animator != null)
        {
            animator.SetBool("IsFalling", false);
            animator.SetBool("IsJumping", false);
        }

        if (gravityDirection.y < 0 && body.linearVelocityY < 0)
        {
            body.linearVelocityY += Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (gravityDirection.y > 0 && body.linearVelocityY > 0)
        {
            body.linearVelocityY -= Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }

        if (Mathf.Abs(body.linearVelocityY) > maxFallSpeed)
        {
            body.linearVelocityY = Mathf.Sign(body.linearVelocityY) * maxFallSpeed;
        }
    }


    private void StartClimbing()
    {
        isClimbing = true;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;

        // refresh jumps so you can jump off the ladder
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

        currentJumps = 0;
        coyoteTimer = coyoteTime;

        if (animator != null)
        {
            animator.SetBool("IsOnLadder", false);
            animator.SetBool("IsMoving", false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & ladderLayer) != 0)
        {
            isOnLadderTrigger = true;
            currentLadderCollider = collision;
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

    private void OnDrawGizmosSelected()
    {
        float checkDist = groundChecker != null ? Mathf.Abs(groundChecker.localPosition.y) : 0.8f;
        Vector2 checkPos = (Vector2)transform.position + gravityDirection * checkDist;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(checkPos, groundCheckSize);
    }

    public void SetFacingDirection(float dir)
    {
        if (Mathf.Abs(dir) > 0.05f)
        {
            currentFacingDirection = Mathf.Sign(dir);
        }
    }
}