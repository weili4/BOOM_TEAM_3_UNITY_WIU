using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityHolder : MonoBehaviour
{
    private AbilityData abilityQ;
    private AbilityData abilityE;
    private AbilityData abilityR;
    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    public void SetupAbilities(AbilityData q, AbilityData e, AbilityData r)
    {
        abilityQ = q;
        abilityE = e;
        abilityR = r;
    }

    private void Update()
    {
        // only active leader can cast abilities
        if (!CompareTag("Player") || Keyboard.current == null) return;

        // block ability usage while climbing ladders
        if (playerController != null && playerController.IsClimbing) return;

        var member = PartyManager.Instance?.ActiveMember;
        if (member == null) return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            TryCastAbility(abilityQ, ref member.cooldownQ, ref member.activeTimerQ);
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryCastAbility(abilityE, ref member.cooldownE, ref member.activeTimerE);
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            TryCastAbility(abilityR, ref member.cooldownR, ref member.activeTimerR);
        }
    }

    private void TryCastAbility(AbilityData ability, ref float cdTimer, ref float activeTimer)
    {
        if (ability == null || ability.effectLogic == null) return;
        if (cdTimer > 0f || activeTimer > 0f) return;

        activeTimer = ability.activeDuration;
        cdTimer = ability.cooldownTime;

        Vector2 mouseWorldPos = GetMouseWorldPosition();

        if (ability.activationSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(ability.activationSound, transform.position, 1.2f);
        }

        if (ability.vfxPrefab != null)
        {
            Instantiate(ability.vfxPrefab, transform.position, Quaternion.identity);
        }

        ability.effectLogic.Activate(gameObject, mouseWorldPos);
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreen = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Input.mousePosition;
        if (Camera.main != null)
        {
            mouseScreen.z = -Camera.main.transform.position.z;
            return Camera.main.ScreenToWorldPoint(mouseScreen);
        }
        return transform.position;
    }
}