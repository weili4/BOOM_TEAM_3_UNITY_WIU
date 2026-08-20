using UnityEngine;

public class MeleeGroundEnemy : GroundEnemyController
{
    [Header("melee swing settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float attackCheckRadius = 1.4f; // increased default radius to reach player

    private bool isPerformingSwing = false;
    private bool hasHitPlayerThisSwing = false;

    protected override void FSMGroundUpdate()
    {
        if (!isGrounded && !isPerformingSwing)
        {
            currentGroundState = GroundState.Chase;
            return;
        }

        if (isPerformingSwing)
        {
            rb.linearVelocityX = 0;
            return;
        }

        base.FSMGroundUpdate();

        if (currentGroundState == GroundState.Attack)
        {
            if (!isGrounded)
            {
                currentGroundState = GroundState.Chase;
                return;
            }

            rb.linearVelocityX = 0;
            FlipTowards(playerTarget.position);

            if (attackCooldownTimer <= 0)
            {
                isPerformingSwing = true;
                hasHitPlayerThisSwing = false;

                int randomAttack = Random.Range(1, 3);
                if (animator != null)
                {
                    animator.SetTrigger(randomAttack == 1 ? "Attack1" : "Attack2");
                }

                if (enemyData != null && enemyData.attackSound != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(enemyData.attackSound, transform.position, 1.2f);
                }

                attackCooldownTimer = enemyData.attackCooldown;
            }
            else
            {
                float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);
                if (distToPlayer > enemyData.attackRange)
                {
                    currentGroundState = GroundState.Chase;
                }
            }
        }
    }

    // animation event called during the swing frame
    public void OnMeleeAttackCheck()
    {
        if (enemyData == null || hasHitPlayerThisSwing) return;

        // use attackPoint if assigned, otherwise check directly in front of enemy
        Vector2 checkCenter = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position + new Vector2(Mathf.Sign(transform.localScale.x) * 0.8f, 0f);

        // find all colliders in swing radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(checkCenter, attackCheckRadius);

        foreach (var col in hits)
        {
            // only damage the active leader, ignore benched allies
            if (col.CompareTag("Player") && col.TryGetComponent<Damageable>(out var playerHealth))
            {
                // calculate directional knockback vector away from enemy
                Vector2 attackDirection = ((Vector2)playerHealth.transform.position - (Vector2)transform.position).normalized;
                playerHealth.TakeDamage(enemyData.attackDamage, attackDirection, knockbackForce: 8.5f);
                hasHitPlayerThisSwing = true;
                break;
            }
        }
    }

    // animation event called when swing animation finishes
    public void OnMeleeAttackEnd()
    {
        isPerformingSwing = false;
        hasHitPlayerThisSwing = false;

        if (playerTarget != null && enemyData != null)
        {
            float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);
            if (distToPlayer > enemyData.attackRange)
            {
                currentGroundState = GroundState.Chase;
            }
        }
    }
}