using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace YusufTest
{
    /// <summary>
    /// Gelişmiş Enemy Spawner Sistemi
    /// - Birden fazla enemy prefab ile çalışır
    /// - Her enemy tipi için spawn oranı (weight) belirlenebilir
    /// - Gece başladığında player konumuna göre rastgele mesafelerden spawn yapar
    /// - Rastgele spawn aralıkları ile düşman doğurur
    /// - Object pooling ile performans optimize edilmiştir
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Enemy Configurations")]
        [Tooltip("Spawn edilecek enemy tiplerini ve oranlarını tanımla")]
        [SerializeField] private List<EnemySpawnData> enemyTypes = new List<EnemySpawnData>();

        [Header("Spawn Settings - Per Night")]
        [Tooltip("Her gece minimum kaç düşman spawn olacak")]
        [SerializeField] private int minEnemiesPerNight = 10;

        [Tooltip("Her gece maksimum kaç düşman spawn olacak")]
        [SerializeField] private int maxEnemiesPerNight = 25;

        [Header("Spawn Timing")]
        [Tooltip("İki spawn arasındaki minimum süre (saniye)")]
        [SerializeField] private float minSpawnInterval = 0.5f;

        [Tooltip("İki spawn arasındaki maksimum süre (saniye)")]
        [SerializeField] private float maxSpawnInterval = 3f;

        [Header("Spawn Distance")]
        [Tooltip("Player'dan minimum spawn mesafesi")]
        [SerializeField] private float minDistanceFromPlayer = 8f;

        [Tooltip("Player'dan maksimum spawn mesafesi")]
        [SerializeField] private float maxDistanceFromPlayer = 20f;

        [Header("Position Validation")]
        [Tooltip("Zemin kontrolü için kullanılacak layer (Ground, Grass, Terrain)")]
        [SerializeField] private LayerMask groundLayer;

        [Tooltip("Engel kontrolü için layer (Wall vb.)")]
        [SerializeField] private LayerMask obstacleLayer;

        [Tooltip("Zemin araması için raycast başlangıç yüksekliği")]
        [SerializeField] private float raycastStartHeight = 10f;

        [Tooltip("Raycast maksimum mesafesi")]
        [SerializeField] private float raycastMaxDistance = 15f;

        [Tooltip("Spawn pozisyonundaki güvenli alan yarıçapı")]
        [SerializeField] private float safeSpawnRadius = 1f;

        [Header("Pool Settings")]
        [Tooltip("Her enemy tipi için başlangıç pool boyutu")]
        [SerializeField] private int initialPoolSizePerType = 15;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showGizmos = true;

        #endregion

        #region Private Fields

        private Transform playerTransform;
        private bool isNightActive = false;
        private bool isSpawning = false;

        // Object Pool - Her prefab için ayrı liste
        private Dictionary<GameObject, Queue<GameObject>> enemyPools = new Dictionary<GameObject, Queue<GameObject>>();

        // Weighted random için toplam ağırlık
        private float totalSpawnWeight = 0f;

        // Spawn istatistikleri
        private int currentNightSpawnCount = 0;
        private int targetSpawnCount = 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Log("Awake - Initialization started");
            InitializeLayerMasks();
            InitializeObjectPools();
            CalculateTotalWeight();
        }

        private void Start()
        {
            FindPlayer();
            ValidateConfiguration();
        }

        private void OnEnable()
        {
            Log("Subscribing to GameManager events");
            GameManager.OnNightStart += OnNightStarted;
            GameManager.OnDayStart += OnDayStarted;
        }

        private void OnDisable()
        {
            Log("Unsubscribing from GameManager events");
            GameManager.OnNightStart -= OnNightStarted;
            GameManager.OnDayStart -= OnDayStarted;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Layer mask'leri otomatik ayarla
        /// </summary>
        private void InitializeLayerMasks()
        {
            if (groundLayer == 0)
            {
                groundLayer = LayerMask.GetMask("Ground", "Grass", "Terrain");
                Log($"Ground layer mask auto-set: {groundLayer.value}");
            }

            if (obstacleLayer == 0)
            {
                obstacleLayer = LayerMask.GetMask("Wall");
                Log($"Obstacle layer mask auto-set: {obstacleLayer.value}");
            }
        }

        /// <summary>
        /// Her enemy tipi için object pool oluştur
        /// </summary>
        private void InitializeObjectPools()
        {
            if (enemyTypes == null || enemyTypes.Count == 0)
            {
                LogError("Enemy types list is empty! Add at least one enemy type.");
                return;
            }

            // Null prefab'ları temizle
            enemyTypes.RemoveAll(e => e.enemyPrefab == null);

            int totalPooled = 0;

            foreach (var enemyData in enemyTypes)
            {
                if (enemyData.enemyPrefab == null) continue;

                Queue<GameObject> pool = new Queue<GameObject>();

                for (int i = 0; i < initialPoolSizePerType; i++)
                {
                    GameObject enemy = Instantiate(enemyData.enemyPrefab, transform);
                    enemy.name = $"{enemyData.enemyPrefab.name}_{i}";
                    enemy.SetActive(false);
                    pool.Enqueue(enemy);
                    totalPooled++;
                }

                enemyPools[enemyData.enemyPrefab] = pool;
                Log($"Pool created for '{enemyData.enemyPrefab.name}': {initialPoolSizePerType} instances");
            }

            Log($"Total pooled enemies: {totalPooled}");
        }

        /// <summary>
        /// Spawn oranları için toplam ağırlığı hesapla
        /// </summary>
        private void CalculateTotalWeight()
        {
            totalSpawnWeight = 0f;
            foreach (var enemyData in enemyTypes)
            {
                if (enemyData.enemyPrefab != null)
                {
                    totalSpawnWeight += enemyData.spawnWeight;
                }
            }

            Log($"Total spawn weight calculated: {totalSpawnWeight}");
        }

        /// <summary>
        /// Konfigürasyonu doğrula
        /// </summary>
        private void ValidateConfiguration()
        {
            if (enemyTypes.Count == 0)
            {
                LogError("No enemy types configured!");
                enabled = false;
                return;
            }

            if (minDistanceFromPlayer >= maxDistanceFromPlayer)
            {
                LogWarning($"Min distance ({minDistanceFromPlayer}) >= Max distance ({maxDistanceFromPlayer}). Adjusting...");
                maxDistanceFromPlayer = minDistanceFromPlayer + 5f;
            }

            if (minSpawnInterval >= maxSpawnInterval)
            {
                LogWarning($"Min interval ({minSpawnInterval}) >= Max interval ({maxSpawnInterval}). Adjusting...");
                maxSpawnInterval = minSpawnInterval + 1f;
            }

            Log("Configuration validated successfully");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Gece başladığında tetiklenir
        /// </summary>
        private void OnNightStarted()
        {
            Log("=== NIGHT STARTED ===");

            if (isSpawning)
            {
                LogWarning("Already spawning, skipping...");
                return;
            }

            isNightActive = true;
            currentNightSpawnCount = 0;
            targetSpawnCount = Random.Range(minEnemiesPerNight, maxEnemiesPerNight + 1);

            Log($"Target enemies for this night: {targetSpawnCount}");

            StartCoroutine(SpawnEnemiesCoroutine());
        }

        /// <summary>
        /// Gündüz başladığında tetiklenir
        /// </summary>
        private void OnDayStarted()
        {
            Log("=== DAY STARTED ===");

            isNightActive = false;
            isSpawning = false;

            StopAllCoroutines();
            DeactivateAllEnemies();

            Log($"Night ended. Total spawned: {currentNightSpawnCount}/{targetSpawnCount}");
        }

        #endregion

        #region Spawning Logic

        /// <summary>
        /// Gece boyunca rastgele aralıklarla düşman spawn eder
        /// </summary>
        private IEnumerator SpawnEnemiesCoroutine()
        {
            isSpawning = true;

            while (isNightActive && currentNightSpawnCount < targetSpawnCount)
            {
                // Düşman spawn et
                SpawnRandomEnemy();
                currentNightSpawnCount++;

                // Rastgele bekleme süresi
                float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
                Log($"Spawned {currentNightSpawnCount}/{targetSpawnCount}. Next spawn in {waitTime:F2}s");

                yield return new WaitForSeconds(waitTime);
            }

            isSpawning = false;
            Log($"All enemies spawned for this night! Total: {currentNightSpawnCount}");
        }

        /// <summary>
        /// Weighted random sistemiyle rastgele enemy spawn eder
        /// </summary>
        private void SpawnRandomEnemy()
        {
            // Weighted random ile enemy tipi seç
            GameObject selectedPrefab = SelectEnemyByWeight();

            if (selectedPrefab == null)
            {
                LogError("Failed to select enemy prefab!");
                return;
            }

            // Pooldan enemy al
            GameObject enemy = GetPooledEnemy(selectedPrefab);

            if (enemy == null)
            {
                LogWarning($"No available enemy in pool for '{selectedPrefab.name}'. Creating new instance...");
                enemy = CreateNewPooledEnemy(selectedPrefab);
            }

            // Geçerli spawn pozisyonu bul
            Vector3 spawnPosition = FindValidSpawnPosition();

            // Enemy'yi aktif et ve pozisyonla
            ActivateEnemy(enemy, spawnPosition);

            Log($"Spawned: {enemy.name} at {spawnPosition}");
        }

        /// <summary>
        /// Spawn oranlarına göre rastgele enemy prefab seçer
        /// </summary>
        private GameObject SelectEnemyByWeight()
        {
            if (totalSpawnWeight <= 0f)
            {
                // Eğer ağırlık yoksa tamamen rastgele seç
                return enemyTypes[Random.Range(0, enemyTypes.Count)].enemyPrefab;
            }

            float randomValue = Random.Range(0f, totalSpawnWeight);
            float cumulativeWeight = 0f;

            foreach (var enemyData in enemyTypes)
            {
                if (enemyData.enemyPrefab == null) continue;

                cumulativeWeight += enemyData.spawnWeight;

                if (randomValue <= cumulativeWeight)
                {
                    return enemyData.enemyPrefab;
                }
            }

            // Fallback: İlk geçerli prefab
            foreach (var enemyData in enemyTypes)
            {
                if (enemyData.enemyPrefab != null)
                    return enemyData.enemyPrefab;
            }

            return null;
        }

        /// <summary>
        /// Pooldan enemy al
        /// </summary>
        private GameObject GetPooledEnemy(GameObject prefab)
        {
            if (!enemyPools.ContainsKey(prefab))
            {
                LogWarning($"No pool exists for prefab '{prefab.name}'. Creating new pool...");
                enemyPools[prefab] = new Queue<GameObject>();
                return null;
            }

            Queue<GameObject> pool = enemyPools[prefab];

            // Deaktif enemy bul
            foreach (GameObject enemy in pool)
            {
                if (!enemy.activeInHierarchy)
                {
                    return enemy;
                }
            }

            return null;
        }

        /// <summary>
        /// Yeni pool instance oluştur
        /// </summary>
        private GameObject CreateNewPooledEnemy(GameObject prefab)
        {
            GameObject enemy = Instantiate(prefab, transform);
            enemy.name = $"{prefab.name}_{enemyPools[prefab].Count}";
            enemy.SetActive(false);

            enemyPools[prefab].Enqueue(enemy);

            return enemy;
        }

        /// <summary>
        /// Enemy'yi aktif et ve pozisyonla
        /// </summary>
        private void ActivateEnemy(GameObject enemy, Vector3 position)
        {
            // AI component reset
            SimpleEnemyAI ai = enemy.GetComponent<SimpleEnemyAI>();
            if (ai != null)
            {
                ai.Respawn(position);
            }
            else
            {
                enemy.transform.position = position;
            }

            // Health reset
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.ResetHealth();
            }

            enemy.SetActive(true);
        }

        #endregion

        #region Position Finding

        /// <summary>
        /// Geçerli spawn pozisyonu bul (player konumuna göre, zeminde, engelsiz)
        /// </summary>
        private Vector3 FindValidSpawnPosition()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null)
                {
                    LogWarning("Player not found! Spawning at origin.");
                    return Vector3.zero;
                }
            }

            const int MAX_ATTEMPTS = 30;

            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                // 1. Player'ın mevcut pozisyonunu al
                Vector2 playerPos2D = new Vector2(playerTransform.position.x, playerTransform.position.y);

                // 2. Rastgele açı ve mesafe
                float angle = Random.Range(0f, 360f);
                float distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);

                // 3. Hedef pozisyon hesapla
                float angleRad = angle * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
                Vector2 targetPos2D = playerPos2D + (direction * distance);

                // 4. Zemin pozisyonu bul
                Vector3 groundPosition;
                if (FindGroundAtPosition(targetPos2D, out groundPosition))
                {
                    // 5. Engel kontrolü
                    if (!IsPositionBlocked(groundPosition))
                    {
                        // Başarılı!
                        return groundPosition;
                    }
                }
            }

            // Son çare: Player yanına spawn et
            LogWarning($"Could not find valid spawn position after {MAX_ATTEMPTS} attempts. Spawning near player.");
            return playerTransform.position + (Vector3)Random.insideUnitCircle.normalized * 3f;
        }

        /// <summary>
        /// Verilen XZ pozisyonunda zemin var mı bul
        /// </summary>
        private bool FindGroundAtPosition(Vector2 xzPosition, out Vector3 groundPosition)
        {
            Vector3 rayStart = new Vector3(xzPosition.x, raycastStartHeight, 0f);

            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, raycastMaxDistance, groundLayer);

            if (hit.collider != null)
            {
                groundPosition = new Vector3(xzPosition.x, hit.point.y, 0f);
                return true;
            }

            groundPosition = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Pozisyon engelle mi kontrol et
        /// </summary>
        private bool IsPositionBlocked(Vector3 position)
        {
            Collider2D hit = Physics2D.OverlapCircle(position, safeSpawnRadius, obstacleLayer);
            return hit != null;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Player'ı bul
        /// </summary>
        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                Log("Player found!");
            }
            else
            {
                LogError("Player not found! Make sure player has 'Player' tag.");
            }
        }

        /// <summary>
        /// Tüm aktif düşmanları deaktif et
        /// </summary>
        private void DeactivateAllEnemies()
        {
            int deactivatedCount = 0;

            foreach (var pool in enemyPools.Values)
            {
                foreach (GameObject enemy in pool)
                {
                    if (enemy.activeInHierarchy)
                    {
                        enemy.SetActive(false);
                        deactivatedCount++;
                    }
                }
            }

            Log($"Deactivated {deactivatedCount} enemies");
        }

        #endregion

        #region Debug & Logging

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[EnemySpawner] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[EnemySpawner] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[EnemySpawner] {message}");
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos || playerTransform == null) return;

            // Min spawn mesafesi (kırmızı)
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(playerTransform.position, minDistanceFromPlayer);

            // Max spawn mesafesi (yeşil)
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(playerTransform.position, maxDistanceFromPlayer);

            // Player pozisyonu
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, 0.5f);
        }

        #endregion
    }

    /// <summary>
    /// Enemy spawn verisi - Her enemy tipi için konfigürasyon
    /// </summary>
    [System.Serializable]
    public class EnemySpawnData
    {
        [Tooltip("Spawn edilecek enemy prefab")]
        public GameObject enemyPrefab;

        [Tooltip("Spawn oranı - Yüksek değer = Daha sık spawn olur (Örn: 1.0 = normal, 2.0 = 2x daha sık)")]
        [Range(0.1f, 10f)]
        public float spawnWeight = 1f;

        [Tooltip("Bu enemy hakkında açıklama (opsiyonel)")]
        public string description = "";
    }
}
