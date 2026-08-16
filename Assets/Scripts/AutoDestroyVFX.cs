using UnityEngine;

public class AutoDestroyVFX : MonoBehaviour
{
    public float delay = 0.5f; // safety fallback

    void Start()
    {
        // get the length of the animation clip and destroy after it finishes
        Animator animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            // get the length of the first clip in the controller
            float clipLength = animator.runtimeAnimatorController.animationClips[0].length;
            Destroy(gameObject, clipLength);
        }
        else
        {
            // fallback if no animator found
            Destroy(gameObject, delay);
        }
    }
}