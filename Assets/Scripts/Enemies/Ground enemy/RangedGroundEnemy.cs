using System.Collections;
using UnityEngine;

public class RangedGroundEnemy : GroundEnemyController
{
    // RANGED GROUND ENEMY WITH AUDIO MIXER ROUTING

    public enum RangedType { Burst, Shotgun }

    [Header("RANGED MODE")]
    [SerializeField] private RangedType rangedType = RangedType.Burst;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private LayerMask playerAndGroundLayer;

    [Header("BURST MODE Settings")]
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstDelay = 0.12f;

    [Header("SHOTGUN MODE SETTINGS")]
    [SerializeField] private int shotgunPelletCount = 5;
    [SerializeField] private float shotgunSpreadAngle = 30f;

    private bool isShootingSequence = false;

    protected override void FSMGroundUpdate()
    {
        if (!isGrounded && !isShootingSequence)
        {
            currentGroundState = GroundState.Chase;
            return;
        }

        if (isShootingSequence)
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
                isShootingSequence = true;

                if (animator != null)
                {
                    animator.SetTrigger("Shoot");
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

    public void OnShootEvent()
    {
        if (playerTarget == null || firePoint == null || bulletPrefab == null) return;

        Vector2 aimDir = (playerTarget.position - firePoint.position).normalized;

        // play sfx via audio mixer
        if (enemyData != null && enemyData.attackSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(enemyData.attackSound, firePoint.position, 1.2f);
            else
                AudioSource.PlayClipAtPoint(enemyData.attackSound, firePoint.position);
        }

        if (rangedType == RangedType.Burst)
        {
            StartCoroutine(FireBurstRoutine(aimDir));
        }
        else if (rangedType == RangedType.Shotgun)
        {
            FireShotgunSpread(aimDir);
        }
    }

    public void OnShootEnd()
    {
        isShootingSequence = false;

        if (playerTarget != null && enemyData != null)
        {
            float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);
            if (distToPlayer > enemyData.attackRange)
            {
                currentGroundState = GroundState.Chase;
            }
        }
    }

    private IEnumerator FireBurstRoutine(Vector2 direction)
    {
        for (int i = 0; i < burstCount; i++)
        {
            SpawnBullet(direction);
            yield return new WaitForSeconds(burstDelay);
        }
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
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        if (bullet.TryGetComponent<EnemyProjectile>(out EnemyProjectile proj))
        {
            proj.damage = enemyData.attackDamage;
            proj.hitLayers = playerAndGroundLayer;
            proj.Launch(direction);
        }
    }
}