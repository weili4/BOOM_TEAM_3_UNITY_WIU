using System.Collections;
using UnityEngine;

public class LaserGateHazard : HazardBase
{
    [Header("laser gate endpoints")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float detectionRange = 12f;

    [Header("same visual width for both states")]
    [SerializeField] private float laserWidth = 0.35f;
    [SerializeField] private float hitboxExtraThickness = 0.25f; // makes hitbox reliable so it never misses

    [Header("laser colors")]
    [SerializeField] private Color safeColor = new Color(0f, 0.7f, 1f, 0.75f);    // blue safe
    [SerializeField] private Color damagingColor = new Color(1f, 0.1f, 0.1f, 1f); // red hazard

    [Header("state durations")]
    [SerializeField] private float safeDuration = 2.0f;     // blue safe time
    [SerializeField] private float damagingDuration = 2.0f; // red damaging time

    private bool isCycleRunning = false;
    private bool isDamagingState = false;

    private void Start()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.startWidth = laserWidth;
            lineRenderer.endWidth = laserWidth;
        }
    }

    private void Update()
    {
        Transform player = GetActivePlayer();
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectionRange && !isCycleRunning)
        {
            StartCoroutine(LaserAlternateCycle());
        }

        // check damage continuously while in red state
        if (isDamagingState && startPoint != null && endPoint != null)
        {
            CheckLaserHitbox();
        }
    }

    private void CheckLaserHitbox()
    {
        Vector2 start = startPoint.position;
        Vector2 end = endPoint.position;
        Vector2 dir = (end - start).normalized;
        float beamLength = Vector2.Distance(start, end);
        Vector2 center = (start + end) * 0.5f;

        float hitThickness = laserWidth + hitboxExtraThickness;

        // boxcast along full length of the laser
        RaycastHit2D[] hits = Physics2D.BoxCastAll(center, new Vector2(hitThickness, beamLength), Vector2.SignedAngle(Vector2.up, dir), Vector2.zero);

        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Player") && hit.collider.TryGetComponent<Damageable>(out var pHealth))
            {
                Vector2 knockback = ((Vector2)pHealth.transform.position - (Vector2)transform.position).normalized;
                pHealth.TakeDamage(contactDamage, knockback, knockbackForce);
            }
        }
    }

    private IEnumerator LaserAlternateCycle()
    {
        isCycleRunning = true;

        Vector3 p0 = startPoint != null ? startPoint.position : transform.position;
        Vector3 p1 = endPoint != null ? endPoint.position : transform.position + Vector3.right * 5f;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, p0);
            lineRenderer.SetPosition(1, p1);
            lineRenderer.startWidth = laserWidth;
            lineRenderer.endWidth = laserWidth;
        }

        while (true)
        {
            Transform player = GetActivePlayer();
            if (player == null || Vector2.Distance(transform.position, player.position) > detectionRange)
            {
                break;
            }

            // 1. safe blue state
            isDamagingState = false;
            if (lineRenderer != null)
            {
                lineRenderer.startColor = safeColor;
                lineRenderer.endColor = safeColor;
            }

            yield return new WaitForSeconds(safeDuration);

            // 2. damaging red state
            isDamagingState = true;
            if (lineRenderer != null)
            {
                lineRenderer.startColor = damagingColor;
                lineRenderer.endColor = damagingColor;
            }

            yield return new WaitForSeconds(damagingDuration);
        }

        if (lineRenderer != null) lineRenderer.enabled = false;
        isDamagingState = false;
        isCycleRunning = false;
    }

    private Transform GetActivePlayer()
    {
        if (PartyManager.Instance != null && PartyManager.Instance.ActivePlayerObj != null)
            return PartyManager.Instance.ActivePlayerObj.transform;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        return p != null ? p.transform : null;
    }
}