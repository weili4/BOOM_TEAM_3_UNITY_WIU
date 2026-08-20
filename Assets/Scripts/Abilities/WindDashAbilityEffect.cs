using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WindDashAbility", menuName = "Party/Effects/WindDashAbility")]
public class WindDashAbilityEffect : AbilityEffect
{
    [Header("dash movement settings")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.16f;

    [Header("dash combat settings")]
    [SerializeField] private int dashDamage = 25;
    [SerializeField] private float enemyKnockback = 7.0f;
    [SerializeField] private float hitRadius = 1.1f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("optional front vfx")]
    [SerializeField] private GameObject frontVFXPrefab;

    private Dictionary<GameObject, Coroutine> activeDashRoutines = new Dictionary<GameObject, Coroutine>();

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        PlayerController controller = user.GetComponent<PlayerController>();
        if (controller == null) return;

        // aim towards mouse cursor
        Vector2 direction = (mouseWorldPos - (Vector2)user.transform.position).normalized;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = new Vector2(Mathf.Sign(user.transform.localScale.x), 0f);
        }

        // 1. tell playercontroller to lock velocity in this direction with zero gravity
        controller.SetForcedVelocity(direction * dashSpeed, dashDuration, true);

        // 2. start ghost trail
        GhostTrail trail = user.GetComponent<GhostTrail>();
        if (trail == null) trail = user.AddComponent<GhostTrail>();
        trail.StartTrail(dashDuration);

        // 3. spawn front vfx facing travel angle
        if (frontVFXPrefab != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);
            Vector3 spawnPos = user.transform.position + (Vector3)(direction * 0.5f);
            GameObject vfx = Instantiate(frontVFXPrefab, spawnPos, rot, user.transform);
            Destroy(vfx, dashDuration + 0.1f);
        }

        if (controller.animator != null)
        {
            controller.animator.SetTrigger("IsAttacking");
        }

        // 4. run self-contained damage collision loop
        MonoBehaviour runner = user.GetComponent<MonoBehaviour>();
        if (runner != null)
        {
            if (activeDashRoutines.TryGetValue(user, out Coroutine oldRoutine) && oldRoutine != null)
            {
                runner.StopCoroutine(oldRoutine);
            }
            activeDashRoutines[user] = runner.StartCoroutine(DashDamageRoutine(user, direction, dashDuration));
        }
    }

    private IEnumerator DashDamageRoutine(GameObject user, Vector2 dashDir, float duration)
    {
        float elapsed = 0f;
        HashSet<Damageable> hitEnemies = new HashSet<Damageable>();

        while (elapsed < duration)
        {
            if (user == null) yield break;

            Collider2D[] hits = Physics2D.OverlapCircleAll(user.transform.position, hitRadius, enemyLayer);
            foreach (var col in hits)
            {
                if (col.CompareTag("Player") || col.CompareTag("Ally")) continue;

                if (col.TryGetComponent<Damageable>(out var enemy) && !hitEnemies.Contains(enemy))
                {
                    hitEnemies.Add(enemy);
                    enemy.TakeDamage(dashDamage, dashDir, enemyKnockback);
                }
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        activeDashRoutines.Remove(user);
    }

    public override void Deactivate(GameObject user)
    {
        if (user != null && activeDashRoutines.TryGetValue(user, out Coroutine routine))
        {
            MonoBehaviour runner = user.GetComponent<MonoBehaviour>();
            if (runner != null && routine != null) runner.StopCoroutine(routine);
            activeDashRoutines.Remove(user);
        }
    }
}