using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DogBomberEnemy : GroundEnemyController
{
    [Header("explosion settings")]
    [SerializeField] private float explosionRadius = 2.8f;
    [SerializeField] private int explosionDamage = 45;
    [SerializeField] private float fuseDuration = 1.2f;
    [SerializeField] private LayerMask playerLayer;

    [Header("warning circle ui (world space canvas)")]
    [SerializeField] private GameObject warningCircleRoot;
    [SerializeField] private Image radialFillImage;
    [SerializeField] private Color warningFillColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    [Header("audio and vfx")]
    [SerializeField] private AudioClip fuseTickSFX;
    [SerializeField] private AudioClip explosionSFX;
    [SerializeField] private GameObject explosionVFX;

    protected override void Awake()
    {
        base.Awake();
        canJump = false;

        if (warningCircleRoot != null)
        {
            warningCircleRoot.SetActive(false);
            warningCircleRoot.transform.localScale = Vector3.one * (explosionRadius * 2f);
        }
    }

    protected override void ExecuteAttack()
    {
        if (!isPerformingAttackAction)
        {
            StartCoroutine(ExplosionChargeRoutine());
        }
    }

    private IEnumerator ExplosionChargeRoutine()
    {
        isPerformingAttackAction = true;
        isStunImmune = true; // unstoppable fuse

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

        Detonate();
    }

    private void Detonate()
    {
        if (warningCircleRoot != null)
            warningCircleRoot.SetActive(false);

        if (explosionSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(explosionSFX, transform.position, 1.5f);

        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        Collider2D hit = Physics2D.OverlapCircle(transform.position, explosionRadius, playerLayer);
        if (hit != null && hit.CompareTag("Player") && hit.TryGetComponent<Damageable>(out var playerHealth))
        {
            Vector2 knockbackDir = ((Vector2)playerHealth.transform.position - (Vector2)transform.position).normalized;
            playerHealth.TakeDamage(explosionDamage, knockbackDir, knockbackForce: 10f);
        }

        Destroy(gameObject);
    }
}