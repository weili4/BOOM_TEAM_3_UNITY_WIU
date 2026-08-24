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

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (blackFadeOverlay != null)
            {
                Color c = blackFadeOverlay.color;
                c.a = 0f;
                blackFadeOverlay.color = c;
                blackFadeOverlay.raycastTarget = false;
            }
        }
        else
        {
            Destroy(gameObject);
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

        // 1. pause gameplay and enable fade overlay
        Time.timeScale = 0f;
        if (blackFadeOverlay != null) blackFadeOverlay.raycastTarget = true;

        // 2. fade to black
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            if (blackFadeOverlay != null)
            {
                Color c = Color.black;
                c.a = t;
                blackFadeOverlay.color = c;
            }
            yield return null;
        }

        // 3. wait 0.1s in black
        yield return new WaitForSecondsRealtime(0.1f);

        // 4. load scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // wait 1 frame for scene objects to initialize
        yield return null;

        // 5. fade out from black
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            if (blackFadeOverlay != null)
            {
                Color c = Color.black;
                c.a = 1f - t;
                blackFadeOverlay.color = c;
            }
            yield return null;
        }

        if (blackFadeOverlay != null)
        {
            Color c = Color.black;
            c.a = 0f;
            blackFadeOverlay.color = c;
            blackFadeOverlay.raycastTarget = false;
        }

        // 6. unpause game in new scene
        Time.timeScale = 1f;
        isTransitioning = false;
    }
}