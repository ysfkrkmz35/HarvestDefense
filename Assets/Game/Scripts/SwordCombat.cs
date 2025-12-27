using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sword Combat System - Handles sword attack hit detection and damage.
/// This works WITH the HappyHarvest inventory system:
/// - SwordItem triggers the animation through inventory system
/// - SwordCombat handles the actual damage dealing
/// 
/// Attach this to the Player GameObject.
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
    
    [Header("═══ TARGET DETECTION ═══")]
    [Tooltip("Tag used to identify enemies (e.g., 'Enemy')")]
    [SerializeField] private string enemyTag = "Enemy";
    
    [Header("═══ ATTACK ARC ═══")]
    [Tooltip("Attack arc angle in degrees (180 = semicircle in front)")]
    [SerializeField] private float attackArc = 180f;

    [Header("═══ INVENTORY CHECK ═══")]
    [Tooltip("If true, only attacks when sword is equipped in HappyHarvest inventory")]
    [SerializeField] private bool requireSwordEquipped = true;
    
    [Tooltip("Item name to check for in equipped slot (case-insensitive contains)")]
    [SerializeField] private string swordItemName = "sword";

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showGizmos = true;

    // Private variables
    private float lastAttackTime = -999f;
    private Vector2 currentLookDirection = Vector2.right;
    private Animator playerAnimator;

    // Animator parameter hashes
    private int dirXHash;
    private int dirYHash;

    private void Awake()
    {
        playerAnimator = GetComponentInChildren<Animator>();
        dirXHash = Animator.StringToHash("DirX");
        dirYHash = Animator.StringToHash("DirY");
    }

    private void Update()
    {
        // Update look direction from animator
        UpdateLookDirection();

        // NOTE: Mouse input is handled by HappyHarvest PlayerController/InventorySystem
        // When player clicks with sword equipped, SwordItem.Use() is called which then
        // calls our PerformAttackDamage() method. We don't handle input here anymore.
    }

    /// <summary>
    /// Attempt to perform sword attack (damage only, no animation - that's handled by inventory)
    /// </summary>
    public void TryPerformAttack()
    {
        // Check if sword is equipped (if required)
        if (requireSwordEquipped && !IsSwordEquipped())
        {
            if (showDebugLogs)
                Debug.Log("[SwordCombat] ⚠️ Sword not equipped, skipping attack damage!");
            return;
        }

        // Check cooldown
        if (Time.time < lastAttackTime + swingCooldown)
        {
            if (showDebugLogs)
                Debug.Log($"[SwordCombat] ⏳ Cooldown! Remaining: {GetRemainingCooldown():F1}s");
            return;
        }

        // Execute attack (damage dealing)
        PerformAttackDamage();
    }

    /// <summary>
    /// Called by SwordItem.Use() to trigger the attack.
    /// Can also be called directly for testing.
    /// </summary>
    public void PerformAttackDamage()
    {
        lastAttackTime = Time.time;

        // NOTE: Animation is NOT triggered here - it's triggered by the HappyHarvest inventory system
        // when SwordItem.Use() returns true. This method ONLY handles hit detection and damage.

        // Get player position
        Vector2 playerPos = transform.position;

        // Find ALL colliders in range (no layer filtering)
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, attackRange);
        int hitCount = 0;

        foreach (var hit in hits)
        {
            // Skip self
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;
                
            // Check if this object has the Enemy tag
            if (!hit.CompareTag(enemyTag))
                continue;

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
                        Debug.Log($"[SwordCombat] ⚔️ Hit {hit.gameObject.name} (tag: {enemyTag}) for {damage} damage!");
                }
                else
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[SwordCombat] ⚠️ {hit.gameObject.name} has '{enemyTag}' tag but no IDamageable component!");
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

        // If no inventory system found, allow attack (standalone mode)
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
        float remaining = (lastAttackTime + swingCooldown) - Time.time;
        return Mathf.Max(0f, remaining);
    }

    /// <summary>
    /// Check if sword is ready to attack
    /// </summary>
    public bool IsReady()
    {
        return Time.time >= lastAttackTime + swingCooldown;
    }

    /// <summary>
    /// Reset cooldown (for testing or power-ups)
    /// </summary>
    public void ResetCooldown()
    {
        lastAttackTime = -999f;
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
