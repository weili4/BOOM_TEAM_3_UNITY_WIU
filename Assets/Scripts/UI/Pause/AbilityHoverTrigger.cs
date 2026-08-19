using UnityEngine;
using UnityEngine.EventSystems;

public class AbilityHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private int abilityIndex = 0; // 0 = Q, 1 = E, 2 = R
    private PartyPanelUI partyPanel;

    private void Awake()
    {
        partyPanel = GetComponentInParent<PartyPanelUI>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // update description when hovered
        if (partyPanel != null)
        {
            partyPanel.DisplayAbilityInfo(abilityIndex);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // keep description displayed when clicked
        if (partyPanel != null)
        {
            partyPanel.DisplayAbilityInfo(abilityIndex);
        }
    }
}