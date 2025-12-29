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
        
        // Station tracking - set when UI is opened from a CraftingTable
        public bool IsAtStation { get; private set; } = false;
        
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
            
            // Check HappyHarvest inventory via bridge
            if (InventoryBridge.GetPlayerInventory() == null) return false;
            
            // Check crafting limit (MaxCraftable)
            if (recipe.MaxCraftable > 0)
            {
                int currentCount = InventoryBridge.GetItemCount(recipe.ResultItem);
                if (currentCount >= recipe.MaxCraftable)
                {
                    return false; // Already have max amount
                }
            }
            
            // Check all ingredients in HH inventory
            foreach (var ingredient in recipe.Ingredients)
            {
                if (!InventoryBridge.HasItem(ingredient.Item, ingredient.Amount))
                {
                    return false;
                }
            }
            
            // Check station requirement
            if (recipe.RequiredStation != null)
            {
                if (!IsAtStation)
                {
                    return false; // Need to be at a crafting station
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Set whether player is at a crafting station (called by UI)
        /// </summary>
        public void SetAtStation(bool atStation)
        {
            IsAtStation = atStation;
            if (showDebugLogs)
                Debug.Log($"[CraftingManager] AtStation: {atStation}");
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
            
            // Consume ingredients from HH inventory
            foreach (var ingredient in recipe.Ingredients)
            {
                InventoryBridge.RemoveItem(ingredient.Item, ingredient.Amount);
            }
            
            // Add result to HH inventory
            bool addSuccess = InventoryBridge.AddItem(recipe.ResultItem, recipe.ResultAmount);
            
            if (showDebugLogs)
            {
                var hhItem = InventoryBridge.GetOrCreateHHItem(recipe.ResultItem);
                Debug.Log($"[CraftingManager] Craft result: {recipe.ResultItem.ItemName}");
                Debug.Log($"[CraftingManager] HH Item: {hhItem?.name ?? "NULL"} (Type: {hhItem?.GetType().Name ?? "?"})");
                Debug.Log($"[CraftingManager] AddItem success: {addSuccess}");
                
                if (addSuccess)
                    Debug.Log($"[CraftingManager] ✅ Crafted {recipe.ResultAmount}x {recipe.ResultItem.ItemName}!");
                else
                    Debug.LogError($"[CraftingManager] ❌ Failed to add {recipe.ResultItem.ItemName} to inventory!");
            }
            
            OnRecipeCrafted?.Invoke(recipe);
            return addSuccess;
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
        /// Add a recipe at runtime (and auto-unlock if UnlockedByDefault)
        /// </summary>
        public void AddRecipe(CraftingRecipe recipe)
        {
            if (recipe == null) return;
            if (allRecipes.Contains(recipe)) return;
            
            allRecipes.Add(recipe);
            
            if (recipe.UnlockedByDefault)
                unlockedRecipes.Add(recipe);
            
            if (showDebugLogs)
                Debug.Log($"[CraftingManager] ➕ Added recipe: {recipe.RecipeName}");
        }
        
        /// <summary>
        /// Get missing ingredients for a recipe
        /// </summary>
        public List<(CraftingItem item, int have, int need)> GetMissingIngredients(CraftingRecipe recipe)
        {
            var result = new List<(CraftingItem, int, int)>();
            if (recipe == null) return result;
            
            foreach (var ingredient in recipe.Ingredients)
            {
                int have = InventoryBridge.GetItemCount(ingredient.Item);
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
            
            if (InventoryBridge.GetPlayerInventory() == null) return "No inventory";
            
            // Check MaxCraftable limit
            if (recipe.MaxCraftable > 0)
            {
                int currentCount = InventoryBridge.GetItemCount(recipe.ResultItem);
                if (currentCount >= recipe.MaxCraftable)
                {
                    return $"Already have {currentCount}/{recipe.MaxCraftable}";
                }
            }
            
            // Check ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                if (!InventoryBridge.HasItem(ingredient.Item, ingredient.Amount))
                {
                    int have = InventoryBridge.GetItemCount(ingredient.Item);
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
