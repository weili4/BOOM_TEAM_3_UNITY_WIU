using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("full screen black fade image")]
    [SerializeField] private Image blackFadeOverlay;
    [SerializeField] private float fadeDuration = 0.35f;

    private Canvas transitionCanvas;
    private bool isTransitioning = false;

    private void Awake()
    {
        // if an instance already exists from previous scene destroy this duplicate immediately
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        transitionCanvas = GetComponent<Canvas>();
        if (transitionCanvas == null) transitionCanvas = gameObject.AddComponent<Canvas>();

        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 999; // draw over all scenes

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (blackFadeOverlay != null)
        {
            Color c = Color.black;
            c.a = 0f;
            blackFadeOverlay.color = c;
            blackFadeOverlay.raycastTarget = false;
        }
    }

    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning || string.IsNullOrEmpty(sceneName)) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;

        // 1. freeze time and enable raycast blocker
        Time.timeScale = 0f;
        if (blackFadeOverlay != null) blackFadeOverlay.raycastTarget = true;

        // 2. fade to black (alpha 0 to 1)
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            // clamp delta time so frame drops do not skip the fade
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (blackFadeOverlay != null)
            {
                Color c = Color.black;
                c.a = t;
                blackFadeOverlay.color = c;
            }
            yield return null;
        }

        // ensure solid black
        if (blackFadeOverlay != null)
        {
            Color c = Color.black;
            c.a = 1f;
            blackFadeOverlay.color = c;
        }

        // 3. wait 0.1s in solid black
        yield return new WaitForSecondsRealtime(0.1f);

        // 4. load scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // wait 2 frames in the new scene for delta time spike to settle
        yield return null;
        yield return new WaitForEndOfFrame();

        // 5. unpause game time in new scene
        Time.timeScale = 1f;

        // 6. smooth fade out from black (alpha 1 to 0)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            // clamp delta time to guarantee a smooth 60fps fade out
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (blackFadeOverlay != null)
            {
                Color c = Color.black;
                c.a = 1f - t;
                blackFadeOverlay.color = c;
            }
            yield return null;
        }

        // reset overlay to completely transparent
        if (blackFadeOverlay != null)
        {
            Color c = Color.black;
            c.a = 0f;
            blackFadeOverlay.color = c;
            blackFadeOverlay.raycastTarget = false;
        }

        isTransitioning = false;
    }
}