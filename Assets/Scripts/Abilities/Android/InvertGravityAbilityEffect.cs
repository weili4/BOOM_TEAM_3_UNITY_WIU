using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[CreateAssetMenu(fileName = "InvertGravityAbility", menuName = "Party/Effects/InvertGravityAbility")]
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

        // toggle gravity direction vector on playercontroller
        bool isCurrentlyInverted = (controller.gravityDirection.y > 0);
        bool targetInverted = !isCurrentlyInverted;

        controller.gravityDirection = targetInverted ? Vector2.up : Vector2.down;

        if (ScreenFlashUI.Instance != null)
        {
            ScreenFlashUI.Instance.TriggerRedFlash();
        }

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
        if (user == null) return;

        PlayerController controller = user.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.gravityDirection = Vector2.down;
        }

        // flip sprite upright
        Vector3 s = user.transform.localScale;
        user.transform.localScale = new Vector3(s.x, 2f, 2f);

        if (user.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.gravityScale = Mathf.Abs(rb.gravityScale);
        }

        // return camera roll back to 0
        CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
        if (cam != null)
        {
            cam.Lens.Dutch = 0f;
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