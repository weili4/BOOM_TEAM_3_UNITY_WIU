using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    public int Damage = 15;
    public float Speed = 12f;
    public float Lifetime = 4f;
    public LayerMask HitLayers;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, Lifetime);
    }

    public void Launch(Vector2 direction)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * Speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & HitLayers) != 0 || collision.CompareTag("Enemy"))
        {
            if (collision.TryGetComponent<Damageable>(out Damageable playerHealth))
            {
                playerHealth.TakeDamage(Damage);
            }
            Destroy(gameObject);
        }
    }
}
