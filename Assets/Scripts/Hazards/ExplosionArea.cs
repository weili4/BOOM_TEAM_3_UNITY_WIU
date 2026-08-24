using UnityEngine;
using Unity.Cinemachine;

public class ExplosionArea : MonoBehaviour
{
    [SerializeField] private AudioClip explosionSFX;
    [SerializeField] private float shakeForce = 2.5f;
    [SerializeField] private float cleanupDelay = 2.0f;

    private void Start()
    {
        if (explosionSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(explosionSFX, transform.position, 1.4f);
        }

        if (TryGetComponent<CinemachineImpulseSource>(out var impulse))
        {
            impulse.GenerateImpulse(shakeForce);
        }

        // auto-destroy after particles finish
        Destroy(gameObject, cleanupDelay);
    }
}