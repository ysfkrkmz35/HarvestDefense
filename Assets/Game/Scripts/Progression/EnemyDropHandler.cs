using UnityEngine;

namespace YusufTest
{
    /// <summary>
    /// Enemy Drop Handler
    /// - Düşman öldüğünde Gold ve XP pickup'ları spawn eder
    /// - Pickup'lar yere düşer ve oyuncu yaklaşınca toplanır
    /// - Minecraft tarzı mıknatıs sistemi
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyDropHandler : MonoBehaviour
    {
        [Header("═══ DROP REWARDS ═══")]
        [Tooltip("XP awarded when this enemy dies")]
        [SerializeField] private int xpReward = 25;

        [Tooltip("Gold awarded when this enemy dies")]
        [SerializeField] private int goldReward = 10;

        [Header("═══ PICKUP PREFABS ═══")]
        [Tooltip("Gold pickup prefab (assign in inspector)")]
        [SerializeField] private GameObject goldPickupPrefab;
        
        [Tooltip("XP pickup prefab (assign in inspector)")]
        [SerializeField] private GameObject xpPickupPrefab;

        [Header("═══ SPAWN SETTINGS ═══")]
        [Tooltip("Number of gold pickups to spawn")]
        [SerializeField] private int goldPickupCount = 3;
        
        [Tooltip("Number of XP pickups to spawn")]
        [SerializeField] private int xpPickupCount = 2;
        
        [Tooltip("Spawn offset from enemy center (Y = yukarı)")]
        [SerializeField] private Vector2 spawnOffset = new Vector2(0, 0.3f);

        [Header("═══ RANDOM VARIANCE ═══")]
        [Tooltip("Enable random variance in rewards")]
        [SerializeField] private bool useRandomVariance = true;

        [Tooltip("Minimum multiplier (e.g., 0.8 = 80% of base)")]
        [SerializeField] private float minMultiplier = 0.8f;

        [Tooltip("Maximum multiplier (e.g., 1.2 = 120% of base)")]
        [SerializeField] private float maxMultiplier = 1.2f;

        [Header("═══ FALLBACK (No Prefab) ═══")]
        [Tooltip("If true, gives rewards directly when no prefab assigned")]
        [SerializeField] private bool fallbackToDirectReward = true;

        [Header("═══ DEBUG ═══")]
        [SerializeField] private bool showDebugLogs = true;

        // Reference to health component
        private EnemyHealth enemyHealth;

        private void Awake()
        {
            enemyHealth = GetComponent<EnemyHealth>();

            if (enemyHealth == null)
            {
                Debug.LogError($"[EnemyDropHandler] ❌ EnemyHealth not found on {gameObject.name}!");
            }
        }

        /// <summary>
        /// Called when the enemy dies. Should be invoked by EnemyHealth.
        /// </summary>
        public void OnEnemyDeath()
        {
            DropRewards();
        }

        /// <summary>
        /// Spawn pickup prefabs or give rewards directly
        /// </summary>
        private void DropRewards()
        {
            // Calculate final rewards (with optional variance)
            int finalXP = CalculateReward(xpReward);
            int finalGold = CalculateReward(goldReward);

            Vector3 spawnPos = transform.position + (Vector3)spawnOffset;

            bool goldDropped = false;
            bool xpDropped = false;

            // ═══ SPAWN GOLD PICKUPS ═══
            if (finalGold > 0)
            {
                if (goldPickupPrefab != null)
                {
                    SpawnPickups(goldPickupPrefab, DropPickup.PickupType.Gold, finalGold, goldPickupCount, spawnPos);
                    goldDropped = true;
                }
                else if (fallbackToDirectReward)
                {
                    GiveGoldDirectly(finalGold);
                    goldDropped = true;
                }
                else if (showDebugLogs)
                {
                    Debug.LogWarning($"[EnemyDropHandler] ⚠️ Gold Pickup Prefab atanmamış!");
                }
            }

            // ═══ SPAWN XP PICKUPS ═══
            if (finalXP > 0)
            {
                if (xpPickupPrefab != null)
                {
                    SpawnPickups(xpPickupPrefab, DropPickup.PickupType.XP, finalXP, xpPickupCount, spawnPos);
                    xpDropped = true;
                }
                else if (fallbackToDirectReward)
                {
                    GiveXPDirectly(finalXP);
                    xpDropped = true;
                }
                else if (showDebugLogs)
                {
                    Debug.LogWarning($"[EnemyDropHandler] ⚠️ XP Pickup Prefab atanmamış!");
                }
            }

            // Summary log
            if (showDebugLogs)
            {
                string goldStatus = goldDropped ? $"+{finalGold} Gold" : "Gold YOK";
                string xpStatus = xpDropped ? $"+{finalXP} XP" : "XP YOK";
                Debug.Log($"[EnemyDropHandler] 💀 {gameObject.name} öldü! Drop: {goldStatus}, {xpStatus}");
            }
        }

        /// <summary>
        /// Spawn multiple pickup objects
        /// </summary>
        private void SpawnPickups(GameObject prefab, DropPickup.PickupType type, int totalAmount, int count, Vector3 position)
        {
            if (count <= 0) count = 1;
            
            // Divide total amount among pickups
            int amountPerPickup = Mathf.Max(1, totalAmount / count);
            int remainder = totalAmount % count;

            for (int i = 0; i < count; i++)
            {
                // Last pickup gets the remainder
                int pickupAmount = amountPerPickup;
                if (i == count - 1)
                    pickupAmount += remainder;

                // Hepsi aynı noktadan spawn olsun, DropPickup kendisi dağılacak
                GameObject pickup = Instantiate(prefab, position, Quaternion.identity);
                
                // Initialize the pickup
                DropPickup dropPickup = pickup.GetComponent<DropPickup>();
                if (dropPickup != null)
                {
                    dropPickup.Initialize(type, pickupAmount);
                }
                else
                {
                    Debug.LogError($"[EnemyDropHandler] ❌ DropPickup component not found on prefab!");
                }
            }

            if (showDebugLogs)
                Debug.Log($"[EnemyDropHandler] ✨ Spawned {count}x {type} pickups (total: {totalAmount})");
        }

        /// <summary>
        /// Fallback: Give gold directly without pickup
        /// </summary>
        private void GiveGoldDirectly(int amount)
        {
            if (HappyHarvest.GameManager.Instance?.Player != null)
            {
                HappyHarvest.GameManager.Instance.Player.Coins += amount;
                
                if (showDebugLogs)
                    Debug.Log($"[EnemyDropHandler] 💰 Direct: +{amount} Gold");
            }
        }

        /// <summary>
        /// Fallback: Give XP directly without pickup
        /// </summary>
        private void GiveXPDirectly(int amount)
        {
            if (PlayerProgression.Instance != null)
            {
                PlayerProgression.Instance.AddXP(amount);
                
                if (showDebugLogs)
                    Debug.Log($"[EnemyDropHandler] ⭐ Direct: +{amount} XP");
            }
        }

        /// <summary>
        /// Calculate reward with optional random variance.
        /// </summary>
        private int CalculateReward(int baseReward)
        {
            if (!useRandomVariance || baseReward <= 0)
            {
                return baseReward;
            }

            float multiplier = Random.Range(minMultiplier, maxMultiplier);
            return Mathf.RoundToInt(baseReward * multiplier);
        }

        #region ═══════ PUBLIC ACCESSORS ═══════

        public int GetBaseXPReward() => xpReward;
        public int GetBaseGoldReward() => goldReward;

        public void SetRewards(int xp, int gold)
        {
            xpReward = xp;
            goldReward = gold;
        }

        public void SetPickupPrefabs(GameObject goldPrefab, GameObject xpPrefab)
        {
            goldPickupPrefab = goldPrefab;
            xpPickupPrefab = xpPrefab;
        }

        #endregion

        #region ═══════ EDITOR TESTS ═══════

        [ContextMenu("💀 Test: Simulate Death Drop")]
        private void TestDropRewards()
        {
            DropRewards();
        }

        #endregion
    }
}