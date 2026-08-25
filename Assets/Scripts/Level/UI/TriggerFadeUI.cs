using System.Collections;
using UnityEngine;

public class TriggerFadeUI : MonoBehaviour
{
    [SerializeField] private GameObject prompt;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.15f;

    private LootChestChanceDrop chest;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        chest = GetComponentInParent<LootChestChanceDrop>();

        canvasGroup.alpha = 0f;
        prompt.SetActive(false);
    }

    private void Update()
    {
        if (chest != null && chest.IsChestOpen)
        {
            HidePrompt();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !chest.IsChestOpen)
        {
            prompt.SetActive(true);
            FadeTo(1f);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HidePrompt();
        }
    }

    private void HidePrompt()
    {
        if (!prompt.activeSelf)
            return;

        FadeTo(0f);
    }

    private void FadeTo(float targetAlpha)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(Fade(targetAlpha));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float t = time / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha == 0f)
            prompt.SetActive(false);
    }
}