using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFlashUI : MonoBehaviour
{
    public static ScreenFlashUI Instance { get; private set; }

    [SerializeField] private Image flashImage;
    [SerializeField] private float maxAlpha = 0.35f;
    [SerializeField] private float flashDuration = 0.18f;

    private Coroutine flashRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (flashImage == null)
            flashImage = GetComponent<Image>();

        if (flashImage != null)
        {
            Color c = flashImage.color;
            c.a = 0f;
            flashImage.color = c;
        }
    }

    public void TriggerRedFlash()
    {
        if (flashImage == null) return;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(DoFlashRoutine());
    }

    private IEnumerator DoFlashRoutine()
    {
        float elapsed = 0f;
        Color c = flashImage.color;

        // quickly fade in and fade out
        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flashDuration;

            // sine curve peaks at half duration then fades out
            c.a = Mathf.Sin(t * Mathf.PI) * maxAlpha;
            flashImage.color = c;

            yield return null;
        }

        c.a = 0f;
        flashImage.color = c;
        flashRoutine = null;
    }
}