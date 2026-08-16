using UnityEngine;

public class HomingShrapnel : MonoBehaviour
{
    [Header("damage n lifetime")]
    public int damage = 25;
    public float lifetime = 3f;

    [Header("homing settings")]
    public float detectionRadius = 6f;
    public float homingSpeed = 10f;
    public float rotateSpeed = 400f;
    public LayerMask enemyLayer;

    private Rigidbody2D rb;
    private Transform targetEnemy;
    private Vector2 currentDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector2 direction, float speed)
    {
        currentDirection = direction;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = currentDirection * speed;
    }

    private void Update()
    {
        // scan for nearest enemy if not targeting one yet
        if (targetEnemy == null)
        {
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyLayer);
            float closestDist = Mathf.Infinity;

            foreach (var col in enemies)
            {
                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    targetEnemy = col.transform;
                }
            }
        }

        // if an enemy is in range, home
        if (targetEnemy != null)
        {
            Vector2 targetDirection = ((Vector2)targetEnemy.position - rb.position).normalized;
            currentDirection = Vector3.RotateTowards(currentDirection, targetDirection, rotateSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
            rb.linearVelocity = currentDirection * homingSpeed;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & enemyLayer) != 0 || collision.CompareTag("Enemy"))
        {
            if (collision.TryGetComponent<Damageable>(out Damageable enemy))
            {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}