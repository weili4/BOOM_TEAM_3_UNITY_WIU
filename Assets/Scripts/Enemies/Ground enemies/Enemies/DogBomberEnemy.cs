using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DogBomberEnemy : GroundEnemyController
{
    [Header("point-blank fuse trigger distance")]
    [SerializeField] private float closeFuseDistance = 0.9f; // runs right up to player face before stopping

    [Header("explosion settings")]
    [SerializeField] private float explosionRadius = 3.0f;
    [SerializeField] private int explosionDamage = 45;
    [SerializeField] private float fuseDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("warning circle ui (world space canvas)")]
    [SerializeField] private GameObject warningCircleRoot;
    [SerializeField] private Image radialFillImage;
    [SerializeField] private Color warningFillColor = new Color(1f, 0.2f, 0.2f, 0.6f);

    [Header("explosion prefab (contains particles, sfx, etc)")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private AudioClip fuseTickSFX;

    private SpriteRenderer spriteRenderer;
    private bool isChargingExplosion = false;

    protected override void Awake()
    {
        base.Awake();
        canJump = false;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (warningCircleRoot != null)
        {
            warningCircleRoot.SetActive(false);
            warningCircleRoot.transform.localScale = Vector3.one * (explosionRadius * 2f);
        }
    }

    // override state machine so dog chases until it is point-blank
    protected override void FSMGroundUpdate()
    {
        if (isChargingExplosion)
        {
            if (rb != null) rb.linearVelocityX = 0f;
            return;
        }

        if (playerTarget == null)
        {
            currentGroundState = GroundState.ReturningToPatrol;
            return;
        }

        float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // trigger fuse only when right next to player
        if (distToPlayer <= closeFuseDistance)
        {
            currentGroundState = GroundState.Attack;
            ExecuteAttack();
        }
        else if (distToPlayer <= enemyData.chaseRange)
        {
            currentGroundState = GroundState.Chase;
            HandleChase();
        }
        else
        {
            currentGroundState = GroundState.ReturningToPatrol;
            HandleReturnToPatrol();
        }
    }

    protected override void ExecuteAttack()
    {
        if (!isChargingExplosion)
        {
            StartCoroutine(ExplosionChargeRoutine());
        }
    }

    private IEnumerator ExplosionChargeRoutine()
    {
        isChargingExplosion = true;
        isStunImmune = true; // unstoppable fuse once triggered

        if (rb != null) rb.linearVelocityX = 0;

        if (animator != null)
            animator.SetBool("IsMoving", false);

        if (warningCircleRoot != null)
        {
            warningCircleRoot.SetActive(true);
            if (radialFillImage != null)
            {
                radialFillImage.color = warningFillColor;
                radialFillImage.fillAmount = 0f;
            }
        }

        if (fuseTickSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(fuseTickSFX, transform.position, 1.0f);

        float elapsed = 0f;

        while (elapsed < fuseDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fuseDuration);

            if (radialFillImage != null)
            {
                radialFillImage.fillAmount = progress;
            }

            yield return null;
        }

        StartCoroutine(DetonateAndFadeOutRoutine());
    }

    private IEnumerator DetonateAndFadeOutRoutine()
    {
        // 1. hide warning circle
        if (warningCircleRoot != null)
            warningCircleRoot.SetActive(false);

        // 2. spawn explosion particle prefab
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 3. bulletproof aoe damage check (finds player on any layer/child)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        bool hasDamagedLeader = false;

        foreach (var col in hits)
        {
            if (hasDamagedLeader) break;

            // check active leader
            if (col.CompareTag("Player") || (col.transform.root != null && col.transform.root.CompareTag("Player")))
            {
                Damageable playerHealth = col.GetComponent<Damageable>();
                if (playerHealth == null) playerHealth = col.GetComponentInParent<Damageable>();

                if (playerHealth != null)
                {
                    Vector2 knockbackDir = ((Vector2)playerHealth.transform.position - (Vector2)transform.position).normalized;
                    if (knockbackDir.sqrMagnitude < 0.01f) knockbackDir = Vector2.up; // default up if right on top

                    playerHealth.TakeDamage(explosionDamage, knockbackDir, knockbackForce: 10f);
                    hasDamagedLeader = true;
                }
            }
        }

        // 4. disable physics so dog is a ghost while fading
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol != null) myCol.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 5. smooth fade-out
        if (spriteRenderer != null)
        {
            float fadeElapsed = 0f;
            Color c = spriteRenderer.color;

            while (fadeElapsed < fadeOutDuration)
            {
                fadeElapsed += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, fadeElapsed / fadeOutDuration);
                spriteRenderer.color = c;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(fadeOutDuration);
        }

        Destroy(gameObject);
    }
}