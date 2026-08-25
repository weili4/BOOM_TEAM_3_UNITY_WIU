using UnityEngine;

[CreateAssetMenu(fileName = "ReviveItemEffect", menuName = "Scriptable Objects/ItemEffects/Revive")]
public class ReviveItemEffect : ItemEffect
{
    public override void Use(GameObject user)
    {
        if (PartyManager.Instance != null && PartyManager.Instance.ActivePlayerObj != null)
        {
            PartyManager.Instance.ReviveAllDead(1.0f , true);
        }
    }

    public override string GetEffectValue()
    {
        string EffectText = "Revive All Party Members";
        return EffectText;
    }
}