using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilitySlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Image activeBorder;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI keybindText;

    public void SetupSlot(Sprite icon, string keyName)
    {
        if (iconImage != null && icon != null)
            iconImage.sprite = icon;

        if (keybindText != null)
            keybindText.text = keyName;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 0f;

        if (cooldownText != null)
            cooldownText.gameObject.SetActive(false);

        if (activeBorder != null)
            activeBorder.gameObject.SetActive(false);
    }

    public void UpdateSlot(float currentCooldown, float totalCooldown, bool isActive)
    {
        // show cooldown state when ability still on cooldown
        if (currentCooldown > 0 && totalCooldown > 0)
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(true);
                cooldownOverlay.fillAmount = Mathf.Clamp01(currentCooldown / totalCooldown);
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = currentCooldown.ToString("F1") + "s";
            }

            if (activeBorder != null)
                activeBorder.gameObject.SetActive(false);
        }
        // while ability is currently running
        else if (isActive)
        {
            if (cooldownOverlay != null)
                cooldownOverlay.gameObject.SetActive(false);

            if (cooldownText != null)
                cooldownText.gameObject.SetActive(false);

            if (activeBorder != null)
                activeBorder.gameObject.SetActive(true);
        }
        // ready
        else
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
                cooldownOverlay.gameObject.SetActive(false);
            }

            if (cooldownText != null)
                cooldownText.gameObject.SetActive(false);

            if (activeBorder != null)
                activeBorder.gameObject.SetActive(false);
        }
    }
}