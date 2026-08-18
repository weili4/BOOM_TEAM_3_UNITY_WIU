using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityHolder : MonoBehaviour
{
    private AbilityData abilityQ;
    private AbilityData abilityE;
    private AbilityData abilityR;

    public void SetupAbilities(AbilityData q, AbilityData e, AbilityData r)
    {
        abilityQ = q;
        abilityE = e;
        abilityR = r;
    }

    private void Update()
    {
        // Only active leader can trigger abilities
        if (!CompareTag("Player") || Keyboard.current == null) return;

        var member = PartyManager.Instance?.ActiveMember;
        if (member == null) return;

        if (Keyboard.current.qKey.wasPressedThisFrame) UseAbility(abilityQ, ref member.cooldownQ, ref member.activeTimerQ);
        if (Keyboard.current.eKey.wasPressedThisFrame) UseAbility(abilityE, ref member.cooldownE, ref member.activeTimerE);
        if (Keyboard.current.rKey.wasPressedThisFrame) UseAbility(abilityR, ref member.cooldownR, ref member.activeTimerR);
    }

    private void UseAbility(AbilityData ability, ref float cdTimer, ref float activeTimer)
    {
        if (ability == null || ability.effectLogic == null) return;
        if (cdTimer > 0) return; // Still on cooldown

        cdTimer = ability.cooldownTime;
        activeTimer = ability.activeDuration;

        Vector2 mousePos = GetMouseWorldPosition();

        if (ability.activationSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(ability.activationSound, transform.position);
        }

        if (ability.vfxPrefab != null)
        {
            Instantiate(ability.vfxPrefab, transform.position, Quaternion.identity);
        }

        ability.effectLogic.Activate(gameObject, mousePos);
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