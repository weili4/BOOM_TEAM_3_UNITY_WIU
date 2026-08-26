using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class BossEnemyController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Attack1, // Laser Attack
        Attack2, // Shotgun Attack
        Attack3, // Jump Attack
        Attack4  // Grenade Barrage Attack
    }

    public enum ShakeType
    {
        Light,
        Medium,
        Heavy
    }

    [Header("State Properties")]
    [SerializeField] private State initialState = State.Idle;
    [SerializeField] private float globalAttackCooldown = 2f;
    private State currentState;
    private float attackCooldownTimer = 0f;

    [Header("Target & Detection Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stopDistance = 3.5f;
    [SerializeField] private LayerMask playerLayer = ~0;

    [Header("Backwards Movement Settings")]
    [SerializeField] private float fleeDuration = 0.5f;
    [SerializeField] private float fleeSpeedMultiplier = 1.2f;

    [Header("Laser Attack Settings (Attack 1)")]
    [SerializeField] private ParticleSystem chargingParticles;
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private float telegraphDuration = 1.5f;
    [SerializeField] private float lockInTime = 0.5f;
    [SerializeField] private float pulseDuration = 0.8f;
    [SerializeField] private float maxLaserWidth = 0.8f;
    [SerializeField] private float maxLaserDistance = 20f;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private ShakeType laserShakeType = ShakeType.Heavy;

    [Header("Shotgun Attack Settings (Attack 2)")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private LayerMask playerAndGroundLayer;
    [SerializeField] private int projectileDamage = 15;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private int shotgunPelletCount = 5;
    [SerializeField] private float shotgunSpreadAngle = 30f;
    [SerializeField] private float shotgunFireDelay = 0.3f;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float delayBetweenBursts = 0.2f;
    [SerializeField] private ShakeType shotgunShakeType = ShakeType.Medium;

    [Header("Jump Slam Attack Settings (Attack 3)")]
    [SerializeField] private float jumpHeightForce = 12f;
    [SerializeField] private float jumpMoveSpeed = 8f;
    [SerializeField] private float jumpWindupTime = 0.3f;
    [SerializeField] private float landingDelay = 0.3f;
    [SerializeField] private ParticleSystem landingParticlePrefab;
    [SerializeField] private Vector2 landingBoxSize = new Vector2(4f, 2f);
    [SerializeField] private Vector2 landingBoxOffset = new Vector2(0f, -0.5f);
    [SerializeField] private int jumpDamage = 30;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private ShakeType jumpSlamShakeType = ShakeType.Heavy;

    [Header("Grenade Barrage Attack Settings (Attack 4)")]
    [SerializeField] private float attack4WindupDelay = 0.4f;
    [SerializeField] private GameObject mortarProjectilePrefab;
    [SerializeField] private Transform mortarFirePoint;
    [SerializeField] private int mortarCount = 3;
    [SerializeField] private float mortarLaunchForce = 12f;
    [SerializeField] private float mortarSpreadAngle = 20f;
    [SerializeField] private float mortarFireDelay = 0.15f;

    [Header("Camera Shake Settings")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float cameraShakeIntensity = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip aimWarningSound;
    [SerializeField] private AudioClip grenadeThrow;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip landSound;

    private Transform target;
    private Vector2 lockedLaserDirection;
    private bool isAttackingSequence = false;
    private List<Damageable> hitTargetsThisPulse = new List<Damageable>();

    [Header("Components")]
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Animator animator;
    private Vector3 baseScale;

    private void Start()
    {
        baseScale = transform.localScale;

        if (body == null)
            body = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (laserLine != null)
            laserLine.enabled = false;

        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();

        ChangeState(initialState);
    }

    private void Update()
    {
        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:
                Idle();
                break;
            case State.Attack1:
                Attack1();
                break;
            case State.Attack2:
                Attack2();
                break;
            case State.Attack3:
                Attack3();
                break;
            case State.Attack4:
                Attack4();
                break;
        }
    }

    // --- STATE FUNCTIONS ---

    private void Idle()
    {
        target = FindPlayerInRange();

        if (target == null)
        {
            StopMovement();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);
        LookAtTarget();

        if (distanceToPlayer <= stopDistance && attackCooldownTimer <= 0 && !isAttackingSequence)
        {
            StopMovement();
            ChangeState(ChooseRandomAttack());
            return;
        }

        if (distanceToPlayer > stopDistance)
        {
            float directionX = Mathf.Sign(target.position.x - transform.position.x);
            if (body != null)
            {
                body.linearVelocityX = directionX * moveSpeed;
            }
        }
        else
        {
            StopMovement();
        }
    }

    private State ChooseRandomAttack()
    {
        int randomIndex = Random.Range(0, 4);
        switch (randomIndex)
        {
            case 0: return State.Attack4;
            case 1: return State.Attack4;
            case 2: return State.Attack4;
            case 3: return State.Attack4;
            default: return State.Idle;
        }
    }

    private void Attack1() { }
    private void Attack2() { }
    private void Attack3() { }
    private void Attack4() { }

    // --- COROUTINES ---

    private IEnumerator GrenadeBarrageRoutine()
    {
        isAttackingSequence = true;
        StopMovement();

        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
        }

        if (grenadeThrow != null)
        {
            AudioSource.PlayClipAtPoint(grenadeThrow, transform.position);
        }

        yield return new WaitForSeconds(attack4WindupDelay);

        Transform spawnPoint = mortarFirePoint != null ? mortarFirePoint : (firePoint != null ? firePoint : transform);

        yield return StartCoroutine(GrenadeBarrageRoutine(spawnPoint));

        yield return new WaitForSeconds(0.2f);

        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
        }

        attackCooldownTimer = globalAttackCooldown;
        isAttackingSequence = false;
        ChangeState(State.Idle);
    }

    private IEnumerator GrenadeBarrageRoutine(Transform spawnPoint)
    {
        if (mortarProjectilePrefab == null) yield break;

        float baseAngle = 90f;
        float startAngle = baseAngle + (mortarSpreadAngle / 2f);
        float angleStep = mortarCount > 1 ? mortarSpreadAngle / (mortarCount - 1) : 0f;

        for (int i = 0; i < mortarCount; i++)
        {
            float currentAngle = startAngle - (i * angleStep);
            Vector2 launchDirection = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));

            GameObject projObj = Instantiate(mortarProjectilePrefab, spawnPoint.position, mortarProjectilePrefab.transform.rotation);

            if (grenadeThrow != null)
            {
                AudioSource.PlayClipAtPoint(grenadeThrow, spawnPoint.position);
            }

            if (projObj.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
            {
                rb.linearVelocity = launchDirection * mortarLaunchForce;
            }

            if (i < mortarCount - 1)
            {
                yield return new WaitForSeconds(mortarFireDelay);
            }
        }
    }

    private IEnumerator JumpAttackRoutine()
    {
        isAttackingSequence = true;
        StopMovement();

        if (animator != null)
        {
            animator.SetBool("IsJumping", true);
        }

        yield return new WaitForSeconds(jumpWindupTime);

        float jumpDirX = 0f;
        float targetXPosition = transform.position.x;

        if (target != null)
        {
            LookAtTarget();
            targetXPosition = target.position.x;
            jumpDirX = Mathf.Sign(targetXPosition - transform.position.x);
        }

        if (body != null)
        {
            body.linearVelocity = new Vector2(jumpDirX * jumpMoveSpeed, jumpHeightForce);
        }

        if (landSound != null)
        {
            AudioSource.PlayClipAtPoint(landSound, transform.position);
        }

        yield return new WaitForFixedUpdate();

        bool stopHorizontalMove = false;

        while (!IsGrounded() || (body != null && body.linearVelocityY > 0.1f))
        {
            if (!stopHorizontalMove && body != null)
            {
                bool reachedFromLeft = jumpDirX > 0 && transform.position.x >= targetXPosition;
                bool reachedFromRight = jumpDirX < 0 && transform.position.x <= targetXPosition;

                if (reachedFromLeft || reachedFromRight)
                {
                    body.linearVelocityX = 0f;
                    stopHorizontalMove = true;
                }
            }

            yield return null;
        }

        StopMovement();

        if (animator != null)
        {
            animator.SetBool("IsJumping", false);
        }

        if (landSound != null)
        {
            AudioSource.PlayClipAtPoint(landSound, transform.position);
        }

        TriggerShake(jumpSlamShakeType);

        if (landingParticlePrefab != null)
        {
            Vector3 spawnPos = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
            Instantiate(landingParticlePrefab, spawnPos, landingParticlePrefab.transform.rotation);
        }

        PerformLandingAOEDamage();

        yield return new WaitForSeconds(landingDelay);

        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
        }

        attackCooldownTimer = globalAttackCooldown;
        isAttackingSequence = false;
        ChangeState(State.Idle);
    }

    private bool IsGrounded()
    {
        Vector3 checkPos = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
        return Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);
    }

    private void PerformLandingAOEDamage()
    {
        Vector2 boxCenter = (Vector2)transform.position + landingBoxOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, landingBoxSize, 0f, playerLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player") || hit.GetComponent<PlayerController>() != null)
            {
                if (hit.TryGetComponent<Damageable>(out Damageable playerHealth))
                {
                    playerHealth.TakeDamage(jumpDamage);
                }
            }
        }
    }

    private IEnumerator ShotgunAttackRoutine()
    {
        isAttackingSequence = true;

        float fleeElapsed = 0f;
        while (fleeElapsed < fleeDuration)
        {
            fleeElapsed += Time.deltaTime;
            if (target != null)
            {
                LookAtTarget();
                float awayDirectionX = Mathf.Sign(transform.position.x - target.position.x);
                if (body != null)
                {
                    body.linearVelocityX = awayDirectionX * (moveSpeed * fleeSpeedMultiplier);
                }
            }
            yield return null;
        }

        StopMovement();

        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
        }

        yield return new WaitForSeconds(shotgunFireDelay);

        for (int i = 0; i < burstCount; i++)
        {
            if (target != null)
            {
                LookAtTarget();
                Vector2 aimDir = (target.position - (firePoint != null ? firePoint.position : transform.position)).normalized;

                if (attackSound != null)
                {
                    AudioSource.PlayClipAtPoint(attackSound, transform.position);
                }

                FireShotgunSpread(aimDir);
                TriggerShake(shotgunShakeType);
            }

            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(delayBetweenBursts);
            }
        }

        yield return new WaitForSeconds(0.1f);

        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
        }

        attackCooldownTimer = globalAttackCooldown;
        isAttackingSequence = false;
        ChangeState(State.Idle);
    }

    private void FireShotgunSpread(Vector2 centerDirection)
    {
        float baseAngle = Mathf.Atan2(centerDirection.y, centerDirection.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - (shotgunSpreadAngle / 2f);
        float angleStep = shotgunSpreadAngle / (shotgunPelletCount - 1);

        for (int i = 0; i < shotgunPelletCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Vector2 pelletDir = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
            SpawnBullet(pelletDir);
        }
    }

    private void SpawnBullet(Vector2 direction)
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        if (bullet.TryGetComponent<EnemyProjectile>(out EnemyProjectile proj))
        {
            proj.damage = projectileDamage;
            proj.hitLayers = playerAndGroundLayer;
            proj.Launch(direction * projectileSpeed);
        }
        else if (bullet.TryGetComponent<Rigidbody2D>(out Rigidbody2D bulletRb))
        {
            bulletRb.linearVelocity = direction * projectileSpeed;
        }
    }

    private IEnumerator LaserAttackRoutine()
    {
        isAttackingSequence = true;

        float fleeElapsed = 0f;
        while (fleeElapsed < fleeDuration)
        {
            fleeElapsed += Time.deltaTime;
            if (target != null)
            {
                LookAtTarget();
                float awayDirectionX = Mathf.Sign(transform.position.x - target.position.x);
                if (body != null)
                {
                    body.linearVelocityX = awayDirectionX * (moveSpeed * fleeSpeedMultiplier);
                }
            }
            yield return null;
        }

        StopMovement();

        ParticleSystem currentParticles = null;
        if (chargingParticles != null)
        {
            Transform spawnPoint = firePoint != null ? firePoint : transform;
            currentParticles = Instantiate(chargingParticles, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            currentParticles.Play();
        }

        if (aimWarningSound != null)
        {
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
            if (target != null)
            {
                LookAtTarget();
                Vector2 aimDir = (target.position - transform.position).normalized;
                DrawLaserRay(aimDir, maxLaserDistance);
            }
            yield return null;
        }

        if (target != null)
        {
            lockedLaserDirection = (target.position - transform.position).normalized;
        }

        if (laserLine != null)
        {
            laserLine.startColor = Color.yellow;
            laserLine.endColor = Color.yellow;
        }

        if (currentParticles != null)
        {
            currentParticles.Stop();
            Destroy(currentParticles.gameObject, 1f);
        }

        yield return new WaitForSeconds(lockInTime);

        if (attackSound != null)
        {
            AudioSource.PlayClipAtPoint(attackSound, transform.position);
        }

        if (laserLine != null)
        {
            laserLine.startColor = Color.cyan;
            laserLine.endColor = Color.cyan;
        }

        TriggerShake(laserShakeType);

        hitTargetsThisPulse.Clear();
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

            Vector2 hitEndpoint = DrawLaserRay(lockedLaserDirection, maxLaserDistance);
            CheckLaserDamage(lockedLaserDirection, hitEndpoint, currentWidth);

            yield return null;
        }

        if (laserLine != null)
            laserLine.enabled = false;

        isAttackingSequence = false;
        ChangeState(State.Idle);
    }

    // --- HELPER FUNCTIONS ---

    private void TriggerShake(ShakeType shakeType)
    {
        if (impulseSource == null) return;

        float multiplier = shakeType switch
        {
            ShakeType.Light => 0.5f,
            ShakeType.Medium => 1.0f,
            ShakeType.Heavy => 2.5f,
            _ => 1.0f
        };

        impulseSource.GenerateImpulse(cameraShakeIntensity * multiplier);
    }

    private Transform FindPlayerInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, playerLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                return hit.transform;
            }
        }
        return null;
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
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player") || hit.collider.GetComponent<PlayerController>() != null)
                {
                    if (hit.collider.TryGetComponent<Damageable>(out Damageable playerHealth))
                    {
                        if (!hitTargetsThisPulse.Contains(playerHealth))
                        {
                            playerHealth.TakeDamage(attackDamage);
                            hitTargetsThisPulse.Add(playerHealth);
                        }
                    }
                }
            }
        }
    }

    private void LookAtTarget()
    {
        if (target == null) return;

        if (target.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }
        else if (target.position.x < transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        }
    }

    private void StopMovement()
    {
        if (body != null)
        {
            body.linearVelocityX = 0f;
        }
    }

    // --- STATE SWITCHING ---

    private void ChangeState(State next)
    {
        currentState = next;

        switch (currentState)
        {
            case State.Attack1:
                StartCoroutine(LaserAttackRoutine());
                break;
            case State.Attack2:
                StartCoroutine(ShotgunAttackRoutine());
                break;
            case State.Attack3:
                StartCoroutine(JumpAttackRoutine());
                break;
            case State.Attack4:
                StartCoroutine(GrenadeBarrageRoutine());
                break;
        }
    }

    // --- VISUALIZE RANGE IN EDITOR ---

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // Landing Impact Box Visualization
        Gizmos.color = Color.magenta;
        Vector2 boxCenter = (Vector2)transform.position + landingBoxOffset;
        Gizmos.DrawWireCube(boxCenter, landingBoxSize);

        // Ground Check Visualization
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}