using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Scriptable Objects/AbilityData")]
public class AbilityData : ScriptableObject
{
    public string abilityName;
    public Sprite icon;
    public KeyCode keybind;
    public float cooldownTime;
    public float activeDuration;
    public AudioClip activationSound;
    public GameObject vfxPrefab; // is optional
}