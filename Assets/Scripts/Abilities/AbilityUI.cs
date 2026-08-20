using System.Collections;
using UnityEngine;

public class AbilityUI : MonoBehaviour
{
    [Header("slots for Q, E, R in order")]
    [SerializeField] private AbilitySlotUI slotQ; // AbilitySlot_1
    [SerializeField] private AbilitySlotUI slotE; // AbilitySlot_2
    [SerializeField] private AbilitySlotUI slotR; // AbilitySlot_3

    [Header("circular arc animation settings")]
    [SerializeField] private float exitDuration = 0.12f;   // fast drop down
    [SerializeField] private float entryDuration = 0.16f;  // smooth curve in
    [SerializeField] private float exitDistanceY = 180f;   // how far it drops down off-screen
    [SerializeField] private float entryDistanceX = 260f;  // how far it starts from the right off-screen
    [SerializeField] private float arcCurveIntensity = 40f; // curved path offset

    private RectTransform panelRect;
    private Vector2 homeAnchoredPosition;
    private Coroutine switchRoutine;
    private PartyMember currentlyDisplayedMember;
    private bool isInitialized = false;

    private void Awake()
    {
        panelRect = GetComponent<RectTransform>();
        if (panelRect != null)
        {
            homeAnchoredPosition = panelRect.anchoredPosition;
            isInitialized = true;
        }
    }

    private void OnEnable()
    {
        PartyManager.OnLeaderSwapped += HandleLeaderSwapped;
        PartyManager.OnPartyUpdated += HandlePartyUpdated;
    }

    private void OnDisable()
    {
        PartyManager.OnLeaderSwapped -= HandleLeaderSwapped;
        PartyManager.OnPartyUpdated -= HandlePartyUpdated;

        // snap back to home position if disabled
        if (panelRect != null && isInitialized)
        {
            panelRect.anchoredPosition = homeAnchoredPosition;
        }
    }

    private void Start()
    {
        if (PartyManager.Instance != null && PartyManager.Instance.ActiveMember != null)
        {
            currentlyDisplayedMember = PartyManager.Instance.ActiveMember;
            ApplyMemberVisuals(currentlyDisplayedMember);
        }
    }

    private void HandleLeaderSwapped(int oldLeaderIdx, int newLeaderIdx)
    {
        if (!gameObject.activeInHierarchy || PartyManager.Instance == null) return;

        var newMember = PartyManager.Instance.ActiveMember;
        if (newMember == null) return;

        // interrupt running animation and play new circular swap
        if (switchRoutine != null) StopCoroutine(switchRoutine);
        switchRoutine = StartCoroutine(CircularArcSwapRoutine(newMember));
    }

    private void HandlePartyUpdated()
    {
        // if no animation is running keep visuals synced
        if (switchRoutine == null && PartyManager.Instance != null)
        {
            currentlyDisplayedMember = PartyManager.Instance.ActiveMember;
            ApplyMemberVisuals(currentlyDisplayedMember);
        }
    }

    private IEnumerator CircularArcSwapRoutine(PartyMember newLeader)
    {
        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 bottomExitPos = homeAnchoredPosition + new Vector2(-arcCurveIntensity, -exitDistanceY);

        // 1. exit downward arc (still showing old character abilities)
        float elapsed = 0f;
        while (elapsed < exitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / exitDuration);

            // fast ease-in curve downward
            float easeIn = t * t;

            // arc curve outward to the left as it drops
            float curveOffset = Mathf.Sin(t * Mathf.PI) * arcCurveIntensity;

            Vector2 currentPos = Vector2.Lerp(startPos, bottomExitPos, easeIn);
            currentPos.x -= curveOffset;
            panelRect.anchoredPosition = currentPos;

            yield return null;
        }

        // 2. midpoint off-screen swap
        // update data to new character while completely hidden
        currentlyDisplayedMember = newLeader;
        ApplyMemberVisuals(currentlyDisplayedMember);

        // teleport to right off-screen entry position
        Vector2 rightEntryPos = homeAnchoredPosition + new Vector2(entryDistanceX, -arcCurveIntensity);
        panelRect.anchoredPosition = rightEntryPos;

        // 3. entry right arc (curving up and into home position)
        elapsed = 0f;
        while (elapsed < entryDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / entryDuration);

            // smooth ease-out landing
            float easeOut = Mathf.Sin(t * (Mathf.PI * 0.5f));

            // arc curve upward as it slides left
            float curveOffset = Mathf.Sin(t * Mathf.PI) * (arcCurveIntensity * 0.6f);

            Vector2 currentPos = Vector2.Lerp(rightEntryPos, homeAnchoredPosition, easeOut);
            currentPos.y += curveOffset;
            panelRect.anchoredPosition = currentPos;

            yield return null;
        }

        // snap firmly to home position
        panelRect.anchoredPosition = homeAnchoredPosition;
        switchRoutine = null;
    }

    private void ApplyMemberVisuals(PartyMember member)
    {
        if (member == null || member.data == null) return;

        if (slotQ != null) slotQ.SetupSlot(member.data.abilityQ != null ? member.data.abilityQ.icon : null, "Q");
        if (slotE != null) slotE.SetupSlot(member.data.abilityE != null ? member.data.abilityE.icon : null, "E");
        if (slotR != null) slotR.SetupSlot(member.data.abilityR != null ? member.data.abilityR.icon : null, "R");
    }

    private void Update()
    {
        // update cooldown and active fills for whichever member is currently displayed
        if (currentlyDisplayedMember == null || currentlyDisplayedMember.data == null) return;

        if (slotQ != null && currentlyDisplayedMember.data.abilityQ != null)
        {
            slotQ.UpdateSlot(
                currentlyDisplayedMember.cooldownQ,
                currentlyDisplayedMember.data.abilityQ.cooldownTime,
                currentlyDisplayedMember.activeTimerQ,
                currentlyDisplayedMember.data.abilityQ.activeDuration
            );
        }

        if (slotE != null && currentlyDisplayedMember.data.abilityE != null)
        {
            slotE.UpdateSlot(
                currentlyDisplayedMember.cooldownE,
                currentlyDisplayedMember.data.abilityE.cooldownTime,
                currentlyDisplayedMember.activeTimerE,
                currentlyDisplayedMember.data.abilityE.activeDuration
            );
        }

        if (slotR != null && currentlyDisplayedMember.data.abilityR != null)
        {
            slotR.UpdateSlot(
                currentlyDisplayedMember.cooldownR,
                currentlyDisplayedMember.data.abilityR.cooldownTime,
                currentlyDisplayedMember.activeTimerR,
                currentlyDisplayedMember.data.abilityR.activeDuration
            );
        }
    }
}