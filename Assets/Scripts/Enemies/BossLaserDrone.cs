using System.Collections;
using Pathfinding;
using UnityEngine;

public class BossLaserDrone : EnemyBase // from enemybase
{
    // BOSS LASER DRONE WITH AIM WARNING AND PULSE SFX

    public enum BossState { Idle, Chase, Flee, Telegraphing, FiringLaser }
    [Header("BOSS STATE")]
    public BossState currentState = BossState.Idle;

    [Header("DISTANCE CONTROLS")]
    [SerializeField] private float fleeDistance = 3.5f;

    [Header("LASER SETTINGS")]
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private float telegraphDuration = 1.5f;
    [SerializeField] private float lockInTime = 0.5f;
    [SerializeField] private float pulseDuration = 0.8f;
    [SerializeField] private float maxLaserWidth = 0.8f;
    [SerializeField] private float maxLaserDistance = 20f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask playerLayer;

    [Header("AIM WARNING AUDIO")]
    [SerializeField] private AudioClip aimWarningSound; // drone warning beep sound

    private AIDestinationSetter destSetter;
    private AIPath aiPath;
    private Vector2 lockedLaserDirection;
    private float attackCooldownTimer = 0f;
    private bool isAttackingSequence = false;
    private Transform tempFleeTarget;

    protected override void Awake()
    {
        base.Awake();
        destSetter = GetComponent<AIDestinationSetter>();
        aiPath = GetComponent<AIPath>();

        if (aiPath != null)
        {
            aiPath.enableRotation = false;
            aiPath.orientation = OrientationMode.YAxisForward;
            aiPath.gravity = Vector3.zero;
        }

        if (laserLine != null)
            laserLine.enabled = false;

        GameObject fleeObj = new GameObject("BossFleeTarget");
        tempFleeTarget = fleeObj.transform;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead || playerTarget == null || enemyData == null) return;

        transform.rotation = Quaternion.identity;
        Vector3 pos = transform.position;
        pos.z = 0;
        transform.position = pos;

