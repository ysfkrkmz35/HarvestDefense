using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// Manages crafting operations - checking requirements, executing crafts.
    /// </summary>
    public class CraftingManager : MonoBehaviour
    {
        public static CraftingManager Instance { get; private set; }
        
        [Header("═══ RECIPES ═══")]
        [SerializeField] private List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();
        
        [Header("═══ DEBUG ═══")]
        [SerializeField] private bool showDebugLogs = true;
        
        // Unlocked recipes (for progression)
        private HashSet<CraftingRecipe> unlockedRecipes = new HashSet<CraftingRecipe>();
        
        // Events
        public event Action<CraftingRecipe> OnRecipeCrafted;
        public event Action OnRecipeUnlocked;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            InitializeRecipes();
        }
        
        private void InitializeRecipes()
        {
            // Unlock default recipes
            foreach (var recipe in allRecipes)
            {
                if (recipe != null && recipe.UnlockedByDefault)
                {
                    unlockedRecipes.Add(recipe);
                }
            }
            
            if (showDebugLogs)
                Debug.Log($"[CraftingManager] Initialized with {allRecipes.Count} recipes, {unlockedRecipes.Count} unlocked");
        }
        
        #region ═══════ PUBLIC API ═══════
        
        /// <summary>
        /// Get all available (unlocked) recipes
        /// </summary>
        public List<CraftingRecipe> GetAvailableRecipes()
        {
            return allRecipes.Where(r => r != null && unlockedRecipes.Contains(r)).ToList();
        }
        
        /// <summary>
        /// Get all recipes (for UI display)
        /// </summary>
        public List<CraftingRecipe> GetAllRecipes()
        {
            return allRecipes.Where(r => r != null).ToList();
        }
        
        /// <summary>
        /// Check if a recipe is unlocked
        /// </summary>
        public bool IsRecipeUnlocked(CraftingRecipe recipe)
        {
            return recipe != null && unlockedRecipes.Contains(recipe);
        }
        
        /// <summary>
        /// Check if player can craft this recipe (has all materials)
        /// </summary>
        public bool CanCraft(CraftingRecipe recipe)
        {
            if (recipe == null) return false;
            if (!IsRecipeUnlocked(recipe)) return false;
            
            var inventory = CraftingInventory.Instance;
            if (inventory == null) return false;
            
            // Check crafting limit (MaxCraftable)
            if (recipe.MaxCraftable > 0)
            {
                int currentCount = inventory.GetItemCount(recipe.ResultItem);
                if (currentCount >= recipe.MaxCraftable)
                {
                    return false; // Already have max amount
                }
            }
            
            // Check all ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                if (!inventory.HasItem(ingredient.Item, ingredient.Amount))
                {
                    return false;
                }
            }
            
            // Check station requirement
            if (recipe.RequiredStation != null)
            {
                // TODO: Check if player is near the required station
                // For now, skip station check
            }
            
            return true;
        }
        
        /// <summary>
        /// Execute crafting - consume ingredients and produce result
        /// </summary>
        public bool Craft(CraftingRecipe recipe)
        {
            if (!CanCraft(recipe))
            {
                if (showDebugLogs) Debug.Log($"[CraftingManager] Cannot craft {recipe?.RecipeName}");
                return false;
            }
            
            var inventory = CraftingInventory.Instance;
            
            // Consume ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                inventory.RemoveItem(ingredient.Item, ingredient.Amount);
            }
            
            // Add result to CraftingInventory
            inventory.AddItem(recipe.ResultItem, recipe.ResultAmount);
            
            // ALSO add to HappyHarvest main inventory (toolbar) if linked
            if (recipe.ResultItem.HappyHarvestItem != null)
            {
                var playerController = FindObjectOfType<HappyHarvest.PlayerController>();
                if (playerController != null && playerController.Inventory != null)
                {
                    playerController.Inventory.AddItem(recipe.ResultItem.HappyHarvestItem, recipe.ResultAmount);
                    if (showDebugLogs)
                        Debug.Log($"[CraftingManager] Added to main inventory: {recipe.ResultItem.HappyHarvestItem.DisplayName}");
                }
                else
                {
                    if (showDebugLogs)
                        Debug.LogWarning("[CraftingManager] PlayerController/Inventory not found for main inventory!");
                }
            }
            
            if (showDebugLogs)
                Debug.Log($"[CraftingManager] ✅ Crafted {recipe.ResultAmount}x {recipe.ResultItem.ItemName}!");
            
            OnRecipeCrafted?.Invoke(recipe);
            return true;
        }
        
        /// <summary>
        /// Unlock a new recipe
        /// </summary>
        public void UnlockRecipe(CraftingRecipe recipe)
        {
            if (recipe == null) return;
            if (unlockedRecipes.Contains(recipe)) return;
            
            unlockedRecipes.Add(recipe);
            if (showDebugLogs)
                Debug.Log($"[CraftingManager] 🔓 Unlocked recipe: {recipe.RecipeName}");
            
            OnRecipeUnlocked?.Invoke();
        }
        
        /// <summary>
        /// Get missing ingredients for a recipe
        /// </summary>
        public List<(CraftingItem item, int have, int need)> GetMissingIngredients(CraftingRecipe recipe)
        {
            var result = new List<(CraftingItem, int, int)>();
            if (recipe == null) return result;
            
            var inventory = CraftingInventory.Instance;
            if (inventory == null) return result;
            
            foreach (var ingredient in recipe.Ingredients)
            {
                int have = inventory.GetItemCount(ingredient.Item);
                if (have < ingredient.Amount)
                {
                    result.Add((ingredient.Item, have, ingredient.Amount));
                }
            }
            
            return result;
        }
        /// <summary>
        /// Get reason why crafting is blocked (for UI display)
        /// </summary>
        public string GetCraftBlockReason(CraftingRecipe recipe)
        {
            if (recipe == null) return "Invalid recipe";
            if (!IsRecipeUnlocked(recipe)) return "Recipe locked";
            
            var inventory = CraftingInventory.Instance;
            if (inventory == null) return "No inventory";
            
            // Check MaxCraftable limit
            if (recipe.MaxCraftable > 0)
            {
                int currentCount = inventory.GetItemCount(recipe.ResultItem);
                if (currentCount >= recipe.MaxCraftable)
                {
                    return $"Already have {currentCount}/{recipe.MaxCraftable}";
                }
            }
            
            // Check ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                if (!inventory.HasItem(ingredient.Item, ingredient.Amount))
                {
                    int have = inventory.GetItemCount(ingredient.Item);
                    return $"Need {ingredient.Amount - have} more {ingredient.Item.ItemName}";
                }
            }
            
            // Check station
            if (recipe.RequiredStation != null)
            {
                return $"Requires {recipe.RequiredStation.ItemName}";
            }
            
            return ""; // Can craft!
        }
        
        #endregion
    }
}
