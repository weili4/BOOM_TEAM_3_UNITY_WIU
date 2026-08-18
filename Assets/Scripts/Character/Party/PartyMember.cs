using UnityEngine;

[System.Serializable]
public class PartyMember
{
    public CharacterData data;
    public bool isUnlocked = false;
    public bool isDead = false;
    public int currentHealth = 100;

    // switch cooldown timer
    [HideInInspector] public float switchCooldownTimer = 0f;

    [HideInInspector] public float cooldownQ = 0f;
    [HideInInspector] public float cooldownE = 0f;
    [HideInInspector] public float cooldownR = 0f;

    [HideInInspector] public float activeTimerQ = 0f;
    [HideInInspector] public float activeTimerE = 0f;
    [HideInInspector] public float activeTimerR = 0f;

    [HideInInspector] public GameObject spawnedInstance;
    [HideInInspector] public PlayerController playerController;
    [HideInInspector] public FollowerAI followerAI;
    [HideInInspector] public Damageable damageable;
    [HideInInspector] public AbilityHolder abilityHolder;

    public void Initialize(Transform parent, Vector3 spawnPos)
    {
        if (data == null || data.characterPrefab == null) return;

        spawnedInstance = Object.Instantiate(data.characterPrefab, spawnPos, Quaternion.identity, parent);
        spawnedInstance.name = data.characterName;

        playerController = spawnedInstance.GetComponent<PlayerController>();
        followerAI = spawnedInstance.GetComponent<FollowerAI>();
        damageable = spawnedInstance.GetComponent<Damageable>();
        abilityHolder = spawnedInstance.GetComponent<AbilityHolder>();

        currentHealth = data.maxHealth;

        if (playerController != null)
        {
            playerController.moveSpeed = data.moveSpeed;
            playerController.jumpHeight = data.jumpHeight;
            playerController.maxJumps = data.maxJumps;
        }

        if (damageable != null)
        {
            damageable.onHealthChanged.AddListener(OnTakeDamage);
        }

        if (followerAI == null)
        {
            followerAI = spawnedInstance.AddComponent<FollowerAI>();
        }

        if (abilityHolder == null)
        {
            abilityHolder = spawnedInstance.AddComponent<AbilityHolder>();
        }

        abilityHolder.SetupAbilities(data.abilityQ, data.abilityE, data.abilityR);
    }

    private void OnTakeDamage(int current, int max)
    {
        currentHealth = current;
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            PartyManager.Instance?.OnLeaderDied();
        }
        PartyHUD.Instance?.RefreshHUD();
    }

    public void TickCooldowns(float deltaTime)
    {
        // tick down switch cooldown
        if (switchCooldownTimer > 0) switchCooldownTimer -= deltaTime;

        if (cooldownQ > 0) cooldownQ -= deltaTime;
        if (cooldownE > 0) cooldownE -= deltaTime;
        if (cooldownR > 0) cooldownR -= deltaTime;

        if (activeTimerQ > 0)
        {
            activeTimerQ -= deltaTime;
            if (activeTimerQ <= 0 && data.abilityQ != null && data.abilityQ.effectLogic != null)
                data.abilityQ.effectLogic.Deactivate(spawnedInstance);
        }

        if (activeTimerE > 0)
        {
            activeTimerE -= deltaTime;
            if (activeTimerE <= 0 && data.abilityE != null && data.abilityE.effectLogic != null)
                data.abilityE.effectLogic.Deactivate(spawnedInstance);
        }

        if (activeTimerR > 0)
        {
            activeTimerR -= deltaTime;
            if (activeTimerR <= 0 && data.abilityR != null && data.abilityR.effectLogic != null)
                data.abilityR.effectLogic.Deactivate(spawnedInstance);
        }
    }
}