using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyPanelUI : MonoBehaviour
{
    [System.Serializable]
    public class HealthBannerUI
    {
        public Button bannerButton;
        public Image healthBarFill;     // vertical fill image
        public Image characterIcon;
    }

    [Header("left side - character display")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private GameObject[] characterDisplays; // 0 = Cool, 1 = Barbara, 2 = Android
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;

    [Header("right side - health banners")]
    [SerializeField] private HealthBannerUI[] healthBanners = new HealthBannerUI[3];

    [Header("right side - abilities")]
    [SerializeField] private Image[] abilityIcons = new Image[3]; // icons for Q, E, R buttons

    [Header("right side - description box")]
    [SerializeField] private TextMeshProUGUI abilityNameHeaderText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("top bar")]
    [SerializeField] private Button closeButton;

    private int inspectedCharacterIndex = 0;

    private void Awake()
    {
        // setup next and back arrow buttons
        if (nextButton != null) nextButton.onClick.AddListener(CycleNext);
        if (backButton != null) backButton.onClick.AddListener(CycleBack);

        // setup health banner buttons to select characters
        for (int i = 0; i < healthBanners.Length; i++)
        {
            int index = i;
            if (healthBanners[i].bannerButton != null)
            {
                healthBanners[i].bannerButton.onClick.AddListener(() => SelectCharacter(index));
            }
        }

        // setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                if (PauseMenuManager.Instance != null)
                {
                    PauseMenuManager.Instance.ResumeGame();
                }
            });
        }
    }

    private void OnEnable()
    {
        // default to active leader when party menu opens
        if (PartyManager.Instance != null && PartyManager.Instance.ActiveMember != null)
        {
            inspectedCharacterIndex = PartyManager.Instance.ActiveMember.data.characterIndex;
        }

        RefreshAllHealthBanners();
        SelectCharacter(inspectedCharacterIndex);
    }

    public void SelectCharacter(int index)
    {
        if (PartyManager.Instance == null || index < 0 || index >= PartyManager.Instance.partyMembers.Count) return;

        inspectedCharacterIndex = index;
        var member = PartyManager.Instance.partyMembers[index];
        if (member == null || member.data == null) return;

        // 1. switch in-game leader if unlocked and alive
        if (member.isUnlocked && !member.isDead)
        {
            PartyManager.Instance.SwitchToCharacter(index);
        }

        // 2. update left side character name
        if (characterNameText != null)
        {
            characterNameText.text = member.data.characterName.ToUpper();
        }

        // 3. toggle left side illustration and pixel art
        for (int i = 0; i < characterDisplays.Length; i++)
        {
            if (characterDisplays[i] != null)
            {
                characterDisplays[i].SetActive(i == inspectedCharacterIndex);
            }
        }

        // 4. update ability button icons for this character
        if (abilityIcons.Length > 0 && abilityIcons[0] != null)
            abilityIcons[0].sprite = member.data.abilityQ != null ? member.data.abilityQ.icon : null;

        if (abilityIcons.Length > 1 && abilityIcons[1] != null)
            abilityIcons[1].sprite = member.data.abilityE != null ? member.data.abilityE.icon : null;

        if (abilityIcons.Length > 2 && abilityIcons[2] != null)
            abilityIcons[2].sprite = member.data.abilityR != null ? member.data.abilityR.icon : null;

        // 5. default description box to first ability Q right away
        DisplayAbilityInfo(0);

        RefreshAllHealthBanners();
    }

    public void DisplayAbilityInfo(int abilitySlot)
    {
        if (PartyManager.Instance == null) return;

        var member = PartyManager.Instance.partyMembers[inspectedCharacterIndex];
        if (member == null || member.data == null) return;

        AbilityData selectedAbility = null;
        if (abilitySlot == 0) selectedAbility = member.data.abilityQ;
        else if (abilitySlot == 1) selectedAbility = member.data.abilityE;
        else if (abilitySlot == 2) selectedAbility = member.data.abilityR;

        if (selectedAbility != null)
        {
            if (abilityNameHeaderText != null) abilityNameHeaderText.text = selectedAbility.abilityName;
            if (descriptionText != null) descriptionText.text = selectedAbility.abilityDescription;
        }
        else
        {
            if (abilityNameHeaderText != null) abilityNameHeaderText.text = "No Ability";
            if (descriptionText != null) descriptionText.text = "No description available.";
        }
    }

    public void RefreshAllHealthBanners()
    {
        if (PartyManager.Instance == null) return;

        var members = PartyManager.Instance.partyMembers;

        for (int i = 0; i < healthBanners.Length && i < members.Count; i++)
        {
            var banner = healthBanners[i];
            var member = members[i];

            if (banner.characterIcon != null && member.data != null)
            {
                banner.characterIcon.sprite = member.data.portraitIcon;
            }

            if (banner.healthBarFill != null && member.data != null)
            {
                // vertical fill amount (0 to 1)
                float fill = member.isUnlocked && !member.isDead ? (float)member.currentHealth / member.data.maxHealth : 0f;
                banner.healthBarFill.fillAmount = Mathf.Clamp01(fill);
            }
        }
    }

    private void CycleNext()
    {
        int total = characterDisplays.Length;
        int nextIndex = (inspectedCharacterIndex + 1) % total;
        SelectCharacter(nextIndex);
    }

    private void CycleBack()
    {
        int total = characterDisplays.Length;
        int prevIndex = (inspectedCharacterIndex - 1 + total) % total;
        SelectCharacter(prevIndex);
    }
}