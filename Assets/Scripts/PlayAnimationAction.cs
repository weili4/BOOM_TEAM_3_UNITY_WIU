using UnityEngine;

[CreateAssetMenu(fileName = "PlayAnimationAction", menuName = "Scriptable Objects/Actions/PlayAnimationAction")]
public class PlayAnimationAction : StateAction
{
    public string animationName;

    public override void Act(StateController controller)
    {
        if (controller.TryGetComponent<Animator>(out Animator anim))
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            float progress = stateInfo.normalizedTime % 1f;
            if (progress >= 0.99f)
                anim.Play(animationName, -1, 0f);
            else
                anim.Play(animationName);
        }
    }
}