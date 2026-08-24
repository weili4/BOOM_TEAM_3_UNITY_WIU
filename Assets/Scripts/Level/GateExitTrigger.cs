using UnityEngine;

public class GateExitTrigger : MonoBehaviour
{
    [Header("gate and progression settings")]
    [SerializeField] private Gate targetGate;
    [SerializeField] private ChunkManager nextChunkManager;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ignore benched followers
        if (collision.CompareTag("Ally")) return;

        // verify that this is strictly the active leader
        bool isLeader = collision.CompareTag("Player");
        if (PartyManager.Instance != null && PartyManager.Instance.ActivePlayerObj != null)
        {
            isLeader = (collision.gameObject == PartyManager.Instance.ActivePlayerObj || collision.transform.root.gameObject == PartyManager.Instance.ActivePlayerObj);
        }

        if (!hasTriggered && isLeader)
        {
            hasTriggered = true;

            // 1. slam gate shut behind the player
            if (targetGate != null)
            {
                targetGate.SlamCloseGate();
            }

            // 2. teleport followers past the gate to the leader
            if (PartyManager.Instance != null)
            {
                PartyManager.Instance.TeleportFollowersToLeader();
            }

            // 3. activate next level chunk
            if (nextChunkManager != null)
            {
                nextChunkManager.ActivateChunk();
            }
        }
    }
}