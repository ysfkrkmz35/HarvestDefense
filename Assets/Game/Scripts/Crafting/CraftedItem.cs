using UnityEngine;
using HappyHarvest;
using System.Collections.Generic;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// A HappyHarvest Item that wraps a CraftingItem.
    /// Created dynamically at runtime to add crafted items to main inventory.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCraftedItem", menuName = "HarvestDefense/Crafting/Crafted Item (HappyHarvest)")]
    public class CraftedItem : Item
    {
        [Header("═══ SOURCE CRAFTING ITEM ═══")]
        [Tooltip("The CraftingItem this wraps")]
        public CraftingItem SourceItem;
        
        // Static cache to avoid creating duplicate runtime items
        private static Dictionary<CraftingItem, CraftedItem> s_Cache = new Dictionary<CraftingItem, CraftedItem>();
        
        /// <summary>
        /// Get or create a CraftedItem from a CraftingItem (cached)
        /// </summary>
        public static CraftedItem GetOrCreate(CraftingItem craftingItem)
        {
            if (craftingItem == null) return null;
            
            // Return cached if exists
            if (s_Cache.TryGetValue(craftingItem, out var cached) && cached != null)
            {
                return cached;
            }
            
            // Use linked HappyHarvestItem if available (convert to CraftedItem for compatibility)
            if (craftingItem.HappyHarvestItem != null)
            {
                // If it's already a CraftedItem, return it
                if (craftingItem.HappyHarvestItem is CraftedItem ci)
                {
                    s_Cache[craftingItem] = ci;
                    return ci;
                }
                // Otherwise return null - let caller use HappyHarvestItem directly
                return null;
            }
            
            // Create new runtime item
            var craftedItem = ScriptableObject.CreateInstance<CraftedItem>();
            craftedItem.SourceItem = craftingItem;
            craftedItem.UniqueID = "Crafted_" + craftingItem.ItemName.Replace(" ", "_");
            craftedItem.DisplayName = craftingItem.ItemName;
            craftedItem.ItemSprite = craftingItem.Icon;
            craftedItem.MaxStackSize = craftingItem.MaxStackSize;
            craftedItem.Consumable = false;
            craftedItem.BuyPrice = -1;
            craftedItem.name = craftingItem.ItemName;
            
            // Cache it
            s_Cache[craftingItem] = craftedItem;
            
            Debug.Log($"[CraftedItem] Created runtime item: {craftedItem.DisplayName}");
            return craftedItem;
        }
        
        /// <summary>
        /// Legacy method - redirects to GetOrCreate
        /// </summary>
        public static CraftedItem CreateFromCraftingItem(CraftingItem craftingItem)
        {
            return GetOrCreate(craftingItem);
        }
        
        /// <summary>
        /// Clear the cache (call on scene unload if needed)
        /// </summary>
        public static void ClearCache()
        {
            s_Cache.Clear();
        }
        
        /// <summary>
        /// Placeable items can always be used
        /// </summary>
        public override bool CanUse(Vector3Int target)
        {
            return SourceItem != null && SourceItem.IsPlaceable;
        }
        
        /// <summary>
        /// Using a placeable item enters placement mode
        /// </summary>
        public override bool Use(Vector3Int target)
        {
            if (SourceItem == null || !SourceItem.IsPlaceable) return false;
            
            // Find the ItemPlacer and start placing
            var placer = Object.FindObjectOfType<ItemPlacer>();
            if (placer != null)
            {
                Debug.Log($"[CraftedItem] Use: Starting placement for {SourceItem.ItemName}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Placeable items don't need a target grid position
        /// </summary>
        public override bool NeedTarget()
        {
            return false;
        }
    }
}

