using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Party/Ability Data")]
public class AbilityData : ScriptableObject
{
    public string abilityName = "Ability Name";
    [TextArea(3, 6)]
    public string abilityDescription = "Ability description details go here...";
    public Sprite icon;
    public float cooldownTime = 5.0f;
    public float activeDuration = 0f; // 0 for instant skills
    public AudioClip activationSound;
    public GameObject vfxPrefab;
    public AbilityEffect effectLogic;
}