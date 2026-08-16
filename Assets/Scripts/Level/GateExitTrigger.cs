using UnityEngine;

public class GateExitTrigger : MonoBehaviour
{
    [Header("Gate and Progression settings")]
    [SerializeField] private Gate targetGate;
    [SerializeField] private ChunkManager nextChunkManager;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;

            // slam gate shut behind player with camera shake
            if (targetGate != null)
            {
                targetGate.SlamCloseGate();
            }

            // activate next chunk
            if (nextChunkManager != null)
            {
                nextChunkManager.ActivateChunk();
            }
        }
    }
}