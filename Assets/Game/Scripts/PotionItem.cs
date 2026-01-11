using UnityEngine;

namespace HappyHarvest
{
    /// <summary>
    /// Potion item that restores health when consumed.
    /// Works with HappyHarvest inventory system - auto-consumed on use.
    /// </summary>
    [CreateAssetMenu(fileName = "PotionItem", menuName = "2D Farming/Items/Potion")]
    public class PotionItem : Item
    {
        [Header("═══ POTION SETTINGS ═══")]
        [Tooltip("Amount of health restored when consumed")]
        public float HealAmount = 25f;
        
        [Tooltip("Optional visual/sound effect prefab to spawn on use")]
        public GameObject HealEffectPrefab;
        
        /// <summary>
        /// Potion does NOT need a tile target - can be used anywhere
        /// </summary>
        public override bool NeedTarget()
        {
            return false;
        }
        
        /// <summary>
        /// Can use potion anytime (as long as we have one)
        /// </summary>
        public override bool CanUse(Vector3Int target)
        {
            return true;
        }
        
        /// <summary>
        /// Consume the potion and heal the player.
        /// The Consumable flag in base Item handles stack reduction.
        /// </summary>
        public override bool Use(Vector3Int target)
        {
            Debug.Log($"[PotionItem] 🧪 Using potion: +{HealAmount} HP");
            
            // Find the health UI and heal
            var healthUI = Object.FindFirstObjectByType<ProHealthManaUI>();
            if (healthUI != null)
            {
                healthUI.Heal(HealAmount);
                Debug.Log($"[PotionItem] ✅ Healed player for {HealAmount} HP");
                
                // Spawn heal effect if assigned
                if (HealEffectPrefab != null && GameManager.Instance?.Player != null)
                {
                    Object.Instantiate(HealEffectPrefab, 
                        GameManager.Instance.Player.transform.position, 
                        Quaternion.identity);
                }
            }
            else
            {
                Debug.LogWarning("[PotionItem] ⚠️ ProHealthManaUI not found! Cannot heal.");
            }
            
            return true; // Return true to consume the item
        }
    }
}
