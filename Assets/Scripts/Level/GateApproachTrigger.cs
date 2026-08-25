using UnityEngine;

public class GateApproachTrigger : MonoBehaviour
{
    [Header("approach settings")]
    [SerializeField] private Gate targetGate;
    [SerializeField] private ChunkManager currentChunkManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ignore benched followers
        if (collision.CompareTag("Ally")) return;

        // make sure only the active leader triggers gate checks
        bool isLeader = collision.CompareTag("Player");
        if (PartyManager.Instance != null && PartyManager.Instance.ActivePlayerObj != null)
        {
            isLeader = (collision.gameObject == PartyManager.Instance.ActivePlayerObj || collision.transform.root.gameObject == PartyManager.Instance.ActivePlayerObj);
        }

        if (!isLeader || targetGate == null) return;

        // check if current chunk objective is fulfilled
        if (currentChunkManager == null || currentChunkManager.IsChunkCleared)
        {
            // inform chunk manager that player made it, stopping active timers
            currentChunkManager?.CompleteChunk();

            targetGate.OpenGate();
            LevelObjectiveUI.Instance?.SetObjectiveText("Proceed through the Unlocked Gate!");
        }
        else
        {
            // display unique locked message based on active objective type
            switch (currentChunkManager.CurrentObjectiveType)
            {
                case ChunkManager.ObjectiveType.KillAllEnemies:
                    LevelObjectiveUI.Instance?.SetObjectiveText("Gate Locked! Defeat all remaining enemies first.");
                    break;

                case ChunkManager.ObjectiveType.WaveCombat:
                    LevelObjectiveUI.Instance?.SetObjectiveText("Gate Locked! Clear all waves to unlock.");
                    break;

                case ChunkManager.ObjectiveType.Keycard:
                    LevelObjectiveUI.Instance?.SetObjectiveText("Gate Locked! You need a keycard to open this gate.");
                    break;

                case ChunkManager.ObjectiveType.Timer:
                    LevelObjectiveUI.Instance?.SetObjectiveText("Gate Locked! Time ran out before you reached the gate.");
                    break;

                case ChunkManager.ObjectiveType.ReachGate:
                    LevelObjectiveUI.Instance?.SetObjectiveText("Proceed through the Unlocked Gate!");
                    break;

                default:
                    LevelObjectiveUI.Instance?.SetObjectiveText("Gate Locked!");
                    break;
            }
        }
    }
}