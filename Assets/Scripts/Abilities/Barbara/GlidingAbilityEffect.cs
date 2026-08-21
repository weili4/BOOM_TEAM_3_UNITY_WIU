using UnityEngine;

[CreateAssetMenu(fileName = "GlidingAbilityEffect", menuName = "Party/Effects/GlidingAbilityEffect")]
public class GlidingAbilityEffect : AbilityEffect
{
    [SerializeField] private float SetFallMultiplier = 0.50f;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        PlayerController controller = user.GetComponent<PlayerController>();
        if (controller == null) return;

        controller.fallMultiplier = SetFallMultiplier;
    }

    public override void Deactivate(GameObject user)
    {
        if (user == null) return;

        PlayerController controller = user.GetComponent<PlayerController>();
        if (controller == null) return;

        controller.fallMultiplier = 2.5f;
    }
}