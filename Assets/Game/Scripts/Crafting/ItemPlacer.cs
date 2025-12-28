using UnityEngine;

namespace HarvestDefense.Crafting
{
    /// <summary>
    /// Handles placing craftable items in the world.
    /// Attach to player or a manager object.
    /// </summary>
    public class ItemPlacer : MonoBehaviour
    {
        [Header("═══ SETTINGS ═══")]
        [SerializeField] private KeyCode placeKey = KeyCode.P;
        [SerializeField] private float placementRange = 2f;
        [SerializeField] private LayerMask groundLayer;
        
        [Header("═══ FALLBACK PREFAB ═══")]
        [Tooltip("Used if item doesn't have a PlacedPrefab assigned")]
        [SerializeField] private GameObject fallbackPrefab;
        
        [Header("═══ PREVIEW ═══")]
        [SerializeField] private Color validPlacementColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color invalidPlacementColor = new Color(1f, 0f, 0f, 0.5f);
        
        // State
        private CraftingItem selectedItem;
        private GameObject previewObject;
        private bool isPlacingMode = false;
        private Transform player;
        
        private void Start()
        {
            // Find player
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                player = transform; // Fallback to self
        }
        
        private void Update()
        {
            // Check if we have a craftingtable to place
            if (Input.GetKeyDown(placeKey))
            {
                TryStartPlacing();
            }
            
            // Handle placement mode
            if (isPlacingMode)
            {
                UpdatePlacementPreview();
                
                // Cancel with right-click or Escape
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelPlacement();
                }
                
                // Confirm with left-click
                if (Input.GetMouseButtonDown(0))
                {
                    ConfirmPlacement();
                }
            }
        }
        
        private void TryStartPlacing()
        {
            var inventory = CraftingInventory.Instance;
            if (inventory == null) return;
            
            // Find a placeable item (CraftingTable)
            var items = inventory.GetAllItems();
            foreach (var kvp in items)
            {
                if (kvp.Key != null && kvp.Key.IsPlaceable && kvp.Value > 0)
                {
                    StartPlacing(kvp.Key);
                    return;
                }
            }
            
            Debug.Log("[ItemPlacer] No placeable items in inventory");
        }
        
        private void StartPlacing(CraftingItem item)
        {
            selectedItem = item;
            isPlacingMode = true;
            
            // Get prefab from item, or use fallback
            GameObject prefab = item.PlacedPrefab != null ? item.PlacedPrefab : fallbackPrefab;
            
            // Create preview object
            if (prefab != null)
            {
                previewObject = Instantiate(prefab);
                previewObject.name = "PlacementPreview";
                
                // Disable ALL scripts on preview (including children)
                var scripts = previewObject.GetComponentsInChildren<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    if (script != null)
                        script.enabled = false;
                }
                
                // Disable colliders on preview (including children)
                var colliders = previewObject.GetComponentsInChildren<Collider2D>();
                foreach (var col in colliders)
                    col.enabled = false;
                
                // Setup sprite for preview visibility
                var sr = previewObject.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.enabled = true;
                    sr.color = validPlacementColor;
                    sr.sortingOrder = 100; // High sorting order for preview
                    
                    // If sprite is null, try to get from item icon
                    if (sr.sprite == null && item.Icon != null)
                    {
                        sr.sprite = item.Icon;
                    }
                    
                    Debug.Log($"[ItemPlacer] Preview sprite: {sr.sprite?.name ?? "NULL"}");
                }
                else
                {
                    // Create a sprite renderer if none exists
                    sr = previewObject.AddComponent<SpriteRenderer>();
                    sr.sprite = item.Icon;
                    sr.color = validPlacementColor;
                    sr.sortingOrder = 100;
                    Debug.Log($"[ItemPlacer] Created preview SR with icon: {item.Icon?.name ?? "NULL"}");
                }
                
                // Ensure Z is 0
                Vector3 pos = previewObject.transform.position;
                pos.z = 0;
                previewObject.transform.position = pos;
            }
            else
            {
                Debug.LogWarning($"[ItemPlacer] No prefab for {item.ItemName}! Create a prefab and assign to PlacedPrefab.");
            }
            
            Debug.Log($"[ItemPlacer] Placing: {item.ItemName}. Left-click to place, Right-click to cancel.");
        }
        
        private void UpdatePlacementPreview()
        {
            if (previewObject == null) return;
            
            // Get world position from mouse
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            // Snap to grid (optional)
            Vector3 snappedPos = new Vector3(
                Mathf.Round(mousePos.x),
                Mathf.Round(mousePos.y),
                0
            );
            
            previewObject.transform.position = snappedPos;
            
            // Check if valid placement
            bool isValid = IsValidPlacement(snappedPos);
            var sr = previewObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = isValid ? validPlacementColor : invalidPlacementColor;
            }
        }
        
        private bool IsValidPlacement(Vector3 position)
        {
            // Check distance from player
            if (player != null)
            {
                float distance = Vector2.Distance(position, player.position);
                if (distance > placementRange)
                    return false;
            }
            
            // Check for obstacles
            var hit = Physics2D.OverlapCircle(position, 0.4f);
            if (hit != null && !hit.isTrigger)
            {
                return false; // Something is blocking
            }
            
            return true;
        }
        
        private void ConfirmPlacement()
        {
            if (previewObject == null || selectedItem == null) return;
            
            Vector3 position = previewObject.transform.position;
            
            if (!IsValidPlacement(position))
            {
                Debug.Log("[ItemPlacer] Invalid placement location!");
                return;
            }
            
            // Consume item from inventory
            var inventory = CraftingInventory.Instance;
            if (inventory != null && inventory.RemoveItem(selectedItem, 1))
            {
                // Destroy preview
                Destroy(previewObject);
                previewObject = null;
                
                // Create actual object using item's prefab
                GameObject prefab = selectedItem.PlacedPrefab != null ? selectedItem.PlacedPrefab : fallbackPrefab;
                if (prefab != null)
                {
                    var placed = Instantiate(prefab, position, Quaternion.identity);
                    placed.name = selectedItem.ItemName;
                    
                    Debug.Log($"[ItemPlacer] Placed {selectedItem.ItemName} at {position}");
                }
                
                isPlacingMode = false;
                selectedItem = null;
            }
            else
            {
                Debug.Log("[ItemPlacer] Failed to remove item from inventory!");
            }
        }
        
        private void CancelPlacement()
        {
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }
            
            isPlacingMode = false;
            selectedItem = null;
            Debug.Log("[ItemPlacer] Placement cancelled");
        }
        
        private void OnGUI()
        {
            if (!isPlacingMode) return;
            
            // Show placement hint
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 10, 400, 30), 
                $"Placing: {selectedItem?.ItemName ?? "?"} | Left-click: Place | Right-click: Cancel");
        }
    }
}
