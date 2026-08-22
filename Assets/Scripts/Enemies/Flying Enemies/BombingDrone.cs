using System.Collections;
using UnityEngine;

public class BombingDrone : EnemyBase
{
    [Header("detection and flight")]
    [SerializeField] private float detectionRange = 9.0f;       // only approaches player if within range
    [SerializeField] private float hoverHeightY = 4.2f;
    [SerializeField] private float horizontalTrackSpeed = 3.2f;
    [SerializeField] private float bombDropCooldown = 2.5f;

    [Header("downward warning line renderer")]
    [SerializeField] private LineRenderer warningLine;          // shoots straight down to ground
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float warningDuration = 0.6f;
    [SerializeField] private float linePostDropLinger = 0.15f;  // stays visible for 0.15s after drop

    [Header("bomb prefab")]
    [SerializeField] private GameObject gravityBombPrefab;
    [SerializeField] private Transform dropPoint;

    private Vector2 spawnPosition;
    private float cooldownTimer = 0f;
    private bool isBombingSequence = false;

    protected override void Start()
    {
        base.Start();
        spawnPosition = transform.position;
        if (warningLine != null) warningLine.enabled = false;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead || isStunned || playerTarget == null) return;

        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;

        float distToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // only aggro and fly to player if within detection range
        if (distToPlayer <= detectionRange)
        {
            if (!isBombingSequence)
            {
                FlyAbovePlayer();

                float xDiff = Mathf.Abs(transform.position.x - playerTarget.position.x);
                if (xDiff < 0.7f && cooldownTimer <= 0f)
                {
                    StartCoroutine(DropBombRoutine());
                }
            }
        }
        else if (!isBombingSequence)
        {
            // return to spawn hover position
            transform.position = Vector3.MoveTowards(transform.position, spawnPosition, horizontalTrackSpeed * 0.5f * Time.deltaTime);
        }
    }

    private void FlyAbovePlayer()
    {
        Vector2 targetPosition = new Vector2(playerTarget.position.x, playerTarget.position.y + hoverHeightY);

        float newX = Mathf.MoveTowards(transform.position.x, targetPosition.x, horizontalTrackSpeed * Time.deltaTime);
        float newY = Mathf.MoveTowards(transform.position.y, targetPosition.y, horizontalTrackSpeed * 1.5f * Time.deltaTime);

        transform.position = new Vector3(newX, newY, 0f);
        FlipTowards(playerTarget.position);
    }

    private IEnumerator DropBombRoutine()
    {
        isBombingSequence = true;

        Vector2 startDropPos = dropPoint != null ? (Vector2)dropPoint.position : (Vector2)transform.position;

        // raycast straight down to find ground endpoint
        Vector2 groundPoint = startDropPos + Vector2.down * 15f;
        RaycastHit2D hit = Physics2D.Raycast(startDropPos, Vector2.down, 15f, groundLayer);
        if (hit.collider != null) groundPoint = hit.point;

        // 1. show downward warning line while drone stops moving
        if (warningLine != null)
        {
            warningLine.enabled = true;
            warningLine.SetPosition(0, startDropPos);
            warningLine.SetPosition(1, groundPoint);
        }

        yield return new WaitForSeconds(warningDuration);

        // 2. drop bomb directly from dropPoint
        if (gravityBombPrefab != null)
        {
            Instantiate(gravityBombPrefab, startDropPos, Quaternion.identity);
        }

        // 3. keep line for 0.15s after drop
        yield return new WaitForSeconds(linePostDropLinger);

        if (warningLine != null)
            warningLine.enabled = false;

        cooldownTimer = bombDropCooldown;
        isBombingSequence = false;
    }

    protected override void InterruptActiveAttack()
    {
        if (warningLine != null)
            warningLine.enabled = false;

        isBombingSequence = false;
    }
}