using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public int damage = 15;
    public float speed = 12f;
    public float lifetime = 4f;
    public LayerMask hitLayers;
    public float knockbackForce = 6.0f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ignore benched followers tagged Ally
        if (collision.CompareTag("Ally")) return;

        // hit the active leader
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<Damageable>(out Damageable playerHealth))
            {
                // knock player away in the direction the bullet was flying
                Vector2 bulletDirection = rb != null ? rb.linearVelocity.normalized : transform.right;
                playerHealth.TakeDamage(damage, bulletDirection, knockbackForce);
            }
            Destroy(gameObject);
            return;
        }

        // hit ground or walls
        if (((1 << collision.gameObject.layer) & hitLayers) != 0)
        {
            Destroy(gameObject);
        }
    }
}