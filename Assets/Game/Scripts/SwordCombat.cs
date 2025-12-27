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

    [Header("═══ DIRECT INPUT (Backup) ═══")]
    [Tooltip("Enable direct mouse input as backup if inventory system doesn't work")]
    [SerializeField] private bool enableDirectInput = true;
    
    [Tooltip("Mouse button for attack (0=Left, 1=Right)")]
    [SerializeField] private int attackMouseButton = 0;

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
        
        if (showDebugLogs)
            Debug.Log("[SwordCombat] ✅ Initialized on " + gameObject.name);
    }

    private void Update()
    {
        // Update look direction from animator
        UpdateLookDirection();

        // Direkt mouse input - backup olarak
        if (enableDirectInput && Input.GetMouseButtonDown(attackMouseButton))
        {
            if (showDebugLogs)
                Debug.Log("[SwordCombat] 🖱️ Mouse click detected!");
            
            TryPerformAttack();
        }
    }

    /// <summary>
    /// Attempt to perform sword attack (damage only, no animation - that's handled by inventory)
    /// </summary>
    public void TryPerformAttack()
    {
        if (showDebugLogs)
            Debug.Log("[SwordCombat] 🗡️ TryPerformAttack() called");

        // Check if sword is equipped (if required)
        if (requireSwordEquipped && !IsSwordEquipped())
        {
            if (showDebugLogs)
                Debug.Log("[SwordCombat] ⚠️ Sword not equipped, skipping attack!");
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

        // Get player position
        Vector2 playerPos = transform.position;

        if (showDebugLogs)
            Debug.Log($"[SwordCombat] ⚔️ Attack started! Position: {playerPos}, Range: {attackRange}, Direction: {currentLookDirection}");

        // Find ALL colliders in range (no layer filtering)
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, attackRange);
        
        if (showDebugLogs)
            Debug.Log($"[SwordCombat] 🔍 Found {hits.Length} colliders in range");

        int hitCount = 0;

        foreach (var hit in hits)
        {
            // Skip self
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            // DÜZELTME: Hem collider'ın objesinde hem de root/parent'ta tag kontrolü yap
            Transform targetTransform = hit.transform;
            bool hasEnemyTag = false;
            
            // Önce collider'ın objesini kontrol et
            if (hit.CompareTag(enemyTag))
            {
                hasEnemyTag = true;
            }
            // Sonra parent'ları kontrol et (root'a kadar)
            else
            {
                Transform parent = hit.transform.parent;
                while (parent != null)
                {
                    if (parent.CompareTag(enemyTag))
                    {
                        hasEnemyTag = true;
                        targetTransform = parent;
                        break;
                    }
                    parent = parent.parent;
                }
            }
            
            // Ayrıca root objeyi de kontrol et
            if (!hasEnemyTag)
            {
                Transform root = hit.transform.root;
                if (root != hit.transform && root.CompareTag(enemyTag))
                {
                    hasEnemyTag = true;
                    targetTransform = root;
                }
            }

            if (!hasEnemyTag)
            {
                if (showDebugLogs && hit.tag != "Untagged" && hit.tag != "Player")
                    Debug.Log($"[SwordCombat] ❌ {hit.gameObject.name} tag '{hit.tag}' != '{enemyTag}'");
                continue;
            }

            if (showDebugLogs)
                Debug.Log($"[SwordCombat] 👀 Enemy found: {targetTransform.name} (collider on: {hit.gameObject.name})");

            // Check if enemy is in attack arc (in front of player)
            Vector2 toEnemy = ((Vector2)targetTransform.position - playerPos).normalized;
            float angle = Vector2.Angle(currentLookDirection, toEnemy);

            if (showDebugLogs)
                Debug.Log($"[SwordCombat] 📐 Angle: {angle:F1}° (max allowed: {attackArc / 2f}°)");

            if (angle > attackArc / 2f)
            {
                if (showDebugLogs)
                    Debug.Log($"[SwordCombat] 📐 {targetTransform.name} outside attack arc, skipping");
                continue;
            }

            // DÜZELTME: IDamageable'ı her yerde ara!
            IDamageable damageable = FindIDamageable(hit.transform);

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                hitCount++;
                
                if (showDebugLogs)
                    Debug.Log($"[SwordCombat] ⚔️ HIT! {targetTransform.name} took {damage} damage!");
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[SwordCombat] ⚠️ {targetTransform.name} has 'Enemy' tag but NO IDamageable found anywhere in hierarchy!");
            }
        }

        // Summary log
        if (showDebugLogs)
        {
            if (hitCount > 0)
                Debug.Log($"[SwordCombat] 💥 Attack complete! Hit {hitCount} enemies for {damage} damage each!");
            else
                Debug.Log("[SwordCombat] 🗡️ Attack complete - no enemies hit");
        }
    }

    /// <summary>
    /// IDamageable'ı tüm hiyerarşide ara (self, parent, children, root)
    /// </summary>
    private IDamageable FindIDamageable(Transform startTransform)
    {
        // 1. Önce kendi üzerinde
        IDamageable damageable = startTransform.GetComponent<IDamageable>();
        if (damageable != null)
        {
            if (showDebugLogs)
                Debug.Log($"[SwordCombat] ✅ IDamageable found on: {startTransform.name}");
            return damageable;
        }

        // 2. Parent'larda ara
        damageable = startTransform.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            if (showDebugLogs)
                Debug.Log($"[SwordCombat] ✅ IDamageable found in PARENT of: {startTransform.name}");
            return damageable;
        }

        // 3. Children'larda ara
        damageable = startTransform.GetComponentInChildren<IDamageable>();
        if (damageable != null)
        {
            if (showDebugLogs)
                Debug.Log($"[SwordCombat] ✅ IDamageable found in CHILD of: {startTransform.name}");
            return damageable;
        }

        // 4. Root objeyi kontrol et
        Transform root = startTransform.root;
        if (root != startTransform)
        {
            damageable = root.GetComponent<IDamageable>();
            if (damageable != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[SwordCombat] ✅ IDamageable found on ROOT: {root.name}");
                return damageable;
            }

            // Root'un children'larında da ara
            damageable = root.GetComponentInChildren<IDamageable>();
            if (damageable != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[SwordCombat] ✅ IDamageable found in ROOT's children: {root.name}");
                return damageable;
            }
        }

        if (showDebugLogs)
            Debug.LogWarning($"[SwordCombat] ❌ IDamageable NOT FOUND anywhere for: {startTransform.name} (root: {startTransform.root.name})");

        return null;
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
                bool hasSword = itemName.ToLower().Contains(swordItemName.ToLower());
                
                if (showDebugLogs && !hasSword)
                    Debug.Log($"[SwordCombat] 📦 Equipped: '{itemName}' (not a sword)");
                    
                return hasSword;
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("[SwordCombat] 📦 No item equipped");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("[SwordCombat] ⚠️ PlayerController or Inventory not found");
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

    // Editor test buttons
    [ContextMenu("🗡️ Test: Force Attack")]
    private void TestForceAttack()
    {
        PerformAttackDamage();
    }

    [ContextMenu("🔍 Test: Check Enemies In Range")]
    private void TestCheckEnemies()
    {
        Vector2 playerPos = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, attackRange);
        
        Debug.Log($"=== ENEMIES IN RANGE ({attackRange} units) ===");
        int enemyCount = 0;
        
        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;
            
            bool hasTag = hit.CompareTag(enemyTag);
            bool parentHasTag = hit.transform.root.CompareTag(enemyTag);
            IDamageable damageable = FindIDamageable(hit.transform);
            
            string tagInfo = hasTag ? "✓ Self" : (parentHasTag ? "✓ Root" : "✗");
            string damageableInfo = damageable != null ? "✓" : "✗";
            
            Debug.Log($"  • {hit.gameObject.name} (root: {hit.transform.root.name}) | Tag: {tagInfo} | IDamageable: {damageableInfo}");
            
            if (hasTag || parentHasTag) enemyCount++;
        }
        
        Debug.Log($"=== Total: {enemyCount} enemies found ===");
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
        
        // Draw current look direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, forward * attackRange);
    }
}