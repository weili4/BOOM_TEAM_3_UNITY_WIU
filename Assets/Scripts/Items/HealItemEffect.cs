using UnityEngine;

[CreateAssetMenu(fileName = "HealItemEffect", menuName = "Scriptable Objects/ItemEffects/Heal")]
public class HealItemEffect : ItemEffect
{
    public int healAmount = 30;

    public override void Use(GameObject user)
    {
        // call heal on damageable script
        if (user.TryGetComponent<Damageable>(out Damageable health))
        {
            health.Heal(healAmount);
        }
    }

    public override string GetEffectValue()
    {
        string EffectText = "Heal " + healAmount + " HP ";
        return EffectText;
        //return string.Empty; // Please Change to the item Effect Value with Text
    }
}