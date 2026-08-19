using UnityEngine;

public class AbilityUI : MonoBehaviour
{
    [Header("slots for Q, E, R in order")]
    [SerializeField] private AbilitySlotUI slotQ; // AbilitySlot_1
    [SerializeField] private AbilitySlotUI slotE; // AbilitySlot_2
    [SerializeField] private AbilitySlotUI slotR; // AbilitySlot_3

    private void OnEnable()
    {
        PartyManager.OnLeaderSwapped += HandleLeaderSwapped;
        PartyManager.OnPartyUpdated += RefreshCurrentLeaderAbilities;
    }

    private void OnDisable()
    {
        PartyManager.OnLeaderSwapped -= HandleLeaderSwapped;
        PartyManager.OnPartyUpdated -= RefreshCurrentLeaderAbilities;
    }

    private void Start()
    {
        RefreshCurrentLeaderAbilities();
    }

    private void HandleLeaderSwapped(int oldLeaderIdx, int newLeaderIdx)
    {
        RefreshCurrentLeaderAbilities();
    }

    private void RefreshCurrentLeaderAbilities()
    {
        if (PartyManager.Instance == null) return;

        var leader = PartyManager.Instance.ActiveMember;
        if (leader == null || leader.data == null) return;

        // setup icons and keybind texts
        if (slotQ != null) slotQ.SetupSlot(leader.data.abilityQ != null ? leader.data.abilityQ.icon : null, "Q");
        if (slotE != null) slotE.SetupSlot(leader.data.abilityE != null ? leader.data.abilityE.icon : null, "E");
        if (slotR != null) slotR.SetupSlot(leader.data.abilityR != null ? leader.data.abilityR.icon : null, "R");
    }

    private void Update()
    {
        if (PartyManager.Instance == null) return;

        var leader = PartyManager.Instance.ActiveMember;
        if (leader == null || leader.data == null) return;

        // update slot Q
        if (slotQ != null && leader.data.abilityQ != null)
        {
            slotQ.UpdateSlot(
                leader.cooldownQ,
                leader.data.abilityQ.cooldownTime,
                leader.activeTimerQ,
                leader.data.abilityQ.activeDuration
            );
        }

        // update slot E
        if (slotE != null && leader.data.abilityE != null)
        {
            slotE.UpdateSlot(
                leader.cooldownE,
                leader.data.abilityE.cooldownTime,
                leader.activeTimerE,
                leader.data.abilityE.activeDuration
            );
        }

        // update slot R
        if (slotR != null && leader.data.abilityR != null)
        {
            slotR.UpdateSlot(
                leader.cooldownR,
                leader.data.abilityR.cooldownTime,
                leader.activeTimerR,
                leader.data.abilityR.activeDuration
            );
        }
    }
}