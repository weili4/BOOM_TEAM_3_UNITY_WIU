using UnityEngine;

public class HazardPit : MonoBehaviour
{
    [SerializeField] private int pitfallDamage = 20;
    [SerializeField] private AudioClip pitfallSound; // pitfall sfx

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<Damageable>(out Damageable playerHealth))
            {
                // PLAY PITFALL SFX
                if (pitfallSound != null)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(pitfallSound, transform.position, 1.2f);
                    else
                        AudioSource.PlayClipAtPoint(pitfallSound, transform.position);
                }

                playerHealth.TakeDamage(pitfallDamage);

                if (playerHealth.CurrentHealth > 0 && ChunkManager.CurrentSpawnPoint != null)
                {
                    collision.transform.position = ChunkManager.CurrentSpawnPoint.position;

                    if (collision.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
                    {
                        rb.linearVelocity = Vector2.zero;
                    }
                }
            }
        }
        else if (collision.TryGetComponent<Damageable>(out Damageable enemyHealth))
        {
            if (pitfallSound != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(pitfallSound, transform.position, 1.2f);
            }

            enemyHealth.TakeDamage(9999);
        }
    }
}