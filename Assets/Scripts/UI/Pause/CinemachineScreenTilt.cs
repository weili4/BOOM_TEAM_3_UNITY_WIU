using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CinemachineScreenTilt : MonoBehaviour
{
    public static CinemachineScreenTilt Instance { get; private set; }

    [Header("cinemachine reference")]
    [SerializeField] private CinemachineCamera cinemachineCam;

    [Header("3d tilt angles")]
    [SerializeField] private Vector3 defaultEulerAngles = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 tiltedEulerAngles = new Vector3(18f, -14f, 4f); // 3d video edit tilt

    [Header("lens adjustments")]
    [SerializeField] private float defaultOrthoSize = 7.0f;
    [SerializeField] private float tiltedOrthoSize = 6.2f; // slight zoom in during 3d tilt

    [Header("transition speed")]
    [SerializeField] private float transitionDuration = 0.35f;

    [Header("debug toggle key")]
    [SerializeField] private bool enableDebugKey = true; // press T to toggle

    private bool is3DModeActive = false;
    private Coroutine tiltRoutine;
    private Transform camTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (cinemachineCam == null)
            cinemachineCam = GetComponent<CinemachineCamera>();

        if (cinemachineCam != null)
            camTransform = cinemachineCam.transform;
    }

    private void Start()
    {
        if (camTransform != null)
        {
            camTransform.localRotation = Quaternion.Euler(defaultEulerAngles);
        }
    }

    private void Update()
    {
        // quick test toggle with T key
        if (enableDebugKey && Keyboard.current != null)
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                Toggle3DMode(!is3DModeActive);
            }
        }
    }

    public void Toggle3DMode(bool enable3D)
    {
        is3DModeActive = enable3D;

        if (tiltRoutine != null) StopCoroutine(tiltRoutine);
        tiltRoutine = StartCoroutine(AnimateTiltRoutine(enable3D));
    }

    private IEnumerator AnimateTiltRoutine(bool enable3D)
    {
        if (camTransform == null || cinemachineCam == null) yield break;

        Quaternion startRot = camTransform.localRotation;
        Quaternion targetRot = Quaternion.Euler(enable3D ? tiltedEulerAngles : defaultEulerAngles);

        float startSize = cinemachineCam.Lens.OrthographicSize;
        float targetSize = enable3D ? tiltedOrthoSize : defaultOrthoSize;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            // use unscaled delta time so it works during pause menu
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // smooth rotation lerp
            camTransform.localRotation = Quaternion.Slerp(startRot, targetRot, smoothT);

            // smooth lens zoom
            cinemachineCam.Lens.OrthographicSize = Mathf.Lerp(startSize, targetSize, smoothT);

            yield return null;
        }

        camTransform.localRotation = targetRot;
        cinemachineCam.Lens.OrthographicSize = targetSize;
        tiltRoutine = null;
    }
}