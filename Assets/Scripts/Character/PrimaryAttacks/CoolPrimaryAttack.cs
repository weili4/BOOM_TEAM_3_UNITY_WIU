using UnityEngine;
using UnityEngine.InputSystem;

public class CoolPrimaryAttack : CharacterPrimaryAttack
{
    [Header("punch timing and damage")]
    [SerializeField] private float punchInterval = 0.18f; // time between punches while holding left click
    [SerializeField] private int punchDamage = 12;
    [SerializeField] private float punchRange = 1.2f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("hitbox point")]
    [SerializeField] private Transform punchPoint;

    [Header("audio and vfx")]
    [SerializeField] private AudioClip punchWhooshSFX;
    [SerializeField] private AudioClip punchHitSFX;
    [SerializeField] private GameObject punchVFXPrefab;

    private float punchTimer = 0f;

    protected override void Update()
    {
        base.Update();
        if (punchTimer > 0f) punchTimer -= Time.deltaTime;
    }

    protected override void HandleAttack()
    {
        bool isHoldingLeftClick = Mouse.current != null && Mouse.current.leftButton.isPressed;

        // while left click is held, punch at intervals
        if (isHoldingLeftClick && punchTimer <= 0f)
        {
            ExecutePunch();
            punchTimer = punchInterval;
        }
    }

    private void ExecutePunch()
    {
        if (animator != null)
        {
            animator.SetTrigger("IsAttacking");
        }

        if (punchWhooshSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(punchWhooshSFX, transform.position, 0.8f);
        }

        // calculate punch point facing direction
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 checkPos = punchPoint != null ? (Vector2)punchPoint.position : (Vector2)transform.position + new Vector2(facingDir * 0.7f, 0f);

        if (punchVFXPrefab != null)
        {
            Instantiate(punchVFXPrefab, checkPos, Quaternion.identity);
        }

        // damage enemies
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, punchRange, enemyLayer);
        foreach (var col in hits)
        {
            if (col.CompareTag("Player") || col.CompareTag("Ally")) continue;

            if (col.TryGetComponent<Damageable>(out var enemy))
            {
                enemy.TakeDamage(punchDamage, new Vector2(facingDir, 0.2f), knockbackForce: 4f);

                if (punchHitSFX != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(punchHitSFX, col.transform.position);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 checkPos = punchPoint != null ? (Vector2)punchPoint.position : (Vector2)transform.position + new Vector2(facingDir * 0.7f, 0f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(checkPos, punchRange);
    }
}