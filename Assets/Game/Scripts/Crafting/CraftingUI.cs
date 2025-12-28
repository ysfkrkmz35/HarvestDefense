using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// Crafting UI Controller for UI Toolkit.
    /// Toggle with 'O' key. Draggable panel.
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        [Header("═══ UI DOCUMENT ═══")]
        [SerializeField] private UIDocument uiDocument;
        
        [Header("═══ INPUT ═══")]
        [SerializeField] private KeyCode toggleKey = KeyCode.O;
        
        [Header("═══ RECIPE ITEM TEMPLATE ═══")]
        [SerializeField] private VisualTreeAsset recipeItemTemplate;
        
        // UI Elements
        private VisualElement root;
        private VisualElement craftingElement;
        private Label titleLabel;
        private Button closeButton;
        private ScrollView recipeList;
        private Button craftButton;
        
        // Runtime
        private List<Button> recipeButtons = new List<Button>();
        private CraftingRecipe selectedRecipe;
        
        // Dragging
        private bool isDragging;
        private Vector2 dragOffset;
        
        private void OnEnable()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            
            if (uiDocument == null)
            {
                Debug.LogError("[CraftingUI] UIDocument not found!");
                return;
            }
            
            root = uiDocument.rootVisualElement;
            SetupUI();
        }
        
        private void SetupUI()
        {
            // Query elements
            craftingElement = root.Q<VisualElement>("CraftingElement");
            titleLabel = root.Q<Label>("Title");
            closeButton = root.Q<Button>("CloseButton");
            recipeList = root.Q<ScrollView>("RecipeList");
            craftButton = root.Q<Button>("CraftButton");
            
            if (craftingElement == null)
            {
                Debug.LogError("[CraftingUI] #CraftingElement not found in UXML!");
                return;
            }
            
            // Ensure the panel can receive clicks
            root.pickingMode = PickingMode.Position;
            craftingElement.pickingMode = PickingMode.Position;
            
            // Setup Close button
            if (closeButton != null)
            {
                closeButton.pickingMode = PickingMode.Position;
                closeButton.focusable = true;
                closeButton.clicked += () => {
                    Debug.Log("[CraftingUI] Close clicked!");
                    Hide();
                };
                Debug.Log("[CraftingUI] CloseButton registered");
            }
            else
            {
                Debug.LogWarning("[CraftingUI] CloseButton not found!");
            }
            
            // Setup Craft button
            if (craftButton != null)
            {
                craftButton.pickingMode = PickingMode.Position;
                craftButton.focusable = true;
                craftButton.clicked += () => {
                    Debug.Log("[CraftingUI] Craft clicked!");
                    CraftSelected();
                };
                craftButton.SetEnabled(false);
                Debug.Log("[CraftingUI] CraftButton registered");
            }
            else
            {
                Debug.LogWarning("[CraftingUI] CraftButton not found!");
            }
            
            // Setup dragging on header
            var header = root.Q<VisualElement>("Header");
            if (header != null)
            {
                header.pickingMode = PickingMode.Position;
                header.RegisterCallback<PointerDownEvent>(OnDragStart);
                header.RegisterCallback<PointerMoveEvent>(OnDragMove);
                header.RegisterCallback<PointerUpEvent>(OnDragEnd);
            }
            
            // Subscribe to inventory changes
            if (CraftingInventory.Instance != null)
                CraftingInventory.Instance.OnInventoryChanged += RefreshRecipes;
            
            Debug.Log("[CraftingUI] Setup complete");
            
            // Hide on start
            Hide();
        }
        
        private void OnDisable()
        {
            if (CraftingInventory.Instance != null)
                CraftingInventory.Instance.OnInventoryChanged -= RefreshRecipes;
        }
        
        private void Update()
        {
            // Toggle with key
            if (Input.GetKeyDown(toggleKey))
            {
                Toggle();
            }
            
            // ESC to close
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }
        
        #region ═══════ VISIBILITY ═══════
        
        public bool IsOpen => craftingElement != null && 
                              craftingElement.style.display == DisplayStyle.Flex;
        
        public void Toggle()
        {
            if (IsOpen)
                Hide();
            else
                Show();
        }
        
        public void Show()
        {
            if (craftingElement == null) return;
            
            craftingElement.style.display = DisplayStyle.Flex;
            RefreshRecipes();
            
            if (titleLabel != null)
                titleLabel.text = "Crafting";
            
            Debug.Log("[CraftingUI] Opened");
        }
        
        public void Hide()
        {
            if (craftingElement == null) return;
            
            craftingElement.style.display = DisplayStyle.None;
            selectedRecipe = null;
            
            Debug.Log("[CraftingUI] Closed");
        }
        
        #endregion
        
        #region ═══════ DRAGGING ═══════
        
        private void OnDragStart(PointerDownEvent evt)
        {
            isDragging = true;
            dragOffset = evt.localPosition;
            evt.target.CapturePointer(evt.pointerId);
        }
        
        private void OnDragMove(PointerMoveEvent evt)
        {
            if (!isDragging || craftingElement == null) return;
            
            Vector2 delta = (Vector2)evt.localPosition - dragOffset;
            craftingElement.style.left = craftingElement.resolvedStyle.left + delta.x;
            craftingElement.style.top = craftingElement.resolvedStyle.top + delta.y;
        }
        
        private void OnDragEnd(PointerUpEvent evt)
        {
            isDragging = false;
            evt.target.ReleasePointer(evt.pointerId);
        }
        
        #endregion
        
        #region ═══════ RECIPES ═══════
        
        private void RefreshRecipes()
        {
            if (recipeList == null) return;
            
            var manager = CraftingManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[CraftingUI] CraftingManager not found!");
                return;
            }
            
            // Clear existing items (keep the template)
            var contentContainer = recipeList.contentContainer;
            
            // Remove all except first (template)
            while (contentContainer.childCount > 1)
            {
                contentContainer.RemoveAt(contentContainer.childCount - 1);
            }
            
            // Get template (first item in RecipeList)
            var template = contentContainer.Q<Button>("RecipeItem");
            if (template != null)
                template.style.display = DisplayStyle.None;
            
            // Add recipe buttons
            recipeButtons.Clear();
            var recipes = manager.GetAllRecipes();
            
            foreach (var recipe in recipes)
            {
                if (recipe == null) continue;
                
                // Create new button
                var btn = new Button();
                btn.AddToClassList("recipe-item");
                btn.text = recipe.RecipeName ?? "Unknown";
                
                // Style based on can craft
                bool canCraft = manager.CanCraft(recipe);
                bool unlocked = manager.IsRecipeUnlocked(recipe);
                
                if (!unlocked)
                {
                    btn.text = "???";
                    btn.SetEnabled(false);
                    btn.style.opacity = 0.5f;
                }
                else if (!canCraft)
                {
                    btn.style.opacity = 0.7f;
                }
                else
                {
                    btn.style.backgroundColor = new Color(0.4f, 0.7f, 0.4f, 1f);
                }
                
                // Click handler
                var capturedRecipe = recipe;
                btn.clicked += () => SelectRecipe(capturedRecipe);
                
                contentContainer.Add(btn);
                recipeButtons.Add(btn);
            }
            
            // Update craft button
            UpdateCraftButton();
        }
        
        private void SelectRecipe(CraftingRecipe recipe)
        {
            selectedRecipe = recipe;
            
            // Highlight selected
            foreach (var btn in recipeButtons)
            {
                btn.RemoveFromClassList("selected");
            }
            
            // Update title to show selected
            if (titleLabel != null && recipe != null)
            {
                titleLabel.text = $"Craft: {recipe.RecipeName}";
            }
            
            UpdateCraftButton();
            
            Debug.Log($"[CraftingUI] Selected: {recipe?.RecipeName}");
        }
        
        private void UpdateCraftButton()
        {
            if (craftButton == null) return;
            
            var manager = CraftingManager.Instance;
            bool canCraft = selectedRecipe != null && 
                           manager != null && 
                           manager.CanCraft(selectedRecipe);
            
            craftButton.SetEnabled(canCraft);
            craftButton.text = canCraft ? "Craft" : "Select Recipe";
        }
        
        private void CraftSelected()
        {
            if (selectedRecipe == null) return;
            
            var manager = CraftingManager.Instance;
            if (manager == null) return;
            
            if (manager.Craft(selectedRecipe))
            {
                Debug.Log($"[CraftingUI] ✅ Crafted {selectedRecipe.RecipeName}!");
                // Refresh to update availability
                RefreshRecipes();
            }
        }
        
        #endregion
    }
}
