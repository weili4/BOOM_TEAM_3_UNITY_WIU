using UnityEngine;

[CreateAssetMenu(fileName = "ResetCooldownsItemEffect", menuName = "Scriptable Objects/ItemEffects/ResetCooldowns")]
public class ResetCooldownsItemEffect : ItemEffect
{
    public override void Use(GameObject user)
    {
        // call reset on ability controller
        if (user.TryGetComponent<AbilityController>(out AbilityController abilities))
        {
            abilities.ResetAllAbilityCooldowns();
        }
    }
}