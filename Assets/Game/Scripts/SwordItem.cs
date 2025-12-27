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
        /// Sword can always be used if cooldown has passed
        /// </summary>
        public override bool CanUse(Vector3Int target)
        {
            // Return false to prevent PlayerController from showing the tile selection highlighter.
            // Since NeedTarget() is false, Use() will still be called by standard input.
            return false;
        }

        
        /// <summary>
        /// Called when the sword is used.
        /// Triggers the SwordCombat component to perform hit detection and damage.
        /// Animation is handled by the inventory system calling this via PlayerAnimatorTriggerUse.
        /// </summary>
        public override bool Use(Vector3Int target)
        {
            Debug.Log($"[SwordItem] Use() called! Setting animation trigger: {PlayerAnimatorTriggerUse}");
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
                }
                else
                {
                    Debug.LogWarning("[SwordItem] SwordCombat component not found on player! Add it for hit detection.");
                }
            }
            
            return true; // Return true to trigger the animation
        }
    }
}
