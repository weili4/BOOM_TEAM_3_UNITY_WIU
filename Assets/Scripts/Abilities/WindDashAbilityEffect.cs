using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private GameObject frontVFXPrefab; // ASSIGN VFX HERE JUN LOONG

    public override void Activate(GameObject user, Vector2 mouseWorldPos)
    {
        if (user == null) return;

        PlayerController controller = user.GetComponent<PlayerController>();
        if (controller == null) return;

        // 1. read 8 directional input from WASD or move stick
        Vector2 inputDir = Vector2.zero;

        try
        {
            if (InputSystem.actions != null && InputSystem.actions["Move"] != null)
            {
                inputDir = InputSystem.actions["Move"].ReadValue<Vector2>();
            }
        }
        catch { }

        if (inputDir.sqrMagnitude < 0.01f && Keyboard.current != null)
        {
            float x = 0f;
            float y = 0f;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;

            inputDir = new Vector2(x, y);
        }

        // 2. perform dash with combat damage and front vfx
        controller.PerformDash(
            inputDir,
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