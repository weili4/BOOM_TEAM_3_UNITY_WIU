using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Scene2";
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene(targetSceneName);
            }
        }
    }
}