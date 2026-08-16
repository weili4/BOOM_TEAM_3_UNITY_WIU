using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public int damage = 15;
    public float speed = 12f;
    public float lifetime = 4f;
    public LayerMask hitLayers;

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
        if (((1 << collision.gameObject.layer) & hitLayers) != 0 || collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<Damageable>(out Damageable playerHealth))
            {
                playerHealth.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}