using UnityEngine;

[CreateAssetMenu(fileName = "OverloadAbility", menuName = "Party/Effects/OverloadAbility")]
public class OverloadAbilityEffect : AbilityEffect
{
    [Header("stat multipliers")]
    [SerializeField] private float leaderSpeedMultiplier = 2.0f;
    [SerializeField] private float allySpeedMultiplier = 1.3f;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        // buff active leader speed
        if (user.TryGetComponent<PlayerController>(out var player))
        {
            player.moveSpeedMultiplier = leaderSpeedMultiplier;
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
                    }
                }
            }
        }
    }
}