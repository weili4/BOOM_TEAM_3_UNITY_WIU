using UnityEngine;

[CreateAssetMenu(fileName = "RageAbility", menuName = "Party/Effects/RageAbility")]
public class RageAbilityEffect : AbilityEffect
{
    [Header("rage multipliers")]
    public float animSpeedMultiplier = 1.5f;
    public float moveSpeedMultiplier = 1.3f;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        if (user.TryGetComponent<PlayerController>(out var controller))
        {
            controller.moveSpeedMultiplier = moveSpeedMultiplier;

            if (controller.animator != null)
            {
                controller.animator.speed = animSpeedMultiplier;
            }
        }
    }

    public override void Deactivate(GameObject user)
    {
        if (user == null) return;

        if (user.TryGetComponent<PlayerController>(out var controller))
        {
            controller.moveSpeedMultiplier = 1.0f;

            if (controller.animator != null)
            {
                controller.animator.speed = 1.0f;
            }
        }
    }
}