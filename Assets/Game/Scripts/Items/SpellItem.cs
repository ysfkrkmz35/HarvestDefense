using UnityEngine;

namespace HappyHarvest
{
    /// <summary>
    /// SpellItem - An Item wrapper for spells that can be purchased from the shop.
    /// When bought, the spell is equipped to a spell slot instead of going to inventory.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpellItem", menuName = "HarvestDefense/Spell Item", order = 2)]
    public class SpellItem : Item
    {
        [Header("═══ SPELL REFERENCE ═══")]
        [Tooltip("The SpellData this item represents")]
        public SpellData spellData;

        [Tooltip("The SpellBase prefab to instantiate when equipped")]
        public GameObject spellPrefab;

        [Tooltip("Target spell slot index (0-3). -1 = first empty slot")]
        public int targetSlotIndex = -1;

        /// <summary>
        /// SpellItems cannot be used from inventory - they auto-equip when bought
        /// </summary>
        public override bool CanUse(Vector3Int target)
        {
            return false;
        }

        /// <summary>
        /// SpellItems don't need a target
        /// </summary>
        public override bool NeedTarget()
        {
            return false;
        }

        /// <summary>
        /// Use does nothing - spells are equipped through BuyItem special handling
        /// </summary>
        public override bool Use(Vector3Int target)
        {
            return false;
        }

        /// <summary>
        /// Try to equip this spell to a spell slot
        /// </summary>
        public bool TryEquipSpell()
        {
            if (SpellManager.Instance == null)
            {
                Debug.LogError("[SpellItem] SpellManager not found!");
                return false;
            }

            if (spellData == null)
            {
                Debug.LogError($"[SpellItem] No spell data assigned for {DisplayName}!");
                return false;
            }

            // Find target slot
            int slot = targetSlotIndex;
            if (slot < 0)
            {
                // Find first empty slot
                for (int i = 0; i < 4; i++)
                {
                    if (SpellManager.Instance.GetSpellAtSlot(i) == null)
                    {
                        slot = i;
                        break;
                    }
                }
            }

            if (slot < 0 || slot >= 4)
            {
                Debug.Log("[SpellItem] No empty spell slots available!");
                return false;
            }

            // Check if spell already equipped in any slot
            for (int i = 0; i < 4; i++)
            {
                var existing = SpellManager.Instance.GetSpellDataAtSlot(i);
                if (existing != null && existing == spellData)
                {
                    Debug.Log($"[SpellItem] Spell {DisplayName} already equipped in slot {i + 1}!");
                    return false;
                }
            }

            // Create spell object dynamically as child of SpellManager
            GameObject spellObj = new GameObject(spellData.spellName);
            spellObj.transform.SetParent(SpellManager.Instance.transform);

            // Add AreaSpell component (or use spellPrefab if provided)
            SpellBase spellBase = null;
            if (spellPrefab != null)
            {
                Debug.Log($"[SpellItem] Using spellPrefab: {spellPrefab.name}");
                
                // Instantiate from prefab
                GameObject prefabInstance = Instantiate(spellPrefab, SpellManager.Instance.transform);
                prefabInstance.name = spellData.spellName;
                spellBase = prefabInstance.GetComponent<SpellBase>();
                
                if (spellBase == null)
                {
                    Debug.LogWarning($"[SpellItem] Prefab '{spellPrefab.name}' does not have a SpellBase component. Adding one dynamically based on spell type: {spellData.spellType}");
                    
                    // Try to add the correct component type dynamically based on spell type
                    switch (spellData.spellType)
                    {
                        case SpellType.Buff:
                            spellBase = prefabInstance.AddComponent<BuffSpell>();
                            break;
                        case SpellType.SelfHeal:
                            spellBase = prefabInstance.AddComponent<HealSpell>();
                            break;
                        case SpellType.Area:
                        case SpellType.Projectile:
                        case SpellType.Melee:
                        default:
                            spellBase = prefabInstance.AddComponent<AreaSpell>();
                            break;
                    }
                }
                
                Destroy(spellObj); // Don't need the empty object
            }
            else
            {
                // Create spell component dynamically based on spell type
                Debug.Log($"[SpellItem] Creating spell component for type: {spellData.spellType}");
                
                try
                {
                    switch (spellData.spellType)
                    {
                        case SpellType.Buff:
                            Debug.Log("[SpellItem] Adding BuffSpell component...");
                            spellBase = spellObj.AddComponent<BuffSpell>();
                            break;
                        case SpellType.SelfHeal:
                            Debug.Log("[SpellItem] Adding HealSpell component...");
                            spellBase = spellObj.AddComponent<HealSpell>();
                            break;
                        case SpellType.Area:
                        case SpellType.Projectile:
                        case SpellType.Melee:
                        default:
                            Debug.Log("[SpellItem] Adding AreaSpell component...");
                            spellBase = spellObj.AddComponent<AreaSpell>();
                            break;
                    }
                    
                    Debug.Log($"[SpellItem] AddComponent result: {(spellBase != null ? "Success" : "NULL")}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SpellItem] Exception adding component: {ex.Message}\n{ex.StackTrace}");
                }
            }

            if (spellBase == null)
            {
                Debug.LogError($"[SpellItem] Failed to create spell component!");
                Destroy(spellObj);
                return false;
            }

            // Assign spell data and equip to slot
            spellBase.SetSpellData(spellData);
            SpellManager.Instance.AssignSpellToSlot(slot, spellBase);
            
            // IMPORTANT: Ensure it's added to unlocked list for saving
            SpellManager.Instance.ForceUnlock(spellData);

            Debug.Log($"[SpellItem] ✨ Equipped {DisplayName} to slot {slot + 1}!");
            return true;
        }
    }
}
