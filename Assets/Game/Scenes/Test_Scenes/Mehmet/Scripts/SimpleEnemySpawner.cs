using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ultra basit enemy spawner
/// - Gece başladığında rastgele sayıda düşman spawn eder
/// - Rastgele interval ile spawn yapar
/// - Object pooling kullanır (performans için)
/// </summary>
public class SimpleEnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int poolSize = 50; // Havuz boyutu

    [Header("Spawn Settings")]
    [SerializeField] private int minEnemiesPerNight = 5;
    [SerializeField] private int maxEnemiesPerNight = 15;
    [SerializeField] private float minSpawnInterval = 0.2f; // En hızlı spawn
    [SerializeField] private float maxSpawnInterval = 0.8f; // En yavaş spawn
    [SerializeField] private float intervalIncrease = 0.05f; // Her spawn'da artış

    [Header("Spawn Distance")]
    [SerializeField] private float minDistanceFromPlayer = 10f;
    [SerializeField] private float maxDistanceFromPlayer = 20f;

    [Header("Obstacle Check")]
    [SerializeField] private LayerMask obstacleLayer; // Wall layer
    [SerializeField] private float spawnSafeRadius = 1f;

    private List<GameObject> enemyPool = new List<GameObject>();
    private Transform player;
    private bool isSpawning = false;

    void Awake()
    {
        // Object pool oluştur
        CreatePool();
    }

    void Start()
    {
        FindPlayer();

        // Layer mask
        obstacleLayer = LayerMask.GetMask("Wall");
    }

    void OnEnable()
    {
        // GameManager event'lerine abone ol
        GameManager.OnNightStart += OnNightStarted;
        GameManager.OnDayStart += OnDayStarted;
    }

    void OnDisable()
    {
        // Abonelikten çık
        GameManager.OnNightStart -= OnNightStarted;
        GameManager.OnDayStart -= OnDayStarted;
    }

    /// <summary>
    /// Object pool oluştur
    /// </summary>
    void CreatePool()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[SimpleEnemySpawner] Enemy Prefab atanmamış!");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab, transform);
            enemy.SetActive(false);
            enemyPool.Add(enemy);
        }

        Debug.Log($"[SimpleEnemySpawner] {poolSize} düşmanlık havuz oluşturuldu");
    }

    /// <summary>
    /// Gece başladığında çağrılır
    /// </summary>
    void OnNightStarted()
    {
        if (isSpawning) return;

        Debug.Log("[SimpleEnemySpawner] 🌙 GECE BAŞLADI - Düşmanlar geliyor!");

        // Rastgele düşman sayısı belirle
        int enemyCount = Random.Range(minEnemiesPerNight, maxEnemiesPerNight + 1);

        Debug.Log($"[SimpleEnemySpawner] Bu gece {enemyCount} düşman spawn olacak");

        StartCoroutine(SpawnEnemiesCoroutine(enemyCount));
    }

    /// <summary>
    /// Gündüz başladığında çağrılır
    /// </summary>
    void OnDayStarted()
    {
        Debug.Log("[SimpleEnemySpawner] ☀️ GÜNDÜZ BAŞLADI - Spawn durduruluyor");

        isSpawning = false;
        StopAllCoroutines();
        DeactivateAllEnemies();
    }

    /// <summary>
    /// Aralıklı şekilde düşman spawn eder (interval gittikçe artar)
    /// </summary>
    IEnumerator SpawnEnemiesCoroutine(int count)
    {
        isSpawning = true;
        float currentInterval = minSpawnInterval;

        for (int i = 0; i < count; i++)
        {
            // İlk spawn hemen
            if (i > 0)
            {
                yield return new WaitForSeconds(currentInterval);
            }

            SpawnEnemy();

            // Her spawn'da interval artar
            currentInterval += intervalIncrease;
            currentInterval = Mathf.Min(currentInterval, maxSpawnInterval);
        }

        isSpawning = false;
        Debug.Log("[SimpleEnemySpawner] Tüm düşmanlar spawn oldu!");
    }

    /// <summary>
    /// Tek bir düşman spawn eder
    /// </summary>
    void SpawnEnemy()
    {
        // Havuzdan pasif düşman al
        GameObject enemy = GetPooledEnemy();
        if (enemy == null)
        {
            Debug.LogWarning("[SimpleEnemySpawner] Havuzda boş düşman kalmadı!");
            return;
        }

        // Geçerli spawn pozisyonu bul
        Vector3 spawnPos = GetRandomSpawnPosition();

        // AI'yi resetle
        SimpleEnemyAI ai = enemy.GetComponent<SimpleEnemyAI>();
        if (ai != null)
        {
            ai.Respawn(spawnPos);
        }
        else
        {
            Debug.LogError("[SimpleEnemySpawner] ❌ ENEMY PREFAB'DA SimpleEnemyAI YOK!");
        }

        // Health'i resetle
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.ResetHealth();
        }

        enemy.SetActive(true);
    }

    /// <summary>
    /// Havuzdan pasif düşman al
    /// </summary>
    GameObject GetPooledEnemy()
    {
        foreach (GameObject enemy in enemyPool)
        {
            if (!enemy.activeInHierarchy)
                return enemy;
        }

        // Havuz doluysa genişlet
        Debug.LogWarning("[SimpleEnemySpawner] Havuz genişletiliyor...");
        GameObject newEnemy = Instantiate(enemyPrefab, transform);
        newEnemy.SetActive(false);
        enemyPool.Add(newEnemy);
        return newEnemy;
    }

    /// <summary>
    /// Rastgele spawn pozisyonu al (player'dan uzak, duvardan uzak)
    /// </summary>
    Vector3 GetRandomSpawnPosition()
    {
        if (player == null)
        {
            FindPlayer();
            if (player == null)
                return Vector3.zero;
        }

        // 10 deneme yap
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // Rastgele açı ve mesafe
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);

            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Vector2 candidatePos = (Vector2)player.position + offset;

            // Duvara yakın mı kontrol et
            if (!IsPositionBlocked(candidatePos))
            {
                return candidatePos;
            }
        }

        // Bulamazsa fallback
        Debug.LogWarning("[SimpleEnemySpawner] Uygun pozisyon bulunamadı, fallback kullanılıyor");
        return (Vector2)player.position + Random.insideUnitCircle * maxDistanceFromPlayer;
    }

    /// <summary>
    /// Pozisyon duvara yakın mı?
    /// </summary>
    bool IsPositionBlocked(Vector2 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, spawnSafeRadius, obstacleLayer);
        return hit != null;
    }

    /// <summary>
    /// Tüm düşmanları deaktif et (gündüz olunca)
    /// </summary>
    void DeactivateAllEnemies()
    {
        int count = 0;
        foreach (GameObject enemy in enemyPool)
        {
            if (enemy.activeInHierarchy)
            {
                enemy.SetActive(false);
                count++;
            }
        }

        Debug.Log($"[SimpleEnemySpawner] {count} düşman deaktif edildi");
    }

    /// <summary>
    /// Player'ı bul
    /// </summary>
    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[SimpleEnemySpawner] Player bulunamadı!");
        }
    }

    // Debug için spawn alanını göster
    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        // Min spawn distance (kırmızı)
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(player.position, minDistanceFromPlayer);

        // Max spawn distance (yeşil)
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(player.position, maxDistanceFromPlayer);
    }
}
