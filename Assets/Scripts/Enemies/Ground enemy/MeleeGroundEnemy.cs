using UnityEngine;

public class MeleeGroundEnemy : GroundEnemyController
{
    // MELEE GROUND ENEMY

    [Header("MELEE SETTINGS")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float attackCheckRadius = 0.8f;

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

                // PLAY MELEE WEAPON SWING SFX VIA AUDIO MIXER
                if (enemyData != null && enemyData.attackSound != null)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(enemyData.attackSound, transform.position, 1.2f);
                    else
                        AudioSource.PlayClipAtPoint(enemyData.attackSound, transform.position);
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

    public void OnMeleeAttackCheck()
    {
        if (attackPoint == null || enemyData == null || hasHitPlayerThisSwing) return;

        Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, attackCheckRadius, playerLayer);
        if (hitPlayer != null && hitPlayer.TryGetComponent<Damageable>(out Damageable playerHealth))
        {
            playerHealth.TakeDamage(enemyData.attackDamage);
            hasHitPlayerThisSwing = true;
        }
    }

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