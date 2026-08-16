using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image cooldownBarImage; // progress bar
    [SerializeField] private GameObject selectionHighlight;

    public void SetupSlot(Sprite icon, int count)
    {
        if (iconImage != null && icon != null) iconImage.sprite = icon;
        if (countText != null) countText.text = count + "x";
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(isSelected);
        }
    }

    public void UpdateCooldownBar(float currentCd, float maxCd)
    {
        if (cooldownBarImage == null) return;

        if (currentCd > 0 && maxCd > 0)
        {
            cooldownBarImage.gameObject.SetActive(true);
            cooldownBarImage.fillAmount = Mathf.Clamp01(currentCd / maxCd);
        }
        else
        {
            cooldownBarImage.fillAmount = 0f;
            cooldownBarImage.gameObject.SetActive(false);
        }
    }
}