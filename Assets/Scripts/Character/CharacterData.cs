using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Party/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string characterName = "Character";
    public int characterIndex = 0; // 0 = Cool, 1 = Barbara, 2 = Android
    public Sprite portraitIcon;
    public GameObject characterPrefab; // The in-game GameObject/Prefab

    [Header("Base Stats")]
    public int maxHealth = 100;
    public float moveSpeed = 6.0f;
    public float jumpHeight = 3.5f;
    public int maxJumps = 2; // 2 for Cool (double jump), 1 for others
    public int baseAttackDamage = 10;

    [Header("Visuals & Animations")]
    public RuntimeAnimatorController animatorController;

    [Header("Audio & VFX")]
    public AudioClip switchInSound;
    public AudioClip jumpSound;
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip deathSound;
    public GameObject switchVFXPrefab;
    public GameObject attackVFXPrefab;

    [Header("Abilities (Q, E, R)")]
    public AbilityData abilityQ;
    public AbilityData abilityE;
    public AbilityData abilityR;
}