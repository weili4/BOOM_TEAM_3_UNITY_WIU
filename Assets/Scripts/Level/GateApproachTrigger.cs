using UnityEngine;

public class GateApproachTrigger : MonoBehaviour
{
    [Header("Approach Settings")]
    [SerializeField] private Gate targetGate;
    [SerializeField] private ChunkManager currentChunkManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && targetGate != null)
        {
            // Check if current chunk objective is fulfilled
            if (currentChunkManager == null || currentChunkManager.IsChunkCleared)
            {
                // Inform chunk manager that player made it, stopping active timers
                currentChunkManager?.CompleteChunk();

                targetGate.OpenGate();
                LevelObjectiveUI.Instance?.SetObjectiveText("Proceed through the Unlocked Gate!");
            }
            else
            {
                // Display unique locked message based on active ObjectiveType
                switch (currentChunkManager.CurrentObjectiveType)
                {
                    case ChunkManager.ObjectiveType.KillAllEnemies:
                        LevelObjectiveUI.Instance?.SetObjectiveText("Gate Locked! Defeat all remaining enemies first.");
                        break;

                    case ChunkManager.ObjectiveType.WaveCombat:
                        LevelObjectiveUI.Instance?.SetObjectiveText("Gate Locked! Clear all waves to unlock.");
                        break;

                    case ChunkManager.ObjectiveType.Keycard: // keycard msg
                        LevelObjectiveUI.Instance?.SetObjectiveText("Gate Locked! You need a keycard to open this gate.");
                        break;

                    case ChunkManager.ObjectiveType.Timer: // timer msg
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
}