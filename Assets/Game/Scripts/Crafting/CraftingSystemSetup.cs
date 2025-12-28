using UnityEngine;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// Editor helper to create initial crafting items and recipes.
    /// Attach to GameManager or any persistent object.
    /// </summary>
    public class CraftingSystemSetup : MonoBehaviour
    {
        [Header("═══ REFERENCES ═══")]
        [SerializeField] private CraftingManager craftingManager;
        [SerializeField] private CraftingInventory craftingInventory;
        
        [Header("═══ TEST ITEMS (Assign in Inspector) ═══")]
        public CraftingItem woodItem;
        public CraftingItem plankItem;
        public CraftingItem ropeItem;
        public CraftingItem craftingTableItem;
        public CraftingItem boatItem;
        
        [Header("═══ DEBUG ═══")]
        [SerializeField] private bool addTestItemsOnStart = true;
        [SerializeField] private int testItemAmount = 20;
        
        private void Start()
        {
            // Ensure singleton components exist
            if (craftingManager == null)
                craftingManager = FindObjectOfType<CraftingManager>();
            if (craftingInventory == null)
                craftingInventory = FindObjectOfType<CraftingInventory>();
            
            if (addTestItemsOnStart)
            {
                AddTestItems();
            }
        }
        
        [ContextMenu("🎁 Add Test Items")]
        public void AddTestItems()
        {
            var inventory = CraftingInventory.Instance;
            if (inventory == null)
            {
                Debug.LogError("[CraftingSystemSetup] CraftingInventory not found!");
                return;
            }
            
            if (woodItem != null) inventory.AddItem(woodItem, testItemAmount);
            if (plankItem != null) inventory.AddItem(plankItem, testItemAmount);
            if (ropeItem != null) inventory.AddItem(ropeItem, testItemAmount);
            
            Debug.Log($"[CraftingSystemSetup] Added {testItemAmount} of each test material");
        }
        
        [ContextMenu("🗑 Clear Inventory")]
        public void ClearInventory()
        {
            var inventory = CraftingInventory.Instance;
            if (inventory != null)
            {
                inventory.ClearInventory();
                Debug.Log("[CraftingSystemSetup] Inventory cleared");
            }
        }
    }
}
