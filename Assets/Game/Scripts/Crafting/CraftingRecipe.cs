using UnityEngine;
using System;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// Defines a crafting recipe - what items are needed and what is produced.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "HarvestDefense/Crafting/Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [Header("═══ RECIPE INFO ═══")]
        public string RecipeName;
        public Sprite RecipeIcon;
        [TextArea] public string Description;
        
        [Header("═══ REQUIREMENTS ═══")]
        public RecipeIngredient[] Ingredients;
        
        [Header("═══ RESULT ═══")]
        public CraftingItem ResultItem;
        public int ResultAmount = 1;
        
        [Header("═══ CRAFTING LIMITS ═══")]
        [Tooltip("Max number player can have. 0 = unlimited. 1 = can only craft if don't have one.")]
        public int MaxCraftable = 0;
        
        [Header("═══ CRAFTING STATION ═══")]
        [Tooltip("Leave null for hand-crafting. Otherwise requires this station.")]
        public CraftingItem RequiredStation;
        
        [Header("═══ UNLOCK ═══")]
        public bool UnlockedByDefault = true;
        
        [Serializable]
        public class RecipeIngredient
        {
            public CraftingItem Item;
            public int Amount = 1;
        }
    }
}
