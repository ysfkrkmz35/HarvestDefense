using UnityEngine;
using System.Collections.Generic;

namespace YusufTest
{
    /// <summary>
    /// Gelişmiş Düşman AI Sistemi
    /// - Devriye (Patrol)
    /// - Algılama menzili
    /// - Kovalama + Vazgeçme
    /// - Çember hareketi (Strafe)
    /// - Saldırı sonrası geri çekilme
    /// - Knockback
    /// - Farklı saldırı desenleri
    /// - Sürü davranışı (üst üste binmeme)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class SimpleEnemyAI : MonoBehaviour
    {
        #region === ENUMS ===
        public enum AIState
        {
            Idle,       // Hareketsiz bekliyor
            Patrol,     // Rastgele dolaşıyor
            Chase,      // Player'ı kovalıyor
            Strafe,     // Player etrafında dönüyor
            Attack,     // Saldırıyor
            Retreat,    // Geri çekiliyor
            Stunned     // Knockback yemiş, hareket edemiyor
        }
        #endregion

        #region === INSPECTOR VARIABLES ===
        [Header("═══ MOVEMENT ═══")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float strafeSpeed = 2.5f;
        [SerializeField] private float retreatSpeed = 3f;

        [Header("═══ DETECTION ═══")]
        [SerializeField] private float detectionRange = 10f;     // Player'ı görme mesafesi
        [SerializeField] private float loseTargetRange = 15f;    // Takibi bırakma mesafesi
        [SerializeField] private float attackRange = 1.5f;       // Saldırı mesafesi
        [SerializeField] private float strafeRange = 3f;         // Strafe başlama mesafesi

        [Header("═══ ATTACK ═══")]
        [SerializeField] private int attackDamage = 10;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float attackWindup = 0.3f;      // Saldırı öncesi bekleme
        [Tooltip("Saldırı sonrası geri çekilme süresi")]
        [SerializeField] private float retreatDuration = 0.5f;
        // [SerializeField] private float retreatDistance = 1.5f; // Unused for now, duration based retreat implemented

        [Header("═══ PATROL ═══")]
        [SerializeField] private float patrolRadius = 5f;        // Dolaşma alanı
        [SerializeField] private float patrolWaitTime = 2f;      // Noktada bekleme
        [SerializeField] private float idleChance = 0.3f;        // Idle kalma olasılığı

        [Header("═══ STRAFE ═══")]
        [SerializeField] private float strafeTime = 1.5f;        // Strafe süresi
        [SerializeField] private float strafeChance = 0.4f;      // Strafe yapma olasılığı

        [Header("═══ KNOCKBACK ═══")]
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private float knockbackDuration = 0.3f;

        [Header("═══ FLOCK BEHAVIOR ═══")]
        [Tooltip("Diğer düşmanlardan kaçınma")]
        [SerializeField] private float separationRadius = 1.2f;
        [SerializeField] private float separationForce = 2f;

        [Header("═══ ANIMATION ═══")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform bodyTransform;
        
        [Header("═══ DIRECT ANIMATION (Optional) ═══")]
        [Tooltip("Enable to use Animator.Play() with state names below. Leave OFF for parameter-based animation (existing mobs).")]
        [SerializeField] private bool useDirectPlayAnimation = false;
        [Tooltip("Animation state name for idle (e.g., 'Idle')")]
        [SerializeField] private string idleAnimState = "";
        [Tooltip("Animation state name for walking (e.g., 'Walking')")]
        [SerializeField] private string walkAnimState = "";
        [Tooltip("Animation state name for attack (e.g., 'Slashing')")]
        [SerializeField] private string attackAnimState = "";
        [Tooltip("Animation state name for hurt (e.g., 'Hurt')")]
        [SerializeField] private string hurtAnimState = "";
        [Tooltip("Animation state name for death (e.g., 'Dying')")]
        [SerializeField] private string deathAnimState = "";
        
        [Header("═══ BODY ROTATION ═══")]
        [Tooltip("Yukarı/aşağı hareket ederken body'nin eğilme açısı")]
        [SerializeField] private float maxBodyTilt = 15f;
        [Tooltip("Rotation geçiş hızı")]
        [SerializeField] private float rotationSmoothSpeed = 8f;

        [Header("═══ DEBUG ═══")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private AIState currentState = AIState.Idle;
        #endregion

        #region === PRIVATE VARIABLES ===
        private Transform player;
        private Rigidbody2D rb;
        private Vector3 spawnPosition;
        
        // State timers
        private float stateTimer = 0f;
        private float nextAttackTime = 0f;
        private float stunEndTime = 0f;
        
        // Patrol
        private Vector2 patrolTarget;
        private bool hasPatrolTarget = false;
        
        // Strafe
        private int strafeDirection = 1; // 1 = saat yönü, -1 = ters
        
        // Attack
        private bool isAttacking = false;
        private bool useAlternateAttack = false;
        private Coroutine attackCoroutine = null;
        
        // Player tracking
        private bool isPlayerDetected = false;
        
        // Body rotation
        private Vector3 targetBodyRotation = Vector3.zero;
        private Vector2 lastMovementDirection = Vector2.zero;
        
        // Cache
        private static List<SimpleEnemyAI> allEnemies = new List<SimpleEnemyAI>();
        #endregion

        #region === UNITY CALLBACKS ===
        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            SetupRigidbody();
        }

        // Animation Hashes
        private int moveBoolHash = 0;
        private int speedFloatHash = 0;
        private bool hasMoveBool = false;
        private bool hasSpeedFloat = false;
        
        // Attack Hashes
        private int attackHash = 0;
        private int attack02Hash = 0;
        private bool hasAttack = false;
        private bool hasAttack02 = false;

        void Start()
        {
            spawnPosition = transform.position;
            FindPlayer();
            FindAnimatorAndSprite();
            
            // Auto-detect Animation Parameters
            if (animator != null)
            {
                var parameters = result_params(animator);
                
                // Check for common boolean movement parameters (Merged from previous fix)
                string[] boolParams = new string[] { "walk", "Walk", "isWalking", "IsWalking", "run", "Run", "isMoving", "IsMoving", "moving", "Moving" };
                foreach (var param in parameters)
                {
                    foreach (var check in boolParams)
                    {
                        if (param.name == check && param.type == AnimatorControllerParameterType.Bool)
                        {
                            moveBoolHash = param.nameHash;
                            hasMoveBool = true;
                            // Debug.Log($"[SimpleEnemyAI] Found movement bool parameter: '{check}'");
                            break;
                        }
                    }
                    if (hasMoveBool) break;
                }

                // Check for common float speed parameters (Merged from previous fix)
                string[] floatParams = new string[] { "Speed", "speed", "Velocity", "velocity" };
                foreach (var param in parameters)
                {
                    foreach (var check in floatParams)
                    {
                        if (param.name == check && param.type == AnimatorControllerParameterType.Float)
                        {
                            speedFloatHash = param.nameHash;
                            hasSpeedFloat = true;
                            // Debug.Log($"[SimpleEnemyAI] Found speed float parameter: '{check}'");
                            break;
                        }
                    }
                    if (hasSpeedFloat) break;
                }
                
                // Check for Attack parameters
                // Primary Attack
                string[] attackParams = new string[] { "attack", "Attack", "fire", "Fire", "slash", "Slash", "hit", "Hit" };
                foreach (var param in parameters)
                {
                    foreach (var check in attackParams)
                    {
                        if (param.name == check && param.type == AnimatorControllerParameterType.Trigger)
                        {
                            attackHash = param.nameHash;
                            hasAttack = true;
                            Debug.Log($"[SimpleEnemyAI] Found primary attack parameter: '{check}'");
                            break;
                        }
                    }
                    if (hasAttack) break;
                }
                
                // Secondary Attack (specific check for attack02 or similar)
                string[] attack02Params = new string[] { "attack02", "Attack02", "attack2", "Attack2", "HeavyAttack" };
                foreach (var param in parameters)
                {
                    foreach (var check in attack02Params)
                    {
                        if (param.name == check && param.type == AnimatorControllerParameterType.Trigger)
                        {
                            attack02Hash = param.nameHash;
                            hasAttack02 = true;
                            Debug.Log($"[SimpleEnemyAI] Found secondary attack parameter: '{check}'");
                            break;
                        }
                    }
                    if (hasAttack02) break;
                }
            }

            // Kendini listeye ekle
            allEnemies.Add(this);
            
            // Rastgele strafe yönü
            strafeDirection = Random.value > 0.5f ? 1 : -1;
        }

        private AnimatorControllerParameter[] result_params(Animator anim) {
            return anim.parameters;
        }

        void OnDestroy()
        {
            allEnemies.Remove(this);
        }

        void FixedUpdate()
        {
            // Stunned durumunda hareket yok
            if (currentState == AIState.Stunned)
            {
                if (Time.time >= stunEndTime)
                {
                    ExitStunned();
                }
                return;
            }

            // Player kontrolü
            if (player == null)
            {
                FindPlayer();
                if (player == null)
                {
                    isPlayerDetected = false;
                    SetState(AIState.Patrol);
                    HandlePatrol();
                    return;
                }
            }

            // Mesafe hesapla
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            // Player algılama durumunu güncelle
            UpdatePlayerDetection(distanceToPlayer);

            // State machine
            UpdateState(distanceToPlayer);
            ExecuteState(distanceToPlayer);
            
            // Animasyonları güncelle
            UpdateAnimations();
        }
        #endregion

        #region === PLAYER DETECTION ===
        void UpdatePlayerDetection(float distanceToPlayer)
        {
            // Player'ı algıla
            if (!isPlayerDetected && distanceToPlayer <= detectionRange)
            {
                isPlayerDetected = true;
                Debug.Log($"[SimpleEnemyAI] 👁️ Player algılandı! Mesafe: {distanceToPlayer:F1}");
            }
            // Player'ı kaybet
            else if (isPlayerDetected && distanceToPlayer > loseTargetRange)
            {
                isPlayerDetected = false;
                Debug.Log($"[SimpleEnemyAI] ❌ Player kaybedildi! Mesafe: {distanceToPlayer:F1}");
            }
        }

        /// <summary>
        /// Player'a doğru bakıyor mu kontrol et
        /// </summary>
        bool IsFacingPlayer()
        {
            if (player == null || spriteRenderer == null) return true; // Güvenli varsayım
            
            float directionToPlayer = player.position.x - transform.position.x;
            
            // flipX = true ise sola bakıyor, false ise sağa bakıyor
            bool facingLeft = spriteRenderer.flipX;
            bool playerOnLeft = directionToPlayer < 0;
            
            return facingLeft == playerOnLeft;
        }

        /// <summary>
        /// Player'a dön
        /// </summary>
        void FacePlayer()
        {
            if (player == null || spriteRenderer == null) return;
            
            float directionToPlayer = player.position.x - transform.position.x;
            spriteRenderer.flipX = directionToPlayer < 0;
        }
        #endregion

        #region === STATE MACHINE ===
        void UpdateState(float distanceToPlayer)
        {
            // Attacking durumundayken state değiştirme
            if (isAttacking) return;

            switch (currentState)
            {
                case AIState.Idle:
                case AIState.Patrol:
                    // Player algılandı mı?
                    if (isPlayerDetected)
                    {
                        SetState(AIState.Chase);
                    }
                    break;

                case AIState.Chase:
                    // Player kaybedildi mi?
                    if (!isPlayerDetected)
                    {
                        SetState(AIState.Patrol);
                    }
                    // Strafe mesafesinde mi?
                    else if (distanceToPlayer <= strafeRange && distanceToPlayer > attackRange)
                    {
                        // Belirli olasılıkla strafe yap
                        if (Random.value < strafeChance * Time.fixedDeltaTime)
                        {
                            SetState(AIState.Strafe);
                        }
                    }
                    // Saldırı mesafesinde mi?
                    else if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
                    {
                        SetState(AIState.Attack);
                    }
                    break;

                case AIState.Strafe:
                    // Player kaybedildi mi?
                    if (!isPlayerDetected)
                    {
                        SetState(AIState.Patrol);
                    }
                    // Strafe süresi doldu mu?
                    else if (stateTimer <= 0)
                    {
                        SetState(AIState.Chase);
                    }
                    // Saldırı mesafesinde mi ve cooldown bitti mi?
                    else if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
                    {
                        SetState(AIState.Attack);
                    }
                    // Player kaçtı mı?
                    else if (distanceToPlayer > strafeRange * 1.5f)
                    {
                        SetState(AIState.Chase);
                    }
                    break;

                case AIState.Attack:
                    // Attack state içinde handle ediliyor
                    break;

                case AIState.Retreat:
                    // Retreat süresi doldu mu?
                    if (stateTimer <= 0)
                    {
                        // Player hala algılanıyorsa chase, değilse patrol
                        if (isPlayerDetected)
                            SetState(AIState.Chase);
                        else
                            SetState(AIState.Patrol);
                    }
                    break;
            }
        }

        void ExecuteState(float distanceToPlayer)
        {
            // State timer'ı azalt
            stateTimer -= Time.fixedDeltaTime;

            Vector2 movement = Vector2.zero;

            switch (currentState)
            {
                case AIState.Idle:
                    HandleIdle();
                    break;

                case AIState.Patrol:
                    HandlePatrol();
                    break;

                case AIState.Chase:
                    movement = HandleChase(distanceToPlayer);
                    break;

                case AIState.Strafe:
                    movement = HandleStrafe();
                    break;

                case AIState.Attack:
                    HandleAttack(distanceToPlayer);
                    break;

                case AIState.Retreat:
                    movement = HandleRetreat();
                    break;
            }

            // Sürü davranışı - separation ekle
            if (currentState != AIState.Stunned && currentState != AIState.Idle && currentState != AIState.Attack)
            {
                movement += CalculateSeparation();
            }

            // Hareketi uygula (Ground tile kontrolü ile)
            if (movement != Vector2.zero)
            {
                // ═══ GROUND TİLE KONTROLÜ ═══
                // Enemy sadece Ground üzerinde hareket edebilir, Water'a giremez!
                Vector2 targetPosition = rb.position + movement * Time.fixedDeltaTime;

                if (IsPositionOnGround(targetPosition))
                {
                    // Ground üzerinde, hareket edebilir
                    rb.linearVelocity = movement;
                    FlipSprite(movement.x);
                }
                else
                {
                    // Water veya boşluk, hareketi engelle
                    rb.linearVelocity = Vector2.zero;
                }
            }
            else if (currentState == AIState.Idle || currentState == AIState.Attack)
            {
                rb.linearVelocity = Vector2.zero;
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

        void SetState(AIState newState)
        {
            if (currentState == newState) return;

            // Eski state'den çıkış
            OnExitState(currentState);

            currentState = newState;

            // Yeni state'e giriş
            OnEnterState(newState);

            Debug.Log($"[SimpleEnemyAI] State: {newState}");
        }

        void OnEnterState(AIState state)
        {
            switch (state)
            {
                case AIState.Idle:
                    stateTimer = Random.Range(1f, patrolWaitTime);
                    rb.linearVelocity = Vector2.zero;
                    break;

                case AIState.Patrol:
                    hasPatrolTarget = false;
                    break;

                case AIState.Strafe:
                    stateTimer = strafeTime;
                    // Rastgele yön değiştir
                    if (Random.value > 0.7f)
                        strafeDirection *= -1;
                    break;

                case AIState.Retreat:
                    stateTimer = retreatDuration;
                    break;

                case AIState.Attack:
                    isAttacking = true;
                    rb.linearVelocity = Vector2.zero;
                    // Önce player'a dön
                    FacePlayer();
                    break;
            }
        }

        void OnExitState(AIState state)
        {
            switch (state)
            {
                case AIState.Attack:
                    isAttacking = false;
                    // Coroutine'i durdur
                    if (attackCoroutine != null)
                    {
                        StopCoroutine(attackCoroutine);
                        attackCoroutine = null;
                    }
                    break;
            }
        }
        #endregion

        #region === STATE HANDLERS ===
        void HandleIdle()
        {
            rb.linearVelocity = Vector2.zero;
            
            // Idle'da rotation'ı yavaşça sıfırla
            ResetBodyRotation();

            if (stateTimer <= 0)
            {
                // Idle veya Patrol'a geç
                if (Random.value < idleChance)
                {
                    stateTimer = Random.Range(1f, patrolWaitTime);
                }
                else
                {
                    SetState(AIState.Patrol);
                }
            }
        }

        void HandlePatrol()
        {
            // Yeni hedef belirle
            if (!hasPatrolTarget)
            {
                patrolTarget = (Vector2)spawnPosition + Random.insideUnitCircle * patrolRadius;
                hasPatrolTarget = true;
            }

            // Hedefe git
            float distanceToTarget = Vector2.Distance(transform.position, patrolTarget);

            if (distanceToTarget < 0.5f)
            {
                // Hedefe ulaştı
                hasPatrolTarget = false;
                SetState(AIState.Idle);
            }
            else
            {
                Vector2 direction = ((Vector2)patrolTarget - (Vector2)transform.position).normalized;
                rb.linearVelocity = direction * patrolSpeed + CalculateSeparation();
                FlipSprite(direction.x);
            }
        }

        Vector2 HandleChase(float distanceToPlayer)
        {
            if (player == null) return Vector2.zero;
            
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            return direction * moveSpeed;
        }

        Vector2 HandleStrafe()
        {
            if (player == null) return Vector2.zero;

            // Player'a doğru vektör
            Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
            
            // Perpendicular (dik) vektör - strafe yönü
            Vector2 perpendicular = new Vector2(-toPlayer.y, toPlayer.x) * strafeDirection;
            
            // Biraz player'a doğru da git (mesafeyi koru)
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            float approachFactor = distanceToPlayer > strafeRange ? 0.5f : 0f;
            
            Vector2 movement = (perpendicular + toPlayer * approachFactor).normalized * strafeSpeed;
            
            // Player'a bak
            FlipSprite(toPlayer.x);
            
            return movement;
        }

        void HandleAttack(float distanceToPlayer)
        {
            // Cooldown kontrolü
            if (Time.time < nextAttackTime)
            {
                SetState(AIState.Chase);
                return;
            }

            // Player hala menzilde mi?
            if (!isPlayerDetected || distanceToPlayer > attackRange * 1.5f)
            {
                Debug.Log($"[SimpleEnemyAI] ⚠️ Saldırı iptal - Player menzil dışı! Mesafe: {distanceToPlayer:F1}");
                SetState(AIState.Chase);
                return;
            }

            // Player'a bak
            FacePlayer();

            // Saldırı başlat (sadece bir kere)
            if (attackCoroutine == null)
            {
                attackCoroutine = StartCoroutine(AttackSequence());
            }
        }

        System.Collections.IEnumerator AttackSequence()
        {
            // Dur
            rb.linearVelocity = Vector2.zero;
            
            // Player'a dön
            FacePlayer();
            
            // Animasyon tetikle
            if (animator != null)
            {
                if (useDirectPlayAnimation && !string.IsNullOrEmpty(attackAnimState))
                {
                    // Direct Play for mobs like Golem
                    PlayAnimationState(attackAnimState);
                }
                else
                {
                    // Parameter-based for existing mobs
                    animator.SetTrigger(useAlternateAttack ? "attack02" : "attack");
                    useAlternateAttack = !useAlternateAttack;
                }
            }

            // Windup - saldırı hazırlığı
            yield return new WaitForSeconds(attackWindup);

            // SON KONTROL: Player hala menzilde mi ve önümde mi?
            if (player != null && isPlayerDetected)
            {
                float dist = Vector2.Distance(transform.position, player.position);
                
                if (dist <= attackRange * 1.3f && IsFacingPlayer())
                {
                    // Hasar ver
                    IDamageable damageable = player.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(attackDamage);
                        Debug.Log($"[SimpleEnemyAI] ⚔️ {attackDamage} hasar verildi! Mesafe: {dist:F1}");
                    }
                }
                else
                {
                    Debug.Log($"[SimpleEnemyAI] ❌ Saldırı ıskaladı! Mesafe: {dist:F1}, Yön doğru: {IsFacingPlayer()}");
                }
            }

            nextAttackTime = Time.time + attackCooldown;
            attackCoroutine = null;

            // Saldırı sonrası geri çekil
            yield return new WaitForSeconds(0.1f);
            SetState(AIState.Retreat);
        }

        Vector2 HandleRetreat()
        {
            if (player == null) return Vector2.zero;

            // Player'dan uzaklaş
            Vector2 awayFromPlayer = ((Vector2)transform.position - (Vector2)player.position).normalized;
            return awayFromPlayer * retreatSpeed;
        }
        #endregion

        #region === KNOCKBACK & DAMAGE ===
        /// <summary>
        /// Hasar alındığında çağır - knockback uygular
        /// </summary>
        public void OnDamaged(Vector2 damageSource)
        {
            // Knockback yönü
            Vector2 knockbackDir = ((Vector2)transform.position - damageSource).normalized;
            
            ApplyKnockback(knockbackDir);
            
            // Damage animasyonu
            if (animator != null)
            {
                if (useDirectPlayAnimation && !string.IsNullOrEmpty(hurtAnimState))
                {
                    // Direct Play for mobs like Golem
                    PlayAnimationState(hurtAnimState);
                }
                else
                {
                    // Parameter-based for existing mobs
                    animator.SetTrigger("damage");
                }
            }
        }

        /// <summary>
        /// Hasar alındığında çağır (konum bilgisi olmadan)
        /// </summary>
        public void OnDamaged()
        {
            if (player != null)
            {
                OnDamaged(player.position);
            }
            else
            {
                // Rastgele yöne knockback
                ApplyKnockback(Random.insideUnitCircle.normalized);
            }
        }

        void ApplyKnockback(Vector2 direction)
        {
            // Saldırıyı iptal et
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }
            
            currentState = AIState.Stunned;
            stunEndTime = Time.time + knockbackDuration;
            isAttacking = false;
            
            // Knockback kuvveti uygula
            rb.linearVelocity = direction * knockbackForce;
        }

        void ExitStunned()
        {
            if (isPlayerDetected)
                SetState(AIState.Chase);
            else
                SetState(AIState.Patrol);
        }
        #endregion

        #region === FLOCK BEHAVIOR ===
        Vector2 CalculateSeparation()
        {
            Vector2 separation = Vector2.zero;
            int neighborCount = 0;

            foreach (var enemy in allEnemies)
            {
                if (enemy == this || enemy == null) continue;

                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                
                if (distance < separationRadius && distance > 0)
                {
                    // Uzaklaşma vektörü
                    Vector2 away = (Vector2)transform.position - (Vector2)enemy.transform.position;
                    separation += away.normalized / distance; // Yakınlık oranında güçlü
                    neighborCount++;
                }
            }

            if (neighborCount > 0)
            {
                separation /= neighborCount;
                separation = separation.normalized * separationForce;
            }

            return separation;
        }
        #endregion

        #region === ANIMATION ===
        void UpdateAnimations()
        {
            if (animator == null) return;

            bool isMoving = rb.linearVelocity.magnitude > 0.1f;
            
            // Use direct Play() method for mobs with configured state names (e.g., Golem)
            if (useDirectPlayAnimation && !string.IsNullOrEmpty(idleAnimState) && !string.IsNullOrEmpty(walkAnimState))
            {
                if (!isAttacking)
                {
                    string targetState = isMoving ? walkAnimState : idleAnimState;
                    PlayAnimationState(targetState);
                }
            }
            else
            {
                // Default: Use parameter-based animation (existing mobs like Spider)
                animator.SetBool("walk", isMoving && !isAttacking);
            }
            
            // Body rotation güncelle
            UpdateBodyRotation();
        }
        
        /// <summary>
        /// Play animation state directly (safe, prevents re-triggering same state)
        /// </summary>
        void PlayAnimationState(string stateName)
        {
            if (animator == null || string.IsNullOrEmpty(stateName)) return;
            
            // Don't restart the same animation
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(stateName))
            {
                animator.Play(stateName);
            }
        }


        /// <summary>
        /// Hareket yönüne göre body'yi döndür (4 yön illüzyonu)
        /// </summary>
        void UpdateBodyRotation()
        {
            if (bodyTransform == null) return;
            
            Vector2 moveDir = rb.linearVelocity.normalized;
            
            // Hareket yoksa son yönü kullan
            if (moveDir.magnitude < 0.1f)
            {
                // Player'a bakıyorsa ona göre ayarla
                if (isPlayerDetected && player != null)
                {
                    moveDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                }
                else
                {
                    moveDir = lastMovementDirection;
                }
            }
            else
            {
                lastMovementDirection = moveDir;
            }
            
            // Hedef rotation hesapla
            // Y ekseni (yukarı/aşağı) -> X rotation (öne/arkaya eğilme)
            // Yukarı gidince arkaya yatık (pozitif X), aşağı gidince öne eğik (negatif X)
            float tiltX = -moveDir.y * maxBodyTilt;
            
            // Hafif Z rotation da ekle (daha dinamik görünüm)
            float tiltZ = -moveDir.x * (maxBodyTilt * 0.3f);
            
            // Eğer sprite flip ise Z tilt'i tersle
            if (spriteRenderer != null && spriteRenderer.flipX)
            {
                tiltZ = -tiltZ;
            }
            
            targetBodyRotation = new Vector3(tiltX, 0f, tiltZ);
            
            // Smooth geçiş
            Vector3 currentRotation = bodyTransform.localEulerAngles;
            
            // Euler açılarını -180 ile 180 arasına normalize et
            if (currentRotation.x > 180) currentRotation.x -= 360;
            if (currentRotation.z > 180) currentRotation.z -= 360;
            
            Vector3 smoothedRotation = Vector3.Lerp(currentRotation, targetBodyRotation, Time.deltaTime * rotationSmoothSpeed);
            bodyTransform.localEulerAngles = smoothedRotation;
        }

        /// <summary>
        /// Sprite'ı hareket yönüne çevir
        /// </summary>
        void FlipSprite(float directionX)
        {
            if (spriteRenderer != null && Mathf.Abs(directionX) > 0.1f)
            {
                spriteRenderer.flipX = directionX < 0;
            }
        }
        
        /// <summary>
        /// Idle durumda rotation'ı sıfırla
        /// </summary>
        void ResetBodyRotation()
        {
            if (bodyTransform == null) return;
            
            targetBodyRotation = Vector3.zero;
            bodyTransform.localEulerAngles = Vector3.Lerp(
                bodyTransform.localEulerAngles, 
                Vector3.zero, 
                Time.deltaTime * rotationSmoothSpeed
            );
        }
        #endregion

        #region === UTILITY ===
        void SetupRigidbody()
        {
            if (rb == null) return;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0;
            rb.linearDamping = 3f; // Biraz sürtünme (knockback için)
            rb.angularDamping = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        void FindAnimatorAndSprite()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            // Body transform'u bul (spriteRenderer'ın parent'ı veya kendisi)
            if (bodyTransform == null && spriteRenderer != null)
                bodyTransform = spriteRenderer.transform;
        }

        public void Die()
        {
            StopAllCoroutines();
            attackCoroutine = null;
            rb.linearVelocity = Vector2.zero;
            currentState = AIState.Idle;
            isAttacking = false;
            isPlayerDetected = false;
            
            // Play death animation if using direct play mode
            if (useDirectPlayAnimation && !string.IsNullOrEmpty(deathAnimState) && animator != null)
            {
                PlayAnimationState(deathAnimState);
                // Delay deactivation to let death animation play
                StartCoroutine(DeathAnimationCoroutine());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        
        System.Collections.IEnumerator DeathAnimationCoroutine()
        {
            // Wait for death animation to play (estimate based on typical animation length)
            yield return new WaitForSeconds(1.5f);
            gameObject.SetActive(false);
        }

        public void Respawn(Vector3 position)
        {
            transform.position = position;
            spawnPosition = position;
            rb.linearVelocity = Vector2.zero;
            nextAttackTime = 0f;
            stunEndTime = 0f;
            isAttacking = false;
            isPlayerDetected = false;
            hasPatrolTarget = false;
            attackCoroutine = null;
            currentState = AIState.Idle;
            gameObject.SetActive(true);
            FindPlayer();
        }
        #endregion

        #region === DEBUG ===
        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            // Detection range (sarı)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Lose target range (turuncu)
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, loseTargetRange);

            // Strafe range (mavi)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, strafeRange);

            // Attack range (kırmızı)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Patrol radius (yeşil)
            Vector3 patrolCenter = Application.isPlaying ? spawnPosition : transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(patrolCenter, patrolRadius);

            // Separation radius (magenta)
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, separationRadius);

            // Patrol target
            if (Application.isPlaying && hasPatrolTarget)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, patrolTarget);
                Gizmos.DrawSphere(patrolTarget, 0.2f);
            }
            
            // Facing direction indicator
            if (Application.isPlaying && spriteRenderer != null)
            {
                Gizmos.color = Color.blue;
                Vector3 facingDir = spriteRenderer.flipX ? Vector3.left : Vector3.right;
                Gizmos.DrawRay(transform.position, facingDir * 2f);
            }
        }
        #endregion
    }
}