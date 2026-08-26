using UnityEngine;

[CreateAssetMenu(fileName = "ShieldItemEffect", menuName = "Scriptable Objects/ItemEffects/Shield")]
public class ShieldItemEffect : ItemEffect
{
    public float invulnerableTime = 4f;

    public override void Use(GameObject user)
    {
        if (user.TryGetComponent<Damageable>(out Damageable User_Damageable))
        {
            User_Damageable.SetInvulnerable(invulnerableTime);
        }
    }

    public override string GetEffectValue()
    {
        string EffectText = "Immune to damage for " + invulnerableTime + "s";
        return EffectText;
    }
}