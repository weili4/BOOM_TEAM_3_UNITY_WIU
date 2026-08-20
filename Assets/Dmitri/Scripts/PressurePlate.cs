using System.Collections;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Target References")]
    [SerializeField] private Transform targetObject;

    [Header("Destination Offset Settings")]
    [Tooltip("Check this to move relative to starting position. Uncheck to move to exact world coordinates.")]
    [SerializeField] private bool RelativeOffset = true;
    [SerializeField] private Vector3 targetPositionOffset = new Vector3(0f, 0f, 0f);

    [Header("Movement Settings")]
    [SerializeField] private bool moveSmoothly = true;
    [SerializeField] private float moveSpeed = 4.0f;

    [Header("Return Settings")]
    [SerializeField] private bool returnOnExit = false;
    [SerializeField] private float returnDelay = 2.0f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip pressSFX;
    [SerializeField][Range(0f, 1f)] private float soundVolume = 1.0f;

    private Vector3 originalPosition;
    private Vector3 calculatedDestination;
    private Coroutine moveCoroutine;
    private Coroutine returnDelayCoroutine;

    private void Awake()
    {
        if (targetObject != null)
        {
            originalPosition = targetObject.position;

            // Calculate destination once at start based on initial position
            calculatedDestination = RelativeOffset
                ? originalPosition + targetPositionOffset
                : targetPositionOffset;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && targetObject != null)
        {
            // Cancel closing delay if player steps back on the plate
            if (returnDelayCoroutine != null)
            {
                StopCoroutine(returnDelayCoroutine);
                returnDelayCoroutine = null;
            }

            // Play sound
            if (pressSFX != null)
            {
                AudioManager.Instance?.PlaySFX(pressSFX, transform.position, soundVolume);
            }

            SetTargetPosition(calculatedDestination);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && returnOnExit && targetObject != null)
        {
            if (returnDelayCoroutine != null) StopCoroutine(returnDelayCoroutine);
            returnDelayCoroutine = StartCoroutine(DelayedReturnRoutine());
        }
    }

    private IEnumerator DelayedReturnRoutine()
    {
        if (returnDelay > 0f)
        {
            yield return new WaitForSeconds(returnDelay);
        }

        SetTargetPosition(originalPosition);
        returnDelayCoroutine = null;
    }

    private void SetTargetPosition(Vector3 targetPos)
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);

        if (moveSmoothly)
        {
            moveCoroutine = StartCoroutine(MoveToRoutine(targetPos));
        }
        else
        {
            targetObject.position = targetPos;
        }
    }

    private IEnumerator MoveToRoutine(Vector3 destination)
    {
        while (Vector3.Distance(targetObject.position, destination) > 0.001f)
        {
            targetObject.position = Vector3.MoveTowards(
                targetObject.position,
                destination,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        targetObject.position = destination;
    }
}