using UnityEngine;

namespace YusufTest
{
    /// <summary>
    /// Drop Pickup - Minecraft tarzı yere düşen ve toplanan item
    /// - Spawn olduğunda havaya zıplar ve etrafa saçılır
    /// - YERE DÜŞMEDEN TOPLANAMAZ
    /// - Oyuncu yaklaşınca mıknatıs gibi çekilir
    /// - Toplandığında Gold veya XP verir
    /// </summary>
    public class DropPickup : MonoBehaviour
    {
        public enum PickupType
        {
            Gold,
            XP
        }

        [Header("═══ PICKUP SETTINGS ═══")]
        [SerializeField] private PickupType pickupType = PickupType.Gold;
        [SerializeField] private int amount = 10;

        [Header("═══ SPAWN ANIMATION ═══")]
        [Tooltip("Initial upward force when spawned")]
        [SerializeField] private float launchForceMin = 4f;
        [SerializeField] private float launchForceMax = 6f;
        
        [Tooltip("Random horizontal scatter force")]
        [SerializeField] private float scatterForceMin = 2f;
        [SerializeField] private float scatterForceMax = 4f;
        
        [Tooltip("Gravity strength")]
        [SerializeField] private float gravity = 12f;

        [Header("═══ IDLE ANIMATION ═══")]
        [Tooltip("Hover amplitude (up/down movement)")]
        [SerializeField] private float hoverAmplitude = 0.08f;
        
        [Tooltip("Hover speed")]
        [SerializeField] private float hoverSpeed = 3f;
        
        [Tooltip("Rotation speed")]
        [SerializeField] private float rotationSpeed = 120f;

        [Header("═══ MAGNET SETTINGS ═══")]
        [Tooltip("Distance at which pickup starts moving towards player")]
        [SerializeField] private float magnetRange = 1.5f;
        
        [Tooltip("Speed of movement towards player")]
        [SerializeField] private float magnetSpeed = 5f;
        
        [Tooltip("Acceleration when being pulled")]
        [SerializeField] private float magnetAcceleration = 10f;
        
        [Tooltip("Distance at which pickup is collected")]
        [SerializeField] private float collectDistance = 0.4f;

        [Header("═══ PICKUP DELAY ═══")]
        [Tooltip("Time after spawn before pickup can be collected (even if grounded)")]
        [SerializeField] private float pickupDelay = 0.5f;
        
        [Tooltip("Must be grounded before can be collected")]
        [SerializeField] private bool requireGrounded = true;

        [Header("═══ LIFETIME ═══")]
        [Tooltip("Time before pickup disappears (0 = never)")]
        [SerializeField] private float lifetime = 60f;
        
        [Tooltip("Start blinking this many seconds before despawn")]
        [SerializeField] private float blinkStartTime = 10f;

