using UnityEngine;
using Unity.Cinemachine;

public abstract class HazardBase : MonoBehaviour
{
    [Header("hazard damage settings")]
    [SerializeField] protected int contactDamage = 20;
    [SerializeField] protected float knockbackForce = 7f;
    [SerializeField] protected float screenShakeForce = 0.8f;
    [SerializeField] protected AudioClip hitSound;

    protected virtual void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision.TryGetComponent<Damageable>(out var playerHealth))
        {
            Vector2 hitDir = ((Vector2)playerHealth.transform.position - (Vector2)transform.position).normalized;
            playerHealth.TakeDamage(contactDamage, hitDir, knockbackForce);

            if (hitSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(hitSound, transform.position);

            if (TryGetComponent<CinemachineImpulseSource>(out var impulse))
                impulse.GenerateImpulse(screenShakeForce);
        }
    }
}