using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sword Combat System - Handles sword swing with left mouse button
/// Works independently from HappyHarvest inventory system
/// Attach this to the Player GameObject
/// </summary>
public class SwordCombat : MonoBehaviour
{
    [Header("═══ SWORD SETTINGS ═══")]
    [Tooltip("Damage dealt to enemies per swing")]
    [SerializeField] private int damage = 10;
    
    [Tooltip("Attack range in units")]
    [SerializeField] private float attackRange = 1.5f;
    
    [Tooltip("Time in seconds between swings")]
    [SerializeField] private float swingCooldown = 0.5f;
    
    [Tooltip("Layer mask for enemies that can be damaged")]
    [SerializeField] private LayerMask enemyLayers;
    
    [Header("═══ ATTACK ARC ═══")]
    [Tooltip("Attack arc angle in degrees (180 = semicircle in front)")]
    [SerializeField] private float attackArc = 180f;

    [Header("═══ ANIMATION ═══")]
    [Tooltip("Animator component for sword swing animation")]
    [SerializeField] private Animator playerAnimator;
    
    [Tooltip("Animation trigger name for sword swing")]
    [SerializeField] private string swingTrigger = "SwordSwing";

    [Header("═══ INVENTORY CHECK ═══")]
    [Tooltip("If true, only swings when sword is equipped in HappyHarvest inventory")]
    [SerializeField] private bool requireSwordEquipped = true;
    
    [Tooltip("Item name to check for in equipped slot (case-insensitive contains)")]
    [SerializeField] private string swordItemName = "sword";

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showGizmos = true;

    // Private variables
    private float lastSwingTime = -999f;
    private Vector2 currentLookDirection = Vector2.right;

    // Animator parameter hashes
    private int dirXHash;
    private int dirYHash;

    private void Awake()
    {
        if (playerAnimator == null)
        {
            playerAnimator = GetComponentInChildren<Animator>();
        }

        dirXHash = Animator.StringToHash("DirX");
        dirYHash = Animator.StringToHash("DirY");
    }

    private void Update()
    {
        // Update look direction from animator
        UpdateLookDirection();

        // Check for left mouse button (Mouse Button 0 = left click)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySwingSword();
        }
    }

    /// <summary>
    /// Attempt to swing the sword
    /// </summary>
    public void TrySwingSword()
    {
        // Check if sword is equipped (if required)
        if (requireSwordEquipped && !IsSwordEquipped())
        {
            if (showDebugLogs)
                Debug.Log("[SwordCombat] ⚠️ Sword not equipped!");
            return;
        }

        // Check cooldown
        if (Time.time < lastSwingTime + swingCooldown)
        {
            if (showDebugLogs)
                Debug.Log($"[SwordCombat] ⏳ Cooldown! Remaining: {GetRemainingCooldown():F1}s");
            return;
        }

        // Execute swing
        SwingSword();
    }

    /// <summary>
    /// Execute the sword swing attack
    /// </summary>
    private void SwingSword()
    {
        lastSwingTime = Time.time;

        // Trigger animation
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(swingTrigger);
        }

        // Get player position
        Vector2 playerPos = transform.position;

        // Find all enemies in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, attackRange, enemyLayers);
        int hitCount = 0;

        foreach (var hit in hits)
        {
            // Check if enemy is in attack arc (in front of player)
            Vector2 toEnemy = ((Vector2)hit.transform.position - playerPos).normalized;
            float angle = Vector2.Angle(currentLookDirection, toEnemy);

            if (angle <= attackArc / 2f)
            {
                // Try to damage the enemy using IDamageable interface
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                    hitCount++;
                    
                    if (showDebugLogs)
                        Debug.Log($"[SwordCombat] ⚔️ Hit {hit.gameObject.name} for {damage} damage!");
                }
            }
        }

        if (showDebugLogs)
        {
            if (hitCount > 0)
                Debug.Log($"[SwordCombat] 💥 Sword swing hit {hitCount} enemies!");
            else
                Debug.Log("[SwordCombat] 🗡️ Sword swing - no enemies hit");
        }
    }

    /// <summary>
    /// Check if a sword is currently equipped in the HappyHarvest inventory
    /// </summary>
    private bool IsSwordEquipped()
    {
        // Try to find the HappyHarvest PlayerController and check equipped item
        var playerController = GetComponent<HappyHarvest.PlayerController>();
        if (playerController == null)
        {
            playerController = GetComponentInParent<HappyHarvest.PlayerController>();
        }

        if (playerController != null && playerController.Inventory != null)
        {
            var equippedItem = playerController.Inventory.EquippedItem;
            if (equippedItem != null)
            {
                // Check if the equipped item name contains "sword" (case-insensitive)
                string itemName = equippedItem.DisplayName ?? equippedItem.UniqueID ?? "";
                return itemName.ToLower().Contains(swordItemName.ToLower());
            }
        }

        // If no inventory system found, allow swing (standalone mode)
        if (!requireSwordEquipped)
            return true;

        return false;
    }

    /// <summary>
    /// Update the look direction from animator parameters
    /// </summary>
    private void UpdateLookDirection()
    {
        if (playerAnimator != null)
        {
            float dirX = playerAnimator.GetFloat(dirXHash);
            float dirY = playerAnimator.GetFloat(dirYHash);

            if (Mathf.Abs(dirX) > 0.1f || Mathf.Abs(dirY) > 0.1f)
            {
                currentLookDirection = new Vector2(dirX, dirY).normalized;
            }
        }
    }

    /// <summary>
    /// Get remaining cooldown time
    /// </summary>
    public float GetRemainingCooldown()
    {
        float remaining = (lastSwingTime + swingCooldown) - Time.time;
        return Mathf.Max(0f, remaining);
    }

    /// <summary>
    /// Check if sword is ready to swing
    /// </summary>
    public bool IsReady()
    {
        return Time.time >= lastSwingTime + swingCooldown;
    }

    /// <summary>
    /// Reset cooldown (for testing or power-ups)
    /// </summary>
    public void ResetCooldown()
    {
        lastSwingTime = -999f;
    }

    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Attack range circle
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Attack arc visualization
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Vector3 forward = Application.isPlaying ? (Vector3)currentLookDirection : transform.right;
        
        float halfArc = attackArc / 2f;
        Vector3 leftBound = Quaternion.Euler(0, 0, halfArc) * forward * attackRange;
        Vector3 rightBound = Quaternion.Euler(0, 0, -halfArc) * forward * attackRange;

        Gizmos.DrawLine(transform.position, transform.position + leftBound);
        Gizmos.DrawLine(transform.position, transform.position + rightBound);
    }
}
