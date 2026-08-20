using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

[CreateAssetMenu(fileName = "WindDashAbility", menuName = "Scriptable Objects/Effects/WindDashAbility")]
public class WindDashAbilityEffect : AbilityEffect
{
    [SerializeField] private float DashForce = 15f;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        Rigidbody2D PlayerRigidBody = user.GetComponent<Rigidbody2D>();

        if (PlayerRigidBody == null) return;

        Vector2 DirectionToMouse = (mouseWorldPos - (Vector2)user.transform.position).normalized;
        PlayerRigidBody.linearVelocityX = DirectionToMouse.x * DashForce;

        Debug.Log(PlayerRigidBody.linearVelocity.x);
    }

    public override void Deactivate(GameObject user)
    {

    }
}