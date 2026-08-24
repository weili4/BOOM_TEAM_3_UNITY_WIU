using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    public static event System.Action<int, int> OnLeaderSwapped;
    public static event System.Action OnPartyUpdated;

    [Header("Character Roster (0 = Cool, 1 = Barbara, 2 = Android)")]
    public List<PartyMember> partyMembers = new List<PartyMember>(3);

    [Header("Current Active Leader Index")]
    [SerializeField] private int activeLeaderIndex = 0;

    [Header("Switch Cooldown")]
    [SerializeField] private float switchCooldownDuration = 2.5f;
    public float SwitchCooldownDuration => switchCooldownDuration;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera cinemachineCam;

    public PartyMember ActiveMember => (partyMembers != null && partyMembers.Count > activeLeaderIndex) ? partyMembers[activeLeaderIndex] : null;
    public GameObject ActivePlayerObj => ActiveMember != null ? ActiveMember.spawnedInstance : null;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // re-find the new scene's cinemachine camera and link active leader immediately
        cinemachineCam = FindFirstObjectByType<CinemachineCamera>();

        if (ActivePlayerObj != null)
        {
            UpdateCameraTarget(ActivePlayerObj.transform);
        }

        UpdateFollowerSpacing();
        UpdateSortingOrders();
    }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeParty();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeParty()
    {
        Vector3 spawnPos = transform.position;

        for (int i = 0; i < partyMembers.Count; i++)
        {
            var member = partyMembers[i];
            if (i == 0) member.isUnlocked = true;

            member.Initialize(transform, spawnPos);

            if (!member.isUnlocked)
            {
                member.spawnedInstance.SetActive(false);
            }
            else
            {
                // make sure only index activeLeaderIndex is player, others are strictly followers
                if (i == activeLeaderIndex)
                {
                    member.spawnedInstance.SetActive(true);
                    member.spawnedInstance.tag = "Player";
                    if (member.playerController != null) member.playerController.enabled = true;
                    if (member.followerAI != null) member.followerAI.enabled = false;
                }
                else
                {
                    member.spawnedInstance.SetActive(true);
                    member.spawnedInstance.tag = "Ally";
                    if (member.playerController != null) member.playerController.enabled = false;
                    if (member.followerAI != null)
                    {
                        member.followerAI.enabled = true;
                        member.followerAI.SetLeader(partyMembers[activeLeaderIndex].spawnedInstance.transform);
                    }
                }
            }
        }

        IgnorePartyCollisions();
        UpdateFollowerSpacing();
        UpdateSortingOrders();
    }


    private void IgnorePartyCollisions()
    {
        for (int i = 0; i < partyMembers.Count; i++)
        {
            if (partyMembers[i].spawnedInstance == null) continue;
            Collider2D colA = partyMembers[i].spawnedInstance.GetComponent<Collider2D>();

            for (int j = i + 1; j < partyMembers.Count; j++)
            {
                if (partyMembers[j].spawnedInstance == null) continue;
                Collider2D colB = partyMembers[j].spawnedInstance.GetComponent<Collider2D>();

                if (colA != null && colB != null)
                {
                    Physics2D.IgnoreCollision(colA, colB, true);
                }
            }
        }
    }

    private void Update()
    {
        // 1. tick background cooldowns
        foreach (var member in partyMembers)
        {
            if (member.isUnlocked)
            {
                member.TickCooldowns(Time.deltaTime);
            }
        }

        // 2. check inputs
        CheckSwitchInput();
        CheckDebugInput();
    }

    private void CheckSwitchInput()
    {
        // block switching during pause, cutscenes, or game over / win
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsCinematicActive) return;
        if (EndGameUIManager.Instance != null && EndGameUIManager.Instance.IsEndGameActive) return;

        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SwitchToCharacter(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SwitchToCharacter(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SwitchToCharacter(2);
    }


    private void CheckDebugInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f1Key.wasPressedThisFrame) UnlockCharacter(0);
        if (Keyboard.current.f2Key.wasPressedThisFrame) UnlockCharacter(1);
        if (Keyboard.current.f3Key.wasPressedThisFrame) UnlockCharacter(2);
        if (Keyboard.current.f4Key.wasPressedThisFrame) ReviveAllDead(1.0f);
    }

    public void SwitchToCharacter(int targetIndex)
    {
        // block switching if a cinematic cutscene is playing
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsCinematicActive) return;

        if (targetIndex < 0 || targetIndex >= partyMembers.Count) return;
        if (targetIndex == activeLeaderIndex) return;

        PartyMember target = partyMembers[targetIndex];
        if (!target.isUnlocked || target.isDead || target.switchCooldownTimer > 0f) return;

        SetLeader(targetIndex, false);
    }

    private void SetLeader(int newIndex, bool isInitialSetup)
    {
        int oldIndex = activeLeaderIndex;
        Vector3 switchPos = Vector3.zero;

        // 1. clean up previous leader
        if (ActiveMember != null && ActiveMember.spawnedInstance != null)
        {
            switchPos = ActiveMember.spawnedInstance.transform.position;

            if (!isInitialSetup)
            {
                ActiveMember.switchCooldownTimer = switchCooldownDuration;
            }

            // deactivate any running active abilities on the old leader (e.g. invert gravity, summons)
            if (ActiveMember.data != null)
            {
                if (ActiveMember.data.abilityQ != null && ActiveMember.data.abilityQ.effectLogic != null)
                    ActiveMember.data.abilityQ.effectLogic.Deactivate(ActiveMember.spawnedInstance);
                if (ActiveMember.data.abilityE != null && ActiveMember.data.abilityE.effectLogic != null)
                    ActiveMember.data.abilityE.effectLogic.Deactivate(ActiveMember.spawnedInstance);
                if (ActiveMember.data.abilityR != null && ActiveMember.data.abilityR.effectLogic != null)
                    ActiveMember.data.abilityR.effectLogic.Deactivate(ActiveMember.spawnedInstance);
            }

            // restore normal downward gravity on old leader
            if (ActiveMember.playerController != null)
            {
                ActiveMember.playerController.gravityDirection = Vector2.down;
                ActiveMember.playerController.ClearForcedVelocity();
                ActiveMember.playerController.enabled = false;
            }

            // make sure sprite is right-side up
            Vector3 s = ActiveMember.spawnedInstance.transform.localScale;
            ActiveMember.spawnedInstance.transform.localScale = new Vector3(s.x, 2f, 2f);

            // restore cinemachine camera roll back to 0
            CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
            if (cam != null)
            {
                cam.Lens.Dutch = 0f;
            }

            if (ActiveMember.spawnedInstance.TryGetComponent<Rigidbody2D>(out var oldRb))
            {
                oldRb.linearVelocity = Vector2.zero;
                oldRb.gravityScale = Mathf.Abs(oldRb.gravityScale); // enforce positive gravity
            }

            if (ActiveMember.spawnedInstance.TryGetComponent<AttackEventHandler>(out var attackHandler))
            {
                attackHandler.AttackEnd();
            }

            ActiveMember.spawnedInstance.tag = "Ally";
            ActiveMember.followerAI.enabled = true;
        }

        activeLeaderIndex = newIndex;
        PartyMember newLeader = ActiveMember;

        // 2. setup new leader
        if (newLeader != null && newLeader.spawnedInstance != null)
        {
            if (!isInitialSetup && switchPos != Vector3.zero)
            {
                newLeader.spawnedInstance.transform.position = switchPos;
            }

            if (newLeader.spawnedInstance.TryGetComponent<Rigidbody2D>(out var newRb))
            {
                newRb.linearVelocity = Vector2.zero;
                newRb.gravityScale = Mathf.Abs(newRb.gravityScale);
            }

            newLeader.spawnedInstance.SetActive(true);
            newLeader.spawnedInstance.tag = "Player";

            if (newLeader.playerController != null)
            {
                newLeader.playerController.gravityDirection = Vector2.down;
                newLeader.playerController.enabled = true;
            }

            newLeader.followerAI.enabled = false;

            if (newLeader.data.switchInSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(newLeader.data.switchInSound, newLeader.spawnedInstance.transform.position);
            }

            if (newLeader.data.switchVFXPrefab != null)
            {
                Instantiate(newLeader.data.switchVFXPrefab, newLeader.spawnedInstance.transform.position, Quaternion.identity);
            }

            UpdateCameraTarget(newLeader.spawnedInstance.transform);
            UpdateFollowerSpacing();
            UpdateSortingOrders();

            if (!isInitialSetup && oldIndex != newIndex)
            {
                OnLeaderSwapped?.Invoke(oldIndex, newIndex);
            }

            OnPartyUpdated?.Invoke();
        }
    }

    private void UpdateSortingOrders()
    {
        for (int i = 0; i < partyMembers.Count; i++)
        {
            var member = partyMembers[i];
            if (member.spawnedInstance == null) continue;

            var renderers = member.spawnedInstance.GetComponentsInChildren<SpriteRenderer>();
            int order = (i == activeLeaderIndex) ? 10 : 5;

            foreach (var sr in renderers)
            {
                if (sr != null) sr.sortingOrder = order;
            }
        }
    }

    private void UpdateCameraTarget(Transform newTarget)
    {
        if (cinemachineCam == null)
            cinemachineCam = FindFirstObjectByType<CinemachineCamera>();

        if (cinemachineCam != null)
        {
            cinemachineCam.Target.TrackingTarget = newTarget;
        }
    }

    private void UpdateFollowerSpacing()
    {
        int followerRank = 1;
        for (int i = 0; i < partyMembers.Count; i++)
        {
            if (i == activeLeaderIndex) continue;

            PartyMember member = partyMembers[i];
            if (member.isUnlocked && !member.isDead && member.followerAI != null)
            {
                member.followerAI.stopFollowDistance = followerRank * 1.2f;
                member.followerAI.startFollowDistance = member.followerAI.stopFollowDistance + 1.0f;
                member.followerAI.SetLeader(ActivePlayerObj.transform);
                followerRank++;
            }
        }
    }

    public void UnlockCharacter(int index)
    {
        if (index < 0 || index >= partyMembers.Count) return;

        PartyMember member = partyMembers[index];
        if (member.isUnlocked) return;

        member.isUnlocked = true;
        member.isDead = false;
        member.currentHealth = member.data.maxHealth;

        if (member.spawnedInstance != null)
        {
            member.spawnedInstance.transform.position = ActivePlayerObj != null ? ActivePlayerObj.transform.position : transform.position;
            member.spawnedInstance.SetActive(true);
            member.spawnedInstance.tag = "Ally";

            if (member.followerAI != null && ActivePlayerObj != null)
            {
                member.followerAI.SetLeader(ActivePlayerObj.transform);
            }

            member.followerAI.enabled = true;
            member.playerController.enabled = false;
        }

        IgnorePartyCollisions();
        UpdateFollowerSpacing();
        UpdateSortingOrders();
        OnPartyUpdated?.Invoke();
    }

    public void OnLeaderDied()
    {
        if (ActiveMember == null) return;

        ActiveMember.isDead = true;
        if (ActiveMember.spawnedInstance != null)
        {
            ActiveMember.spawnedInstance.SetActive(false);
        }

        // find next alive party member
        int nextAliveIndex = -1;
        for (int i = 0; i < partyMembers.Count; i++)
        {
            if (partyMembers[i].isUnlocked && !partyMembers[i].isDead)
            {
                nextAliveIndex = i;
                break;
            }
        }

        // auto-swap to next living character
        if (nextAliveIndex != -1)
        {
            SetLeader(nextAliveIndex, false);
        }
        else
        {
            // only trigger lose screen when all characters are dead
            EndGameUIManager.Instance?.TriggerLose();
        }

        OnPartyUpdated?.Invoke();
    }


    public void ReviveAllDead(float healthPercent = 1.0f)
    {
        // safe respawn location
        Vector3 respawnPos = transform.position;
        if (ChunkManager.CurrentSpawnPoint != null)
        {
            respawnPos = ChunkManager.CurrentSpawnPoint.position;
        }
        else if (ActivePlayerObj != null)
        {
            respawnPos = ActivePlayerObj.transform.position;
        }

        // keep the last character that died as the revived leader
        int leaderIndexToRevive = activeLeaderIndex;
        if (leaderIndexToRevive < 0 || leaderIndexToRevive >= partyMembers.Count || !partyMembers[leaderIndexToRevive].isUnlocked)
        {
            leaderIndexToRevive = 0; // fallback to first character if invalid
        }

        for (int i = 0; i < partyMembers.Count; i++)
        {
            var member = partyMembers[i];
            if (member.isUnlocked)
            {
                member.isDead = false;
                member.currentHealth = member.data.maxHealth;

                if (member.spawnedInstance != null)
                {
                    member.spawnedInstance.transform.position = respawnPos;
                    member.spawnedInstance.SetActive(true);

                    // heal damageable component so it does not re-trigger death
                    if (member.damageable != null)
                    {
                        member.damageable.Heal(member.damageable.MaxHealth);
                    }

                    if (member.spawnedInstance.TryGetComponent<Rigidbody2D>(out var rb))
                    {
                        rb.linearVelocity = Vector2.zero;
                    }

                    // assign the last person that died as the active player leader
                    if (i == leaderIndexToRevive)
                    {
                        member.spawnedInstance.tag = "Player";
                        if (member.playerController != null)
                        {
                            member.playerController.enabled = true;
                            member.playerController.isInputLocked = false;
                        }
                        if (member.followerAI != null)
                        {
                            member.followerAI.enabled = false;
                        }
                    }
                    // assign other living members as followers
                    else
                    {
                        member.spawnedInstance.tag = "Ally";
                        if (member.playerController != null)
                        {
                            member.playerController.enabled = false;
                        }
                        if (member.followerAI != null)
                        {
                            member.followerAI.enabled = true;
                            member.followerAI.SetLeader(partyMembers[leaderIndexToRevive].spawnedInstance.transform);
                        }
                    }
                }
            }
        }

        activeLeaderIndex = leaderIndexToRevive;

        // update camera tracking target to the revived leader immediately
        if (partyMembers[leaderIndexToRevive].spawnedInstance != null)
        {
            UpdateCameraTarget(partyMembers[leaderIndexToRevive].spawnedInstance.transform);
        }

        IgnorePartyCollisions();
        UpdateFollowerSpacing();
        UpdateSortingOrders();
        PartyHUD.Instance?.RefreshHUD();
        OnPartyUpdated?.Invoke();
    }

    public void TeleportEntireParty(Vector3 position)
    {
        foreach (var member in partyMembers)
        {
            if (member.isUnlocked && !member.isDead && member.spawnedInstance != null)
            {
                member.spawnedInstance.transform.position = position;
                if (member.spawnedInstance.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
    }
}