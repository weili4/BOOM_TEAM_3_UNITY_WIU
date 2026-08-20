using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerMode { OnWalkIn, OnInteractKey }

    [Header("trigger mode")]
    [SerializeField] private TriggerMode triggerMode = TriggerMode.OnWalkIn;
    [SerializeField] private bool isCinematicCutscene = true; // pauses game time and dims background
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("dialogue script (list of lines)")]
    [SerializeField] private List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("events when full dialogue finishes")]
    [SerializeField] private UnityEvent onDialogueCompleted;

    private bool hasTriggered = false;
    private bool playerIsInside = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerIsInside = true;

            if (triggerMode == TriggerMode.OnWalkIn && (!hasTriggered || !triggerOnlyOnce))
            {
                StartConversation();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerIsInside = false;
        }
    }

    private void Update()
    {
        // interact key trigger (F or E key)
        if (triggerMode == TriggerMode.OnInteractKey && playerIsInside && (!hasTriggered || !triggerOnlyOnce))
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueRunning) return;

            bool interactPressed = false;
            if (Keyboard.current != null)
            {
                interactPressed = Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame;
            }

            if (interactPressed)
            {
                StartConversation();
            }
        }
    }

    public void StartConversation()
    {
        if (DialogueManager.Instance == null || dialogueLines.Count == 0) return;

        hasTriggered = true;

        DialogueManager.Instance.StartDialogue(dialogueLines, isCinematicCutscene, () =>
        {
            onDialogueCompleted?.Invoke();

            if (triggerOnlyOnce)
            {
                Collider2D col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
            }
        });
    }
}