using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;
            if (EndGameUIManager.Instance != null)
            {
                EndGameUIManager.Instance.TriggerWin();
            }
        }
    }

    public void GameWin()
    {
        if (EndGameUIManager.Instance != null)
        {
            EndGameUIManager.Instance.TriggerWin();
        }
    }
}