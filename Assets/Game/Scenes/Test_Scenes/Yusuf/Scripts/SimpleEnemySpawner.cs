using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace YusufTest
{
    /// <summary>
    /// Ultra basit enemy spawner
    /// - Gece başladığında rastgele sayıda düşman spawn eder
    /// - Rastgele interval ile spawn yapar
    /// - Object pooling kullanır (performans için)
    /// - Multiple enemy prefab destekler (rastgele seçer)
    /// </summary>
    public class SimpleEnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [Tooltip("Birden fazla prefab eklerseniz, spawn sırasında rastgele seçilir")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private int poolSizePerPrefab = 20; // Her prefab tipi için havuz boyutu

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
        Debug.Log("[SimpleEnemySpawner] ⚙️ Awake çağrıldı");
        // Object pool oluştur
        CreatePool();
    }

    void Start()
    {
        Debug.Log("[SimpleEnemySpawner] 🚀 Start çağrıldı");
        FindPlayer();

        // Layer mask
        obstacleLayer = LayerMask.GetMask("Wall");

        // GameManager kontrolü
        if (GameManager.Instance == null)
        {
            Debug.LogError("[SimpleEnemySpawner] ❌ GameManager.Instance NULL! Sahnede GameManager var mı?");
        }
        else
        {
            Debug.Log($"[SimpleEnemySpawner] ✅ GameManager bulundu. Current State: {GameManager.Instance.CurrentState}");
        }
    }

    void OnEnable()
    {
        Debug.Log("[SimpleEnemySpawner] ✅ OnEnable - Event'lere abone olunuyor");
        // GameManager event'lerine abone ol
        GameManager.OnNightStart += OnNightStarted;
        GameManager.OnDayStart += OnDayStarted;
        Debug.Log("[SimpleEnemySpawner] ✅ Event'lere abone olundu!");
    }

    void OnDisable()
    {
        Debug.Log("[SimpleEnemySpawner] ⚠️ OnDisable - Event'lerden çıkılıyor");
        // Abonelikten çık
        GameManager.OnNightStart -= OnNightStarted;
        GameManager.OnDayStart -= OnDayStarted;
    }

    /// <summary>
    /// Object pool oluştur (her prefab tipi için)
    /// </summary>
    void CreatePool()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogError("[SimpleEnemySpawner] Enemy Prefab listesi boş! En az 1 prefab ekleyin.");
            return;
        }

        // Null prefab kontrolü
        enemyPrefabs.RemoveAll(p => p == null);
        if (enemyPrefabs.Count == 0)
        {
            Debug.LogError("[SimpleEnemySpawner] Geçerli enemy prefab bulunamadı!");
            return;
        }

        int totalCreated = 0;

        // Her prefab tipi için havuz oluştur
        foreach (GameObject prefab in enemyPrefabs)
        {
            for (int i = 0; i < poolSizePerPrefab; i++)
            {
                GameObject enemy = Instantiate(prefab, transform);
                enemy.SetActive(false);
                enemyPool.Add(enemy);
                totalCreated++;
            }
        }

        Debug.Log($"[SimpleEnemySpawner] {totalCreated} düşmanlık havuz oluşturuldu ({enemyPrefabs.Count} farklı tip)");
    }

    /// <summary>
    /// Gece başladığında çağrılır
    /// </summary>
    void OnNightStarted()
    {
        Debug.Log("[SimpleEnemySpawner] 🌙🌙🌙 OnNightStarted ÇAĞRILDI! 🌙🌙🌙");

        if (isSpawning)
        {
            Debug.LogWarning("[SimpleEnemySpawner] ⚠️ Zaten spawn işlemi devam ediyor, atlanıyor");
            return;
        }

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
        Debug.Log("[SimpleEnemySpawner] ☀️☀️☀️ OnDayStarted ÇAĞRILDI! ☀️☀️☀️");
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

        Debug.Log($"[SimpleEnemySpawner] 👾 Düşman spawn ediliyor: {enemy.name} at {spawnPos}");

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
    /// Havuzdan pasif düşman al (rastgele TİP seç)
    /// </summary>
    GameObject GetPooledEnemy()
    {
        // Önce rastgele bir prefab tipi seç
        GameObject targetPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        string targetPrefabName = targetPrefab.name;

        // O prefab tipinden pasif olanları bul
        List<GameObject> inactiveOfType = new List<GameObject>();
        foreach (GameObject enemy in enemyPool)
        {
            if (!enemy.activeInHierarchy && enemy.name.StartsWith(targetPrefabName))
            {
                inactiveOfType.Add(enemy);
            }
        }

        // O tipten pasif düşman varsa döndür
        if (inactiveOfType.Count > 0)
        {
            int randomIndex = Random.Range(0, inactiveOfType.Count);
            Debug.Log($"[SimpleEnemySpawner] Pooldan alındı: {inactiveOfType[randomIndex].name}");
            return inactiveOfType[randomIndex];
        }

        // Yoksa yeni oluştur
        Debug.LogWarning($"[SimpleEnemySpawner] {targetPrefabName} için havuz genişletiliyor...");
        GameObject newEnemy = Instantiate(targetPrefab, transform);
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
            {
                Debug.LogWarning("[SimpleEnemySpawner] Player bulunamadı, (0,0,0) döndürülüyor");
                return Vector3.zero;
            }
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
                // ÖNEMLİ: Z pozisyonunu 0 yap (2D oyun için)
                return new Vector3(candidatePos.x, candidatePos.y, 0f);
            }
        }

        // Bulamazsa fallback
        Debug.LogWarning("[SimpleEnemySpawner] Uygun pozisyon bulunamadı, fallback kullanılıyor");
        Vector2 fallbackPos = (Vector2)player.position + Random.insideUnitCircle * maxDistanceFromPlayer;
        // ÖNEMLİ: Z pozisyonunu 0 yap
        return new Vector3(fallbackPos.x, fallbackPos.y, 0f);
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
}
