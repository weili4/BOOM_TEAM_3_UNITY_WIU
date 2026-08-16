using UnityEngine;

[CreateAssetMenu(fileName = "RageAbility", menuName = "Scriptable Objects/Effects/RageAbility")]
public class RageAbilityEffect : AbilityEffect
{
    public float animSpeedMultiplier = 1.5f;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        PlayerController controller = user.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.isRaging = true;
            controller.animator.speed = animSpeedMultiplier;
        }
    }

    public override void Deactivate(GameObject user)
    {
        PlayerController controller = user.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.isRaging = false;
            controller.animator.speed = 1.0f;
        }
    }
}