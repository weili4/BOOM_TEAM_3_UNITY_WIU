using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Party/Ability Data")]
public class AbilityData : ScriptableObject
{
    public string abilityName;
    public Sprite icon;
    public float cooldownTime = 5.0f;
    public float activeDuration = 0f; // 0 for instant skills (e.g. dash/slam)
    public AudioClip activationSound;
    public GameObject vfxPrefab;
    public AbilityEffect effectLogic;
}