using UnityEngine;
[CreateAssetMenu(fileName = "PotionItemEffect", menuName = "Scriptable Objects/PotionItemEffect")]
public class PotionItemEffect : ItemEffect
{
    public int healAmount = 0;
    public override void Use(GameObject user)
    {
        var health = user.GetComponent<Damageable>();
        if (health != null)
        {
            health.Heal(healAmount);
        }
    }
}