using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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
        }

        IgnorePartyCollisions();
        SetLeader(0, true);
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
        if (targetIndex < 0 || targetIndex >= partyMembers.Count) return;
        if (targetIndex == activeLeaderIndex) return;

        PartyMember target = partyMembers[targetIndex];

        // block swap if character is locked, dead, or on switch cooldown
        if (!target.isUnlocked || target.isDead || target.switchCooldownTimer > 0f) return;

        SetLeader(targetIndex, false);
    }

    private void SetLeader(int newIndex, bool isInitialSetup)
    {
        int oldIndex = activeLeaderIndex;
        Vector3 switchPos = Vector3.zero;

        // 1. clean up previous leader and apply switch cooldown to them
        if (ActiveMember != null && ActiveMember.spawnedInstance != null)
        {
            // revert inverted gravity if active on old leader
            if (ActiveMember.playerController != null && ActiveMember.playerController.IsGravityInverted)
            {
                ActiveMember.playerController.SetGravityInverted(false);

                // restore camera roll back to 0
                CinemachineCamera cam = FindFirstObjectByType<CinemachineCamera>();
                if (cam != null) cam.Lens.Dutch = 0f;
            }

            switchPos = ActiveMember.spawnedInstance.transform.position;

            if (!isInitialSetup)
            {
                ActiveMember.switchCooldownTimer = switchCooldownDuration;
            }

            if (ActiveMember.spawnedInstance.TryGetComponent<Rigidbody2D>(out var oldRb))
            {
                oldRb.linearVelocity = Vector2.zero;
            }

            if (ActiveMember.playerController != null)
            {
                ActiveMember.playerController.animator?.Play("Idle", 0, 0f);
                ActiveMember.playerController.enabled = false;
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
            }

            newLeader.spawnedInstance.SetActive(true);
            newLeader.spawnedInstance.tag = "Player";

            if (newLeader.playerController != null)
            {
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

        int nextAliveIndex = -1;
        for (int i = 0; i < partyMembers.Count; i++)
        {
            if (partyMembers[i].isUnlocked && !partyMembers[i].isDead)
            {
                nextAliveIndex = i;
                break;
            }
        }

        if (nextAliveIndex != -1)
        {
            SetLeader(nextAliveIndex, false);
        }
        else
        {
            GameUIManager.Instance?.TriggerGameOver();
        }

        OnPartyUpdated?.Invoke();
    }

    public void ReviveAllDead(float healthPercent = 0.5f)
    {
        Vector3 respawnPos = ActivePlayerObj != null ? ActivePlayerObj.transform.position : transform.position;

        foreach (var member in partyMembers)
        {
            if (member.isUnlocked && member.isDead)
            {
                member.isDead = false;
                member.currentHealth = Mathf.RoundToInt(member.data.maxHealth * healthPercent);

                if (member.spawnedInstance != null)
                {
                    member.spawnedInstance.transform.position = respawnPos;
                    member.spawnedInstance.SetActive(true);
                    member.followerAI.enabled = true;
                    member.playerController.enabled = false;
                }
            }
        }

        IgnorePartyCollisions();
        UpdateFollowerSpacing();
        UpdateSortingOrders();
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