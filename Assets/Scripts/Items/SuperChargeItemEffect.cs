using UnityEngine;

[CreateAssetMenu(fileName = "SuperChargeItemEffect", menuName = "Scriptable Objects/ItemEffects/SuperCharge")]
public class SuperChargeItemEffect : ItemEffect
{
    public override void Use(GameObject user)
    {
        // find attack event handler on player or child objects
        AttackEventHandler attack = user.GetComponent<AttackEventHandler>();
        if (attack == null)
        {
            attack = user.GetComponentInChildren<AttackEventHandler>();
        }

        if (attack != null)
        {
            attack.OnComboReady(true);
        }
    }
}