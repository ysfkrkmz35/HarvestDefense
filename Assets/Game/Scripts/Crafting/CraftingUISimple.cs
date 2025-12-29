using UnityEngine;
using System.Collections.Generic;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// Simple IMGUI-based Crafting UI that reliably handles clicks.
    /// Toggle with 'O' key. Draggable window with inventory display.
    /// </summary>
    public class CraftingUISimple : MonoBehaviour
    {
        [Header("═══ INPUT ═══")]
        [SerializeField] private KeyCode toggleKey = KeyCode.O;
        
        [Header("═══ STYLING ═══")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        [SerializeField] private Color headerColor = new Color(0.2f, 0.4f, 0.6f, 1f);
        [SerializeField] private Color buttonColor = new Color(0.3f, 0.5f, 0.3f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.4f, 0.6f, 0.8f, 1f);
        [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        
        // State
        private bool isOpen = false;
        private bool isAtStation = false;  // TRUE = opened from crafting table, FALSE = opened via O key
        private Rect windowRect = new Rect(100, 100, 420, 580);
        private Vector2 recipeScrollPosition;
        private Vector2 inventoryScrollPosition;
        private CraftingRecipe selectedRecipe;
        
        // Styles
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle successLabelStyle;
        private GUIStyle ingredientLabelStyle;
        private bool stylesInitialized = false;
        
        // Craft success feedback
        private string craftSuccessMessage = "";
        private float craftSuccessTimer = 0f;
        private const float CRAFT_SUCCESS_DURATION = 2f;
        
        // Public accessors
        public bool IsOpen => isOpen;
        public bool IsAtStation => isAtStation;
        
        /// <summary>
        /// Called by CraftingTableObject to open the UI (shows ALL recipes)
        /// </summary>
        public void OpenFromStation()
        {
            isOpen = true;
            isAtStation = true;  // Can craft station-required recipes
            
            // Sync to CraftingManager so CanCraft() works
            if (CraftingManager.Instance != null)
                CraftingManager.Instance.SetAtStation(true);
                
            Debug.Log("[CraftingUISimple] Opened from crafting station - ALL recipes available");
        }
        
        /// <summary>
        /// Open UI without station (only hand-craftable recipes)
        /// </summary>
        public void OpenHandCrafting()
        {
            isOpen = true;
            isAtStation = false;  // Only hand-craftable recipes
            
            // Sync to CraftingManager
            if (CraftingManager.Instance != null)
                CraftingManager.Instance.SetAtStation(false);
                
            Debug.Log("[CraftingUISimple] Opened for hand crafting only");
        }
        
        /// <summary>
        /// Close the UI
        /// </summary>
        public void Close()
        {
            isOpen = false;
            isAtStation = false;
            
            // Sync to CraftingManager
            if (CraftingManager.Instance != null)
                CraftingManager.Instance.SetAtStation(false);
        }
        
        private void Update()
        {
            // O key = toggle hand-crafting mode (no station recipes)
            if (Input.GetKeyDown(toggleKey))
            {
                if (isOpen)
                {
                    Close();
                }
                else
                {
                    OpenHandCrafting();  // O key = hand crafting only
                }
            }
            
            // Update success message timer
            if (craftSuccessTimer > 0)
            {
                craftSuccessTimer -= Time.deltaTime;
                if (craftSuccessTimer <= 0)
                    craftSuccessMessage = "";
            }
        }
        
        private void OnGUI()
        {
            if (!isOpen) return;
            
            InitStyles();
            
            // Draw main window
            windowRect = GUI.Window(12345, windowRect, DrawWindow, "", boxStyle);
        }
        
        private void InitStyles()
        {
            if (stylesInitialized) return;
            stylesInitialized = true;
            
            // Box style
            boxStyle = new GUIStyle(GUI.skin.box);
            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, backgroundColor);
            bgTex.Apply();
            boxStyle.normal.background = bgTex;
            boxStyle.padding = new RectOffset(10, 10, 10, 10);
            
            // Header style
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = Color.white;
            
            // Button style
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;
            buttonStyle.padding = new RectOffset(10, 10, 8, 8);
            
            // Label style
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 12;
            labelStyle.normal.textColor = Color.white;
            
            // Success label style
            successLabelStyle = new GUIStyle(GUI.skin.label);
            successLabelStyle.fontSize = 14;
            successLabelStyle.fontStyle = FontStyle.Bold;
            successLabelStyle.alignment = TextAnchor.MiddleCenter;
            successLabelStyle.normal.textColor = successColor;
            
            // Ingredient label style
            ingredientLabelStyle = new GUIStyle(GUI.skin.label);
            ingredientLabelStyle.fontSize = 11;
        }
        
        private void DrawWindow(int windowID)
        {
            // ═══ HEADER ═══
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            // Show station status for debugging
            string stationStatus = isAtStation ? "⚒ CRAFTING (at table)" : "⚒ CRAFTING (hand)";
            GUILayout.Label(stationStatus, headerStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(30), GUILayout.Height(25)))
            {
                Close();  // Use Close() to properly reset isAtStation
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // ═══ SUCCESS MESSAGE ═══
            if (!string.IsNullOrEmpty(craftSuccessMessage))
            {
                GUILayout.Label(craftSuccessMessage, successLabelStyle);
                GUILayout.Space(5);
            }
            
            // ═══ MAIN CONTENT AREA ═══
            GUILayout.BeginHorizontal();
            
            // ═══ LEFT: RECIPE LIST ═══
            GUILayout.BeginVertical(GUILayout.Width(180));
            GUILayout.Label("──── Recipes ────", labelStyle);
            
            recipeScrollPosition = GUILayout.BeginScrollView(recipeScrollPosition, GUILayout.Height(200));
            
            if (CraftingManager.Instance != null)
            {
                var recipes = CraftingManager.Instance.GetAvailableRecipes();
                // Use internal flag to determine if we have station access
                
                if (recipes == null || recipes.Count == 0)
                {
                    GUILayout.Label("No recipes available", labelStyle);
                }
                else
                {
                    bool anyShown = false;
                    
                    foreach (var recipe in recipes)
                    {
                        if (recipe == null) continue;
                        
                        // FILTER: Skip station-required recipes if not at station
                        if (recipe.RequiredStation != null && !isAtStation)
                        {
                            continue; // Don't show this recipe
                        }
                        
                        anyShown = true;
                        bool isSelected = selectedRecipe == recipe;
                        bool canCraft = CraftingManager.Instance.CanCraft(recipe);
                        
                        // Color based on state
                        if (isSelected)
                            GUI.backgroundColor = selectedColor;
                        else if (canCraft)
                            GUI.backgroundColor = buttonColor;
                        else
                            GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                        
                        string icon = canCraft ? "✓" : "✗";
                        if (GUILayout.Button($"{icon} {recipe.RecipeName}", buttonStyle, GUILayout.Height(30)))
                        {
                            selectedRecipe = recipe;
                        }
                    }
                    
                    if (!anyShown)
                    {
                        GUILayout.Label("No hand-craftable recipes\n(Use crafting table for more)", labelStyle);
                    }
                }
            }
            else
            {
                GUILayout.Label("CraftingManager not found!", labelStyle);
            }
            
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            
            GUI.backgroundColor = backgroundColor;
            
            GUILayout.Space(10);
            
            // ═══ RIGHT: RECIPE DETAILS ═══
            GUILayout.BeginVertical(GUILayout.Width(180));
            
            if (selectedRecipe != null)
            {
                // Recipe Name
                GUILayout.Label($"──── {selectedRecipe.RecipeName} ────", labelStyle);
                
                // Description
                if (!string.IsNullOrEmpty(selectedRecipe.Description))
                {
                    GUILayout.Label(selectedRecipe.Description, labelStyle);
                }
                
                GUILayout.Space(5);
                
                // Ingredients
                GUILayout.Label("Required:", labelStyle);
                foreach (var ingredient in selectedRecipe.Ingredients)
                {
                    if (ingredient.Item == null) continue;
                    
                    int have = InventoryBridge.GetItemCount(ingredient.Item);
                    bool enough = have >= ingredient.Amount;
                    
                    ingredientLabelStyle.normal.textColor = enough ? Color.green : Color.red;
                    GUILayout.Label($"  • {ingredient.Item.ItemName}: {have}/{ingredient.Amount}", ingredientLabelStyle);
                }
                
                GUILayout.Space(5);
                
                // Result
                GUILayout.Label("Result:", labelStyle);
                ingredientLabelStyle.normal.textColor = Color.cyan;
                GUILayout.Label($"  → {selectedRecipe.ResultAmount}x {selectedRecipe.ResultItem?.ItemName ?? "???"}", ingredientLabelStyle);
                
                GUILayout.FlexibleSpace();
                
                // Craft Button
                bool canCraft = CraftingManager.Instance?.CanCraft(selectedRecipe) ?? false;
                GUI.enabled = canCraft;
                GUI.backgroundColor = canCraft ? new Color(0.2f, 0.7f, 0.2f) : Color.gray;
                
                if (GUILayout.Button("⚒ CRAFT", buttonStyle, GUILayout.Height(40)))
                {
                    DoCraft();
                }
                
                GUI.enabled = true;
                GUI.backgroundColor = backgroundColor;
                
                // Show block reason if can't craft
                if (!canCraft && CraftingManager.Instance != null)
                {
                    string reason = CraftingManager.Instance.GetCraftBlockReason(selectedRecipe);
                    if (!string.IsNullOrEmpty(reason))
                    {
                        ingredientLabelStyle.normal.textColor = new Color(1f, 0.6f, 0.2f); // Orange
                        GUILayout.Label($"⚠ {reason}", ingredientLabelStyle);
                    }
                }
            }
            else
            {
                GUILayout.Label("Select a recipe", labelStyle);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Note: Inventory is now shown in HappyHarvest toolbar at bottom of screen
            GUILayout.Label("──── Inventory: See bottom toolbar ────", labelStyle);
            
            
            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 30));
        }
        
        private void DoCraft()
        {
            if (selectedRecipe == null) return;
            if (CraftingManager.Instance == null) return;
            
            if (CraftingManager.Instance.Craft(selectedRecipe))
            {
                // Show success message
                craftSuccessMessage = $"✓ Crafted {selectedRecipe.ResultAmount}x {selectedRecipe.ResultItem?.ItemName}!";
                craftSuccessTimer = CRAFT_SUCCESS_DURATION;
                
                Debug.Log($"[CraftingUISimple] {craftSuccessMessage}");
            }
        }
    }
}
