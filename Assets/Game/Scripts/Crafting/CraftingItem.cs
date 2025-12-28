using UnityEngine;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// Item used in crafting recipes.
    /// Separate from HappyHarvest items for clean separation.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCraftingItem", menuName = "HarvestDefense/Crafting/Item")]
    public class CraftingItem : ScriptableObject
    {
        [Header("═══ BASIC INFO ═══")]
        public string ItemName;
        public Sprite Icon;
        [TextArea] public string Description;
        
        [Header("═══ STACKING ═══")]
        public int MaxStackSize = 99;
        
        [Header("═══ CATEGORY ═══")]
        public ItemCategory Category = ItemCategory.Material;
        
        public enum ItemCategory
        {
            Material,    // Wood, Stone, etc.
            Tool,        // Crafting Table
            Deployable,  // Boat, buildings
            Consumable   // Potions, food
        }
    }
}
