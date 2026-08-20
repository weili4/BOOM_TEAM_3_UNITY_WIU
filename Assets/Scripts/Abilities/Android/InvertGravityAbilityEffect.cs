using UnityEngine;

[CreateAssetMenu(fileName = "InvertGravityAbility", menuName = "Scriptable Objects/Effects/InvertGravityAbility")]
public class InvertGravityAbilityEffect : AbilityEffect
{
    //keep track of original gravity scale
    private float originalGravityScale;
    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        Rigidbody2D rb = user.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log("User:" + user.name);
            originalGravityScale = rb.gravityScale;
            rb.gravityScale = -originalGravityScale; // flip gravity
            Debug.Log("GravityScale: " +  rb.gravityScale);
        }

    }

    public override void Deactivate(GameObject user)
    {
        Rigidbody2D rb = user.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = originalGravityScale; // restore normal gravity
            Debug.Log("gravity reverted");
        }
    }
}
