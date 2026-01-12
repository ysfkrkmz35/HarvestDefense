using UnityEngine;

namespace HappyHarvest
{
    /// <summary>
    /// Auto-equips all spells when the Boss Scene loads.
    /// Add this component to a GameObject in the Boss Scene.
    /// </summary>
    public class BossSceneSpellSetup : MonoBehaviour
    {
        [Header("═══ SPELL ITEMS TO EQUIP ═══")]
        [Tooltip("Assign all SpellItem assets to equip in boss scene")]
        public SpellItem[] spellItems;
        
        [Header("═══ DEBUG ═══")]
        public bool showDebugLogs = true;
        
        private void Start()
        {
            // Wait a frame for SpellManager to initialize
            StartCoroutine(EquipSpellsDelayed());
        }
        
        private System.Collections.IEnumerator EquipSpellsDelayed()
        {
            yield return null; // Wait one frame
            
            if (SpellManager.Instance == null)
            {
                if (showDebugLogs) Debug.LogError("[BossSceneSpellSetup] SpellManager not found!");
                yield break;
            }
            
            if (showDebugLogs) Debug.Log($"[BossSceneSpellSetup] ⚡ Equipping {spellItems?.Length ?? 0} spells for boss fight...");
            
            if (spellItems == null || spellItems.Length == 0)
            {
                if (showDebugLogs) Debug.LogWarning("[BossSceneSpellSetup] No spell items assigned!");
                yield break;
            }
            
            int equipped = 0;
            foreach (var spellItem in spellItems)
            {
                if (spellItem == null) continue;
                
                if (spellItem.TryEquipSpell())
                {
                    equipped++;
                    if (showDebugLogs) Debug.Log($"[BossSceneSpellSetup] ✅ Equipped: {spellItem.DisplayName}");
                }
                else
                {
                    if (showDebugLogs) Debug.LogWarning($"[BossSceneSpellSetup] ⚠️ Failed to equip: {spellItem.DisplayName}");
                }
            }
            
            if (showDebugLogs) Debug.Log($"[BossSceneSpellSetup] 🎮 Boss fight ready! {equipped}/{spellItems.Length} spells equipped.");
        }
    }
}
