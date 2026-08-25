using UnityEngine;

[CreateAssetMenu(fileName = "ResetCooldownsItemEffect", menuName = "Scriptable Objects/ItemEffects/ResetCooldowns")]
public class ResetCooldownsItemEffect : ItemEffect
{
    public override void Use(GameObject user)
    {
        PartyManager.Instance.ActiveMember.cooldownQ = 0;
        PartyManager.Instance.ActiveMember.cooldownE = 0;
        PartyManager.Instance.ActiveMember.cooldownR = 0;
    }

    public override string GetEffectValue()
    {
        string EffectText = "Resets all ability cooldowns";
        return EffectText;
    }
}