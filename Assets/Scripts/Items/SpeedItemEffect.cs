using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedItemEffect", menuName = "Scriptable Objects/ItemEffects/Speed")]
public class SpeedItemEffect : ItemEffect
{
    public float speedMultiplier = 1.6f;
    public float duration = 5f;

    public override void Use(GameObject user)
    {
        // start coroutine on player to boost speed
        if (user.TryGetComponent<PlayerController>(out PlayerController player))
        {
            player.StartCoroutine(ApplySpeedBoost(player));
        }
    }

    private IEnumerator ApplySpeedBoost(PlayerController player)
    {
        float origSpeed = player.moveSpeed;
        player.moveSpeed *= speedMultiplier;

        yield return new WaitForSeconds(duration);

        player.moveSpeed = origSpeed;
    }
}