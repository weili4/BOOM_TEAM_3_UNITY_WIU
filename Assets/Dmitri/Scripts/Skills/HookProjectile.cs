using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class HookProjectile : MonoBehaviour
{
    private enum HookTargetType { None, Ground, Enemy }

    private GameObject owner;
    private Rigidbody2D playerRb;
    private Animator ownerAnimator;
    private LineRenderer lineRenderer;
    private Rigidbody2D hookRb;

    private float pullSpeed;
    private float enemyPullSpeed;
    private float retractSpeed;
    private float pullDelay;
    private float stopDistance;
    private float maxDistance;
    private float maxPullDuration;
    private int damageAmount;
    private LayerMask groundLayer;
    private LayerMask enemyLayer;
    private AudioClip attachSFX;
    private float soundVolume;

    private GameObject groundImpactVFXPrefab;
    private GameObject enemyImpactVFXPrefab;

    private bool isAttached = false;
    private bool isPulling = false;
    private bool isReturning = false;
    private HookTargetType targetType = HookTargetType.None;

    private Vector2 groundHitPoint;
    private GameObject hookedEnemy;
    private Rigidbody2D enemyRb;

    [Header("Camera Shake Settings")]
    public bool enableCameraShake = true;
    public float shakeForce = 1.0f;

    private void Awake()
    {
        hookRb = GetComponent<Rigidbody2D>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void Initialize(
        GameObject user,
        float pullSpd,
        float enemyPullSpd,
        float delay,
        float stopDist,
        float maxDist,
        int damage,
        LayerMask gLayer,
        LayerMask eLayer,
        AudioClip sfx,
        float vol,
        GameObject groundVfx = null,
        GameObject enemyVfx = null,
        float pullTimeout = 1.0f,
        float retractSpd = 20.0f,
        bool enableShake = true,
        float cameraShakeForce = 0.2f)
    {
        owner = user;
        pullSpeed = pullSpd;
        enemyPullSpeed = enemyPullSpd;
        retractSpeed = retractSpd;
        pullDelay = delay;
        stopDistance = stopDist;
        maxDistance = maxDist;
        damageAmount = damage;
        groundLayer = gLayer;
        enemyLayer = eLayer;
        attachSFX = sfx;
        soundVolume = vol;
        groundImpactVFXPrefab = groundVfx;
        enemyImpactVFXPrefab = enemyVfx;
        maxPullDuration = pullTimeout;
        enableCameraShake = enableShake;
        shakeForce = cameraShakeForce;

        if (owner != null)
        {
            playerRb = owner.GetComponent<Rigidbody2D>();
            ownerAnimator = owner.GetComponent<Animator>();
            if (ownerAnimator == null)
            {
                ownerAnimator = owner.GetComponentInChildren<Animator>();
            }
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
    }

    private void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        DrawRopeLine();

        // 1. CHECK FOR MAXIMUM EXTENSION TO TRIGGER RETRACTION
        if (!isAttached && !isReturning)
        {
            if (Vector2.Distance(owner.transform.position, transform.position) >= maxDistance)
            {
                isReturning = true;
            }
        }

        // 2. RETRACT BACK TO PLAYER
        if (isReturning && !isAttached)
        {
            Vector2 returnDir = ((Vector2)owner.transform.position - (Vector2)transform.position).normalized;

            if (hookRb != null && hookRb.bodyType != RigidbodyType2D.Kinematic)
            {
                hookRb.linearVelocity = returnDir * retractSpeed;
            }
            else
            {
                transform.position = Vector2.MoveTowards(transform.position, owner.transform.position, retractSpeed * Time.deltaTime);
            }

            // Clean up once returning hook reaches player
            if (Vector2.Distance(owner.transform.position, transform.position) <= stopDistance)
            {
                Destroy(gameObject);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isAttached || !isPulling || owner == null) return;

        // Set player animation only during ground pull
        if (ownerAnimator != null && targetType == HookTargetType.Ground)
        {
            ownerAnimator.SetBool("IsFalling", true);
            ownerAnimator.SetBool("IsJumping", false);
        }

        // 1. PULL PLAYER TO GROUND
        if (targetType == HookTargetType.Ground && playerRb != null)
        {
            Vector2 pullDir = (groundHitPoint - (Vector2)owner.transform.position).normalized;
            playerRb.linearVelocity = pullDir * pullSpeed;

            if (Vector2.Distance(owner.transform.position, groundHitPoint) <= stopDistance)
            {
                Destroy(gameObject);
            }
        }
        // 2. PULL ENEMY TO PLAYER
        else if (targetType == HookTargetType.Enemy)
        {
            if (hookedEnemy == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 enemyPos = hookedEnemy.transform.position;
            Vector2 playerPos = owner.transform.position;
            Vector2 pullDir = (playerPos - enemyPos).normalized;

            if (enemyRb != null)
            {
                enemyRb.linearVelocity = pullDir * enemyPullSpeed;
            }
            else
            {
                hookedEnemy.transform.position = Vector2.MoveTowards(enemyPos, playerPos, enemyPullSpeed * Time.fixedDeltaTime);
            }

            if (Vector2.Distance(enemyPos, playerPos) <= stopDistance)
            {
                if (enemyRb != null)
                {
                    enemyRb.linearVelocity = Vector2.zero;
                }
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleImpact(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.gameObject);
    }

    private void HandleImpact(GameObject hitObject)
    {
        // Don't trigger if already attached to a target
        if (isAttached) return;

        // Ignore collisions with the owner (Player)
        if (hitObject == owner || hitObject.transform.IsChildOf(owner.transform)) return;

        bool isGround = IsInLayerMask(hitObject, groundLayer);
        bool isEnemy = IsInLayerMask(hitObject, enemyLayer);

        if (!isGround && !isEnemy) return;

        // Trigger Cinemachine Camera Shake on owner (player) upon valid hook attach
        if (enableCameraShake && owner != null)
        {
            if (owner.TryGetComponent<CinemachineImpulseSource>(out CinemachineImpulseSource impulse))
            {
                impulse.GenerateImpulseWithForce(shakeForce);
            }
            else if (owner.GetComponentInChildren<CinemachineImpulseSource>() is CinemachineImpulseSource childImpulse)
            {
                childImpulse.GenerateImpulseWithForce(shakeForce);
            }
        }

        // Stop returning sequence since hook has latched onto something
        isReturning = false;
        isAttached = true;

        if (isEnemy)
        {
            hookedEnemy = GetRootHitObject(hitObject);
            enemyRb = hookedEnemy.GetComponent<Rigidbody2D>();

            // 1. Deal damage to boss/enemy as normal
            if (hookedEnemy.TryGetComponent<Damageable>(out Damageable damageable))
            {
                damageable.TakeDamage(damageAmount);
            }

            // 2. CHECK IF TARGET IS UNPULLABLE (Boss Tag or Boss Component)
            bool isUnpullable = hookedEnemy.CompareTag("Boss") || hookedEnemy.GetComponent<BossDeathHandler>() != null;

            if (isUnpullable)
            {
                // Treat Boss like static ground: Pull the PLAYER towards the Boss position
                targetType = HookTargetType.Ground;
                groundHitPoint = transform.position;
                transform.SetParent(hookedEnemy.transform);
            }
            else
            {
                // Regular Enemy: Pull the enemy towards the player
                targetType = HookTargetType.Enemy;
                transform.SetParent(hookedEnemy.transform);
            }

            if (hookRb != null)
            {
                hookRb.linearVelocity = Vector2.zero;
                hookRb.bodyType = RigidbodyType2D.Kinematic;
            }

            SpawnImpactVFX(enemyImpactVFXPrefab);
        }
        else if (isGround)
        {
            targetType = HookTargetType.Ground;
            groundHitPoint = transform.position;

            if (hookRb != null)
            {
                hookRb.linearVelocity = Vector2.zero;
                hookRb.bodyType = RigidbodyType2D.Kinematic;
            }

            SpawnImpactVFX(groundImpactVFXPrefab);
        }

        if (attachSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(attachSFX, transform.position, soundVolume);
        }

        if (pullDelay > 0f)
        {
            StartCoroutine(PullDelayRoutine());
        }
        else
        {
            StartPulling();
        }
    }

    private bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        if (((1 << obj.layer) & mask) != 0) return true;
        if (obj.transform.parent != null && ((1 << obj.transform.parent.gameObject.layer) & mask) != 0) return true;
        return false;
    }

    private GameObject GetRootHitObject(GameObject obj)
    {
        if (((1 << obj.layer) & enemyLayer) != 0) return obj;
        if (obj.transform.parent != null && ((1 << obj.transform.parent.gameObject.layer) & enemyLayer) != 0)
        {
            return obj.transform.parent.gameObject;
        }
        return obj;
    }

    private void SpawnImpactVFX(GameObject vfxPrefab)
    {
        if (vfxPrefab == null) return;

        GameObject vfxInstance = Instantiate(vfxPrefab, transform.position, transform.rotation);

        if (vfxInstance.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
        {
            float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(vfxInstance, totalDuration);
        }
        else
        {
            Destroy(vfxInstance, 2.0f);
        }
    }

    private IEnumerator PullDelayRoutine()
    {
        yield return new WaitForSeconds(pullDelay);
        StartPulling();
    }

    private void StartPulling()
    {
        isPulling = true;

        if (ownerAnimator != null && targetType == HookTargetType.Ground)
        {
            ownerAnimator.SetBool("IsFalling", true);
            ownerAnimator.SetBool("IsJumping", false);
        }

        if (maxPullDuration > 0f)
        {
            StartCoroutine(PullTimeoutRoutine());
        }

        if (targetType == HookTargetType.Ground && playerRb != null && owner != null)
        {
            Vector2 pullDir = (groundHitPoint - (Vector2)owner.transform.position).normalized;
            playerRb.linearVelocity = pullDir * pullSpeed;
        }
        else if (targetType == HookTargetType.Enemy && hookedEnemy != null && owner != null)
        {
            Vector2 pullDir = ((Vector2)owner.transform.position - (Vector2)hookedEnemy.transform.position).normalized;
            if (enemyRb != null)
            {
                enemyRb.linearVelocity = pullDir * enemyPullSpeed;
            }
        }
    }

    private IEnumerator PullTimeoutRoutine()
    {
        yield return new WaitForSeconds(maxPullDuration);
        Destroy(gameObject);
    }

    private void DrawRopeLine()
    {
        if (lineRenderer != null && owner != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, owner.transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }
    }

    private void OnDestroy()
    {
        if (ownerAnimator != null && isPulling && targetType == HookTargetType.Ground)
        {
            ownerAnimator.SetBool("IsFalling", false);
        }
    }
}