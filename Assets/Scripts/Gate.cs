using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class Gate : MonoBehaviour
{
    [Header("GATE MOVEMENT SETTINGS")]
    [SerializeField] private float openHeight = 3.5f;
    [SerializeField] private float openSpeed = 5f;
    [SerializeField] private float slamSpeed = 25f;

    [Header("GATE AUDIO CLIPS")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip slamSound;

    [Header("CAMERA SHAKE ON SLAM")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float slamShakeForce = 2.5f;

    [Header("GATE STATE")]
    [SerializeField] private bool isOpen = false;

    private Vector3 closedPos;
    private Vector3 openPos;
    private Coroutine moveRoutine;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        closedPos = transform.position;
        openPos = closedPos + Vector3.up * openHeight;

        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void OpenGate()
    {
        if (isOpen) return;
        isOpen = true;

        // PLAY GATE OPEN SFX
        if (openSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(openSound, transform.position, 1.2f);
            else
                AudioSource.PlayClipAtPoint(openSound, transform.position);
        }

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(AnimateGate(openPos, openSpeed, false));
    }

    public void SlamCloseGate()
    {
        if (!isOpen) return;
        isOpen = false;

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(AnimateGate(closedPos, slamSpeed, true));
    }

    public void CloseGate()
    {
        SlamCloseGate();
    }

    private IEnumerator AnimateGate(Vector3 targetPos, float speed, bool isSlamming)
    {
        while (Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;

        if (isSlamming)
        {
            if (impulseSource != null)
                impulseSource.GenerateImpulse(slamShakeForce);

            // PLAY SLAM SFX
            if (slamSound != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(slamSound, transform.position, 1.5f);
                else
                    AudioSource.PlayClipAtPoint(slamSound, transform.position);
            }
        }
    }
}