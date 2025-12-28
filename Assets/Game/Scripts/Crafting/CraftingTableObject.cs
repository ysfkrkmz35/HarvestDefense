using UnityEngine;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// World object for the Crafting Table.
    /// Place in world, click to open crafting menu.
    /// </summary>
    public class CraftingTableObject : MonoBehaviour
    {
        [Header("═══ VISUALS ═══")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite tableSprite;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 10;
        
        [Header("═══ INTERACTION ═══")]
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        
        [Header("═══ HIGHLIGHT ═══")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.7f);
        
        private bool playerInRange = false;
        private Transform player;
        private CraftingUISimple craftingUI;
        
        private void Awake()
        {
            // Setup sprite renderer first
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            // Assign sprite if provided
            if (tableSprite != null)
            {
                spriteRenderer.sprite = tableSprite;
            }
            
            // Set sorting to ensure visibility
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
            
            // Ensure Z position is 0
            Vector3 pos = transform.position;
            pos.z = 0;
            transform.position = pos;
            
            Debug.Log($"[CraftingTable] Awake - Sprite: {spriteRenderer.sprite?.name ?? "NULL"}, SortingOrder: {sortingOrder}");
        }
        
        private void Start()
        {
            // Add collider for interaction detection
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(1f, 1f);
            }
            
            // Find player
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            
            // Find crafting UI
            craftingUI = FindObjectOfType<CraftingUISimple>();
            
            Debug.Log($"[CraftingTable] Placed at {transform.position}, Visible: {spriteRenderer.enabled}");
        }
        
        private void Update()
        {
            // Check distance to player
            if (player != null)
            {
                float distance = Vector2.Distance(transform.position, player.position);
                bool wasInRange = playerInRange;
                playerInRange = distance <= interactionRange;
                
                // Highlight when in range
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = playerInRange ? highlightColor : normalColor;
                }
                
                // Show hint when entering range
                if (playerInRange && !wasInRange)
                {
                    Debug.Log("[CraftingTable] Press E to use");
                }
                
                // Handle interaction
                if (playerInRange && Input.GetKeyDown(interactKey))
                {
                    Interact();
                }
            }
        }
        
        private void Interact()
        {
            Debug.Log("[CraftingTable] Interacted!");
            
            // Open crafting UI
            if (craftingUI != null)
            {
                // Use SendMessage or direct method call
                craftingUI.SendMessage("OpenFromStation", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                // Try to find it again
                craftingUI = FindObjectOfType<CraftingUISimple>();
                if (craftingUI != null)
                {
                    craftingUI.SendMessage("OpenFromStation", SendMessageOptions.DontRequireReceiver);
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
        
        /// <summary>
        /// Pick up the crafting table (return to inventory)
        /// </summary>
        public void PickUp()
        {
            // Return to inventory
            var resultItem = FindCraftingTableItem();
            if (resultItem != null && CraftingInventory.Instance != null)
            {
                CraftingInventory.Instance.AddItem(resultItem, 1);
                Debug.Log("[CraftingTable] Picked up!");
            }
            
            Destroy(gameObject);
        }
        
        private CraftingItem FindCraftingTableItem()
        {
            // Find the CraftingTable item in resources
            var allItems = Resources.FindObjectsOfTypeAll<CraftingItem>();
            foreach (var item in allItems)
            {
                if (item.ItemName.ToLower().Contains("crafting") && 
                    item.ItemName.ToLower().Contains("table"))
                {
                    return item;
                }
            }
            return null;
        }
    }
}
