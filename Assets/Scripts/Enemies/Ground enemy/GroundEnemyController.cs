using UnityEngine;

public abstract class GroundEnemyController : EnemyBase
{
    public enum GroundState { Patrol, Chase, Attack }
    [Header("ground state")]
    public GroundState currentGroundState = GroundState.Patrol;

    [Header("auto patrol stuff")]
    [SerializeField] protected float patrolDistance = 5f;
    protected Vector2 patrolPointA;
    protected Vector2 patrolPointB;
    protected Vector2 currentPatrolTargetPos;

    [Header("Ground check and jump settings")]
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected Vector2 groundCheckSize = new Vector2(0.3f, 0.1f);
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected float jumpForce = 8.0f;
    [SerializeField] protected float jumpForwardSpeed = 3.0f;
    [SerializeField] protected float wallCheckDistance = 2.0f;

    protected Rigidbody2D rb;
    protected bool isGrounded;
    protected float attackCooldownTimer = 0f;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
    }

    protected override void Start()
    {
        base.Start();

        Vector2 spawnPos = transform.position;
        patrolPointA = spawnPos + Vector2.left * patrolDistance;
        patrolPointB = spawnPos + Vector2.right * patrolDistance;
        currentPatrolTargetPos = patrolPointB;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        isGrounded = groundCheck != null && Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        FSMGroundUpdate();
        UpdateAnimatorParameters();
    }

    protected virtual void FSMGroundUpdate()
    {
        if (playerTarget == null || enemyData == null) return;

        float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        switch (currentGroundState)
        {
            case GroundState.Patrol:
                HandlePatrol();

                if (distToPlayer <= enemyData.chaseRange)
                {
                    currentGroundState = GroundState.Chase;
                }
                break;

            case GroundState.Chase:
                if (distToPlayer > enemyData.chaseRange)
                {
                    currentGroundState = GroundState.Patrol;
                }
                else if (distToPlayer <= enemyData.attackRange && attackCooldownTimer <= 0 && isGrounded && HasLineOfSightToPlayer())
                {
                    currentGroundState = GroundState.Attack;
                    rb.linearVelocityX = 0;
                }
                else
                {
                    HandleChase();
                }
                break;

            case GroundState.Attack:
                break;
        }
    }

    protected virtual void HandlePatrol()
    {
        if (enemyData == null) return;

        FlipTowards(currentPatrolTargetPos);
        float moveDir = (currentPatrolTargetPos.x - transform.position.x) > 0 ? 1f : -1f;

        // force forward horizontal velocity every frame
        float speedToUse = isGrounded ? enemyData.moveSpeed : Mathf.Max(enemyData.moveSpeed, jumpForwardSpeed);
        rb.linearVelocityX = moveDir * speedToUse;

        CheckAndJumpObstacle(moveDir);

        if (Mathf.Abs(transform.position.x - currentPatrolTargetPos.x) < 0.5f)
        {
            currentPatrolTargetPos = (currentPatrolTargetPos == patrolPointA) ? patrolPointB : patrolPointA;
        }
    }

    protected virtual void HandleChase()
    {
        if (playerTarget == null || enemyData == null) return;

        float moveDir = (playerTarget.position.x - transform.position.x) > 0 ? 1f : -1f;
        FlipTowards(playerTarget.position);

        // ALWAYS FORCE FORWARD VELOCITY EVERY FRAME (in mid air especially)
        float speedToUse = isGrounded ? enemyData.chaseSpeed : Mathf.Max(enemyData.chaseSpeed, jumpForwardSpeed);
        rb.linearVelocityX = moveDir * speedToUse;

        CheckAndJumpObstacle(moveDir);
    }

    protected virtual void CheckAndJumpObstacle(float moveDir)
    {
        if (!isGrounded) return;

        Vector2 rayOrigin = transform.position;
        Vector2 rayDir = new Vector2(moveDir, 0);

        RaycastHit2D wallHit = Physics2D.Raycast(rayOrigin, rayDir, wallCheckDistance, groundLayer);
        if (wallHit.collider != null)
        {
            // set jump velocity
            rb.linearVelocityY = jumpForce;

            // immediately set forward horizontal velocity
            rb.linearVelocityX = moveDir * Mathf.Max(enemyData.chaseSpeed, jumpForwardSpeed);

            if (animator != null) animator.SetTrigger("Jump");
        }
    }

    protected bool HasLineOfSightToPlayer()
    {
        if (playerTarget == null) return false;

        Vector2 direction = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, playerTarget.position);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, groundLayer);
        return hit.collider == null;
    }

    protected virtual void UpdateAnimatorParameters()
    {
        if (animator == null) return;

        bool isMoving = Mathf.Abs(rb.linearVelocityX) > 0.1f && currentGroundState == GroundState.Patrol;
        bool isRunning = Mathf.Abs(rb.linearVelocityX) > 0.1f && currentGroundState == GroundState.Chase;

        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsRunning", isRunning);
        animator.SetBool("IsGrounded", isGrounded);
    }
}