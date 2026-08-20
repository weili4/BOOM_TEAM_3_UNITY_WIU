using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Screen3DTilt : MonoBehaviour
{
    public static Screen3DTilt Instance { get; private set; }

    [Header("target to tilt (camera or canvas recttransform)")]
    [SerializeField] private Transform targetToTilt;

    [Header("3d tilt angles")]
    [SerializeField] private Vector3 defaultEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 tiltedEulerAngles = new Vector3(8f, -14f, 2f); // x is pitch, y is yaw, z is roll

    [Header("scale / zoom during tilt")]
    [SerializeField] private float defaultScale = 1.0f;
    [SerializeField] private float tiltedScale = 1.08f; // zooms in slightly so tilted edges dont show void

    [Header("animation settings")]
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private bool isTilted = false;

    [Header("test hotkey")]
    [SerializeField] private bool enableDebugHotkey = true; // press T to test tilt

    private Coroutine tiltRoutine;
    private Vector3 initialPosition;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (targetToTilt == null)
        {
            targetToTilt = transform;
        }

        initialPosition = targetToTilt.localPosition;
    }

    private void Update()
    {
        // quick test hotkey to preview the effect
        if (enableDebugHotkey && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            Toggle3DTilt(!isTilted);
        }
    }

    public void Toggle3DTilt(bool enable)
    {
        isTilted = enable;

        if (tiltRoutine != null) StopCoroutine(tiltRoutine);
        tiltRoutine = StartCoroutine(AnimateTiltRoutine(isTilted));
    }

    private IEnumerator AnimateTiltRoutine(bool targetState)
    {
        Quaternion startRot = targetToTilt.localRotation;
        Quaternion targetRot = Quaternion.Euler(targetState ? tiltedEulerAngles : defaultEulerAngles);

        Vector3 startScaleVec = targetToTilt.localScale;
        Vector3 targetScaleVec = Vector3.one * (targetState ? tiltedScale : defaultScale);

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            // use unscaled time so it works during pause menu or hitstops
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            targetToTilt.localRotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            targetToTilt.localScale = Vector3.Lerp(startScaleVec, targetScaleVec, smoothT);

            yield return null;
        }

        targetToTilt.localRotation = targetRot;
        targetToTilt.localScale = targetScaleVec;
        tiltRoutine = null;
    }
}