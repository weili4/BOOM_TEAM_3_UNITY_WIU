using System.Collections;
using UnityEngine;

public class TimerStartTrigger : MonoBehaviour
{
    [Header("Chunk Target")]
    [SerializeField] private ChunkManager targetChunkManager;

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer triggerSprite;
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip triggerSFX;
    [SerializeField][Range(0f, 1f)] private float soundVolume = 1.0f;

    private Coroutine fadeCoroutine;
    private Color originalColor;

    private void Awake()
    {
        if (triggerSprite != null)
        {
            originalColor = triggerSprite.color;
            triggerSprite.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && targetChunkManager != null)
        {
            // Enable and Fade Sprite
            if (triggerSprite != null)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }

                fadeCoroutine = StartCoroutine(EnableAndFadeSpriteRoutine());
            }

            // Play Audio
            if (triggerSFX != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(triggerSFX, transform.position, soundVolume);
            }

            // Start or restart the timer in ChunkManager
            targetChunkManager.StartTimerObjective();
        }
    }

    private IEnumerator EnableAndFadeSpriteRoutine()
    {
        // Reset color and enable sprite
        triggerSprite.color = originalColor;
        triggerSprite.enabled = true;

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(originalColor.a, 0f, elapsedTime / fadeDuration);

            // Update alpha on the sprite renderer
            Color currentColor = triggerSprite.color;
            currentColor.a = alpha;
            triggerSprite.color = currentColor;

            yield return null;
        }

        // Disable sprite after fading out completely
        triggerSprite.enabled = false;
        triggerSprite.color = originalColor; // Restore original alpha for next trigger
    }
}