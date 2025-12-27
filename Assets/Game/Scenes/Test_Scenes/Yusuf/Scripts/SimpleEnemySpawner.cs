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

    [Header("Ground Check")]
    [Tooltip("Zemin layer'ı (Ground, Grass vb.)")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Zeminden yukarı raycast mesafesi")]
    [SerializeField] private float groundCheckDistance = 5f;
    [Tooltip("Raycast'in başlangıç yüksekliği")]
    [SerializeField] private float raycastStartHeight = 10f;

    [Header("Variety Settings")]
    [Tooltip("Spawn çeşitliliğini artır (shuffle listesi kullanır)")]
    [SerializeField] private bool useShuffleForVariety = true;
    [Tooltip("Son spawn edilen düşmanın hemen sonra tekrar gelmesini engelle")]
    [SerializeField] private bool preventConsecutiveSame = true;

    [Header("Spawn Position Spread")]
    [Tooltip("Spawn pozisyonlarını dağıt (her spawn farklı açıda)")]
    [SerializeField] private bool spreadSpawnPositions = true;
    [Tooltip("Minimum açı farkı (derece)")]
    [SerializeField] private float minAngleDifference = 45f;

    private List<GameObject> enemyPool = new List<GameObject>();
    private Transform player;
    private bool isSpawning = false;

    // Shuffle sistemi için
    private List<int> shuffledPrefabIndices = new List<int>();
    private int currentShuffleIndex = 0;
    private int lastSpawnedPrefabIndex = -1;

    // Spawn pozisyon dağılımı için
    private float lastSpawnAngle = 0f;

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

        // Layer masks
        obstacleLayer = LayerMask.GetMask("Wall");
        groundLayer = LayerMask.GetMask("Ground", "Grass", "Terrain");

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
    /// Havuzdan pasif düşman al (rastgele TİP seç - çeşitlilik artırılmış)
    /// </summary>
    GameObject GetPooledEnemy()
    {
        GameObject targetPrefab;

        if (useShuffleForVariety)
        {
            // Shuffle listesi ile dengeli dağılım
            targetPrefab = GetNextShuffledPrefab();
        }
        else
        {
            // Tamamen rastgele (eski yöntem)
            targetPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        }

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
    /// Shuffle listesinden sıradaki prefab'ı al (dengeli dağılım için)
    /// </summary>
    GameObject GetNextShuffledPrefab()
    {
        // Liste boşsa veya sona geldiyse yeniden karıştır
        if (shuffledPrefabIndices.Count == 0 || currentShuffleIndex >= shuffledPrefabIndices.Count)
        {
            RefreshShuffledList();
        }

        int prefabIndex = shuffledPrefabIndices[currentShuffleIndex];

        // Ardışık aynı düşman engelleme
        if (preventConsecutiveSame && prefabIndex == lastSpawnedPrefabIndex && shuffledPrefabIndices.Count > 1)
        {
            // Sonraki farklı olanı bul
            int searchIndex = currentShuffleIndex + 1;
            bool foundDifferent = false;

            // Listenin kalanını tara
            for (int i = searchIndex; i < shuffledPrefabIndices.Count; i++)
            {
                if (shuffledPrefabIndices[i] != lastSpawnedPrefabIndex)
                {
                    // Swap ile yer değiştir
                    int temp = shuffledPrefabIndices[currentShuffleIndex];
                    shuffledPrefabIndices[currentShuffleIndex] = shuffledPrefabIndices[i];
                    shuffledPrefabIndices[i] = temp;
                    prefabIndex = shuffledPrefabIndices[currentShuffleIndex];
                    foundDifferent = true;
                    Debug.Log($"[SimpleEnemySpawner] 🔄 Ardışık aynı engellendi: {enemyPrefabs[lastSpawnedPrefabIndex].name} → {enemyPrefabs[prefabIndex].name}");
                    break;
                }
            }

            // Bulamazsa liste yenile
            if (!foundDifferent)
            {
                RefreshShuffledList();
                prefabIndex = shuffledPrefabIndices[currentShuffleIndex];
            }
        }

        currentShuffleIndex++;
        lastSpawnedPrefabIndex = prefabIndex;

        return enemyPrefabs[prefabIndex];
    }

    /// <summary>
    /// Shuffle listesini yeniden oluştur ve karıştır
    /// </summary>
    void RefreshShuffledList()
    {
        shuffledPrefabIndices.Clear();

        // Her prefab tipinden eşit sayıda index ekle
        // Örnek: 3 prefab varsa, her birinden 3'er tane -> [0,0,0,1,1,1,2,2,2]
        int repeatCount = Mathf.Max(1, enemyPrefabs.Count);

        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            for (int j = 0; j < repeatCount; j++)
            {
                shuffledPrefabIndices.Add(i);
            }
        }

        // Listeyi karıştır (Fisher-Yates shuffle)
        ShuffleList(shuffledPrefabIndices);

        currentShuffleIndex = 0;

        Debug.Log($"[SimpleEnemySpawner] 🔀 Shuffle listesi yenilendi: {shuffledPrefabIndices.Count} eleman (Prefab sayısı: {enemyPrefabs.Count})");
    }

    /// <summary>
    /// Fisher-Yates shuffle algoritması
    /// </summary>
    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    /// <summary>
    /// Rastgele spawn pozisyonu al (player'dan uzak, zemin üzerinde, dağınık)
    /// YENİ VERSİYON: Zemin kontrolü ile
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

        // ANA SPAWN DÖNGÜSÜ - Zemin üzerinde yer bulana kadar dene
        int totalAttempts = 0;
        const int MAX_TOTAL_ATTEMPTS = 50;

        while (totalAttempts < MAX_TOTAL_ATTEMPTS)
        {
            totalAttempts++;

            // 1. Player'ın MEVCUT pozisyonunu al
            Vector2 currentPlayerPos = new Vector2(player.position.x, player.position.y);

            // 2. Açı hesapla (DERECE cinsinden)
            float angleDegrees;

            if (spreadSpawnPositions && totalAttempts <= 10)
            {
                // İlk 10 denemede spread kullan
                float nextAngle = lastSpawnAngle + Random.Range(minAngleDifference, 360f - minAngleDifference);
                while (nextAngle >= 360f) nextAngle -= 360f;
                angleDegrees = nextAngle;
            }
            else
            {
                // Sonraki denemelerde tamamen rastgele
                angleDegrees = Random.Range(0f, 360f);
            }

            // 3. Rastgele mesafe
            float distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);

            // 4. Pozisyon hesapla
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
            Vector2 targetPosition = currentPlayerPos + (direction * distance);

            // 5. ZEMİN KONTROLÜ - En önemli kısım!
            Vector3 groundPosition;
            if (FindGroundPosition(targetPosition, out groundPosition))
            {
                // 6. Duvar kontrolü
                if (!IsPositionBlocked(groundPosition))
                {
                    // BAŞARILI! Geçerli spawn pozisyonu bulundu
                    lastSpawnAngle = angleDegrees; // Spread için kaydet
                    Debug.Log($"[SimpleEnemySpawner] ✅ Spawn: Açı={angleDegrees:F0}°, Mesafe={distance:F1}, Zemin Y={groundPosition.y:F1}");
                    return groundPosition;
                }
                else
                {
                    Debug.Log($"[SimpleEnemySpawner] ⚠️ Deneme {totalAttempts}: Zemin bulundu ama duvar var");
                }
            }
            else
            {
                Debug.Log($"[SimpleEnemySpawner] ⚠️ Deneme {totalAttempts}: Açı {angleDegrees:F0}°, Mesafe {distance:F1} - ZEMİN YOK!");
            }
        }

        // SON ÇARE: Player'ın yanına spawn et
        Debug.LogError("[SimpleEnemySpawner] ❌ 50 denemede uygun yer bulunamadı! Player yanına spawn ediliyor.");
        Vector3 playerGroundPos;
        if (FindGroundPosition(player.position, out playerGroundPos))
        {
            // Player'dan biraz uzaklaştır
            Vector2 randomOffset = Random.insideUnitCircle.normalized * 3f;
            return playerGroundPos + new Vector3(randomOffset.x, 0f, randomOffset.y);
        }

        return player.position;
    }

    /// <summary>
    /// Verilen XZ pozisyonunda zemin var mı, varsa Y pozisyonunu bul
    /// </summary>
    bool FindGroundPosition(Vector2 xzPosition, out Vector3 groundPosition)
    {
        // Yukarıdan aşağı raycast at
        Vector3 rayStart = new Vector3(xzPosition.x, raycastStartHeight, 0f);

        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, groundCheckDistance, groundLayer);

        if (hit.collider != null)
        {
            // Zemin bulundu!
            groundPosition = new Vector3(xzPosition.x, hit.point.y, 0f);
            return true;
        }

        // Zemin bulunamadı (deniz, boşluk vb.)
        groundPosition = Vector3.zero;
        return false;
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
