using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("portal configuration")]
    [SerializeField] private string nextSceneName = "Scene2";
    [SerializeField] private bool showWinScreen = false;
    [SerializeField] private AudioClip portalSound;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;

            // PLAY SFX
            if (portalSound != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(portalSound, transform.position, 1.3f);
            }

            // IF SHOW WIN SCREEN IS CHECKED, DISPLAY WIN UI
            if (showWinScreen)
            {
                if (GameUIManager.Instance != null)
                {
                    GameUIManager.Instance.TriggerWinScreen();
                }
            }
            // DIRECTLY LOAD NEXT SCENE
            else if (!string.IsNullOrEmpty(nextSceneName))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}