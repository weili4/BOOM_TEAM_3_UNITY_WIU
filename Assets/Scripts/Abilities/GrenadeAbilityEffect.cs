using UnityEngine;

[CreateAssetMenu(fileName = "GrenadeAbility", menuName = "Scriptable Objects/Effects/GrenadeAbility")]
public class GrenadeAbilityEffect : AbilityEffect
{
    public GameObject grenadePrefab;
    public float throwSpeed = 8f;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        // play normal attack animation when grenade is used
        if (user.TryGetComponent<Animator>(out Animator animator))
        {
            animator.SetTrigger("IsAttacking");
        }

        // spawn grenade at player and throw towards mouse position
        if (grenadePrefab != null)
        {
            GameObject spawnedGrenade = Instantiate(grenadePrefab, user.transform.position, Quaternion.identity);

            // get direction from player to mouse and keep it same length
            Vector2 direction = (mouseWorldPos - (Vector2)user.transform.position).normalized;

            if (spawnedGrenade.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
            {
                // set grenade velocity so it move towards mouse
                rb.linearVelocity = direction * throwSpeed;
            }
        }
    }

    public override void Deactivate(GameObject user)
    {
        // instant, no deactivation
    }
}