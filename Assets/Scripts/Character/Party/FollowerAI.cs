using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class FollowerAI : MonoBehaviour
{
    [Header("Follow Distances (Dead Zone)")]
    public Transform leaderTarget;
    public float startFollowDistance = 2.2f;
    public float stopFollowDistance = 1.2f;
    public float followSpeed = 6.0f;
    public float teleportCatchupDistance = 12.0f;

    [Header("Switch Spread-Out Behavior")]
    [SerializeField] private float spreadOutDuration = 0.18f; // shorter nudge time
    [SerializeField] private float spreadOutSpeed = 2.0f;     // small gentle step
    private bool isSpreadingOut = false;

    [Header("Visual Transparency")]
    [Range(0.1f, 1f)] public float followerAlpha = 0.65f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.2f, 0.04f);
    public LayerMask groundLayer;
    private bool isGrounded = false;

    [Header("Obstacle Jump")]
    [SerializeField] private float wallCheckDistance = 0.8f;
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float jumpCooldown = 0.6f;
    private float jumpCooldownTimer = 0f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer[] spriteRenderers;
    private bool isActivelyMoving = false;
    private float targetAlpha = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        targetAlpha = followerAlpha;
        isActivelyMoving = false;

        // make sure follower always has normal positive gravity and upright scale
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = Mathf.Abs(rb.gravityScale);
        }

        // ensure sprite is upright
        transform.localScale = new Vector3(transform.localScale.x, 2f, 2f);

        StopAllCoroutines();
        StartCoroutine(SpreadOutRoutine());
    }

    private void OnDisable()
    {
        targetAlpha = 1.0f;
        isSpreadingOut = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        ApplyAlpha(1.0f);
    }

    public void SetLeader(Transform leader)
    {
        leaderTarget = leader;
    }

    private IEnumerator SpreadOutRoutine()
    {
        isSpreadingOut = true;
        float elapsed = 0f;

        // step away from the leader slightly
        float direction = 1f;
        if (leaderTarget != null)
        {
            direction = transform.position.x >= leaderTarget.position.x ? 1f : -1f;
        }

        while (elapsed < spreadOutDuration)
        {
            elapsed += Time.deltaTime;

            // small nudge away
            rb.linearVelocityX = direction * spreadOutSpeed;

            // face nudge direction
            transform.localScale = new Vector3(direction > 0 ? 2 : -2, 2, 2);
            yield return null;
        }

        // stop movement completely when nudge ends
        rb.linearVelocityX = 0f;
        isSpreadingOut = false;
    }

    private void Update()
    {
        if (leaderTarget == null || isSpreadingOut) return;

        // 1. ground check
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        }

        if (jumpCooldownTimer > 0)
            jumpCooldownTimer -= Time.deltaTime;

        float distToLeader = Vector2.Distance(transform.position, leaderTarget.position);

        // 2. emergency teleport if follower gets stuck
        if (distToLeader > teleportCatchupDistance)
        {
            transform.position = leaderTarget.position;
            rb.linearVelocity = Vector2.zero;
            isActivelyMoving = false;
            return;
        }

        float diffX = leaderTarget.position.x - transform.position.x;
        float absDiffX = Mathf.Abs(diffX);

        // 3. dead zone logic to prevent jitter
        if (!isActivelyMoving)
        {
            if (absDiffX > startFollowDistance)
            {
                isActivelyMoving = true;
            }
        }
        else
        {
            if (absDiffX <= stopFollowDistance)
            {
                isActivelyMoving = false;
            }
        }

        // 4. move follower
        if (isActivelyMoving)
        {
            float moveDir = Mathf.Sign(diffX);
            rb.linearVelocityX = moveDir * followSpeed;
            transform.localScale = new Vector3(moveDir > 0 ? 2 : -2, 2, 2);
            CheckObstacleAndJump(moveDir);
        }
        else
        {
            rb.linearVelocityX = 0f;
        }

        // 5. animations
        UpdateAnimations();
    }

    private void LateUpdate()
    {
        ApplyAlpha(targetAlpha);
    }

    private void ApplyAlpha(float alpha)
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        foreach (var sr in spriteRenderers)
        {
            if (sr != null)
            {
                Color c = sr.color;
                if (Mathf.Abs(c.a - alpha) > 0.01f)
                {
                    c.a = alpha;
                    sr.color = c;
                }
            }
        }
    }

    private void CheckObstacleAndJump(float moveDir)
    {
        if (!isGrounded || jumpCooldownTimer > 0f) return;

        Vector2 rayOrigin = (Vector2)transform.position + new Vector2(moveDir * 0.4f, 0f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, new Vector2(moveDir, 0f), wallCheckDistance, groundLayer);

        if (hit.collider != null)
        {
            rb.linearVelocityY = jumpForce;
            jumpCooldownTimer = jumpCooldown;

            if (animator != null)
            {
                animator.SetBool("IsJumping", true);
                animator.SetBool("IsFalling", false);
            }
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = Mathf.Abs(rb.linearVelocityX) > 0.1f;

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsMoving", isMoving);

        if (!isGrounded)
        {
            if (rb.linearVelocityY < -0.1f)
            {
                animator.SetBool("IsFalling", true);
                animator.SetBool("IsJumping", false);
            }
            else if (rb.linearVelocityY > 0.1f)
            {
                animator.SetBool("IsJumping", true);
                animator.SetBool("IsFalling", false);
            }
        }
        else
        {
            animator.SetBool("IsFalling", false);
            animator.SetBool("IsJumping", false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}