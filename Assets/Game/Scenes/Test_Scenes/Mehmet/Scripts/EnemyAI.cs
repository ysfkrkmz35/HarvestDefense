using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enemy AI System - Harvest Defense
/// Akıllı düşman yapay zeka sistemi
/// - Player takibi ve hareket tahmini
/// - Akıllı engel kaçınma (context steering)
/// - Grup davranışı (flocking)
/// - Görüş sistemi ve sıkışma çözme
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("=== AI STATE ===")]
    public AIState currentState = AIState.Seeking;

    [Header("=== REFERENCES ===")]
    private Transform playerTransform;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Transform currentTarget;

    [Header("=== MOVEMENT ===")]
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 12f;
    private Vector2 velocity = Vector2.zero;
    private Vector2 desiredVelocity = Vector2.zero;

    [Header("=== DETECTION ===")]
    [SerializeField] private float visionRange = 12f;
    [SerializeField] private float visionAngle = 130f;
    [SerializeField] private float closeRangeDetection = 3f;
    [SerializeField] private LayerMask visionBlockLayer;

    [Header("=== ATTACK ===")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackLungeSpeed = 6f;
    private float lastAttackTime = -999f;
    private bool isLunging = false;

    [Header("=== OBSTACLE AVOIDANCE ===")]
    [SerializeField] private float obstacleAvoidanceDistance = 2.5f;
    [SerializeField] private int avoidanceRayCount = 7;
    [SerializeField] private float avoidanceRayAngle = 90f;
    [SerializeField] private float obstacleAvoidanceWeight = 2.5f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("=== FLOCKING ===")]
    [SerializeField] private bool useFlocking = true;
    [SerializeField] private float separationDistance = 1.5f;
    [SerializeField] private float separationWeight = 2f;
    [SerializeField] private float cohesionDistance = 4f;
    [SerializeField] private float cohesionWeight = 0.5f;
    private List<EnemyAI> nearbyEnemies = new List<EnemyAI>();

    [Header("=== PREDICTION ===")]
    [SerializeField] private bool usePrediction = true;
    [SerializeField] private float predictionTime = 0.4f;
    private Vector2 playerLastPosition;
    private Vector2 playerVelocity;

    [Header("=== STUCK DETECTION ===")]
    [SerializeField] private float stuckCheckTime = 2f;
    [SerializeField] private float stuckThreshold = 0.5f;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private bool isStuck = false;
    private Vector2 unstuckDirection = Vector2.zero;

    [Header("=== WANDERING ===")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float wanderChangeInterval = 3f;
    private Vector2 wanderTarget;
    private float wanderTimer = 0f;

    [Header("=== LAYERS ===")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask enemyLayer;

    [Header("=== ADVANCED AI ===")]
    [SerializeField] private bool useAStarPathfinding = false;
    [SerializeField] private bool useBehaviorTree = false;
    [SerializeField] private float pathUpdateInterval = 0.5f;
    private List<Vector3> currentPath;
    private int currentPathIndex = 0;
    private float lastPathUpdateTime = 0f;
    private BehaviorTree behaviorTree;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugRays = false;
    [SerializeField] private bool showPath = true;

    public enum AIState
    {
        Seeking,
        Pursuing,
        Attacking,
        Wandering,
        Stuck,
        Dead
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Rigidbody2D setup
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        // Layer masks
        playerLayer = LayerMask.GetMask("Player");
        wallLayer = LayerMask.GetMask("Wall");
        enemyLayer = LayerMask.GetMask("Enemy");
        obstacleLayer = wallLayer;
        visionBlockLayer = wallLayer;

        FindPlayer();
        ChangeState(AIState.Seeking);

        wanderTarget = GetRandomWanderTarget();
        lastPosition = transform.position;

        // Setup Behavior Tree if enabled
        if (useBehaviorTree)
        {
            SetupBehaviorTree();
        }
    }

    private void Update()
    {
        if (currentState == AIState.Dead)
            return;

        // Use Behavior Tree if enabled
        if (useBehaviorTree && behaviorTree != null)
        {
            behaviorTree.Tick();
            return;
        }

        // Flocking: Find nearby enemies
        if (useFlocking)
            FindNearbyEnemies();

        // Prediction: Calculate player velocity
        if (usePrediction && playerTransform != null)
        {
            playerVelocity = ((Vector2)playerTransform.position - playerLastPosition) / Time.deltaTime;
            playerLastPosition = playerTransform.position;
        }

        // Check if stuck
        CheckIfStuck();

        // AI behavior
        UpdateAI();
    }

    private void FixedUpdate()
    {
        if (currentState == AIState.Dead)
            return;

        ApplyMovement();
    }

    private void UpdateAI()
    {
        if (isStuck && currentState != AIState.Stuck)
        {
            ChangeState(AIState.Stuck);
        }

        switch (currentState)
        {
            case AIState.Seeking:
                HandleSeeking();
                break;
            case AIState.Pursuing:
                HandlePursuing();
                break;
            case AIState.Attacking:
                HandleAttacking();
                break;
            case AIState.Wandering:
                HandleWandering();
                break;
            case AIState.Stuck:
                HandleStuck();
                break;
        }
    }

    private void HandleSeeking()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null)
            {
                ChangeState(AIState.Wandering);
                return;
            }
        }

        if (CanSeeTarget(playerTransform.position))
        {
            ChangeState(AIState.Pursuing);
            return;
        }

        ChangeState(AIState.Wandering);
    }

    private void HandlePursuing()
    {
        if (playerTransform == null)
        {
            ChangeState(AIState.Seeking);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackRange)
        {
            ChangeState(AIState.Attacking);
            return;
        }

        if (!CanSeeTarget(playerTransform.position))
        {
            ChangeState(AIState.Seeking);
            return;
        }

        Vector2 targetPosition = usePrediction ?
            PredictPlayerPosition() :
            (Vector2)playerTransform.position;

        desiredVelocity = CalculateSteeringForce(targetPosition);
    }

    private void HandleAttacking()
    {
        if (currentTarget == null)
            currentTarget = playerTransform;

        if (currentTarget == null)
        {
            ChangeState(AIState.Seeking);
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

        if (distanceToTarget > attackRange * 1.5f)
        {
            ChangeState(AIState.Pursuing);
            return;
        }

        if (!isLunging)
        {
            desiredVelocity = Vector2.Lerp(desiredVelocity, Vector2.zero, Time.deltaTime * deceleration);
        }

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack(currentTarget);
            lastAttackTime = Time.time;
        }
    }

    private void HandleWandering()
    {
        if (playerTransform != null && CanSeeTarget(playerTransform.position))
        {
            ChangeState(AIState.Pursuing);
            return;
        }

        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderChangeInterval || Vector2.Distance(transform.position, wanderTarget) < 1f)
        {
            wanderTarget = GetRandomWanderTarget();
            wanderTimer = 0f;
        }

        desiredVelocity = CalculateSteeringForce(wanderTarget);
    }

    private void HandleStuck()
    {
        if (unstuckDirection == Vector2.zero)
        {
            unstuckDirection = Random.insideUnitCircle.normalized;
        }

        desiredVelocity = unstuckDirection * maxSpeed;

        stuckTimer += Time.deltaTime;
        if (stuckTimer > 2f)
        {
            isStuck = false;
            unstuckDirection = Vector2.zero;
            ChangeState(AIState.Seeking);
        }
    }

    private Vector2 CalculateSteeringForce(Vector2 targetPosition)
    {
        Vector2 desiredDirection = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        Vector2 steering = desiredDirection * maxSpeed;

        // Obstacle avoidance
        Vector2 avoidanceForce = CalculateObstacleAvoidance();
        steering += avoidanceForce * obstacleAvoidanceWeight;

        // Flocking
        if (useFlocking && nearbyEnemies.Count > 0)
        {
            Vector2 separation = CalculateSeparation();
            Vector2 cohesion = CalculateCohesion();

            steering += separation * separationWeight;
            steering += cohesion * cohesionWeight;
        }

        return Vector2.ClampMagnitude(steering, maxSpeed);
    }

    private Vector2 CalculateObstacleAvoidance()
    {
        Vector2 avoidanceDirection = Vector2.zero;
        Vector2 moveDirection = velocity.normalized;

        if (moveDirection == Vector2.zero)
            moveDirection = (playerTransform != null) ?
                ((Vector2)playerTransform.position - (Vector2)transform.position).normalized :
                Vector2.right;

        float[] rayWeights = new float[avoidanceRayCount];
        float angleStep = avoidanceRayAngle / (avoidanceRayCount - 1);

        for (int i = 0; i < avoidanceRayCount; i++)
        {
            float angle = -avoidanceRayAngle / 2f + angleStep * i;
            Vector2 rayDirection = Quaternion.Euler(0, 0, angle) * moveDirection;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection,
                obstacleAvoidanceDistance, obstacleLayer);

            if (hit.collider != null)
            {
                float distanceFactor = 1f - (hit.distance / obstacleAvoidanceDistance);
                rayWeights[i] = -distanceFactor;

                if (showDebugRays)
                    Debug.DrawRay(transform.position, rayDirection * hit.distance, Color.red);
            }
            else
            {
                rayWeights[i] = 1f;

                if (showDebugRays)
                    Debug.DrawRay(transform.position, rayDirection * obstacleAvoidanceDistance, Color.green);
            }
        }

        int bestDirectionIndex = 0;
        float bestWeight = rayWeights[0];

        for (int i = 1; i < avoidanceRayCount; i++)
        {
            if (rayWeights[i] > bestWeight)
            {
                bestWeight = rayWeights[i];
                bestDirectionIndex = i;
            }
        }

        float bestAngle = -avoidanceRayAngle / 2f + angleStep * bestDirectionIndex;
        avoidanceDirection = Quaternion.Euler(0, 0, bestAngle) * moveDirection;

        return avoidanceDirection * bestWeight;
    }

    private Vector2 CalculateSeparation()
    {
        Vector2 separationForce = Vector2.zero;
        int count = 0;

        foreach (var enemy in nearbyEnemies)
        {
            if (enemy == null || enemy == this)
                continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < separationDistance && distance > 0.01f)
            {
                Vector2 awayFromEnemy = ((Vector2)transform.position - (Vector2)enemy.transform.position).normalized;
                separationForce += awayFromEnemy / distance;
                count++;
            }
        }

        if (count > 0)
            separationForce /= count;

        return separationForce;
    }

    private Vector2 CalculateCohesion()
    {
        Vector2 centerOfMass = Vector2.zero;
        int count = 0;

        foreach (var enemy in nearbyEnemies)
        {
            if (enemy == null || enemy == this)
                continue;

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < cohesionDistance)
            {
                centerOfMass += (Vector2)enemy.transform.position;
                count++;
            }
        }

        if (count > 0)
        {
            centerOfMass /= count;
            return (centerOfMass - (Vector2)transform.position).normalized;
        }

        return Vector2.zero;
    }

    private Vector2 PredictPlayerPosition()
    {
        if (playerTransform == null)
            return Vector2.zero;

        Vector2 predictedPosition = (Vector2)playerTransform.position + playerVelocity * predictionTime;

        if (showDebugRays)
            Debug.DrawLine(playerTransform.position, predictedPosition, Color.cyan);

        return predictedPosition;
    }

    private bool CanSeeTarget(Vector3 targetPosition)
    {
        Vector2 directionToTarget = (targetPosition - transform.position).normalized;
        float distanceToTarget = Vector2.Distance(transform.position, targetPosition);

        // Close range detection (360°)
        if (distanceToTarget <= closeRangeDetection)
            return true;

        // Outside vision range
        if (distanceToTarget > visionRange)
            return false;

        // Check vision angle
        Vector2 forward = velocity.normalized;
        if (forward == Vector2.zero)
            forward = Vector2.right;

        float angleToTarget = Vector2.Angle(forward, directionToTarget);
        if (angleToTarget > visionAngle / 2f)
            return false;

        // Check line of sight
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget,
            distanceToTarget, visionBlockLayer);

        if (hit.collider != null)
        {
            if (showDebugRays)
                Debug.DrawLine(transform.position, hit.point, Color.red);
            return false;
        }

        if (showDebugRays)
            Debug.DrawLine(transform.position, targetPosition, Color.green);

        return true;
    }

    private void ApplyMovement()
    {
        float accelRate = (desiredVelocity.magnitude > 0.01f) ? acceleration : deceleration;
        velocity = Vector2.MoveTowards(velocity, desiredVelocity, accelRate * Time.fixedDeltaTime);

        rb.linearVelocity = velocity;

        if (velocity.magnitude > 0.1f && spriteRenderer != null)
        {
            spriteRenderer.flipX = velocity.x < 0;
        }
    }

    private void PerformAttack(Transform target)
    {
        if (target == null)
            return;

        // Lunge attack
        Vector2 lungeDirection = (target.position - transform.position).normalized;
        desiredVelocity = lungeDirection * attackLungeSpeed;
        isLunging = true;
        Invoke(nameof(StopLunge), 0.2f);

        // Deal damage
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(attackDamage);
        }
    }

    private void StopLunge()
    {
        isLunging = false;
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerLastPosition = playerTransform.position;
        }
    }

    private void FindNearbyEnemies()
    {
        nearbyEnemies.Clear();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, cohesionDistance, enemyLayer);

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject)
                continue;

            EnemyAI enemyAI = col.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                nearbyEnemies.Add(enemyAI);
            }
        }
    }

    private void CheckIfStuck()
    {
        float movedDistance = Vector3.Distance(transform.position, lastPosition);

        if (movedDistance < stuckThreshold && desiredVelocity.magnitude > 0.5f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckCheckTime)
            {
                isStuck = true;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    private Vector2 GetRandomWanderTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        return (Vector2)transform.position + randomDirection * wanderRadius;
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        switch (newState)
        {
            case AIState.Attacking:
                currentTarget = playerTransform;
                break;
            case AIState.Stuck:
                stuckTimer = 0f;
                break;
        }
    }

    public void Die()
    {
        ChangeState(AIState.Dead);
        rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
    }

    public void Respawn(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        velocity = Vector2.zero;
        desiredVelocity = Vector2.zero;
        currentTarget = null;
        isStuck = false;
        stuckTimer = 0f;
        unstuckDirection = Vector2.zero;
        lastAttackTime = -999f;
        lastPosition = spawnPosition;

        FindPlayer();
        ChangeState(AIState.Seeking);
    }

    // ===== A* PATHFINDING METHODS =====

    private void UpdatePathToTarget(Vector3 targetPosition)
    {
        if (Time.time >= lastPathUpdateTime + pathUpdateInterval)
        {
            if (AStarPathfinding.Instance != null)
            {
                currentPath = AStarPathfinding.Instance.FindPath(transform.position, targetPosition);
                currentPathIndex = 0;
                lastPathUpdateTime = Time.time;
            }
        }
    }

    private void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        if (currentPathIndex >= currentPath.Count)
        {
            currentPath = null;
            return;
        }

        Vector3 targetWaypoint = currentPath[currentPathIndex];
        float distance = Vector2.Distance(transform.position, targetWaypoint);

        if (distance < 0.5f)
        {
            currentPathIndex++;
            if (currentPathIndex >= currentPath.Count)
            {
                currentPath = null;
                return;
            }
        }

        Vector2 direction = (targetWaypoint - transform.position).normalized;
        desiredVelocity = direction * maxSpeed;
    }

    // ===== BEHAVIOR TREE METHODS =====

    private void SetupBehaviorTree()
    {
        // Build behavior tree structure
        var rootNode = new BehaviorTree.Selector(new List<BehaviorTree.Node>
        {
            // Priority 1: Handle stuck situation
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                new CheckIfStuck(this),
                new TaskUnstuck(this)
            }),

            // Priority 2: Attack if in range
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                new CheckPlayerInVision(this),
                new CheckInAttackRange(this, attackRange),
                new TaskAttack(this, attackCooldown)
            }),

            // Priority 3: Pursue if visible
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                new CheckPlayerInVision(this),
                new TaskMoveToTarget(this)
            }),

            // Priority 4: Wander
            new TaskWander(this)
        });

        behaviorTree = new BehaviorTree(rootNode);
    }

    // Public methods for Behavior Tree nodes
    public Transform GetPlayerTransform() => playerTransform;
    public bool CanSeePlayer() => playerTransform != null && CanSeeTarget(playerTransform.position);
    public bool IsStuck() => isStuck;

    public void PerformAttackBT(Transform target)
    {
        PerformAttack(target);
    }

    public void MoveTowardsBT(Vector3 targetPosition)
    {
        if (useAStarPathfinding && AStarPathfinding.Instance != null)
        {
            UpdatePathToTarget(targetPosition);
            if (currentPath != null && currentPath.Count > 0)
            {
                FollowPath();
                return;
            }
        }

        desiredVelocity = CalculateSteeringForce(targetPosition);
    }

    public void WanderBT()
    {
        HandleWandering();
    }

    public void UnstuckBT()
    {
        HandleStuck();
    }

    // ===== GIZMOS =====

    private void OnDrawGizmosSelected()
    {
        // Vision range
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Close range detection
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, closeRangeDetection);

        // Attack range
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Flocking distances
        if (useFlocking)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, separationDistance);

            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, cohesionDistance);
        }

        // A* Path visualization
        if (useAStarPathfinding && showPath && currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                Gizmos.DrawSphere(currentPath[i], 0.2f);
            }
            if (currentPath.Count > 0)
            {
                Gizmos.DrawSphere(currentPath[currentPath.Count - 1], 0.2f);
            }
        }
    }
}
