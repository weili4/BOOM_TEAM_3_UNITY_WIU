using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActiveBuffWidget : MonoBehaviour
{
    [Header("ui references")]
    [SerializeField] private Image abilityIconImage;
    [SerializeField] private Image characterPortraitBadge; // optional tiny portrait of who owns the buff
    [SerializeField] private Image radialFillImage;         // radial 360 fill image
    [SerializeField] private TextMeshProUGUI durationText;  // optional countdown text

    private PartyMember ownerMember;
    private AbilityData trackedAbility;
    private int abilitySlotIndex = 0; // 0 = Q, 1 = E, 2 = R
    private float maxDuration = 1f;

    public string BuffKey => $"{ownerMember?.data?.characterName}_{abilitySlotIndex}";

    public void SetupBuff(PartyMember member, AbilityData ability, int slotIndex)
    {
        ownerMember = member;
        trackedAbility = ability;
        abilitySlotIndex = slotIndex;
        maxDuration = Mathf.Max(0.1f, ability.activeDuration);

        if (abilityIconImage != null && ability.icon != null)
        {
            abilityIconImage.sprite = ability.icon;
        }

        if (characterPortraitBadge != null && member.data != null)
        {
            characterPortraitBadge.sprite = member.data.portraitIcon;
            characterPortraitBadge.gameObject.SetActive(true);
        }

        if (radialFillImage != null)
        {
            radialFillImage.fillAmount = 1f;
        }

        UpdateVisuals();
    }

    private void Update()
    {
        if (ownerMember == null || trackedAbility == null)
        {
            Destroy(gameObject);
            return;
        }

        float currentTimer = GetCurrentActiveTimer();

        // destroy when duration expires
        if (currentTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        UpdateVisuals();
    }

    private float GetCurrentActiveTimer()
    {
        if (abilitySlotIndex == 0) return ownerMember.activeTimerQ;
        if (abilitySlotIndex == 1) return ownerMember.activeTimerE;
        if (abilitySlotIndex == 2) return ownerMember.activeTimerR;
        return 0f;
    }

    private void UpdateVisuals()
    {
        float currentTimer = GetCurrentActiveTimer();
        float progress = Mathf.Clamp01(currentTimer / maxDuration);

        // drain radial fill downwards
        if (radialFillImage != null)
        {
            radialFillImage.fillAmount = progress;
        }

        if (durationText != null)
        {
            durationText.text = currentTimer.ToString("F1") + "s";
        }
    }
}