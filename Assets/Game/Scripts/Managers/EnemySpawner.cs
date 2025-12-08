using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Düşman Spawn Sistemi (NavMesh Kullanmayan Versiyon)
/// Object Pooling kullanarak performanslı çalışır
/// GameManager.OnNightStart eventi gelince spawn başlar
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int poolSize = 20;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private Vector2 spawnCenter = Vector2.zero;

    [Header("Wave Settings")]
    [SerializeField] private int initialEnemiesPerWave = 5;
    [SerializeField] private float enemyIncreasePerWave = 2f;
    [SerializeField] private float spawnInterval = 2f;

    [Header("Spawn Validation")]
    [SerializeField] private float minDistanceFromBase = 5f;
    [SerializeField] private LayerMask obstacleLayer; // Wall layer (spawn engellemek için)

    private List<GameObject> enemyPool;
    private Queue<GameObject> availableEnemies; // Queue for O(1) pooled enemy retrieval
    private int currentWave = 0;
    private bool isSpawning = false;

    private void Awake()
    {
        // Object Pool oluştur
        InitializePool();
    }

    private void OnEnable()
    {
        // GameManager eventlerine abone ol
        GameManager.OnNightStart += StartSpawning;
        GameManager.OnDayStart += StopSpawning;
    }

    private void OnDisable()
    {
        // Event aboneliğini kaldır
        GameManager.OnNightStart -= StartSpawning;
        GameManager.OnDayStart -= StopSpawning;
    }

    /// <summary>
    /// Object Pool'u başlat
    /// </summary>
    private void InitializePool()
    {
        enemyPool = new List<GameObject>();
        availableEnemies = new Queue<GameObject>();

        // Pool'u doldur
        for (int i = 0; i < poolSize; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab, transform);
            enemy.SetActive(false);
            enemyPool.Add(enemy);
            availableEnemies.Enqueue(enemy); // Add to available queue
        }

        Debug.Log($"EnemySpawner: {poolSize} düşmanlık pool oluşturuldu.");
    }

    /// <summary>
    /// Pool'dan pasif düşman al - Optimized with Queue for O(1) retrieval
    /// </summary>
    private GameObject GetPooledEnemy()
    {
        // Use queue for O(1) retrieval instead of O(n) linear search
        if (availableEnemies.Count > 0)
        {
            return availableEnemies.Dequeue();
        }

        // Pool doluysa yeni düşman oluştur (Dinamik genişletme)
        GameObject newEnemy = Instantiate(enemyPrefab, transform);
        newEnemy.SetActive(false);
        enemyPool.Add(newEnemy);
        Debug.LogWarning("EnemySpawner: Pool dolu, yeni düşman eklendi.");
        return newEnemy;
    }

    /// <summary>
    /// Gece başladığında spawn'ı başlat
    /// </summary>
    private void StartSpawning()
    {
        if (isSpawning)
            return;

        currentWave++;
        Debug.Log($"EnemySpawner: Gece başladı! Dalga {currentWave} başlıyor...");

        StartCoroutine(SpawnWave());
    }

    /// <summary>
    /// Gündüz olduğunda spawn'ı durdur
    /// </summary>
    private void StopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();

        // Tüm düşmanları pasif hale getir (Gündüz olunca düşmanlar kaybolur)
        DeactivateAllEnemies();

        Debug.Log("EnemySpawner: Gündüz oldu, spawn durduruldu.");
    }

    /// <summary>
    /// Dalga spawn sistemi
    /// </summary>
    private IEnumerator SpawnWave()
    {
        isSpawning = true;

        // Bu dalgada spawn edilecek düşman sayısı
        int enemiesToSpawn = Mathf.RoundToInt(initialEnemiesPerWave + (currentWave - 1) * enemyIncreasePerWave);

        Debug.Log($"EnemySpawner: {enemiesToSpawn} düşman spawn edilecek.");

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }

        isSpawning = false;
        Debug.Log("EnemySpawner: Dalga tamamlandı.");
    }

    /// <summary>
    /// Tek düşman spawn et
    /// </summary>
    private void SpawnEnemy()
    {
        GameObject enemy = GetPooledEnemy();

        if (enemy == null)
        {
            Debug.LogError("EnemySpawner: Pool'dan düşman alınamadı!");
            return;
        }

        // Geçerli bir pozisyon bul
        Vector3 spawnPosition = GetValidSpawnPosition();

        enemy.transform.position = spawnPosition;

        // EnemyAI'yi yeniden başlat
        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.Respawn();
        }

        enemy.SetActive(true);

        Debug.Log($"EnemySpawner: Düşman spawn edildi ({spawnPosition})");
    }

    /// <summary>
    /// Geçerli spawn pozisyonu bul (Engel ve mesafe kontrolü ile)
    /// </summary>
    private Vector3 GetValidSpawnPosition()
    {
        int maxAttempts = 30; // Maksimum deneme sayısı

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Rastgele pozisyon hesapla
            Vector2 randomPosition = GetRandomSpawnPosition();
            Vector3 spawnPos = new Vector3(randomPosition.x, randomPosition.y, 0f);

            // Base'e olan mesafeyi kontrol et
            float distanceFromBase = Vector2.Distance(spawnPos, spawnCenter);
            if (distanceFromBase < minDistanceFromBase)
            {
                continue; // Çok yakın, tekrar dene
            }

            // O noktada engel var mı kontrol et - Optimized: Direct bool check is faster
            if (!Physics2D.OverlapCircle(spawnPos, 0.5f, obstacleLayer))
            {
                // Geçerli pozisyon bulundu
                return spawnPos;
            }
        }

        // Hiçbir geçerli pozisyon bulunamadıysa merkeze uzak bir nokta kullan
        Debug.LogWarning("EnemySpawner: Geçerli spawn pozisyonu bulunamadı, varsayılan pozisyon kullanılıyor.");

        // Merkeze uzak, güvenli bir pozisyon (sağ üst köşe)
        return new Vector3(spawnCenter.x + spawnRadius * 0.7f, spawnCenter.y + spawnRadius * 0.7f, 0f);
    }

    /// <summary>
    /// Rastgele spawn pozisyonu hesapla (Dairesel)
    /// </summary>
    private Vector2 GetRandomSpawnPosition()
    {
        // Rastgele açı
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        // Spawn radius'un %60-100'ü arası kullan (merkeze çok yakın olmasın)
        float randomRadius = Random.Range(spawnRadius * 0.6f, spawnRadius);

        // Dairesel pozisyon
        float x = spawnCenter.x + Mathf.Cos(angle) * randomRadius;
        float y = spawnCenter.y + Mathf.Sin(angle) * randomRadius;

        return new Vector2(x, y);
    }

    /// <summary>
    /// Tüm düşmanları pasif hale getir
    /// </summary>
    private void DeactivateAllEnemies()
    {
        // Clear and rebuild the queue
        availableEnemies.Clear();
        
        foreach (GameObject enemy in enemyPool)
        {
            if (enemy.activeInHierarchy)
            {
                enemy.SetActive(false);
            }
        }
        
        // All enemies are now inactive, add them all back to queue
        foreach (GameObject enemy in enemyPool)
        {
            availableEnemies.Enqueue(enemy);
        }
    }

    /// <summary>
    /// Pool'u manuel başlatmak için (Test amaçlı)
    /// </summary>
    [ContextMenu("Test Spawn")]
    public void TestSpawn()
    {
        SpawnEnemy();
    }

    /// <summary>
    /// Wave'i manuel başlatmak için (Test amaçlı)
    /// </summary>
    [ContextMenu("Test Start Wave")]
    public void TestStartWave()
    {
        StartSpawning();
    }

    /// <summary>
    /// Gizmos ile spawn alanını görselleştir
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Spawn alanı (Cyan)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);

        // Minimum mesafe alanı (Kırmızı)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnCenter, minDistanceFromBase);
    }
}
