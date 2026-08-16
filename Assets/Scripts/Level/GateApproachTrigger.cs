using UnityEngine;

public class GateApproachTrigger : MonoBehaviour
{
    [Header("approach settings")]
    [SerializeField] private Gate targetGate;
    [SerializeField] private ChunkManager currentChunkManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && targetGate != null)
        {
            // check if current chunk objective is fulfilled
            if (currentChunkManager == null || currentChunkManager.IsChunkCleared)
            {
                targetGate.OpenGate();
                LevelObjectiveUI.Instance?.SetObjectiveText("Proceed through the Unlocked Gate!");
            }
            else
            {
                LevelObjectiveUI.Instance?.SetObjectiveText("Gate Locked! Defeat all remaining enemies first.");
            }
        }
    }
}