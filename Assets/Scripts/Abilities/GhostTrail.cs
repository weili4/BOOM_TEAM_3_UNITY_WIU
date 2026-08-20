using System.Collections;
using UnityEngine;

public class GhostTrail : MonoBehaviour
{
    [Header("ghost settings")]
    [SerializeField] private float ghostSpawnInterval = 0.035f; // time between ghost copies
    [SerializeField] private float ghostLifetime = 0.25f;       // how fast ghost fades out
    [SerializeField] private Color ghostColor = new Color(0.2f, 0.8f, 1f, 0.6f); // cyan wind tint

    private SpriteRenderer playerSprite;
    private Coroutine trailRoutine;

    private void Awake()
    {
        playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite == null)
            playerSprite = GetComponentInChildren<SpriteRenderer>();
    }

    public void StartTrail(float duration)
    {
        if (trailRoutine != null) StopCoroutine(trailRoutine);
        trailRoutine = StartCoroutine(SpawnTrailRoutine(duration));
    }

    private IEnumerator SpawnTrailRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            SpawnGhost();
            elapsed += ghostSpawnInterval;
            yield return new WaitForSeconds(ghostSpawnInterval);
        }

        trailRoutine = null;
    }

    private void SpawnGhost()
    {
        if (playerSprite == null || playerSprite.sprite == null) return;

        GameObject ghostObj = new GameObject("GhostAfterimage");
        ghostObj.transform.position = transform.position;
        ghostObj.transform.localScale = transform.localScale;
        ghostObj.transform.rotation = transform.rotation;

        SpriteRenderer sr = ghostObj.AddComponent<SpriteRenderer>();
        sr.sprite = playerSprite.sprite;
        sr.color = ghostColor;
        sr.sortingOrder = playerSprite.sortingOrder - 1;

        StartCoroutine(FadeGhost(ghostObj, sr));
    }

    private IEnumerator FadeGhost(GameObject ghostObj, SpriteRenderer sr)
    {
        float elapsed = 0f;
        Color initial = ghostColor;

        while (elapsed < ghostLifetime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(initial.a, 0f, elapsed / ghostLifetime);
            sr.color = new Color(initial.r, initial.g, initial.b, alpha);
            yield return null;
        }

        Destroy(ghostObj);
    }
}