        bool isMoving = aiPath != null && aiPath.canMove && aiPath.desiredVelocity.sqrMagnitude > 0.05f;
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
        }

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (!isAttackingSequence)
        {
            FSMUpdate();
        }
    }

    private bool HasLineOfSightToPlayer()
    {
        if (playerTarget == null) return false;

        Vector2 direction = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, playerTarget.position);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, groundLayer);
        return hit.collider == null;
    }

    private void FSMUpdate()
    {
        float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        if (distToPlayer < fleeDistance)
        {
            currentState = BossState.Flee;
            FlipTowards(playerTarget.position);

            Vector2 fleeDirection = ((Vector2)transform.position - (Vector2)playerTarget.position).normalized;
            tempFleeTarget.position = (Vector2)transform.position + fleeDirection * 4f;

            if (aiPath != null)
            {
                aiPath.canMove = true;
                aiPath.maxSpeed = enemyData.chaseSpeed * 1.3f;
                if (destSetter != null) destSetter.target = tempFleeTarget;
            }
            return;
        }

        switch (currentState)
        {
            case BossState.Idle:
            case BossState.Flee:
                FlipTowards(playerTarget.position);
                if (aiPath != null) aiPath.canMove = false;

                if (distToPlayer <= enemyData.chaseRange)
                {
                    currentState = BossState.Chase;
                }
                break;

            case BossState.Chase:
                FlipTowards(playerTarget.position);

                if (distToPlayer > enemyData.chaseRange)
                {
                    currentState = BossState.Idle;
                    if (aiPath != null) aiPath.canMove = false;
                }
                else if (distToPlayer <= enemyData.attackRange && attackCooldownTimer <= 0 && HasLineOfSightToPlayer())
                {
                    StartCoroutine(LaserAttackRoutine());
                }
                else
                {
                    if (aiPath != null)
                    {
                        aiPath.canMove = true;
                        aiPath.maxSpeed = enemyData.chaseSpeed;
                        if (destSetter != null) destSetter.target = playerTarget;
                    }
                }
                break;
        }
    }

    private IEnumerator LaserAttackRoutine()
    {
        isAttackingSequence = true;
        currentState = BossState.Telegraphing;

        if (aiPath != null) aiPath.canMove = false;

        // play aim warning sfx
        if (aimWarningSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(aimWarningSound, transform.position, 1.2f);
            else
                AudioSource.PlayClipAtPoint(aimWarningSound, transform.position);
        }

        if (laserLine != null)
        {
            laserLine.enabled = true;
            laserLine.startWidth = 0.08f;
            laserLine.endWidth = 0.08f;
            laserLine.startColor = new Color(1f, 0f, 0f, 0.4f);
            laserLine.endColor = new Color(1f, 0f, 0f, 0.4f);
        }

        float elapsed = 0f;
        float trackingTime = telegraphDuration - lockInTime;

        while (elapsed < trackingTime)
        {
            elapsed += Time.deltaTime;
            Vector2 aimDir = (playerTarget.position - transform.position).normalized;
            DrawLaserRay(aimDir, maxLaserDistance);
            yield return null;
        }

        lockedLaserDirection = (playerTarget.position - transform.position).normalized;
        if (laserLine != null)
        {
            laserLine.startColor = Color.yellow;
            laserLine.endColor = Color.yellow;
        }

        yield return new WaitForSeconds(lockInTime);

        currentState = BossState.FiringLaser;

        // play laser pulse sfx like very loud
        if (enemyData.attackSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(enemyData.attackSound, transform.position, 1.5f);
            else
                AudioSource.PlayClipAtPoint(enemyData.attackSound, transform.position);
        }

        if (laserLine != null)
        {
            laserLine.startColor = Color.cyan;
            laserLine.endColor = Color.cyan;
        }

        Vector2 initialHitEndpoint = DrawLaserRay(lockedLaserDirection, maxLaserDistance);
        CheckLaserDamage(lockedLaserDirection, initialHitEndpoint, maxLaserWidth);

        float pulseElapsed = 0f;
        while (pulseElapsed < pulseDuration)
        {
            pulseElapsed += Time.deltaTime;
            float progress = pulseElapsed / pulseDuration;

            float currentWidth = Mathf.Sin(progress * Mathf.PI) * maxLaserWidth;
            if (laserLine != null)
            {
                laserLine.startWidth = currentWidth;
                laserLine.endWidth = currentWidth;
            }

            DrawLaserRay(lockedLaserDirection, maxLaserDistance);
            yield return null;
        }

        if (laserLine != null) laserLine.enabled = false;

        attackCooldownTimer = enemyData.attackCooldown;
        isAttackingSequence = false;
        currentState = BossState.Chase;
    }

    private Vector2 DrawLaserRay(Vector2 direction, float distance)
    {
        Vector2 endPoint = (Vector2)transform.position + direction * distance;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, groundLayer);
        if (hit.collider != null)
        {
            endPoint = hit.point;
        }

        if (laserLine != null)
        {
            laserLine.SetPosition(0, transform.position);
            laserLine.SetPosition(1, endPoint);
        }

        return endPoint;
    }

    private void CheckLaserDamage(Vector2 direction, Vector2 endPoint, float width)
    {
        float actualDistance = Vector2.Distance(transform.position, endPoint);

        RaycastHit2D[] hits = Physics2D.BoxCastAll(transform.position, new Vector2(width, width), 0f, direction, actualDistance, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Player") || hit.collider.GetComponent<PlayerController>() != null)
            {
                if (hit.collider.TryGetComponent<Damageable>(out Damageable playerHealth))
                {
                    playerHealth.TakeDamage(enemyData.attackDamage);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (tempFleeTarget != null)
            Destroy(tempFleeTarget.gameObject);
    }
}