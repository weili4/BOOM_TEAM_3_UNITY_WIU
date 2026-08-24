using System.Collections;
using UnityEngine;

public class TurretHazard : MonoBehaviour
{
    [Header("targeting settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float aimingDuration = 1.0f;
    [SerializeField] private float attackCooldown = 2.0f;
    [SerializeField] private float spreadAngle = 20f;
    [SerializeField] private float fullScreenLineDistance = 40f; // lines extend across full screen

    [Header("aiming line renderers (size 3)")]
    [SerializeField] private LineRenderer[] aimLines = new LineRenderer[3];
    [SerializeField] private LayerMask obstacleLayer;

    [Header("projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 11f;
    [SerializeField] private int projectileDamage = 15;
    [SerializeField] private AudioClip shootSFX;

    private Transform playerTarget;
    private float cooldownTimer = 0f;
    private bool isAiming = false;

    private void Start()
    {
        foreach (var line in aimLines)
        {
            if (line != null) line.enabled = false;
        }
    }

    private void Update()
    {
        UpdatePlayerTarget();

        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
        if (playerTarget == null) return;

        float dist = Vector2.Distance(transform.position, playerTarget.position);

        if (dist <= detectionRange && cooldownTimer <= 0f && !isAiming)
        {
            StartCoroutine(AimAndShootRoutine());
        }
    }

    private void UpdatePlayerTarget()
    {
        if (PartyManager.Instance != null && PartyManager.Instance.ActivePlayerObj != null)
        {
            playerTarget = PartyManager.Instance.ActivePlayerObj.transform;
        }
        else
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }
    }

    private IEnumerator AimAndShootRoutine()
    {
        isAiming = true;

        foreach (var line in aimLines)
        {
            if (line != null) line.enabled = true;
        }

        float elapsed = 0f;

        while (elapsed < aimingDuration)
        {
            elapsed += Time.deltaTime;
            if (playerTarget == null) break;

            Vector3 startPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 baseDir = ((Vector2)playerTarget.position - (Vector2)startPos).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

            float[] angles = new float[] { baseAngle - spreadAngle, baseAngle, baseAngle + spreadAngle };

            for (int i = 0; i < aimLines.Length; i++)
            {
                if (aimLines[i] == null) continue;

                float rad = angles[i] * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                Vector2 endPos = (Vector2)startPos + dir * fullScreenLineDistance;

                // check wall collision or extend to full screen edge
                RaycastHit2D hit = Physics2D.Raycast(startPos, dir, fullScreenLineDistance, obstacleLayer);
                if (hit.collider != null) endPos = hit.point;

                aimLines[i].SetPosition(0, startPos);
                aimLines[i].SetPosition(1, endPos);
            }

            yield return null;
        }

        // fire 3 projectiles
        if (playerTarget != null && projectilePrefab != null)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            Vector2 centerDir = ((Vector2)playerTarget.position - (Vector2)spawnPos).normalized;
            float baseAngle = Mathf.Atan2(centerDir.y, centerDir.x) * Mathf.Rad2Deg;
            float[] finalAngles = new float[] { baseAngle - spreadAngle, baseAngle, baseAngle + spreadAngle };

            if (shootSFX != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(shootSFX, spawnPos);

            for (int i = 0; i < 3; i++)
            {
                float rad = finalAngles[i] * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
                if (projObj.TryGetComponent<EnemyProjectile>(out var proj))
                {
                    proj.damage = projectileDamage;
                    proj.speed = projectileSpeed;
                    proj.Launch(dir);
                }
            }
        }

        foreach (var line in aimLines)
        {
            if (line != null) line.enabled = false;
        }

        cooldownTimer = attackCooldown;
        isAiming = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}