using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityController : MonoBehaviour
{
    // ABILITY CONTROLLER WITH INPUT SYSTEM ACTIONS (input got fallback just in case)

    [SerializeField] private List<AbilityData> abilities;
    [SerializeField] private List<AbilityEffect> abilityEffects;
    [SerializeField] private List<AbilitySlotUI> uiSlots;

    private Dictionary<AbilityData, float> cooldownTimers = new Dictionary<AbilityData, float>();
    private Dictionary<AbilityData, float> activeTimers = new Dictionary<AbilityData, float>();
    private Dictionary<AbilityData, bool> isActive = new Dictionary<AbilityData, bool>();

    private void Start()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            var ability = abilities[i];
            if (ability != null)
            {
                cooldownTimers[ability] = 0f;
                activeTimers[ability] = 0f;
                isActive[ability] = false;

                if (i < uiSlots.Count && uiSlots[i] != null)
                {
                    string keyName = (i + 1).ToString();
                    uiSlots[i].SetupSlot(ability.icon, keyName);
                }
            }
        }
    }

    public void ResetAllAbilityCooldowns()
    {
        List<AbilityData> keys = new List<AbilityData>(cooldownTimers.Keys);
        foreach (var key in keys)
        {
            cooldownTimers[key] = 0f;
        }
    }

    private void Update()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            AbilityData ability = abilities[i];
            if (ability == null || i >= abilityEffects.Count || abilityEffects[i] == null) continue;

            if (cooldownTimers[ability] > 0)
                cooldownTimers[ability] -= Time.deltaTime;

            if (isActive[ability])
            {
                activeTimers[ability] -= Time.deltaTime;
                if (activeTimers[ability] <= 0)
                {
                    isActive[ability] = false;
                    abilityEffects[i].Deactivate(gameObject);
                    cooldownTimers[ability] = ability.cooldownTime;
                }
            }

            if (i < uiSlots.Count && uiSlots[i] != null)
            {
                uiSlots[i].SetupSlot(ability.icon, (i + 1).ToString());
                uiSlots[i].UpdateSlot(cooldownTimers[ability], ability.cooldownTime, isActive[ability]);
            }

            // CHECK INPUT VIA NATIVE INPUT SYSTEM ACTIONS AS TAUGHT IN PRACTICALS
            bool keyPressed = CheckAbilityInput(i);
            if (keyPressed)
            {
                if (cooldownTimers[ability] <= 0 && !isActive[ability])
                {
                    isActive[ability] = true;
                    activeTimers[ability] = ability.activeDuration;

                    Vector3 mouseScreenPos = Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Input.mousePosition;
                    Vector3 mouseWorldPos = Vector3.zero;

                    if (Camera.main != null)
                    {
                        mouseScreenPos.z = -Camera.main.transform.position.z;
                        mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
                        mouseWorldPos.z = 0;
                    }

                    if (ability.activationSound != null)
                    {
                        if (AudioManager.Instance != null)
                            AudioManager.Instance.PlaySFX(ability.activationSound, transform.position, 1.3f);
                        else
                            AudioSource.PlayClipAtPoint(ability.activationSound, transform.position);
                    }

                    if (ability.vfxPrefab != null)
                        Instantiate(ability.vfxPrefab, transform.position, Quaternion.identity);

                    abilityEffects[i].Activate(gameObject, mouseWorldPos);
                }
            }
        }
    }

    private bool CheckAbilityInput(int index)
    {
        // READ INPUT SYSTEM ACTIONS AS TAUGHT IN PRACTICAL SLIDES
        string actionName = "Ability" + (index + 1);

        try
        {
            if (InputSystem.actions != null && InputSystem.actions[actionName] != null)
            {
                return InputSystem.actions[actionName].WasPressedThisFrame();
            }
        }
        catch { }

        // FALLBACK TO KEYBOARD NUMBERS
        if (Keyboard.current != null)
        {
            switch (index)
            {
                case 0: return Keyboard.current.digit1Key.wasPressedThisFrame;
                case 1: return Keyboard.current.digit2Key.wasPressedThisFrame;
                case 2: return Keyboard.current.digit3Key.wasPressedThisFrame;
                default: return false;
            }
        }

        return false;
    }
}