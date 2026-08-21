using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "OverloadAbility", menuName = "Party/Effects/OverloadAbility")]
public class OverLoadAbilityEffect : AbilityEffect
{
    [Header("stat multipliers")]
    [SerializeField] private float leaderSpeedMultiplier = 2.0f;
    [SerializeField] private float allySpeedMultiplier = 1.3f;
    [SerializeField] private float startupDelay = 0.3f; // seconds
    private Animator mainAnimator;
    private Animator allyAnimator;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        // Start coroutine on a MonoBehaviour attached to the user
        var runner = user.GetComponent<MonoBehaviour>();
        if (runner != null)
        {
            runner.StartCoroutine(ApplyOverload(user));
        }
    }

    private IEnumerator ApplyOverload(GameObject user)
    {
        // buff active leader speed
        if (user.TryGetComponent<PlayerController>(out var player))
        {
            mainAnimator = player.GetComponent<Animator>();
            player.moveSpeedMultiplier = 0;

            mainAnimator.SetTrigger("IsBoosting");

            // Wait before applying buffs
            yield return new WaitForSeconds(startupDelay);

            player.moveSpeedMultiplier = leaderSpeedMultiplier;

            if (mainAnimator != null) mainAnimator.speed *= leaderSpeedMultiplier;
        }

        // buff followers if partymanager exists
        if (PartyManager.Instance != null)
        {
            foreach (var member in PartyManager.Instance.partyMembers)
            {
                if (member.spawnedInstance != null && member.spawnedInstance != user)
                {
                    if (member.spawnedInstance.TryGetComponent<FollowerAI>(out var ai))
                    {
                        ai.followSpeed *= allySpeedMultiplier;
                        allyAnimator = ai.GetComponent<Animator>();
                        if (allyAnimator != null) allyAnimator.speed *= allySpeedMultiplier;
                    }

                }
            }
        }
    }

    public override void Deactivate(GameObject user)
    {
        if (user == null) return;

        // restore normal speed
        if (user.TryGetComponent<PlayerController>(out var player))
        {
            player.moveSpeedMultiplier = 1.0f;
            mainAnimator = player.GetComponent<Animator>();
            if (mainAnimator != null) mainAnimator.speed = 1;
        }

        if (PartyManager.Instance != null)
        {
            foreach (var member in PartyManager.Instance.partyMembers)
            {
                if (member.spawnedInstance != null && member.spawnedInstance != user)
                {
                    if (member.spawnedInstance.TryGetComponent<FollowerAI>(out var ai))
                    {
                        ai.followSpeed = 6.0f; // default follow speed
                        allyAnimator = ai.GetComponent<Animator>();
                        if (allyAnimator != null) allyAnimator.speed = 1;
                    }

                }
            }
        }
    }
}
