using UnityEngine;

public class ExplosiveEnemy : GroundEnemyController
{
    [Header("8-way burst settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private int burstDamage = 15;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private AudioClip burstSFX;

    protected override void Awake()
    {
        base.Awake();
        canJump = false;
    }

    protected override void ExecuteAttack()
    {
        isPerformingAttackAction = true;

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        else
        {
            OnExplosiveBurstEvent();
        }
    }

    // animation event called on the attack frame
    public void OnExplosiveBurstEvent()
    {
        if (isDead) return;

        if (burstSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(burstSFX, transform.position, 1.2f);
        }

        if (projectilePrefab != null)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                GameObject projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

                if (projObj.TryGetComponent<EnemyProjectile>(out var proj))
                {
                    proj.damage = burstDamage;
                    proj.speed = projectileSpeed;
                    proj.hitLayers = hitLayers;
                    proj.Launch(dir);
                }
            }
        }

        attackCooldownTimer = attackCooldown;
        isPerformingAttackAction = false;
    }

    protected override void InterruptActiveAttack()
    {
        isPerformingAttackAction = false;
    }
}