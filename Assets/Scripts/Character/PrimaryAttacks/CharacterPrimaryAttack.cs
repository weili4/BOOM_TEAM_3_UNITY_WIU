using UnityEngine;
using UnityEngine.InputSystem;

public abstract class CharacterPrimaryAttack : MonoBehaviour
{
    protected PlayerController playerController;
    protected Animator animator;

    protected virtual void Awake()
    {
        playerController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    protected virtual void Update()
    {
        // reminder: only active leader can attack, and block while on ladders
        if (!CompareTag("Player")) return;
        if (playerController != null && playerController.IsClimbing) return;

        // block attacking during cutscenes
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueRunning) return;

        HandleAttack();
    }

    protected abstract void HandleAttack();

    protected Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreen = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Input.mousePosition;
        if (Camera.main != null)
        {
            mouseScreen.z = -Camera.main.transform.position.z;
            return Camera.main.ScreenToWorldPoint(mouseScreen);
        }
        return transform.position;
    }
}