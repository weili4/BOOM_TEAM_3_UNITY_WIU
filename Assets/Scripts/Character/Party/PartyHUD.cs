using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyHUD : MonoBehaviour
{
    public static PartyHUD Instance { get; private set; }

    [System.Serializable]
    public class CharacterCardUI
    {
        public RectTransform cardRect;
        public Image portrait;
        public Image healthBarFill;
        public Image cooldownRadialFill; // radial 360 image
        public GameObject lockedOverlay;
        public GameObject deadOverlay;
    }

    [Header("single shared leader texts")]
    [SerializeField] private TextMeshProUGUI leaderNameText;
    [SerializeField] private TextMeshProUGUI leaderHealthText;

    [Header("character cards (0 = cool [1], 1 = barbara [2], 2 = android [3])")]
    [SerializeField] private CharacterCardUI[] characterCards = new CharacterCardUI[3];

    [Header("scale animation settings")]
    [SerializeField] private float leaderScale = 1.15f;
    [SerializeField] private float followerScale = 0.95f;
    [SerializeField] private float scaleSpeed = 9.0f;

    private int activeLeaderIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        PartyManager.OnLeaderSwapped += HandleLeaderSwapped;
        PartyManager.OnPartyUpdated += RefreshHUD;
    }

    private void OnDisable()
    {
        PartyManager.OnLeaderSwapped -= HandleLeaderSwapped;
        PartyManager.OnPartyUpdated -= RefreshHUD;
    }

    private void Start()
    {
        if (PartyManager.Instance != null && PartyManager.Instance.ActiveMember != null)
        {
            activeLeaderIndex = PartyManager.Instance.ActiveMember.data.characterIndex;
        }

        RefreshHUD();
    }

    private void HandleLeaderSwapped(int oldLeaderIdx, int newLeaderIdx)
    {
        activeLeaderIndex = newLeaderIdx;
        RefreshHUD();
    }

    private void Update()
    {
        if (PartyManager.Instance == null) return;

        var members = PartyManager.Instance.partyMembers;
        float maxCd = PartyManager.Instance.SwitchCooldownDuration;

        for (int i = 0; i < characterCards.Length; i++)
        {
            var card = characterCards[i];
            if (card.cardRect == null) continue;

            // 1. smooth scale animation
            float targetScale = (i == activeLeaderIndex) ? leaderScale : followerScale;
            Vector3 targetVector = Vector3.one * targetScale;

            card.cardRect.localScale = Vector3.Lerp(
                card.cardRect.localScale,
                targetVector,
                Time.unscaledDeltaTime * scaleSpeed
            );

            // 2. update radial cooldown fill in real time
            if (card.cooldownRadialFill != null && i < members.Count)
            {
                var member = members[i];
                if (member.switchCooldownTimer > 0f && maxCd > 0f)
                {
                    card.cooldownRadialFill.gameObject.SetActive(true);
                    card.cooldownRadialFill.fillAmount = Mathf.Clamp01(member.switchCooldownTimer / maxCd);
                }
                else
                {
                    card.cooldownRadialFill.fillAmount = 0f;
                    card.cooldownRadialFill.gameObject.SetActive(false);
                }
            }
        }
    }

    public void RefreshHUD()
    {
        if (PartyManager.Instance == null) return;

        var members = PartyManager.Instance.partyMembers;
        var leader = PartyManager.Instance.ActiveMember;

        // update leader texts
        if (leader != null && leader.data != null)
        {
            if (leaderNameText != null) leaderNameText.text = leader.data.characterName;
            if (leaderHealthText != null) leaderHealthText.text = $"{leader.currentHealth} / {leader.data.maxHealth}";
        }

        // update card visuals
        for (int i = 0; i < characterCards.Length && i < members.Count; i++)
        {
            UpdateCardVisuals(characterCards[i], members[i]);
        }
    }

    private void UpdateCardVisuals(CharacterCardUI card, PartyMember member)
    {
        if (card == null || member == null || member.data == null) return;

        if (card.portrait != null) card.portrait.sprite = member.data.portraitIcon;

        if (!member.isUnlocked)
        {
            if (card.lockedOverlay != null) card.lockedOverlay.SetActive(true);
            if (card.deadOverlay != null) card.deadOverlay.SetActive(false);
            if (card.healthBarFill != null) card.healthBarFill.fillAmount = 0f;
        }
        else if (member.isDead)
        {
            if (card.lockedOverlay != null) card.lockedOverlay.SetActive(false);
            if (card.deadOverlay != null) card.deadOverlay.SetActive(true);
            if (card.healthBarFill != null) card.healthBarFill.fillAmount = 0f;
        }
        else
        {
            if (card.lockedOverlay != null) card.lockedOverlay.SetActive(false);
            if (card.deadOverlay != null) card.deadOverlay.SetActive(false);

            float fill = (float)member.currentHealth / member.data.maxHealth;
            if (card.healthBarFill != null) card.healthBarFill.fillAmount = Mathf.Clamp01(fill);
        }
    }
}