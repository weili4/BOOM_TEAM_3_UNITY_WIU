using UnityEngine;

public class GravityBomb : MonoBehaviour
{
    [Header("bomb damage and radius")]
    [SerializeField] private int damage = 35;
    [SerializeField] private float explosionRadius = 2.4f;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private LayerMask playerLayer;

    [Header("audio and vfx")]
    [SerializeField] private AudioClip explodeSFX;
    [SerializeField] private GameObject explosionVFX;

    private bool hasExploded = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasExploded) return;
        if (collision.CompareTag("Ally")) return;

        // explode on contact with player or floor
        if (collision.CompareTag("Player") || ((1 << collision.gameObject.layer) & hitLayers) != 0)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;

        if (explodeSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(explodeSFX, transform.position, 1.3f);

        if (explosionVFX != null)
            Instantiate(explosionVFX, transform.position, Quaternion.identity);

        // check player damage in aoe radius
        Collider2D hit = Physics2D.OverlapCircle(transform.position, explosionRadius, playerLayer);
        if (hit != null && hit.CompareTag("Player") && hit.TryGetComponent<Damageable>(out var playerHealth))
        {
            Vector2 knockback = ((Vector2)playerHealth.transform.position - (Vector2)transform.position).normalized;
            playerHealth.TakeDamage(damage, knockback, knockbackForce: 8f);
        }

        Destroy(gameObject);
    }
}