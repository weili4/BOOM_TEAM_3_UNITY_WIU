using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilitySlotUI : MonoBehaviour
{
    [Header("ui references from hierarchy")]
    [SerializeField] private Image borderDefault;      // Border
    [SerializeField] private Image borderActive;       // Border Active (radial 360 yellow image)
    [SerializeField] private Image iconImage;          // Icon
    [SerializeField] private Image cooldownOverlay;    // CooldownOverlay (radial 360 dark image)
    [SerializeField] private TextMeshProUGUI cooldownText; // CooldownText
    [SerializeField] private TextMeshProUGUI keybindText;  // KeybindText

    public void SetupSlot(Sprite icon, string keyName)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = (icon != null);
        }

        if (keybindText != null)
        {
            keybindText.text = keyName;
        }

        if (borderActive != null)
        {
            borderActive.gameObject.SetActive(false);
            borderActive.fillAmount = 0f;
        }

        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(false);
            cooldownOverlay.fillAmount = 0f;
        }

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }
    }

    public void UpdateSlot(float currentCooldown, float maxCooldown, float currentActive, float maxActive)
    {
        // 1. ability is currently active (e.g. summon/buff running)
        if (currentActive > 0f && maxActive > 0f)
        {
            if (borderActive != null)
            {
                borderActive.gameObject.SetActive(true);
                borderActive.fillAmount = Mathf.Clamp01(currentActive / maxActive);
            }

            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(false);
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = currentActive.ToString("F1") + "s";
            }
        }
        // 2. ability is on cooldown
        else if (currentCooldown > 0f && maxCooldown > 0f)
        {
            if (borderActive != null)
            {
                borderActive.gameObject.SetActive(false);
            }

            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(true);
                cooldownOverlay.fillAmount = Mathf.Clamp01(currentCooldown / maxCooldown);
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = currentCooldown.ToString("F1") + "s";
            }
        }
        // 3. ability is ready to use
        else
        {
            if (borderActive != null)
            {
                borderActive.gameObject.SetActive(false);
                borderActive.fillAmount = 0f;
            }

            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(false);
                cooldownOverlay.fillAmount = 0f;
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }

    // fallback overload for older scripts
    public void UpdateSlot(float currentCooldown, float maxCooldown, bool isActive)
    {
        UpdateSlot(currentCooldown, maxCooldown, isActive ? 1f : 0f, isActive ? 1f : 0f);
    }
}