        [Header("═══ VISUAL ═══")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool autoFindSprite = true;

        [Header("═══ AUDIO ═══")]
        [SerializeField] private AudioClip collectSound;
        [SerializeField] private float collectVolume = 0.5f;

        [Header("═══ DEBUG ═══")]
        [SerializeField] private bool showDebugLogs = false;

        // Internal state
        private Vector2 velocity;
        private float currentMagnetSpeed;
        private bool isGrounded = false;
        private bool canBeCollected = false;
        private float groundY;
        private float spawnTime;
        private float hoverOffset;
        private Transform playerTransform;
        private bool isInitialized = false;

        private void Awake()
        {
            if (autoFindSprite && spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                    spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Start()
        {
            if (!isInitialized)
            {
                Initialize(pickupType, amount);
            }
        }

        /// <summary>
        /// Initialize the pickup with type and amount
        /// </summary>
        public void Initialize(PickupType type, int value)
        {
            pickupType = type;
            amount = value;
            spawnTime = Time.time;
            hoverOffset = Random.Range(0f, Mathf.PI * 2f);
            
            // Find player
            FindPlayer();

            // Random launch direction - daha güçlü ve rastgele
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float horizontalForce = Random.Range(scatterForceMin, scatterForceMax);
            float verticalForce = Random.Range(launchForceMin, launchForceMax);
            
            // Rastgele yöne fırlat
            velocity = new Vector2(
                Mathf.Cos(randomAngle) * horizontalForce,
                verticalForce
            );
            
            isGrounded = false;
            canBeCollected = false;
            currentMagnetSpeed = magnetSpeed;
            groundY = transform.position.y - 1f; // Başlangıç tahmini

            isInitialized = true;

            if (showDebugLogs)
                Debug.Log($"[DropPickup] ✨ Spawned {type} x{value} | Velocity: {velocity}");
        }

        private void FindPlayer()
        {
            // Try HappyHarvest player first
            if (HappyHarvest.GameManager.Instance?.Player != null)
            {
                playerTransform = HappyHarvest.GameManager.Instance.Player.transform;
            }
            else
            {
                // Fallback: find by tag
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            // Check pickup delay
            float timeSinceSpawn = Time.time - spawnTime;
            
            // Yere düşmeden ve delay geçmeden toplanamaz
            if (!canBeCollected)
            {
                if (isGrounded && timeSinceSpawn >= pickupDelay)
                {
                    canBeCollected = true;
                    if (showDebugLogs)
                        Debug.Log($"[DropPickup] ✓ Can now be collected!");
                }
            }

            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            // Sadece toplanabilir durumdaysa topla
            if (canBeCollected && distanceToPlayer <= collectDistance)
            {
                Collect();
                return;
            }

            // Magnet effect - sadece toplanabilir durumdaysa çekilsin
            if (canBeCollected && distanceToPlayer <= magnetRange)
            {
                MoveTowardsPlayer();
            }
            else if (!isGrounded)
            {
                // Havadayken fizik uygula
                ApplyPhysics();
            }
            else
            {
                // Yerdeyken idle animasyon
                IdleAnimation();
            }

            // Lifetime check
            HandleLifetime();
        }

        private void ApplyPhysics()
        {
            // Apply gravity
            velocity.y -= gravity * Time.deltaTime;

            // Move
            Vector3 newPos = transform.position + (Vector3)velocity * Time.deltaTime;
            
            // Simple ground check - raycast down
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Abs(velocity.y) * Time.deltaTime + 0.1f);
            
            if (hit.collider != null && velocity.y < 0)
            {
                // Hit ground
                newPos.y = hit.point.y + 0.1f;
                groundY = newPos.y;
                isGrounded = true;
                velocity = Vector2.zero;
                
                if (showDebugLogs)
                    Debug.Log($"[DropPickup] 📍 Grounded at Y={groundY:F2}");
            }
            else if (velocity.y < 0 && newPos.y < groundY)
            {
                // Fallback ground level
                newPos.y = groundY;
                isGrounded = true;
                velocity = Vector2.zero;
            }

            transform.position = newPos;

            // Havadayken hafif dönsün
            transform.Rotate(0, 0, rotationSpeed * 0.5f * Time.deltaTime);
        }

        private void MoveTowardsPlayer()
        {
            // Accelerate towards player
            currentMagnetSpeed += magnetAcceleration * Time.deltaTime;
            currentMagnetSpeed = Mathf.Min(currentMagnetSpeed, magnetSpeed * 3f);

            // Move towards player
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * currentMagnetSpeed * Time.deltaTime;

            // Spin faster when being collected
            transform.Rotate(0, 0, rotationSpeed * 2f * Time.deltaTime);
        }

        private void IdleAnimation()
        {
            // Hover up/down
            float hover = Mathf.Sin((Time.time + hoverOffset) * hoverSpeed) * hoverAmplitude;
            Vector3 pos = transform.position;
            pos.y = groundY + hover + 0.15f;
            transform.position = pos;

            // Gentle rotation
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }

        private void HandleLifetime()
        {
            if (lifetime <= 0) return;

            float age = Time.time - spawnTime;
            float timeLeft = lifetime - age;

            // Blink effect before despawn
            if (timeLeft <= blinkStartTime && spriteRenderer != null)
            {
                float blinkRate = Mathf.Lerp(2f, 10f, 1f - (timeLeft / blinkStartTime));
                bool visible = Mathf.Sin(Time.time * blinkRate * Mathf.PI * 2f) > 0;
                spriteRenderer.enabled = visible;
            }

            // Despawn
            if (age >= lifetime)
            {
                if (showDebugLogs)
                    Debug.Log($"[DropPickup] ⏰ Despawned (timeout)");
                Destroy(gameObject);
            }
        }

        private void Collect()
        {
            if (showDebugLogs)
                Debug.Log($"[DropPickup] ✅ Collected {pickupType} x{amount}");

            // Give reward based on type
            switch (pickupType)
            {
                case PickupType.Gold:
                    AddGold();
                    break;
                case PickupType.XP:
                    AddXP();
                    break;
            }

            // Play sound
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position, collectVolume);
            }

            // Destroy pickup
            Destroy(gameObject);
        }

        private void AddGold()
        {
            // HappyHarvest gold system
            if (HappyHarvest.GameManager.Instance?.Player != null)
            {
                HappyHarvest.GameManager.Instance.Player.Coins += amount;
                
                if (showDebugLogs)
                    Debug.Log($"[DropPickup] 💰 +{amount} Gold → HappyHarvest");
            }
        }

        private void AddXP()
        {
            // PlayerProgression XP system
            if (PlayerProgression.Instance != null)
            {
                PlayerProgression.Instance.AddXP(amount);
                
                if (showDebugLogs)
                    Debug.Log($"[DropPickup] ⭐ +{amount} XP → PlayerProgression");
            }
        }

        // Gizmos for debugging
        private void OnDrawGizmosSelected()
        {
            // Magnet range
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, magnetRange);

            // Collect distance
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, collectDistance);
        }
    }
}