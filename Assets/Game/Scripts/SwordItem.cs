using UnityEngine;

namespace HappyHarvest
{
    /// <summary>
    /// Sword item that works with HappyHarvest inventory system.
    /// Does NOT require tile targeting - can attack anywhere with left click.
    /// Attack logic is handled by separate SwordCombat component on the player.
    /// </summary>
    [CreateAssetMenu(fileName = "SwordItem", menuName = "2D Farming/Items/Sword")]
    public class SwordItem : Item
    {
        [Header("═══ SWORD SETTINGS ═══")]
        [Tooltip("Damage dealt per attack (used by SwordCombat)")]
        public int Damage = 10;
        
        [Tooltip("Attack range in units (used by SwordCombat)")]
        public float AttackRange = 1.5f;
        
        [Tooltip("Cooldown between attacks in seconds")]
        public float AttackCooldown = 0.5f;
        
        [Tooltip("Attack arc in degrees (180 = half circle in front)")]
        public float AttackArc = 180f;
        
        // NonSerialized so it doesn't persist across play sessions in the ScriptableObject
        [System.NonSerialized]
        private float lastUseTime = -999f;
        
        /// <summary>
        /// Sword does NOT need a tile target - can be used anywhere
        /// </summary>
        public override bool NeedTarget()
        {
            return false;
        }
        
        /// <summary>
        /// Sword can always be used - no tile target needed
        /// </summary>
        public override bool CanUse(Vector3Int target)
        {
            // DÜZELTME: true döndür ki Use() çağrılabilsin
            return true;
        }

        
        /// <summary>
        /// Called when the sword is used.
        /// Triggers the SwordCombat component to perform hit detection and damage.
        /// Animation is handled by the inventory system calling this via PlayerAnimatorTriggerUse.
        /// </summary>
        public override bool Use(Vector3Int target)
        {
            Debug.Log($"[SwordItem] ⚔️ Use() called! Animation trigger: {PlayerAnimatorTriggerUse}");
            lastUseTime = Time.time;
            
            // Find the SwordCombat component and trigger the attack damage
            var player = GameManager.Instance?.Player;
            if (player != null)
            {
                var swordCombat = player.GetComponent<SwordCombat>();
                if (swordCombat != null)
                {
                    // Trigger the actual attack (hit detection + damage)
                    swordCombat.PerformAttackDamage();
                    Debug.Log("[SwordItem] ✅ SwordCombat.PerformAttackDamage() called!");
                }
                else
                {
                    Debug.LogError("[SwordItem] ❌ SwordCombat component not found on player! Add it for hit detection.");
                }
            }
            else
            {
                Debug.LogError("[SwordItem] ❌ GameManager.Instance.Player is null!");
            }
            
            return true; // Return true to trigger the animation
        }
    }
}