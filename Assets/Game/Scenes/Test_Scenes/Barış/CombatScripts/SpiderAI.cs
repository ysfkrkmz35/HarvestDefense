using UnityEngine;

/// <summary>
/// Simple Spider AI for baris_test2 scene.
/// Behavior: Idle/Patrol -> Detect Player -> Chase -> Attack (melee)
/// Uses Rigidbody2D for movement, consistent with existing setup.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SpiderAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private LayerMask targetLayer; // Set to Player layer or use tag
    [SerializeField] private string targetTag = "Player";
    
    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float patrolSpeed = 2f;
    
    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 10;
    
    [Header("Patrol (when no target)")]
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float idleTime = 2f;
    
    private enum State { Idle, Patrol, Chase, Attack }
    private State currentState = State.Idle;
    
    private Rigidbody2D rb;
    private Transform target;
    private Vector2 patrolStartPos;
    private bool patrollingRight = true;
    private float stateTimer;
    private float attackTimer;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        patrolStartPos = transform.position;
    }
    
    private void Start()
    {
        // Try to find player by tag
        GameObject player = GameObject.FindGameObjectWithTag(targetTag);
        if (player != null)
        {
            target = player.transform;
        }
        
        stateTimer = idleTime;
    }
    
    private void Update()
    {
        // Update attack cooldown
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;
        
        // State machine
        switch (currentState)
        {
            case State.Idle:
                HandleIdle();
                break;
            case State.Patrol:
                HandlePatrol();
                break;
            case State.Chase:
                HandleChase();
                break;
            case State.Attack:
                HandleAttack();
                break;
        }
        
        // Always check for target detection (can interrupt patrol/idle)
        CheckForTarget();
    }
    
    private void FixedUpdate()
    {
        Vector2 myPos = (Vector2)transform.position;

        // ═══ MEVCUT POZİSYON KONTROLÜ ═══
        // Eğer şu anda Water üzerindeyse, en yakın Ground'a geri dön
        if (!IsPositionOnGround(myPos))
        {
            Debug.LogWarning($"[SpiderAI] {gameObject.name} Water üzerinde! En yakın Ground'a dönüyor...");
            Vector2? nearestGround = FindNearestGroundPosition(myPos, 10f);
            if (nearestGround.HasValue)
            {
                transform.position = nearestGround.Value;
            }
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Apply movement based on state (Ground tile kontrolü ile)
        Vector2 targetVelocity = Vector2.zero;

        switch (currentState)
        {
            case State.Patrol:
                float patrolDir = patrollingRight ? 1f : -1f;
                targetVelocity = new Vector2(patrolDir * patrolSpeed, rb.linearVelocity.y);
                break;

            case State.Chase:
                if (target != null)
                {
                    Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
                    targetVelocity = direction * chaseSpeed;
                }
                break;

            case State.Idle:
            case State.Attack:
                rb.linearVelocity = Vector2.zero;
                return; // Hareket yok, kontrol gerekmiyor
        }

        // ═══ HEDEF POZİSYON KONTROLÜ (Player gibi) ═══
        // Enemy sadece Ground üzerinde hareket edebilir, Water'a giremez!
        if (targetVelocity.sqrMagnitude > 0.01f)
        {
            Vector2 targetPosition = myPos + targetVelocity * Time.fixedDeltaTime;

            // Ground tile var mı kontrol et
            if (HappyHarvest.GameManager.Instance?.Terrain != null)
            {
                var grid = HappyHarvest.GameManager.Instance.Terrain.Grid;
                var groundTilemap = HappyHarvest.GameManager.Instance.Terrain.GroundTilemap;

                if (grid != null && groundTilemap != null)
                {
                    Vector3Int targetCell = grid.WorldToCell(targetPosition);
                    bool hasGroundTile = groundTilemap.HasTile(targetCell);

                    // Ground tile yoksa hareket etme (Player ile aynı sistem)
                    if (!hasGroundTile)
                    {
                        rb.linearVelocity = Vector2.zero;
                        return; // Hareketi iptal et
                    }
                }
            }

            // Ground üzerinde, hareket edebilir
            rb.linearVelocity = targetVelocity;
        }
    }

    /// <summary>
    /// Pozisyon Ground tile üzerinde mi kontrol et
    /// </summary>
    bool IsPositionOnGround(Vector2 position)
    {
        if (HappyHarvest.GameManager.Instance?.Terrain != null)
        {
            var grid = HappyHarvest.GameManager.Instance.Terrain.Grid;
            var groundTilemap = HappyHarvest.GameManager.Instance.Terrain.GroundTilemap;

            if (grid != null && groundTilemap != null)
            {
                Vector3Int cellPosition = grid.WorldToCell(new Vector3(position.x, position.y, 0f));

                // Ground tile var mı?
                return groundTilemap.HasTile(cellPosition);
            }
        }

        // Tilemap yoksa güvenli kabul et (eski davranış)
        return true;
    }

    /// <summary>
    /// En yakın Ground pozisyonunu bul (spiral search)
    /// </summary>
    Vector2? FindNearestGroundPosition(Vector2 currentPosition, float maxSearchDistance)
    {
        if (HappyHarvest.GameManager.Instance?.Terrain == null) return null;

        var grid = HappyHarvest.GameManager.Instance.Terrain.Grid;
        var groundTilemap = HappyHarvest.GameManager.Instance.Terrain.GroundTilemap;

        if (grid == null || groundTilemap == null) return null;

        Vector3Int startCell = grid.WorldToCell(new Vector3(currentPosition.x, currentPosition.y, 0f));
        int maxRadius = Mathf.CeilToInt(maxSearchDistance / grid.cellSize.x);

        // Spiral search - yakından uzağa
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Sadece kenarları kontrol et
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        continue;

                    Vector3Int checkCell = startCell + new Vector3Int(x, y, 0);

                    if (groundTilemap.HasTile(checkCell))
                    {
                        Vector3 groundWorldPos = grid.GetCellCenterWorld(checkCell);
                        return new Vector2(groundWorldPos.x, groundWorldPos.y);
                    }
                }
            }
        }

        return null;
    }
    
    private void HandleIdle()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            // Switch to patrol
            currentState = State.Patrol;
            patrollingRight = !patrollingRight; // Alternate direction
        }
    }
    
    private void HandlePatrol()
    {
        float distFromStart = transform.position.x - patrolStartPos.x;
        
        // Check if we've patrolled far enough
        if (patrollingRight && distFromStart >= patrolDistance)
        {
            currentState = State.Idle;
            stateTimer = idleTime;
        }
        else if (!patrollingRight && distFromStart <= -patrolDistance)
        {
            currentState = State.Idle;
            stateTimer = idleTime;
        }
    }
    
    private void HandleChase()
    {
        if (target == null)
        {
            currentState = State.Idle;
            stateTimer = idleTime;
            return;
        }
        
        float distToTarget = Vector2.Distance(transform.position, target.position);
        
        // If close enough, attack
        if (distToTarget <= attackRange)
        {
            currentState = State.Attack;
        }
        // If too far, lose interest
        else if (distToTarget > detectionRange * 1.5f)
        {
            currentState = State.Idle;
            stateTimer = idleTime;
        }
    }
    
    private void HandleAttack()
    {
        if (target == null)
        {
            currentState = State.Idle;
            stateTimer = idleTime;
            return;
        }
        
        float distToTarget = Vector2.Distance(transform.position, target.position);
        
        // If target moved away, chase again
        if (distToTarget > attackRange * 1.2f)
        {
            currentState = State.Chase;
            return;
        }
        
        // Perform attack if cooldown is ready
        if (attackTimer <= 0)
        {
            PerformAttack();
            attackTimer = attackCooldown;
        }
    }
    
    private void PerformAttack()
    {
        if (target == null) return;
        
        // Try to damage the target
        // First try IDamageableB interface (our local interface)
        IDamageableB damageable = target.GetComponent<IDamageableB>();
        if (damageable != null)
        {
            damageable.TakeDamage(attackDamage);
            Debug.Log($"{gameObject.name} attacked {target.name} for {attackDamage} damage!");
        }
        else
        {
            // Fallback: try HealthB directly
            HealthB health = target.GetComponent<HealthB>();
            if (health != null)
            {
                health.TakeDamage(attackDamage);
                Debug.Log($"{gameObject.name} attacked {target.name} for {attackDamage} damage!");
            }
            else
            {
                Debug.Log($"{gameObject.name} attacked {target.name} but no damageable component found!");
            }
        }
    }
    
    private void CheckForTarget()
    {
        if (target == null) return;
        
        float distToTarget = Vector2.Distance(transform.position, target.position);
        
        // If in detection range and not already chasing/attacking
        if (distToTarget <= detectionRange && currentState != State.Chase && currentState != State.Attack)
        {
            currentState = State.Chase;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Detection range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Patrol bounds (cyan)
        Gizmos.color = Color.cyan;
        Vector3 startPos = Application.isPlaying ? (Vector3)patrolStartPos : transform.position;
        Gizmos.DrawLine(startPos + Vector3.left * patrolDistance, startPos + Vector3.right * patrolDistance);
    }
}