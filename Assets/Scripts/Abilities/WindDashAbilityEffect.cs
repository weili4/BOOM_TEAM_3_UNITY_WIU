using UnityEngine;

[CreateAssetMenu(fileName = "WindDashAbility", menuName = "Scriptable Objects/Effects/WindDashAbility")]
public class WindDashAbilityEffect : AbilityEffect
{
    [Header("movement settings")]
    [SerializeField] private float dashForce = 18f;
    [SerializeField] private float dashDuration = 0.16f;

    [Header("combat settings")]
    [SerializeField] private int dashDamage = 25;
    [SerializeField] private float enemyKnockback = 7.0f;
    [SerializeField] private float hitRadius = 1.1f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("optional front vfx")]
    [SerializeField] private GameObject frontVFXPrefab;

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        PlayerController controller = user.GetComponent<PlayerController>();
        if (controller == null) return;

        // calculate direct aim vector from player to mouse cursor
        Vector2 directionToMouse = (mouseWorldPos - (Vector2)user.transform.position).normalized;

        // fallback if cursor is right on top of player center
        if (directionToMouse.sqrMagnitude < 0.01f)
        {
            directionToMouse = new Vector2(Mathf.Sign(user.transform.localScale.x), 0f);
        }

        // flip character sprite to face mouse direction
        if (Mathf.Abs(directionToMouse.x) > 0.1f)
        {
            float dirSign = Mathf.Sign(directionToMouse.x);
            user.transform.localScale = new Vector3(dirSign * Mathf.Abs(user.transform.localScale.x), user.transform.localScale.y, user.transform.localScale.z);
        }

        // execute dash directly towards mouse cursor
        controller.PerformDash(
            directionToMouse,
            dashForce,
            dashDuration,
            dashDamage,
            enemyKnockback,
            hitRadius,
            enemyLayer,
            frontVFXPrefab
        );
    }

    public override void Deactivate(GameObject user)
    {
    }
}