using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class HookProjectile : MonoBehaviour
{
    private GameObject owner;
    private Rigidbody2D playerRb;
    private LineRenderer lineRenderer;
    private Rigidbody2D hookRb;

    private float pullSpeed;
    private float pullDelay;
    private float stopDistance;
    private float maxDistance;
    private LayerMask groundLayer;
    private AudioClip attachSFX;
    private float soundVolume;

    private bool isAttached = false;
    private bool isPulling = false;
    private Vector2 hitPoint;

    private void Awake()
    {
        hookRb = GetComponent<Rigidbody2D>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void Initialize(
        GameObject user,
        float pullSpd,
        float delay,
        float stopDist,
        float maxDist,
        LayerMask layer,
        AudioClip sfx,
        float vol)
    {
        owner = user;
        pullSpeed = pullSpd;
        pullDelay = delay;
        stopDistance = stopDist;
        maxDistance = maxDist;
        groundLayer = layer;
        attachSFX = sfx;
        soundVolume = vol;

        if (owner != null)
        {
            playerRb = owner.GetComponent<Rigidbody2D>();
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
    }

    private void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        DrawRopeLine();

        // Range check while flying
        if (!isAttached)
        {
            if (Vector2.Distance(owner.transform.position, transform.position) >= maxDistance)
            {
                Destroy(gameObject);
            }
        }
    }

    private void FixedUpdate()
    {
        // Only pull after attachment AND the pull delay timer finishes
        if (isAttached && isPulling && playerRb != null && owner != null)
        {
            Vector2 pullDir = (hitPoint - (Vector2)owner.transform.position).normalized;
            playerRb.linearVelocity = pullDir * pullSpeed;

            if (Vector2.Distance(owner.transform.position, hitPoint) <= stopDistance)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleImpact(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.gameObject);
    }

    private void HandleImpact(GameObject hitObject)
    {
        if (isAttached) return;

        bool isGround = ((1 << hitObject.layer) & groundLayer) != 0;

        if (!isGround && hitObject.transform.parent != null)
        {
            isGround = ((1 << hitObject.transform.parent.gameObject.layer) & groundLayer) != 0;
        }

        if (isGround)
        {
            isAttached = true;
            hitPoint = transform.position;

            // Lock hook position in place instantly
            if (hookRb != null)
            {
                hookRb.linearVelocity = Vector2.zero;
                hookRb.bodyType = RigidbodyType2D.Kinematic;
            }

            // Play impact sound
            if (attachSFX != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(attachSFX, hitPoint, soundVolume);
            }

            // Start delay timer before pulling
            if (pullDelay > 0f)
            {
                StartCoroutine(PullDelayRoutine());
            }
            else
            {
                StartPulling();
            }
        }
    }

    private IEnumerator PullDelayRoutine()
    {
        yield return new WaitForSeconds(pullDelay);
        StartPulling();
    }

    private void StartPulling()
    {
        isPulling = true;

        // Apply initial impulse velocity
        if (playerRb != null && owner != null)
        {
            Vector2 pullDir = (hitPoint - (Vector2)owner.transform.position).normalized;
            playerRb.linearVelocity = pullDir * pullSpeed;
        }
    }

    private void DrawRopeLine()
    {
        if (lineRenderer != null && owner != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, owner.transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }
    }
}