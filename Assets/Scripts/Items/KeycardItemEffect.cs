using UnityEngine;

[CreateAssetMenu(fileName = "KeycardItemEffect", menuName = "Scriptable Objects/ItemEffects/Keycard")]
public class KeycardItemEffect : ItemEffect
{
 
    public override void Use(GameObject user)
    {
    }

    public override string GetEffectValue()
    {
        return string.Empty;
    }
}