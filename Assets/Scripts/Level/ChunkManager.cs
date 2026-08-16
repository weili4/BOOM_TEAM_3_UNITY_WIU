using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    // CHUNK MANAGER WITH MUSIC TRANSITION
    public enum ObjectiveType { ReachGate, KillAllEnemies, WaveCombat }

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

    [Header("CHUNK MUSIC")]
    [SerializeField] private AudioClip chunkBGM;

    [Header("KILL ALL ENEMIES CONFIG")]
    [SerializeField] private List<Damageable> chunkEnemies = new List<Damageable>();

    [Header("WAVE COMBAT CONFIG")]
    [SerializeField] private List<Transform> arenaSpawnPoints;
    [SerializeField] private List<Wave> waves;
    [SerializeField] private float delayBetweenWaves = 2.0f;

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

    public bool IsChunkCleared => isChunkCleared || objectiveType == ObjectiveType.ReachGate;

    private void Start()
    {
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

        if (chunkBGM != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(chunkBGM, 0.4f);
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
            // ALL WAVES CLEARED
            isChunkCleared = true;
            LevelObjectiveUI.Instance?.SetObjectiveText("Boss Zone Cleared! Proceed through the Unlocked Gate.");

            if (bossZoneClearedSound != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(bossZoneClearedSound, transform.position, 1.4f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic(0.4f);
            }

            if (exitGate != null) exitGate.OpenGate();
            yield break;
        }

        Wave currentWave = waves[currentWaveIndex];
        int waveNum = currentWaveIndex + 1;
        LevelObjectiveUI.Instance?.SetObjectiveText($"Wave {waveNum}/{waves.Count} Starting...");

        if (waveStartSound != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(waveStartSound, transform.position, 1.2f);
        }

        yield return new WaitForSeconds(delayBetweenWaves);

        activeWaveEnemies.Clear();

        for (int i = 0; i < currentWave.enemyPrefabs.Count; i++)
        {
            GameObject prefab = currentWave.enemyPrefabs[i];
            Transform sp = arenaSpawnPoints.Count > 0 ? arenaSpawnPoints[i % arenaSpawnPoints.Count] : transform;

            GameObject spawnedEnemy = Instantiate(prefab, sp.position, Quaternion.identity);
            activeWaveEnemies.Add(spawnedEnemy);

            if (enemySpawnSound != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(enemySpawnSound, sp.position, 1.0f);
            }

            if (spawnedEnemy.TryGetComponent<Damageable>(out Damageable health))
            {
                health.onHealthChanged.AddListener((hp, maxHp) => OnWaveEnemyHealthChanged(spawnedEnemy, hp));
            }
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
                if (waveClearedSound != null)
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
        if (isChunkCleared)
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
    }
}