using UnityEngine;
using System.Collections;

public class ShieldDome : MonoBehaviour
{
    [SerializeField] private float duration = 10f;
    [SerializeField] private float pulseInterval = 2f;
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private GameObject ringPulsePrefab;

    private void Start()
    {
        // Initial dome size-up
        StartCoroutine(SizeUp());

        // First activation pulse
        if (ringPulsePrefab != null)
            StartCoroutine(RingPulse());

        // Schedule refresh pulses
        StartCoroutine(PulseRoutine());

        Destroy(gameObject, duration);
    }

    private IEnumerator SizeUp()
    {
        float growTime = 0.5f;
        float elapsed = 0f;
        Vector3 targetScale = transform.localScale;
        transform.localScale = Vector3.zero;

        while (elapsed < growTime)
        {
            float t = elapsed / growTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(pulseInterval);

            if (ringPulsePrefab != null)
                StartCoroutine(RingPulse());
        }
    }

    private IEnumerator RingPulse()
    {
        // Spawn a ring instance as child
        GameObject ring = Instantiate(ringPulsePrefab, transform.position, Quaternion.identity, transform);

        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * 1.0f; // expand outward

        while (elapsed < pulseDuration)
        {
            float t = elapsed / pulseDuration;
            ring.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Fade out or destroy after pulse
        Destroy(ring);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("EnemyProjectile"))
        {
            Destroy(other.gameObject); // block bullets
        }
    }
}
