using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Enemy Spawner System - Harvest Defense
/// Akıllı düşman spawn sistemi
/// - Object pooling (performans)
/// - Formation spawning (6 farklı tip)
/// - Zorluk eğrisi (AnimationCurve)
/// - Wave ve continuous spawn modları
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("=== PREFAB & POOLING ===")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int initialPoolSize = 30;
    private List<GameObject> enemyPool = new List<GameObject>();

    [Header("=== SPAWN ZONE ===")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnDistance = 10f;
    [SerializeField] private float maxSpawnDistance = 18f;

    [Header("=== WAVE SYSTEM ===")]
    [SerializeField] private bool useWaveSystem = true;
    [SerializeField] private int baseEnemiesPerWave = 6;
    [SerializeField] private float enemyIncreaseRate = 2f;
    [SerializeField] private AnimationCurve difficultyScaling = AnimationCurve.Linear(0, 1, 10, 2);
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private float waveDelay = 2f;

    [Header("=== CONTINUOUS MODE ===")]
    [SerializeField] private float continuousSpawnRate = 4f;

    [Header("=== FORMATION SPAWNING ===")]
    [SerializeField] private bool useFormations = true;
    [SerializeField] private FormationType[] availableFormations = new FormationType[]
    {
        FormationType.Random,
        FormationType.Line,
        FormationType.Arc,
        FormationType.Circle,
        FormationType.Surrounding
    };

    [Header("=== VALIDATION ===")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private int maxSpawnAttempts = 20;
    [SerializeField] private float spawnSafeRadius = 0.8f;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLogs = true;

    private int currentWave = 0;
    private bool isSpawning = false;
    private int totalEnemiesSpawned = 0;

    public enum FormationType
    {
        Random,
        Line,
        Arc,
        Circle,
        Surrounding
    }

    private void Awake()
    {
        InitializePool();
    }

    private void Start()
    {
        if (playerTransform == null)
            FindPlayer();

        obstacleLayer = LayerMask.GetMask("Wall");
    }

    private void OnEnable()
    {
        GameManager.OnNightStart += OnNightStarted;
        GameManager.OnDayStart += OnDayStarted;
    }

    private void OnDisable()
    {
        GameManager.OnNightStart -= OnNightStarted;
        GameManager.OnDayStart -= OnDayStarted;
    }

    private void InitializePool()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] Enemy Prefab not assigned!");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab, transform);
            enemy.SetActive(false);
            enemyPool.Add(enemy);
        }

        if (showDebugLogs)
            Debug.Log($"[EnemySpawner] Object Pool created: {initialPoolSize} enemies");
    }

    private void OnNightStarted()
    {
        if (isSpawning)
            return;

        currentWave++;

        if (showDebugLogs)
            Debug.Log($"[EnemySpawner] *** NIGHT {currentWave} STARTED ***");

        if (useWaveSystem)
        {
            StartCoroutine(SpawnWaveCoroutine());
        }
        else
        {
            StartCoroutine(ContinuousSpawnCoroutine());
        }
    }

    private void OnDayStarted()
    {
        if (showDebugLogs)
            Debug.Log("[EnemySpawner] Day started - Stopping spawn");

        isSpawning = false;
        StopAllCoroutines();
        DeactivateAllEnemies();
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        isSpawning = true;
        yield return new WaitForSeconds(waveDelay);

        // Calculate difficulty
        float difficultyMultiplier = difficultyScaling.Evaluate(currentWave);
        int enemyCount = Mathf.RoundToInt(baseEnemiesPerWave + (currentWave - 1) * enemyIncreaseRate);
        enemyCount = Mathf.RoundToInt(enemyCount * difficultyMultiplier);

        if (showDebugLogs)
            Debug.Log($"[EnemySpawner] Spawning {enemyCount} enemies (Difficulty: x{difficultyMultiplier:F2})");

        // Spawn with formation
        if (useFormations && availableFormations.Length > 0)
        {
            yield return StartCoroutine(SpawnInFormation(enemyCount));
        }
        else
        {
            for (int i = 0; i < enemyCount; i++)
            {
                SpawnSingleEnemy();
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        isSpawning = false;

        if (showDebugLogs)
            Debug.Log($"[EnemySpawner] Wave complete! Total spawned: {totalEnemiesSpawned}");
    }

    private IEnumerator ContinuousSpawnCoroutine()
    {
        isSpawning = true;

        while (isSpawning)
        {
            SpawnSingleEnemy();
            yield return new WaitForSeconds(continuousSpawnRate);
        }
    }

    private IEnumerator SpawnInFormation(int enemyCount)
    {
        FormationType formation = availableFormations[Random.Range(0, availableFormations.Length)];

        if (showDebugLogs)
            Debug.Log($"[EnemySpawner] Formation: {formation}");

        Vector2 centerPoint = GetFormationCenter();
        List<Vector2> formationPositions = GenerateFormationPositions(formation, enemyCount, centerPoint);

        foreach (Vector2 position in formationPositions)
        {
            SpawnEnemyAtPosition(position);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private Vector2 GetFormationCenter()
    {
        if (playerTransform == null)
            return Vector2.zero;

        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

        return (Vector2)playerTransform.position + new Vector2(
            Mathf.Cos(randomAngle) * distance,
            Mathf.Sin(randomAngle) * distance
        );
    }

    private List<Vector2> GenerateFormationPositions(FormationType formation, int count, Vector2 center)
    {
        List<Vector2> positions = new List<Vector2>();

        switch (formation)
        {
            case FormationType.Line:
                Vector2 lineDirection = Random.insideUnitCircle.normalized;
                for (int i = 0; i < count; i++)
                {
                    float offset = (i - count / 2f) * 2f;
                    positions.Add(center + lineDirection * offset);
                }
                break;

            case FormationType.Arc:
                float arcAngle = 120f;
                float startAngle = Random.Range(0f, 360f);
                for (int i = 0; i < count; i++)
                {
                    float angle = (startAngle + (arcAngle / count) * i) * Mathf.Deg2Rad;
                    positions.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3f);
                }
                break;

            case FormationType.Circle:
                for (int i = 0; i < count; i++)
                {
                    float angle = (360f / count) * i * Mathf.Deg2Rad;
                    positions.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 4f);
                }
                break;

            case FormationType.Surrounding:
                if (playerTransform != null)
                {
                    for (int i = 0; i < count; i++)
                    {
                        float angle = (360f / count) * i * Mathf.Deg2Rad;
                        positions.Add((Vector2)playerTransform.position +
                            new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * minSpawnDistance);
                    }
                }
                break;

            default: // Random
                for (int i = 0; i < count; i++)
                {
                    positions.Add(center + Random.insideUnitCircle * 5f);
                }
                break;
        }

        return positions;
    }

    private void SpawnSingleEnemy()
    {
        Vector3 spawnPosition = GetValidSpawnPosition();
        SpawnEnemyAtPosition(spawnPosition);
    }

    private void SpawnEnemyAtPosition(Vector2 position)
    {
        GameObject enemy = GetPooledEnemy();

        if (enemy == null)
        {
            Debug.LogError("[EnemySpawner] Failed to get pooled enemy!");
            return;
        }

        enemy.transform.position = position;

        // Reset AI
        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.Respawn(position);
        }

        // Reset Health
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.ResetHealth();
        }

        enemy.SetActive(true);
        totalEnemiesSpawned++;
    }

    private GameObject GetPooledEnemy()
    {
        foreach (GameObject enemy in enemyPool)
        {
            if (!enemy.activeInHierarchy)
                return enemy;
        }

        // Expand pool if needed
        if (showDebugLogs)
            Debug.LogWarning("[EnemySpawner] Pool full - Expanding...");

        GameObject newEnemy = Instantiate(enemyPrefab, transform);
        newEnemy.SetActive(false);
        enemyPool.Add(newEnemy);
        return newEnemy;
    }

    private Vector3 GetValidSpawnPosition()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 candidatePosition = GetRandomPositionAroundPlayer();

            if (IsPositionValid(candidatePosition))
            {
                return candidatePosition;
            }
        }

        Debug.LogWarning("[EnemySpawner] No valid spawn position found - Using fallback");
        return playerTransform != null ?
            (Vector3)((Vector2)playerTransform.position + Random.insideUnitCircle * maxSpawnDistance) :
            Vector3.zero;
    }

    private Vector2 GetRandomPositionAroundPlayer()
    {
        if (playerTransform == null)
            return Vector2.zero;

        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);

        return (Vector2)playerTransform.position + new Vector2(
            Mathf.Cos(randomAngle) * randomDistance,
            Mathf.Sin(randomAngle) * randomDistance
        );
    }

    private bool IsPositionValid(Vector2 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, spawnSafeRadius, obstacleLayer);
        return hit == null;
    }

    private void DeactivateAllEnemies()
    {
        int deactivatedCount = 0;

        foreach (GameObject enemy in enemyPool)
        {
            if (enemy.activeInHierarchy)
            {
                enemy.SetActive(false);
                deactivatedCount++;
            }
        }

        if (showDebugLogs)
            Debug.Log($"[EnemySpawner] {deactivatedCount} enemies deactivated");
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
    }

    [ContextMenu("Test: Spawn Single Enemy")]
    public void TestSpawnSingleEnemy()
    {
        SpawnSingleEnemy();
    }

    [ContextMenu("Test: Start Wave")]
    public void TestStartWave()
    {
        OnNightStarted();
    }

    [ContextMenu("Test: Deactivate All")]
    public void TestDeactivateAll()
    {
        DeactivateAllEnemies();
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null)
            return;

        // Min spawn distance
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(playerTransform.position, minSpawnDistance);

        // Max spawn distance
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(playerTransform.position, maxSpawnDistance);
    }
}
