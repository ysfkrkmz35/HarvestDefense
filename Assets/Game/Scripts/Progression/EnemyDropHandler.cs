using UnityEngine;

namespace YusufTest
{
    /// <summary>
    /// Enemy Drop Handler
    /// - Attach to enemies to define XP/Gold rewards
    /// - Listens to EnemyHealth death and broadcasts drops
    /// - Automatically finds PlayerProgression and adds rewards
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyDropHandler : MonoBehaviour
    {
        [Header("═══ DROP REWARDS ═══")]
        [Tooltip("XP awarded when this enemy dies")]
        [SerializeField] private int xpReward = 25;

        [Tooltip("Gold awarded when this enemy dies")]
        [SerializeField] private int goldReward = 10;

        [Header("═══ RANDOM VARIANCE ═══")]
        [Tooltip("Enable random variance in rewards")]
        [SerializeField] private bool useRandomVariance = true;

        [Tooltip("Minimum multiplier (e.g., 0.8 = 80% of base)")]
        [SerializeField] private float minMultiplier = 0.8f;

        [Tooltip("Maximum multiplier (e.g., 1.2 = 120% of base)")]
        [SerializeField] private float maxMultiplier = 1.2f;

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
        /// Calculate and give rewards to player.
        /// </summary>
        private void DropRewards()
        {
            // Calculate final rewards (with optional variance)
            int finalXP = CalculateReward(xpReward);
            int finalGold = CalculateReward(goldReward);

            // Find PlayerProgression and add rewards
            if (PlayerProgression.Instance != null)
            {
                if (finalXP > 0)
                {
                    PlayerProgression.Instance.AddXP(finalXP);
                }

                if (finalGold > 0)
                {
                    PlayerProgression.Instance.AddGold(finalGold);
                }

                if (showDebugLogs)
                {
                    Debug.Log($"[EnemyDropHandler] 💀 {gameObject.name} dropped: +{finalXP} XP, +{finalGold} Gold");
                }
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning("[EnemyDropHandler] ⚠️ PlayerProgression.Instance is null! Rewards not given.");
                }
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

        /// <summary>Get base XP reward (before variance)</summary>
        public int GetBaseXPReward() => xpReward;

        /// <summary>Get base gold reward (before variance)</summary>
        public int GetBaseGoldReward() => goldReward;

        /// <summary>Set rewards at runtime (for scaling difficulty)</summary>
        public void SetRewards(int xp, int gold)
        {
            xpReward = xp;
            goldReward = gold;
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
