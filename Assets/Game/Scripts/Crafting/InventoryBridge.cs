using UnityEngine;
using HappyHarvest;
using System.Collections.Generic;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// Bridge between CraftingSystem and HappyHarvest InventorySystem.
    /// Provides crafting-friendly API to check/modify HH inventory.
    /// </summary>
    public static class InventoryBridge
    {
        /// <summary>
        /// Get the player's HappyHarvest inventory
        /// </summary>
        public static InventorySystem GetPlayerInventory()
        {
            var player = Object.FindObjectOfType<PlayerController>();
            return player?.Inventory;
        }
        
        /// <summary>
        /// Count how many of a CraftingItem the player has in HH inventory
        /// </summary>
        public static int GetItemCount(CraftingItem craftingItem)
        {
            var inventory = GetPlayerInventory();
            if (inventory == null || craftingItem == null) return 0;
            
            // Get the HH Item for this CraftingItem
            var hhItem = GetHHItem(craftingItem);
            if (hhItem == null) return 0;
            
            int count = 0;
            for (int i = 0; i < InventorySystem.InventorySize; i++)
            {
                if (inventory.Entries[i].Item == hhItem)
                {
                    count += inventory.Entries[i].StackSize;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Check if player has at least 'amount' of a CraftingItem
        /// </summary>
        public static bool HasItem(CraftingItem craftingItem, int amount = 1)
        {
            return GetItemCount(craftingItem) >= amount;
        }
        
        /// <summary>
        /// Add a CraftingItem to HH inventory
        /// </summary>
        public static bool AddItem(CraftingItem craftingItem, int amount = 1)
        {
            var inventory = GetPlayerInventory();
            if (inventory == null || craftingItem == null) return false;
            
            var hhItem = GetOrCreateHHItem(craftingItem);
            if (hhItem == null) return false;
            
            Debug.Log($"[InventoryBridge] Adding '{hhItem.name}' (Sprite: {hhItem.ItemSprite?.name ?? "NULL"})");
            
            bool success = inventory.AddItem(hhItem, amount);
            
            // Refresh UI after adding
            if (success)
            {
                UIHandler.UpdateInventory(inventory);
                
                // Debug: Show what's in inventory now
                Debug.Log("[InventoryBridge] Current inventory contents:");
                for (int i = 0; i < InventorySystem.InventorySize; i++)
                {
                    var entry = inventory.Entries[i];
                    if (entry.Item != null)
                    {
                        Debug.Log($"  Slot {i}: {entry.Item.name} x{entry.StackSize} (Sprite: {entry.Item.ItemSprite?.name ?? "NULL"})");
                    }
                }
            }
            
            return success;
        }
        
        /// <summary>
        /// Remove a CraftingItem from HH inventory
        /// </summary>
        public static bool RemoveItem(CraftingItem craftingItem, int amount = 1)
        {
            var inventory = GetPlayerInventory();
            if (inventory == null || craftingItem == null) return false;
            
            var hhItem = GetHHItem(craftingItem);
            if (hhItem == null) return false;
            
            // Check if we have enough first
            if (GetItemCount(craftingItem) < amount) return false;
            
            // Remove from inventory
            int remaining = amount;
            for (int i = 0; i < InventorySystem.InventorySize && remaining > 0; i++)
            {
                if (inventory.Entries[i].Item == hhItem)
                {
                    int remove = Mathf.Min(inventory.Entries[i].StackSize, remaining);
                    remaining -= inventory.Remove(i, remove);
                }
            }
            
            return remaining == 0;
        }
        
        /// <summary>
        /// Get the HH Item for a CraftingItem (uses linked or cached)
        /// </summary>
        public static Item GetHHItem(CraftingItem craftingItem)
        {
            if (craftingItem == null) return null;
            
            // Use linked HH Item if available
            if (craftingItem.HappyHarvestItem != null)
                return craftingItem.HappyHarvestItem;
            
            // Otherwise use cached CraftedItem wrapper
            return CraftedItem.GetOrCreate(craftingItem);
        }
        
        /// <summary>
        /// Get or create HH Item for a CraftingItem (always returns something)
        /// </summary>
        public static Item GetOrCreateHHItem(CraftingItem craftingItem)
        {
            if (craftingItem == null) return null;
            
            // Use linked HH Item if available
            if (craftingItem.HappyHarvestItem != null)
                return craftingItem.HappyHarvestItem;
            
            // Create CraftedItem wrapper
            return CraftedItem.GetOrCreate(craftingItem);
        }
        
        /// <summary>
        /// Get all CraftingItems and their counts from HH inventory
        /// (For UI display - maps back from HH Items to CraftingItems)
        /// </summary>
        public static Dictionary<CraftingItem, int> GetAllCraftingItems()
        {
            var result = new Dictionary<CraftingItem, int>();
            var inventory = GetPlayerInventory();
            if (inventory == null) return result;
            
            for (int i = 0; i < InventorySystem.InventorySize; i++)
            {
                var entry = inventory.Entries[i];
                if (entry.Item == null) continue;
                
                // If it's a CraftedItem, get the source
                if (entry.Item is CraftedItem ci && ci.SourceItem != null)
                {
                    if (result.ContainsKey(ci.SourceItem))
                        result[ci.SourceItem] += entry.StackSize;
                    else
                        result[ci.SourceItem] = entry.StackSize;
                }
            }
            
            return result;
        }
    }
}
