using UnityEngine;

public abstract class GroundEnemyController : EnemyBase
{
    public enum GroundState { Patrol, Chase, Attack, ReturningToPatrol }

    [Header("ground state")]
    public GroundState currentGroundState = GroundState.Patrol;

    [Header("patrol and leash settings")]
    [SerializeField] protected float patrolDistance = 5f;
    [SerializeField] protected float maxLeashDistance = 12f;
    protected Vector2 spawnPosition;
    protected Vector2 patrolPointA;
    protected Vector2 patrolPointB;
    protected Vector2 currentPatrolTargetPos;

    [Header("jump settings")]
    [SerializeField] protected bool canJump = true;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected Vector2 groundCheckSize = new Vector2(0.3f, 0.1f);
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected float jumpForce = 8.0f;
    [SerializeField] protected float jumpForwardSpeed = 3.0f;
    [SerializeField] protected float wallCheckDistance = 2.0f;

    protected bool isGrounded;
    protected float attackCooldownTimer = 0f;
    protected bool isPerformingAttackAction = false;

    protected override void Start()
    {
        base.Start();
        spawnPosition = transform.position;
        patrolPointA = spawnPosition + Vector2.left * patrolDistance;
        patrolPointB = spawnPosition + Vector2.right * patrolDistance;
        currentPatrolTargetPos = patrolPointB;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead || isStunned) return;

        isGrounded = groundCheck != null && Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        FSMGroundUpdate();
        UpdateAnimatorParameters();
    }

    protected virtual void FSMGroundUpdate()
    {
        if (enemyData == null) return;

        float distFromSpawn = Vector2.Distance(transform.position, spawnPosition);

        if (distFromSpawn > maxLeashDistance && currentGroundState == GroundState.Chase && !isPerformingAttackAction)
        {
            currentGroundState = GroundState.ReturningToPatrol;
        }

        switch (currentGroundState)
        {
            case GroundState.Patrol:
                HandlePatrol();
                if (playerTarget != null && Vector2.Distance(transform.position, playerTarget.position) <= enemyData.chaseRange)
                {
                    currentGroundState = GroundState.Chase;
                }
                break;

            case GroundState.Chase:
                if (playerTarget == null)
                {
                    currentGroundState = GroundState.ReturningToPatrol;
                    return;
                }

                float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);

                if (distToPlayer > enemyData.chaseRange)
                {
                    currentGroundState = GroundState.ReturningToPatrol;
                }
                else if (distToPlayer <= enemyData.attackRange)
                {
                    currentGroundState = GroundState.Attack;
                    if (rb != null) rb.linearVelocityX = 0f;
                }
                else
                {
                    HandleChase();
                }
                break;

            case GroundState.Attack:
                if (rb != null) rb.linearVelocityX = 0f; // completely stop moving during attack state

                if (playerTarget != null)
                {
                    FlipTowards(playerTarget.position);
                    float d = Vector2.Distance(transform.position, playerTarget.position);

                    // if not currently in an attack animation/coroutine
                    if (!isPerformingAttackAction)
                    {
                        if (d <= enemyData.attackRange && attackCooldownTimer <= 0f)
                        {
                            ExecuteAttack();
                        }
                        else if (d > enemyData.attackRange && d <= enemyData.chaseRange)
                        {
                            currentGroundState = GroundState.Chase;
                        }
                        else if (d > enemyData.chaseRange)
                        {
                            currentGroundState = GroundState.ReturningToPatrol;
                        }
                    }
                }
                break;

            case GroundState.ReturningToPatrol:
                HandleReturnToPatrol();
                break;
        }
    }

    protected virtual void HandlePatrol()
    {
        FlipTowards(currentPatrolTargetPos);
        float moveDir = (currentPatrolTargetPos.x - transform.position.x) > 0 ? 1f : -1f;

        float speedToUse = isGrounded ? enemyData.moveSpeed : Mathf.Max(enemyData.moveSpeed, jumpForwardSpeed);
        if (rb != null) rb.linearVelocityX = moveDir * speedToUse;

        if (canJump) CheckAndJumpObstacle(moveDir);

        if (Mathf.Abs(transform.position.x - currentPatrolTargetPos.x) < 0.5f)
        {
            currentPatrolTargetPos = (currentPatrolTargetPos == patrolPointA) ? patrolPointB : patrolPointA;
        }
    }

    protected virtual void HandleChase()
    {
        if (playerTarget == null) return;

        float moveDir = (playerTarget.position.x - transform.position.x) > 0 ? 1f : -1f;
        FlipTowards(playerTarget.position);

        float speedToUse = isGrounded ? enemyData.chaseSpeed : Mathf.Max(enemyData.chaseSpeed, jumpForwardSpeed);
        if (rb != null) rb.linearVelocityX = moveDir * speedToUse;

        if (canJump) CheckAndJumpObstacle(moveDir);
    }

    protected virtual void HandleReturnToPatrol()
    {
        FlipTowards(spawnPosition);
        float moveDir = (spawnPosition.x - transform.position.x) > 0 ? 1f : -1f;

        if (rb != null) rb.linearVelocityX = moveDir * enemyData.moveSpeed;
        if (canJump) CheckAndJumpObstacle(moveDir);

        if (Mathf.Abs(transform.position.x - spawnPosition.x) < 1.0f)
        {
            currentGroundState = GroundState.Patrol;
        }
    }

    protected virtual void CheckAndJumpObstacle(float moveDir)
    {
        if (!canJump || !isGrounded) return;

        Vector2 rayOrigin = transform.position;
        Vector2 rayDir = new Vector2(moveDir, 0);

        RaycastHit2D wallHit = Physics2D.Raycast(rayOrigin, rayDir, wallCheckDistance, groundLayer);
        if (wallHit.collider != null)
        {
            if (rb != null)
            {
                rb.linearVelocityY = jumpForce;
                rb.linearVelocityX = moveDir * Mathf.Max(enemyData.chaseSpeed, jumpForwardSpeed);
            }

            if (animator != null) animator.SetTrigger("Jump");
        }
    }

    protected virtual void ExecuteAttack() { }

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