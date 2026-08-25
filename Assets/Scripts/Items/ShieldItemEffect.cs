using UnityEngine;

[CreateAssetMenu(fileName = "ShieldItemEffect", menuName = "Scriptable Objects/ItemEffects/Shield")]
public class ShieldItemEffect : ItemEffect
{
    public float invulnerableTime = 4f;

    public override void Use(GameObject user)
    {
        // trigger iframe on damageable
        if (user.TryGetComponent<Damageable>(out Damageable health))
        {
            health.TakeDamage(0); // triggers iframe flash without losing hp
        }
    }

    public override string GetEffectValue()
    {
        string EffectText = "Immune to damage for " + invulnerableTime + "s";
        return EffectText;
    }
}