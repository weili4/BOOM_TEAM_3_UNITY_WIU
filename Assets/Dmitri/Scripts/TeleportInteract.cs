using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportInteract : MonoBehaviour
{
    [Header("Teleport Destination")]
    [SerializeField] private Transform teleportPoint;

    [Header("Chunk Reference")]
    [SerializeField] private ChunkManager currentChunkManager;

    [Header("Prompt Settings")]
    [SerializeField] private string interactMessage = "Press 'E' to Interact";

    [Header("Line Renderer Settings")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float lineDisplayDuration = 0.25f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip teleportSFX;
    [SerializeField][Range(0f, 1f)] private float soundVolume = 1.0f;

    private bool isPlayerInside = false;
    private GameObject playerObject;
    private Coroutine lineCoroutine;

    private void Awake()
    {
        // Ensure line starts hidden
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = true;
            playerObject = collision.gameObject;

            // Show UI Prompt
            LevelObjectiveUI.Instance?.SetObjectiveText(interactMessage);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
            playerObject = null;

            RestorePreviousUI();
        }
    }

    private void Update()
    {
        bool eKeyPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (isPlayerInside && eKeyPressed && teleportPoint != null)
        {
            TeleportPlayer();
        }
    }

    private void TeleportPlayer()
    {
        // Play sound
        if (teleportSFX != null)
        {
            AudioManager.Instance?.PlaySFX(teleportSFX, transform.position, soundVolume);
        }

        // Draw laser/beam effect between points
        if (lineRenderer != null)
        {
            if (lineCoroutine != null) StopCoroutine(lineCoroutine);
            lineCoroutine = StartCoroutine(ShowLineRoutine());
        }

        if (playerObject == null) return;

        // Reset Rigidbody2D velocity
        if (playerObject.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Teleport player
        playerObject.transform.position = teleportPoint.position;

        RestorePreviousUI();
    }

    private IEnumerator ShowLineRoutine()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, teleportPoint.position);
        lineRenderer.enabled = true;

        yield return new WaitForSeconds(lineDisplayDuration);

        lineRenderer.enabled = false;
    }

    private void RestorePreviousUI()
    {
        // Refresh UI from ChunkManager if assigned
        if (currentChunkManager != null)
        {
            currentChunkManager.UpdateChunkObjectiveUI();
        }
        else
        {
            // Fallback clear if no ChunkManager is assigned
            LevelObjectiveUI.Instance?.SetObjectiveText("");
        }
    }
}