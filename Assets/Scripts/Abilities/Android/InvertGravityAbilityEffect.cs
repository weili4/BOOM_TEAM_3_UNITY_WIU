using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[CreateAssetMenu(fileName = "InvertGravityAbility", menuName = "Scriptable Objects/Effects/InvertGravityAbility")]
public class InvertGravityAbilityEffect : AbilityEffect
{
    [Header("camera dutch roll")]
    [SerializeField] private bool rotateCamera180 = true;
    [SerializeField] private float rollDuration = 0.28f;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        PlayerController controller = user.GetComponent<PlayerController>();
        if (controller == null) return;

        // toggle gravity state (normal -> inverted -> normal)
        bool targetInverted = !controller.IsGravityInverted;
        controller.SetGravityInverted(targetInverted);

        // trigger screen flash
        if (ScreenFlashUI.Instance != null)
        {
            ScreenFlashUI.Instance.TriggerRedFlash();
        }

        // rotate cinemachine camera 180 degrees
        if (rotateCamera180)
        {
            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
            if (cam != null)
            {
                float targetDutch = targetInverted ? 180f : 0f;
                controller.StartCoroutine(AnimateDutchRoll(cam, targetDutch, rollDuration));
            }
        }
    }

    public override void Deactivate(GameObject user)
    {
        // cleanup failsafe
        if (user == null) return;
        PlayerController controller = user.GetComponent<PlayerController>();
        if (controller != null && controller.IsGravityInverted)
        {
            controller.SetGravityInverted(false);
            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
            if (cam != null) cam.Lens.Dutch = 0f;
        }
    }

    private IEnumerator AnimateDutchRoll(CinemachineCamera cam, float targetDutch, float duration)
    {
        float startDutch = cam.Lens.Dutch;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            cam.Lens.Dutch = Mathf.Lerp(startDutch, targetDutch, smooth);
            yield return null;
        }

        cam.Lens.Dutch = targetDutch;
    }
}