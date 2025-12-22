using UnityEngine;

/// <summary>
/// Spider targeting - moves toward and attacks nearest tower/structure with HealthB.
/// Added at runtime by SceneInitializer.
/// </summary>
public class SpiderTargeting : MonoBehaviour
{
    [Header("Settings")]
    public float detectRange = 100f;
    public float attackRange = 15f; // Increased for the 10x scale
    public float moveSpeed = 5f;
    public int damage = 10;
    public float attackRate = 1f;
    
    private Rigidbody2D rb;
    private Transform target;
    private HealthB targetHP;
    private float nextAttack;
    private bool initialized;
    private HealthB myHealth;
    private bool isDead;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Disable old patrol/AI scripts
        EnemyPatrol patrol = GetComponent<EnemyPatrol>();
        if (patrol != null) patrol.enabled = false;
        
        SpiderAI ai = GetComponent<SpiderAI>();
        if (ai != null) ai.enabled = false;
        
        myHealth = GetComponent<HealthB>();
        if (myHealth != null) myHealth.OnDeath += OnDeath;
        initialized = true;
        Debug.Log($"[SpiderTargeting] Initialized on {gameObject.name}");
        
        FindTarget();
    }
    
    void FindTarget()
    {
        HealthB[] all = FindObjectsByType<HealthB>(FindObjectsSortMode.None);
        float best = float.MaxValue;
        HealthB chosen = null;
        
        foreach (HealthB h in all)
        {
            // Skip self
            if (h.gameObject == gameObject) continue;
            
            // Skip other enemies (layer 9)
            if (h.gameObject.layer == 9) continue;
            
            float d = Vector2.Distance(transform.position, h.transform.position);
            if (d < best && d <= detectRange)
            {
                best = d;
                chosen = h;
            }
        }
        
        if (chosen != null)
        {
            target = chosen.transform;
            targetHP = chosen;
            Debug.Log($"[SpiderTargeting] {gameObject.name} targeting: {target.name} at distance {best}");
        }
        else
        {
            Debug.Log($"[SpiderTargeting] {gameObject.name} found no valid target");
        }
    }
    
    void Update()
    {
        if (!initialized || isDead) return;
        
        // Backup death check in case OnDeath doesn't fire
        if (myHealth != null && myHealth.GetCurrentHealth() <= 0)
        {
            OnDeath();
            return;
        }
        
        if (target == null || targetHP == null)
        {
            FindTarget();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }
        
        float dist = Vector2.Distance(transform.position, target.position);
        
        if (dist <= attackRange)
        {
            // Stop and attack
            if (rb != null) rb.linearVelocity = Vector2.zero;
            
            if (Time.time >= nextAttack)
            {
                Attack();
                nextAttack = Time.time + attackRate;
            }
        }
        else if (dist <= detectRange)
        {
            // Move toward target
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            if (rb != null) rb.linearVelocity = dir * moveSpeed;
        }
        else
        {
            // Lost target
            target = null;
            targetHP = null;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
    
    void Attack()
    {
        if (targetHP == null) return;
        
        targetHP.TakeDamage(damage);
        Debug.Log($"[SpiderTargeting] {gameObject.name} hit {target.name} for {damage} dmg! Target HP: {targetHP.GetCurrentHealth()}/{targetHP.GetMaxHealth()}");
        
        if (targetHP.GetCurrentHealth() <= 0)
        {
            target = null;
            targetHP = null;
            FindTarget();
        }
    }
    
    void OnDeath()
    {
        isDead = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Debug.Log($"[SpiderTargeting] {gameObject.name} died!");
    }
    
    
void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}