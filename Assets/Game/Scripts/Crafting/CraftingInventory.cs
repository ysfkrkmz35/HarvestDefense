using UnityEngine;
using System;
using System.Collections.Generic;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// Manages the player's crafting inventory (separate from HappyHarvest inventory).
    /// Handles materials used for crafting.
    /// </summary>
    public class CraftingInventory : MonoBehaviour
    {
        public static CraftingInventory Instance { get; private set; }
        
        [Header("═══ SETTINGS ═══")]
        [SerializeField] private int maxSlots = 20;
        
        [Header("═══ DEBUG ═══")]
        [SerializeField] private bool showDebugLogs = true;
        
        // Inventory data
        private Dictionary<CraftingItem, int> items = new Dictionary<CraftingItem, int>();
        
        // Events
        public event Action OnInventoryChanged;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        #region ═══════ PUBLIC API ═══════
        
        /// <summary>
        /// Add items to inventory
        /// </summary>
        public bool AddItem(CraftingItem item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;
            
            if (items.ContainsKey(item))
            {
                items[item] = Mathf.Min(items[item] + amount, item.MaxStackSize);
            }
            else
            {
                if (items.Count >= maxSlots)
                {
                    if (showDebugLogs) Debug.Log("[CraftingInventory] Inventory full!");
                    return false;
                }
                items[item] = Mathf.Min(amount, item.MaxStackSize);
            }
            
            if (showDebugLogs) Debug.Log($"[CraftingInventory] Added {amount}x {item.ItemName}");
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Remove items from inventory
        /// </summary>
        public bool RemoveItem(CraftingItem item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;
            if (!items.ContainsKey(item)) return false;
            
            if (items[item] < amount)
            {
                if (showDebugLogs) Debug.Log($"[CraftingInventory] Not enough {item.ItemName}");
                return false;
            }
            
            items[item] -= amount;
            if (items[item] <= 0)
            {
                items.Remove(item);
            }
            
            if (showDebugLogs) Debug.Log($"[CraftingInventory] Removed {amount}x {item.ItemName}");
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Check how many of an item we have
        /// </summary>
        public int GetItemCount(CraftingItem item)
        {
            if (item == null) return 0;
            return items.ContainsKey(item) ? items[item] : 0;
        }
        
        /// <summary>
        /// Check if we have enough of an item
        /// </summary>
        public bool HasItem(CraftingItem item, int amount = 1)
        {
            return GetItemCount(item) >= amount;
        }
        
        /// <summary>
        /// Get all items in inventory
        /// </summary>
        public Dictionary<CraftingItem, int> GetAllItems()
        {
            return new Dictionary<CraftingItem, int>(items);
        }
        
        /// <summary>
        /// Clear all items (for testing)
        /// </summary>
        public void ClearInventory()
        {
            items.Clear();
            OnInventoryChanged?.Invoke();
        }
        
        #endregion
        
        #region ═══════ DEBUG ═══════
        
        [ContextMenu("🎁 Add Test Materials")]
        private void AddTestMaterials()
        {
            // Find all crafting items in project and add some
            var allItems = Resources.FindObjectsOfTypeAll<CraftingItem>();
            foreach (var item in allItems)
            {
                AddItem(item, 10);
            }
        }
        
        #endregion
    }
}
