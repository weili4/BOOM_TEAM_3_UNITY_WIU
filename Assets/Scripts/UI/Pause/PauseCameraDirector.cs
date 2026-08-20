using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class PauseCameraDirector : MonoBehaviour
{
    public static PauseCameraDirector Instance { get; private set; }

    [Header("cinemachine references")]
    [SerializeField] private CinemachineCamera cinemachineCam;

    [Header("screen framing")]
    [SerializeField] private float defaultScreenX = 0.5f;
    [SerializeField] private float pausedScreenX = 0.25f; // frames leader on left half
    [SerializeField] private float defaultScreenY = 0.5f;
    [SerializeField] private float pausedScreenY = 0.45f;

    [Header("perspective 3d tilt angles")]
    [SerializeField] private Vector3 defaultEulerAngles = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 pausedEulerAngles = new Vector3(10f, 8f, -1f);

    [Header("perspective fov zoom")]
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float pausedFOV = 40f;

    [Header("transition duration")]
    [SerializeField] private float transitionDuration = 0.16f;

    private CinemachinePositionComposer positionComposer;
    private Transform camTransform;
    private Coroutine transitionRoutine;
    private Vector3 originalDamping;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (cinemachineCam == null)
            cinemachineCam = GetComponent<CinemachineCamera>();

        if (cinemachineCam != null)
        {
            camTransform = cinemachineCam.transform;
            defaultFOV = cinemachineCam.Lens.FieldOfView;
            positionComposer = cinemachineCam.GetComponent<CinemachinePositionComposer>();

            if (positionComposer != null)
            {
                defaultScreenX = positionComposer.Composition.ScreenPosition.x;
                defaultScreenY = positionComposer.Composition.ScreenPosition.y;
                originalDamping = positionComposer.Damping;
            }
        }
    }

    public void AnimateToPauseView(bool isPausing, System.Action onComplete = null)
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionRoutine(isPausing, onComplete));
    }

    private IEnumerator TransitionRoutine(bool isPausing, System.Action onComplete)
    {
        if (cinemachineCam == null || camTransform == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Quaternion startRot = camTransform.localRotation;
        Quaternion targetRot = Quaternion.Euler(isPausing ? pausedEulerAngles : defaultEulerAngles);

        float startFOV = cinemachineCam.Lens.FieldOfView;
        float targetFOV = isPausing ? pausedFOV : defaultFOV;

        float startX = positionComposer != null ? positionComposer.Composition.ScreenPosition.x : defaultScreenX;
        float targetX = isPausing ? pausedScreenX : defaultScreenX;

        float startY = positionComposer != null ? positionComposer.Composition.ScreenPosition.y : defaultScreenY;
        float targetY = isPausing ? pausedScreenY : defaultScreenY;

        if (positionComposer != null)
        {
            positionComposer.Damping = isPausing ? Vector3.zero : originalDamping;
        }

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);

            camTransform.localRotation = Quaternion.Slerp(startRot, targetRot, easeOut);
            cinemachineCam.Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, easeOut);

            if (positionComposer != null)
            {
                var comp = positionComposer.Composition;
                comp.ScreenPosition = new Vector2(
                    Mathf.Lerp(startX, targetX, easeOut),
                    Mathf.Lerp(startY, targetY, easeOut)
                );
                positionComposer.Composition = comp;
            }

            yield return null;
        }

        camTransform.localRotation = targetRot;
        cinemachineCam.Lens.FieldOfView = targetFOV;

        if (positionComposer != null)
        {
            var comp = positionComposer.Composition;
            comp.ScreenPosition = new Vector2(targetX, targetY);
            positionComposer.Composition = comp;

            if (!isPausing)
            {
                positionComposer.Damping = originalDamping;
            }
        }

        transitionRoutine = null;
        onComplete?.Invoke();
    }
}