using UnityEngine;
using HappyHarvest;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// A HappyHarvest Item for crafting resources (Wood, Stone, etc.)
    /// Can be created as assets or dynamically from CraftingItems.
    /// </summary>
    [CreateAssetMenu(fileName = "NewResource", menuName = "HarvestDefense/Crafting/Resource Item")]
    public class ResourceItem : Item
    {
        [Header("═══ RESOURCE INFO ═══")]
        [Tooltip("The CraftingItem this resource represents")]
        public CraftingItem SourceCraftingItem;
        
        [Tooltip("Sell price when sold to market")]
        public int SellPrice = 1;
        
        /// <summary>
        /// Resources can always be used (dropped, etc.)
        /// </summary>
        public override bool CanUse(Vector3Int target)
        {
            return true;
        }
        
        /// <summary>
        /// Using a resource - could be eating, dropping, etc.
        /// </summary>
        public override bool Use(Vector3Int target)
        {
            // Resources don't have special use - just exist in inventory
            return true;
        }
        
        /// <summary>
        /// Resources don't need a grid target
        /// </summary>
        public override bool NeedTarget()
        {
            return false;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Automatically set fields from CraftingItem if linked
        /// </summary>
        private void OnValidate()
        {
            if (SourceCraftingItem != null)
            {
                if (string.IsNullOrEmpty(DisplayName))
                    DisplayName = SourceCraftingItem.ItemName;
                if (ItemSprite == null)
                    ItemSprite = SourceCraftingItem.Icon;
                if (string.IsNullOrEmpty(UniqueID))
                    UniqueID = "Resource_" + SourceCraftingItem.ItemName.Replace(" ", "_");
                if (MaxStackSize == 10) // Default value
                    MaxStackSize = SourceCraftingItem.MaxStackSize;
            }
        }
#endif
    }
}
