using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    [Header("bullet stats")]
    public int damage = 15;
    public float speed = 18f;
    public float lifetime = 3.0f;
    public float knockbackForce = 4.5f;

    [Header("curving trajectory")]
    [SerializeField] private float initialStraightDelay = 0.06f; // time flying along gun angle before curving
    [SerializeField] private float curveRotateSpeed = 450f;     // how fast it turns towards cursor point
    public LayerMask hitLayers;

    [Header("impact feedback")]
    [SerializeField] private GameObject impactVFXPrefab;
    [SerializeField] private AudioClip impactSFX;

    private Rigidbody2D rb;
    private Vector2 targetCursorPos;
    private Vector2 currentFlyDirection;
    private float delayTimer = 0f;
    private bool hasReachedTargetArea = false;

    public LayerMask HitLayers
    {
        get => hitLayers;
        set => hitLayers = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    // launches along gun angle then curves to the clicked mouse point
    public void LaunchCurved(Vector2 initialGunDirection, Vector2 clickedMousePosition)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        targetCursorPos = clickedMousePosition;
        currentFlyDirection = initialGunDirection.normalized;
        delayTimer = initialStraightDelay;

        rb.linearVelocity = currentFlyDirection * speed;

        float angle = Mathf.Atan2(currentFlyDirection.y, currentFlyDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void Launch(Vector2 direction)
    {
        LaunchCurved(direction, (Vector2)transform.position + direction * 10f);
    }

    private void Update()
    {
        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
            return;
        }

        // after initial delay smoothly curve towards the point clicked
        if (!hasReachedTargetArea)
        {
            Vector2 toTarget = (targetCursorPos - (Vector2)transform.position);

            if (toTarget.sqrMagnitude < 0.25f)
            {
                hasReachedTargetArea = true; // continue flying straight past target
            }
            else
            {
                Vector2 desiredDir = toTarget.normalized;
                currentFlyDirection = Vector3.RotateTowards(
                    currentFlyDirection,
                    desiredDir,
                    curveRotateSpeed * Mathf.Deg2Rad * Time.deltaTime,
                    0f
                );

                rb.linearVelocity = currentFlyDirection * speed;

                float angle = Mathf.Atan2(currentFlyDirection.y, currentFlyDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("Ally")) return;

        if (collision.TryGetComponent<Damageable>(out var enemyHealth))
        {
            enemyHealth.TakeDamage(damage, transform.position, knockbackForce);
            SpawnImpactFeedback();
            Destroy(gameObject);
            return;
        }

        if (((1 << collision.gameObject.layer) & hitLayers) != 0)
        {
            SpawnImpactFeedback();
            Destroy(gameObject);
        }
    }

    private void SpawnImpactFeedback()
    {
        if (impactSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(impactSFX, transform.position, 0.7f);

        if (impactVFXPrefab != null)
            Instantiate(impactVFXPrefab, transform.position, Quaternion.identity);
    }
}