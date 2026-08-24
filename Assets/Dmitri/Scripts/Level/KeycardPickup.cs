using UnityEngine;

public class KeycardPickup : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private GameObject pickupParticlePrefab;
    [SerializeField] private AudioClip pickupSFX;

    [Header("Chunk Reference")]
    [SerializeField] private ChunkManager currentChunkManager;

    private bool isPickedUp = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Guard against multiple collisions in the same frame
        if (isPickedUp) return;

        if (collision.CompareTag("Player"))
        {
            isPickedUp = true;

            // 1. Spawn Particle Effect (if assigned)
            if (pickupParticlePrefab != null)
            {
                GameObject particleInstance = Instantiate(
                    pickupParticlePrefab,
                    transform.position,
                    Quaternion.identity
                );

                // Auto-destroy the particle object after its duration (default 2 seconds fallback)
                if (particleInstance.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
                {
                    Destroy(particleInstance, ps.main.duration + ps.main.startLifetime.constantMax);
                }
                else
                {
                    Destroy(particleInstance, 2.0f);
                }
            }

            // 2. Play Audio (if assigned)
            if (pickupSFX != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(pickupSFX, transform.position, 1.0f);
            }

            // 3. Update Objective UI
            if (currentChunkManager != null)
            {
                currentChunkManager.Invoke(nameof(ChunkManager.UpdateChunkObjectiveUI), 0.05f);
            }
            else
            {
                LevelObjectiveUI.Instance?.SetObjectiveText("Proceed through the Unlocked Gate!");
            }

            // 4. Destroy Keycard GameObject
            Destroy(gameObject);
        }
    }
}