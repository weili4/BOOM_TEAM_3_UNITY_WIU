using UnityEngine;

[CreateAssetMenu(fileName = "OverloadAbility", menuName = "Scriptable Objects/Effects/OverloadAbility")]

public class OverLoadAbilityEffect : AbilityEffect
{
    public PlayerController controller;
    public Animator animator;
    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        controller = user.GetComponent<PlayerController>();

        controller.moveSpeed = controller.moveSpeed * 2;
        animator = user.GetComponent<Animator>();

        animator.speed = animator.speed * 2;

    }

    public override void Deactivate(GameObject user)
    {
        controller = user.GetComponent<PlayerController>();
        controller.moveSpeed = controller.moveSpeed / 2;

        animator = user.GetComponent<Animator>();

        animator.speed = animator.speed / 2;
    }
}
