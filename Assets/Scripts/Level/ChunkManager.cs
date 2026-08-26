using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public enum ObjectiveType { ReachGate, KillAllEnemies, WaveCombat, Keycard, Timer }

    [System.Serializable]
    public class Wave
    {
        public List<GameObject> enemyPrefabs;
    }

    [Header("CHUNK CONFIGURATION")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private ObjectiveType objectiveType = ObjectiveType.ReachGate;
    [SerializeField] private Gate exitGate;
    [SerializeField] private Gate entranceGate;
    [SerializeField] private bool isStartingChunk = false;

    [Header("CHUNK MUSIC")]
    [SerializeField] private AudioClip chunkBGM;

    [Header("KILL ALL ENEMIES CONFIG")]
    [SerializeField] private List<Damageable> chunkEnemies = new List<Damageable>();

    [Header("WAVE COMBAT CONFIG")]
    [SerializeField] private List<Transform> arenaSpawnPoints;
    [SerializeField] private List<Wave> waves;
    [SerializeField] private float delayBetweenWaves = 2.0f;

    [Header("TIMER CONFIG")]
    [SerializeField] private float timeLimit = 30.0f;
    [SerializeField] private GameObject timerStartTriggerObject;

    [Header("WAVE COMBAT AUDIO CLIPS")]
    [SerializeField] private AudioClip waveStartSound;
    [SerializeField] private AudioClip enemySpawnSound;
    [SerializeField] private AudioClip waveClearedSound;
    [SerializeField] private AudioClip bossZoneClearedSound;

    private static Transform currentActiveSpawnPoint;
    public static Transform CurrentSpawnPoint => currentActiveSpawnPoint;

    private int totalEnemies;
    private int defeatedEnemies = 0;
    private bool isChunkCleared = false;

    private List<GameObject> activeWaveEnemies = new List<GameObject>();
    private int currentWaveIndex = 0;
    private bool isWaveCombatStarted = false;

    // Timer Variables
    private float currentTimeLeft;
    private bool isTimerRunning = false;
    private bool isTimerFailed = false;
    private Coroutine timerCoroutine;

    // Public getter for GateApproachTrigger
    public ObjectiveType CurrentObjectiveType => objectiveType;

    // IsChunkCleared logic
    public bool IsChunkCleared
    {
        get
        {
            if (objectiveType == ObjectiveType.ReachGate) return true;
            if (objectiveType == ObjectiveType.Keycard)
            {
                Inventory inventory = FindFirstObjectByType<Inventory>();
                bool HasItemToUnlock = false;
                int ItemIndex = -1;

                for (int index = 0; index < inventory.itemStacks.Count; index++)
                {
                    if (inventory.itemStacks[index].itemData.itemName == "Keycard")
                    {
                        HasItemToUnlock = true;
                        ItemIndex = index;
                    }
                }
                if (HasItemToUnlock)
                {
                    inventory.RemoveItem(ItemIndex); 
                    return true;
                }
                return false;

            }
            if (objectiveType == ObjectiveType.Timer) return isChunkCleared || (isTimerRunning && !isTimerFailed);
            return isChunkCleared;
        }
    }

    private IEnumerator Start()
    {
        yield return null;

        // Only force UI update on Start if this is designated as the starting chunk
        if (isStartingChunk)
        {
            ActivateChunk();
        }

        if (objectiveType == ObjectiveType.KillAllEnemies)
        {
            totalEnemies = chunkEnemies.Count;
            foreach (var enemy in chunkEnemies)
            {
                if (enemy != null)
                {
                    enemy.onHealthChanged.AddListener((hp, maxHp) => OnEnemyHealthChanged(enemy, hp));
                }
            }
        }
    }

    public void ActivateChunk()
    {
        if (spawnPoint != null)
        {
            currentActiveSpawnPoint = spawnPoint;
        }

        // if chunk has music, crossfade to it; if null, fade out to silence
        if (chunkBGM != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(chunkBGM, 0.4f);
        }
        else if (chunkBGM == null && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic(0.5f);
        }

        UpdateChunkObjectiveUI();

        if (objectiveType == ObjectiveType.ReachGate && exitGate != null)
        {
            exitGate.OpenGate();
        }

        if (objectiveType == ObjectiveType.WaveCombat && !isWaveCombatStarted)
        {
            isWaveCombatStarted = true;

            if (entranceGate != null)
                entranceGate.SlamCloseGate();

            StartCoroutine(StartNextWaveRoutine());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ActivateChunk();
        }
    }

    // Called by TimerStartTrigger script when player hits the start trigger
    public void StartTimerObjective()
    {
        if (objectiveType != ObjectiveType.Timer || isChunkCleared) return;

        // Reset fail state & open gate
        isTimerFailed = false;

        if (exitGate != null)
        {
            exitGate.OpenGate();
        }

        // Restart timer coroutine
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    // Called when player successfully reaches the gate on time
    public void CompleteChunk()
    {
        isChunkCleared = true;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            isTimerRunning = false;
        }
    }

    private IEnumerator TimerRoutine()
    {
        isTimerRunning = true;
        currentTimeLeft = timeLimit;

        while (currentTimeLeft > 0)
        {
            if (isChunkCleared)
            {
                isTimerRunning = false;
                yield break;
            }

            currentTimeLeft -= Time.deltaTime;

            int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(currentTimeLeft));
            LevelObjectiveUI.Instance?.SetObjectiveText($"Escape before gate locks! Time Left: {secondsLeft}s");

            yield return null;
        }

        // Timer Expired Logic
        currentTimeLeft = 0;
        isTimerRunning = false;
        isTimerFailed = true;

        if (exitGate != null)
        {
            exitGate.SlamCloseGate();
        }

        LevelObjectiveUI.Instance?.SetObjectiveText("Time ran out! Try again from the start!");
    }

    private void OnEnemyHealthChanged(Damageable enemy, int currentHealth)
    {
        if (currentHealth <= 0 && chunkEnemies.Contains(enemy))
        {
            chunkEnemies.Remove(enemy);
            defeatedEnemies++;

            if (objectiveType == ObjectiveType.KillAllEnemies && !isChunkCleared)
            {
                if (defeatedEnemies >= totalEnemies)
                {
                    isChunkCleared = true;
                    if (exitGate != null) exitGate.OpenGate();
                    LevelObjectiveUI.Instance?.SetObjectiveText("Proceed through the Unlocked Gate!");
                }
                else
                {
                    LevelObjectiveUI.Instance?.SetObjectiveText($"Enemies Defeated: {defeatedEnemies} / {totalEnemies}");
                }
            }
        }
    }

    private IEnumerator StartNextWaveRoutine()
    {
        if (currentWaveIndex >= waves.Count)
        {
            isChunkCleared = true;
            LevelObjectiveUI.Instance?.SetObjectiveText("Boss Zone Cleared! Proceed through the Unlocked Gate.");

            if (bossZoneClearedSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(bossZoneClearedSound, transform.position, 1.4f);
            }

            //if (AudioManager.Instance != null)
            //{
            //    AudioManager.Instance.StopMusic(0.4f);
            //}

            if (exitGate != null) exitGate.OpenGate();
            yield break;
        }

        Wave currentWave = waves[currentWaveIndex];
        int waveNum = currentWaveIndex + 1;
        LevelObjectiveUI.Instance?.SetObjectiveText($"Wave {waveNum}/{waves.Count} Starting...");

        if (waveStartSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(waveStartSound, transform.position, 1.2f);
        }

        yield return new WaitForSeconds(delayBetweenWaves);

        activeWaveEnemies.Clear();

        // reminder: stagger enemy spawning by 0.06s so we do not cause a single-frame cpu spike
        for (int i = 0; i < currentWave.enemyPrefabs.Count; i++)
        {
            GameObject prefab = currentWave.enemyPrefabs[i];
            if (prefab == null) continue;

            Transform sp = arenaSpawnPoints.Count > 0 ? arenaSpawnPoints[i % arenaSpawnPoints.Count] : transform;

            GameObject spawnedEnemy = Instantiate(prefab, sp.position, Quaternion.identity);
            activeWaveEnemies.Add(spawnedEnemy);

            if (enemySpawnSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(enemySpawnSound, sp.position, 1.0f);
            }

            if (spawnedEnemy.TryGetComponent<Damageable>(out Damageable health))
            {
                health.onHealthChanged.AddListener((hp, maxHp) => OnWaveEnemyHealthChanged(spawnedEnemy, hp));
            }

            // small stagger between each enemy spawn in the wave
            yield return new WaitForSeconds(0.06f);
        }

        UpdateWaveUI();
    }

    private void OnWaveEnemyHealthChanged(GameObject enemy, int currentHealth)
    {
        if (currentHealth <= 0 && activeWaveEnemies.Contains(enemy))
        {
            activeWaveEnemies.Remove(enemy);
            UpdateWaveUI();

            if (activeWaveEnemies.Count == 0)
            {
                if (waveClearedSound != null && AudioManager.Instance != null)
                {
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(waveClearedSound, transform.position, 1.2f);
                }

                currentWaveIndex++;
                StartCoroutine(StartNextWaveRoutine());
            }
        }
    }

    private void UpdateWaveUI()
    {
        int waveNum = currentWaveIndex + 1;
        int totalWaves = waves.Count;
        int remaining = activeWaveEnemies.Count;

        // only update objective if ui instance exist
        LevelObjectiveUI.Instance?.SetObjectiveText($"Wave {waveNum}/{totalWaves} - Enemies Left: {remaining}");
    }

    public void UpdateChunkObjectiveUI()
    {
        // Exclude ReachGate here so ReachGate chunks display "Get to the Gate!" instead of defaulting to cleared
        if (IsChunkCleared && objectiveType != ObjectiveType.ReachGate)
        {
            LevelObjectiveUI.Instance?.SetObjectiveText("Proceed through the Unlocked Gate!");
            return;
        }

        if (objectiveType == ObjectiveType.ReachGate)
        {
            LevelObjectiveUI.Instance?.SetObjectiveText("Get to the Gate!");
        }
        else if (objectiveType == ObjectiveType.KillAllEnemies)
        {
            LevelObjectiveUI.Instance?.SetObjectiveText($"Enemies Defeated: {defeatedEnemies} / {totalEnemies}");
        }
        else if (objectiveType == ObjectiveType.WaveCombat)
        {
            UpdateWaveUI();
        }
        else if (objectiveType == ObjectiveType.Keycard)
        {
            LevelObjectiveUI.Instance?.SetObjectiveText("Find a Keycard to unlock the Gate!");
        }
        else if (objectiveType == ObjectiveType.Timer)
        {
            if (isTimerFailed)
            {
                LevelObjectiveUI.Instance?.SetObjectiveText("Time Expired! Retry from the start!");
            }
            else if (isTimerRunning)
            {
                int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(currentTimeLeft));
                LevelObjectiveUI.Instance?.SetObjectiveText($"Escape before gate locks! Time Left: {secondsLeft}s");
            }
            else
            {
                LevelObjectiveUI.Instance?.SetObjectiveText("Reach the end before time runs out!");
            }
        }
    }
}