using UnityEngine;
using HappyHarvest;
using System.Collections.Generic;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// Sets up crafting demo with Sword (Wood+Stone) and Potion (Veggie).
    /// Both require CraftingTable. Clears and resets inventory on start.
    /// </summary>
    public class CraftingSystemSetup : MonoBehaviour
    {
        [Header("═══ EXISTING ITEMS (Assign in Inspector) ═══")]
        public CraftingItem woodItem;
        public CraftingItem craftingTableItem;
        
        [Header("═══ CRAFTED RESULT ITEMS (Assign HH Items) ═══")]
        [Tooltip("Assign existing SwordItem (Barış/Sword.asset) - will be given when sword is crafted")]
        public HappyHarvest.Item existingSwordItem;
        [Tooltip("Leave null to create runtime potion")]
        public HappyHarvest.Item existingPotionItem;
        
        [Header("═══ DEBUG ═══")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private int demoResourceAmount = 10;
        
        [Header("═══ SCENE-SPECIFIC RESET ═══")]
        [Tooltip("Only reset inventory in specific scene")]
        [SerializeField] private bool resetInventoryOnStart = true;
        [SerializeField] private string demoSceneName = "Yusuf_Test 9";
        
        // Runtime-created items
        private CraftingItem stoneItem;
        private CraftingItem veggieItem;
        private CraftingItem swordItem;
        private CraftingItem potionItem;
        
        // Runtime-created recipes
        private CraftingRecipe swordRecipe;
        private CraftingRecipe potionRecipe;
        
        private void Start()
        {
            if (setupOnStart)
            {
                // Delay to let HH inventory initialize
                Invoke(nameof(SetupDemoScene), 0.5f);
            }
        }
        
        [ContextMenu("🎮 Setup Demo Scene")]
        public void SetupDemoScene()
        {
            // Check if we're in the correct scene
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            Debug.Log($"[CraftingSetup] ═══ Setting up demo scene (Current: {currentScene}) ═══");
            
            // Only reset inventory if in demo scene
            if (resetInventoryOnStart && currentScene == demoSceneName)
            {
                Debug.Log($"[CraftingSetup] Scene matches '{demoSceneName}' - resetting inventory");
                ClearInventory();
            }
            else if (resetInventoryOnStart)
            {
                Debug.Log($"[CraftingSetup] Scene '{currentScene}' != '{demoSceneName}' - skipping reset");
            }
            
            // Create items and recipes
            CreateDemoItems();
            CreateDemoRecipes();
            
            // Add demo resources to inventory
            AddDemoResources();
            
            Debug.Log("[CraftingSetup] ═══ Demo setup complete! ═══");
        }
        
        private void ClearInventory()
        {
            var inventory = InventoryBridge.GetPlayerInventory();
            if (inventory == null) return;
            
            // Clear all slots safely
            for (int i = 0; i < InventorySystem.InventorySize; i++)
            {
                if (inventory.Entries[i] != null)
                {
                    inventory.Entries[i].Item = null;
                    inventory.Entries[i].StackSize = 0;
                }
            }
            
            // Only update UI if HH GameManager is ready
            if (HappyHarvest.GameManager.Instance != null)
            {
                UIHandler.UpdateInventory(inventory);
            }
            
            Debug.Log("[CraftingSetup] Inventory cleared");
        }
        
        private void CreateDemoItems()
        {
            // Stone (material)
            if (stoneItem == null)
            {
                stoneItem = ScriptableObject.CreateInstance<CraftingItem>();
                stoneItem.name = "Stone";
                stoneItem.ItemName = "Stone";
                stoneItem.Description = "A hard rock. Used for crafting.";
                stoneItem.Category = CraftingItem.ItemCategory.Material;
                stoneItem.MaxStackSize = 99;
            }
            
            // Veggie (material)
            if (veggieItem == null)
            {
                veggieItem = ScriptableObject.CreateInstance<CraftingItem>();
                veggieItem.name = "Veggie";
                veggieItem.ItemName = "Veggie";
                veggieItem.Description = "Fresh vegetables. Can be used for potions.";
                veggieItem.Category = CraftingItem.ItemCategory.Material;
                veggieItem.MaxStackSize = 99;
            }
            
            // Sword (result - tool)
            if (swordItem == null)
            {
                swordItem = ScriptableObject.CreateInstance<CraftingItem>();
                swordItem.name = "Sword";
                swordItem.ItemName = "Sword";
                swordItem.Description = "A wooden sword for combat.";
                swordItem.Category = CraftingItem.ItemCategory.Tool;
                swordItem.MaxStackSize = 1;
                
                // Link to existing HH SwordItem if assigned
                if (existingSwordItem != null)
                {
                    swordItem.HappyHarvestItem = existingSwordItem;
                    swordItem.Icon = existingSwordItem.ItemSprite;
                    Debug.Log($"[CraftingSetup] Linked Sword to HH item: {existingSwordItem.name}");
                }
            }
            
            // Potion (result - consumable)
            if (potionItem == null)
            {
                potionItem = ScriptableObject.CreateInstance<CraftingItem>();
                potionItem.name = "Health Potion";
                potionItem.ItemName = "Health Potion";
                potionItem.Description = "Restores health when consumed.";
                potionItem.Category = CraftingItem.ItemCategory.Consumable;
                potionItem.MaxStackSize = 10;
                
                // Link to existing HH item if assigned
                if (existingPotionItem != null)
                {
                    potionItem.HappyHarvestItem = existingPotionItem;
                    potionItem.Icon = existingPotionItem.ItemSprite;
                    Debug.Log($"[CraftingSetup] Linked Potion to HH item: {existingPotionItem.name}");
                }
            }
            
            Debug.Log("[CraftingSetup] Demo items created");
        }
        
        private void CreateDemoRecipes()
        {
            var manager = CraftingManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[CraftingSetup] CraftingManager not found!");
                return;
            }
            
            // DEBUG: Check if craftingTableItem is assigned
            if (craftingTableItem == null)
            {
                Debug.LogError("[CraftingSetup] ⚠️ craftingTableItem is NULL! Assign it in Inspector. Recipes won't require station!");
            }
            else
            {
                Debug.Log($"[CraftingSetup] craftingTableItem OK: {craftingTableItem.ItemName}");
            }
            
            // Sword Recipe: 3 Wood + 2 Stone → 1 Sword (requires CraftingTable)
            if (swordRecipe == null && woodItem != null && stoneItem != null)
            {
                swordRecipe = ScriptableObject.CreateInstance<CraftingRecipe>();
                swordRecipe.name = "SwordRecipe";
                swordRecipe.RecipeName = "Wooden Sword";
                swordRecipe.Description = "A basic sword for combat.";
                swordRecipe.Ingredients = new CraftingRecipe.RecipeIngredient[]
                {
                    new CraftingRecipe.RecipeIngredient { Item = woodItem, Amount = 3 },
                    new CraftingRecipe.RecipeIngredient { Item = stoneItem, Amount = 2 }
                };
                swordRecipe.ResultItem = swordItem;
                swordRecipe.ResultAmount = 1;
                swordRecipe.RequiredStation = craftingTableItem;  // Requires table!
                swordRecipe.UnlockedByDefault = true;
                
                manager.AddRecipe(swordRecipe);
            }
            
            // Potion Recipe: 3 Veggie → 1 Health Potion (requires CraftingTable)
            if (potionRecipe == null && veggieItem != null)
            {
                potionRecipe = ScriptableObject.CreateInstance<CraftingRecipe>();
                potionRecipe.name = "PotionRecipe";
                potionRecipe.RecipeName = "Health Potion";
                potionRecipe.Description = "A healing potion made from veggies.";
                potionRecipe.Ingredients = new CraftingRecipe.RecipeIngredient[]
                {
                    new CraftingRecipe.RecipeIngredient { Item = veggieItem, Amount = 3 }
                };
                potionRecipe.ResultItem = potionItem;
                potionRecipe.ResultAmount = 1;
                potionRecipe.RequiredStation = craftingTableItem;  // Requires table!
                potionRecipe.UnlockedByDefault = true;
                
                manager.AddRecipe(potionRecipe);
            }
            
            Debug.Log("[CraftingSetup] Demo recipes created (require CraftingTable)");
        }
        
        private void AddDemoResources()
        {
            // Add Wood
            if (woodItem != null)
                InventoryBridge.AddItem(woodItem, demoResourceAmount);
            
            // Add Stone
            if (stoneItem != null)
                InventoryBridge.AddItem(stoneItem, demoResourceAmount);
            
            // Add Veggie
            if (veggieItem != null)
                InventoryBridge.AddItem(veggieItem, demoResourceAmount);
            
            Debug.Log($"[CraftingSetup] Added {demoResourceAmount} of each demo resource");
            
            // Refresh UI if HH GameManager is ready
            var inventory = InventoryBridge.GetPlayerInventory();
            if (inventory != null && HappyHarvest.GameManager.Instance != null)
            {
                UIHandler.UpdateInventory(inventory);
            }
        }
        
        [ContextMenu("📋 Debug: Show Inventory")]
        public void DebugShowInventory()
        {
            var inventory = InventoryBridge.GetPlayerInventory();
            if (inventory == null)
            {
                Debug.Log("[CraftingSetup] No inventory found");
                return;
            }
            
            Debug.Log("═══ HH INVENTORY ═══");
            for (int i = 0; i < InventorySystem.InventorySize; i++)
            {
                var entry = inventory.Entries[i];
                if (entry.Item != null)
                    Debug.Log($"  Slot {i}: {entry.Item.DisplayName} x{entry.StackSize}");
            }
        }
    }
}


