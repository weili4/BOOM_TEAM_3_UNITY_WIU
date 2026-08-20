using System.Collections.Generic;
using UnityEngine;

public class ActiveBuffContainerUI : MonoBehaviour
{
    [Header("prefab and container")]
    [SerializeField] private GameObject buffWidgetPrefab;
    [SerializeField] private Transform widgetContainer; // transform with vertical layout group

    // tracks active widgets so we dont spawn duplicates
    private Dictionary<string, ActiveBuffWidget> activeWidgets = new Dictionary<string, ActiveBuffWidget>();

    private void Awake()
    {
        if (widgetContainer == null)
            widgetContainer = transform;
    }

    private void Update()
    {
        if (PartyManager.Instance == null || buffWidgetPrefab == null) return;

        var members = PartyManager.Instance.partyMembers;

        // check all members (both leader and benched followers)
        for (int i = 0; i < members.Count; i++)
        {
            var member = members[i];
            if (!member.isUnlocked || member.isDead || member.data == null) continue;

            // check ability Q
            CheckAndSpawnBuff(member, member.data.abilityQ, member.activeTimerQ, 0);

            // check ability E
            CheckAndSpawnBuff(member, member.data.abilityE, member.activeTimerE, 1);

            // check ability R
            CheckAndSpawnBuff(member, member.data.abilityR, member.activeTimerR, 2);
        }

        // clean up destroyed widgets from dictionary
        List<string> keysToRemove = new List<string>();
        foreach (var pair in activeWidgets)
        {
            if (pair.Value == null)
            {
                keysToRemove.Add(pair.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            activeWidgets.Remove(key);
        }
    }

    private void CheckAndSpawnBuff(PartyMember member, AbilityData ability, float activeTimer, int slotIndex)
    {
        if (ability == null || ability.activeDuration <= 0f) return;

        string key = $"{member.data.characterName}_{slotIndex}";

        // if ability is currently active and widget does not exist yet spawn it
        if (activeTimer > 0f && !activeWidgets.ContainsKey(key))
        {
            GameObject obj = Instantiate(buffWidgetPrefab, widgetContainer);
            if (obj.TryGetComponent<ActiveBuffWidget>(out var widget))
            {
                widget.SetupBuff(member, ability, slotIndex);
                activeWidgets.Add(key, widget);
            }
        }
    }
}