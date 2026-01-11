using UnityEngine;

namespace YusufTest
{
    /// <summary>
    /// Bot kontrolü - Player botta iken hareket ve yönelme kontrolü
    /// Sağa/sola dönüş ve ileri/geri hareket desteği
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BoatController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Movement Settings")]
        [Tooltip("Botun ileri/geri hareket hızı")]
        [SerializeField] private float moveSpeed = 5f;

        // [Tooltip("Botun dönüş hızı (derece/saniye)")]
        // [SerializeField] private float rotationSpeed = 120f;

        [Header("Rotation Constraint")]
        [Tooltip("Rotation kısıtlamasını aktif et - Bot sadece yatay düzlemde kalır")]
        [SerializeField] private bool lockRotation = true;

        [Header("Physics")]
        [Tooltip("Hareket damping - Yavaşça durma efekti (daha düşük = daha hızlı durur)")]
        [SerializeField] private float movementDamping = 0.75f; // 0.85'ten 0.75'e düşürüldü - daha hızlı durma

        // [Tooltip("Dönüş damping - Yavaşça durma efekti")]
        // [SerializeField] private float rotationDamping = 0.9f;

        [Header("Water Physics")]
        [Tooltip("Su sürtünmesi - Botun su üzerindeki hareketi için")]
        [SerializeField] private float waterDrag = 3.5f; // 2.5'ten 3.5'e artırıldı - daha fazla sürtünme

        [Header("Visual")]
        [Tooltip("Bot visueli (sprite renderer)")]
        [SerializeField] private SpriteRenderer boatSprite;

        [Tooltip("Sprite'ın varsayılan baktığı yön (true = sağ, false = sol)")]
        [SerializeField] private bool spriteDefaultFacingRight = false;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        #endregion

        #region Private Fields

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private float rotationInput;
        private bool isPlayerOnBoard = false;

        private Vector2 currentVelocity;
        // private float currentAngularVelocity;

        // Auto-unstuck detection
        private Vector2 lastStuckCheckPosition;
        private float stuckCheckTimer = 0f;
        private const float stuckCheckInterval = 0.5f; // Check every 0.5 seconds (daha hızlı)
        private const float stuckMovementThreshold = 1.5f; // Minimum 1.5 units movement required (daha hassas)
        private int consecutiveStuckCount = 0; // Kaç kere üst üste sıkıştı
        private const int maxStuckAttempts = 3; // 3 kere sıkışırsa agresif mod

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            ConfigureRigidbody();
            lastStuckCheckPosition = transform.position;
        }

        private void Start()
        {
            if (boatSprite == null)
            {
                boatSprite = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void FixedUpdate()
        {
            if (!isPlayerOnBoard)
            {
                ApplyDamping();
                return;
            }

            HandleMovement();
            HandleRotation();
        }

        private void LateUpdate()
        {
            // Rotation constraint - Bot'un X ve Y rotasyonunu sıfırla
            if (lockRotation)
            {
                Vector3 euler = transform.eulerAngles;
                euler.x = 0f;
                euler.y = 0f;
                transform.eulerAngles = euler;
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Rigidbody2D ayarlarını yapılandır
        /// </summary>
        private void ConfigureRigidbody()
        {
            rb.gravityScale = 0f; // Su üzerinde, yerçekimi yok
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterDrag;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Rotation tamamen kilitli - Bot hiç dönmez
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        #endregion

        #region Movement & Rotation

        /// <summary>
        /// Bot hareketini kontrol et - X/Y eksenlerinde serbest hareket
        /// </summary>
        private void HandleMovement()
        {
            // X ve Y eksenlerinde doğrudan hareket (WASD)
            // W = yukarı, S = aşağı, A = sol, D = sağ
            if (moveInput.sqrMagnitude > 0.01f)
            {
                // Auto-unstuck detection - Check if boat is stuck
                stuckCheckTimer += Time.fixedDeltaTime;

                if (stuckCheckTimer >= stuckCheckInterval)
                {
                    float distanceMoved = Vector2.Distance(rb.position, lastStuckCheckPosition);

                    if (distanceMoved < stuckMovementThreshold)
                    {
                        // STUCK! Increment stuck counter
                        consecutiveStuckCount++;

                        // Kurtarma stratejisi - sıkışma sayısına göre artan kuvvet
                        Vector2 escapeDirection;
                        float escapePower;

                        if (consecutiveStuckCount >= maxStuckAttempts)
                        {
                            // AGRESIF MOD - En yakın Ground'dan uzaklaş
                            Vector2 escapeToWater = FindEscapeDirectionToWater();
                            escapeDirection = escapeToWater != Vector2.zero ? escapeToWater : moveInput.normalized;
                            escapePower = 5.0f; // ÇOK GÜÇLÜ itme

                            if (showDebugLogs)
                            {
                                Debug.LogWarning($"[BoatController] AGGRESSIVE UNSTUCK MODE! ({consecutiveStuckCount} attempts) Force: {escapePower}x");
                            }
                        }
                        else
                        {
                            // Normal mod - bastığın yöne GÜÇLÜ itme (sıkışmayı kır)
                            escapeDirection = moveInput.normalized;
                            escapePower = 3.5f + (consecutiveStuckCount * 0.5f); // Her seferinde biraz daha güçlü

                            if (showDebugLogs)
                            {
                                Debug.Log($"[BoatController] AUTO-UNSTUCK! Moved only {distanceMoved:F2} units. Attempt #{consecutiveStuckCount}, Force: {escapePower}x");
                            }
                        }

                        // Kurtarma itme kuvveti uygula
                        rb.linearVelocity = escapeDirection * moveSpeed * escapePower;

                        // Timer sıfırla ama pozisyonu bir sonraki check için sakla
                        stuckCheckTimer = 0f;
                        lastStuckCheckPosition = rb.position;
                        return; // Skip normal movement this frame
                    }
                    else
                    {
                        // Hareket ediyor, sıkışık değil - counter'ı sıfırla
                        consecutiveStuckCount = 0;
                    }

                    // Not stuck, update check position
                    lastStuckCheckPosition = rb.position;
                    stuckCheckTimer = 0f;
                }
            }
            else
            {
                // No input, reset stuck detection
                stuckCheckTimer = 0f;
                lastStuckCheckPosition = rb.position;
                consecutiveStuckCount = 0;
            }

            // WATER BOUNDARY KORUMASI - HER FRAME KONTROL ET
            if (isPlayerOnBoard && HappyHarvest.GameManager.Instance.Terrain != null)
            {
                var grid = HappyHarvest.GameManager.Instance.Terrain.Grid;
                var waterTilemap = HappyHarvest.GameManager.Instance.Terrain.WaterTilemap;

                if (grid != null && waterTilemap != null)
                {
                    Vector3Int currentCell = grid.WorldToCell(rb.position);

                    // Mevcut pozisyonda Water var mı?
                    bool currentHasWater = waterTilemap.HasTile(currentCell);

                    if (!currentHasWater)
                    {
                        // ZATEN Water dışındayız! En yakın Water'a TP et
                        Vector3Int? nearestWaterCell = FindNearestWaterCell(grid, waterTilemap, currentCell, 5);

                        if (nearestWaterCell.HasValue)
                        {
                            Vector2 waterPosition = grid.GetCellCenterWorld(nearestWaterCell.Value);
                            rb.position = waterPosition;
                            rb.linearVelocity = Vector2.zero;
                            currentVelocity = Vector2.zero;

                            if (showDebugLogs)
                            {
                                Debug.LogWarning($"[BoatController] OUTSIDE WATER! Teleporting to nearest water: {nearestWaterCell.Value}");
                            }
                        }
                        return;
                    }

                    // Velocity ile gideceğimiz yer kontrol et
                    if (rb.linearVelocity.magnitude > 0.01f)
                    {
                        Vector2 projectedPosition = rb.position + rb.linearVelocity * Time.fixedDeltaTime;
                        Vector3Int projectedCell = grid.WorldToCell(projectedPosition);

                        bool projectedHasWater = waterTilemap.HasTile(projectedCell);

                        if (!projectedHasWater)
                        {
                            // Sürüklenme Water dışına çıkaracak - DURDUR
                            rb.linearVelocity = Vector2.zero;
                            currentVelocity = Vector2.zero;

                            if (showDebugLogs)
                            {
                                Debug.Log($"[BoatController] Drift would exit water at {projectedCell}! Stopping.");
                            }
                            return;
                        }
                    }
                }
            }

            if (moveInput.sqrMagnitude > 0.01f)
            {
                // Hedef pozisyonu hesapla
                Vector2 targetPosition = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;

                // Ground kontrolü - Boat Ground'a girmesin (SADECE PLAYER BOTTA İKEN)
                if (isPlayerOnBoard && HappyHarvest.GameManager.Instance.Terrain != null)
                {
                    var grid = HappyHarvest.GameManager.Instance.Terrain.Grid;
                    var groundTilemap = HappyHarvest.GameManager.Instance.Terrain.GroundTilemap;
                    var waterTilemap = HappyHarvest.GameManager.Instance.Terrain.WaterTilemap;

                    if (grid != null && groundTilemap != null && waterTilemap != null)
                    {
                        Vector3Int targetCell = grid.WorldToCell(targetPosition);
                        Vector3Int currentCell = grid.WorldToCell(rb.position);

                        // WATER INPUT KONTROLÜ - Tuşla gidilmek istenen yerde Water var mı?
                        bool hasWaterAtTarget = waterTilemap.HasTile(targetCell);

                        if (!hasWaterAtTarget)
                        {
                            // Water yok! Bu yöne hareket kabul etme
                            if (showDebugLogs)
                            {
                                Debug.Log($"[BoatController] NO WATER at {targetCell}! Input blocked for this direction.");
                            }
                            return; // Sadece bu frame'in input'unu engelle
                        }

                        // 2. GROUND KONTROLÜ - Çoklu cell kontrolü - geminin etrafındaki 3x3 alan
                        bool hasGroundNearby = false;
                        Vector3Int closestGroundCell = currentCell;
                        float closestDistance = float.MaxValue;

                        // 3x3 grid kontrol et (gemi büyükse daha geniş alan kapsasın)
                        for (int x = -1; x <= 1; x++)
                        {
                            for (int y = -1; y <= 1; y++)
                            {
                                Vector3Int checkCell = currentCell + new Vector3Int(x, y, 0);

                                if (groundTilemap.HasTile(checkCell))
                                {
                                    hasGroundNearby = true;

                                    // En yakın Ground cell'i bul
                                    Vector2 cellCenter = grid.GetCellCenterWorld(checkCell);
                                    float distance = Vector2.Distance(rb.position, cellCenter);

                                    if (distance < closestDistance)
                                    {
                                        closestDistance = distance;
                                        closestGroundCell = checkCell;
                                    }
                                }
                            }
                        }

                        // Hedef cell'i de kontrol et
                        if (groundTilemap.HasTile(targetCell))
                        {
                            hasGroundNearby = true;
                            Vector2 targetCellCenter = grid.GetCellCenterWorld(targetCell);
                            float targetDistance = Vector2.Distance(rb.position, targetCellCenter);

                            if (targetDistance < closestDistance)
                            {
                                closestDistance = targetDistance;
                                closestGroundCell = targetCell;
                            }
                        }

                        if (hasGroundNearby)
                        {
                            // En yakın Ground'dan uzaklaşma yönü hesapla
                            Vector2 groundCenter = grid.GetCellCenterWorld(closestGroundCell);
                            Vector2 pushDirection = (rb.position - groundCenter).normalized;

                            // ÇOK GÜÇLÜ itme kuvveti uygula (Ground'a hiç girmesin)
                            // Mesafeye göre kuvvet artır - yakındaysa daha güçlü
                            float pushMultiplier = closestDistance < 0.5f ? 2.5f : 1.8f;
                            rb.linearVelocity = pushDirection * moveSpeed * pushMultiplier;

                            if (showDebugLogs)
                            {
                                Debug.Log($"[BoatController] Ground at {closestGroundCell} (dist: {closestDistance:F2}), pushing x{pushMultiplier}!");
                            }
                            return; // Hareketi engelle
                        }
                    }
                }

                // Dünya koordinat sisteminde hareket (rotation'dan bağımsız)
                Vector2 targetVelocity = moveInput * moveSpeed;

                // Daha hızlı acceleration - daha responsive (5f -> 8f)
                currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * 8f);
                rb.linearVelocity = currentVelocity;

                // Sprite flip - Hareket yönüne göre sağa/sola flip
                UpdateSpriteFlip(moveInput.x);

                if (showDebugLogs)
                {
                    Debug.Log($"[BoatController] Moving: X={moveInput.x:F2}, Y={moveInput.y:F2}, Velocity: {rb.linearVelocity.magnitude:F2}");
                }
            }
            else
            {
                ApplyDamping();
            }
        }

        /// <summary>
        /// Sprite'ı hareket yönüne göre flip et (sağa/sola)
        /// </summary>
        private void UpdateSpriteFlip(float horizontalInput)
        {
            if (boatSprite == null || Mathf.Abs(horizontalInput) < 0.01f)
                return;

            // Sağa gidiyorsa
            if (horizontalInput > 0)
            {
                boatSprite.flipX = !spriteDefaultFacingRight; // Varsayılan sağsa flip yok, solsa flip var
            }
            // Sola gidiyorsa
            else if (horizontalInput < 0)
            {
                boatSprite.flipX = spriteDefaultFacingRight; // Varsayılan sağsa flip var, solsa flip yok
            }
        }

        /// <summary>
        /// Bot dönüşünü kontrol et - DEVRE DIŞI (Bot rotasyon yapmaz)
        /// </summary>
        private void HandleRotation()
        {
            // Rotation tamamen devre dışı - Bot orijinal rotation'ını korur
            // Angular velocity'yi sıfırla
            rb.angularVelocity = 0f;
            // currentAngularVelocity = 0f;
        }

        /// <summary>
        /// Hareket damping uygula - Yavaşça dur
        /// </summary>
        private void ApplyDamping()
        {
            currentVelocity *= movementDamping;
            rb.linearVelocity = currentVelocity;

            if (rb.linearVelocity.magnitude < 0.01f)
            {
                rb.linearVelocity = Vector2.zero;
                currentVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// Dönüş damping uygula - Yavaşça dönmeyi bırak
        /// </summary>
        // private void ApplyRotationDamping()
        // {
        //     currentAngularVelocity *= rotationDamping;
        //     rb.angularVelocity = currentAngularVelocity;

        //     if (Mathf.Abs(rb.angularVelocity) < 0.1f)
        //     {
        //         rb.angularVelocity = 0f;
        //         currentAngularVelocity = 0f;
        //     }
        // }

        /// <summary>
        /// En yakın Water tile'ını bulur
        /// </summary>
        private Vector3Int? FindNearestWaterCell(Grid grid, UnityEngine.Tilemaps.Tilemap waterTilemap, Vector3Int startCell, int maxRadius)
        {
            // Spiral arama - en yakın Water'ı bul
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        // Sadece mevcut radius sınırındaki cell'lere bak
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                            continue;

                        Vector3Int checkCell = startCell + new Vector3Int(x, y, 0);

                        if (waterTilemap.HasTile(checkCell))
                        {
                            return checkCell;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// En yakın Ground'dan en uzak yönü bulur (suya kaçış yönü)
        /// </summary>
        private Vector2 FindEscapeDirectionToWater()
        {
            if (!isPlayerOnBoard || HappyHarvest.GameManager.Instance.Terrain == null)
                return Vector2.zero;

            var grid = HappyHarvest.GameManager.Instance.Terrain.Grid;
            var groundTilemap = HappyHarvest.GameManager.Instance.Terrain.GroundTilemap;

            if (grid == null || groundTilemap == null)
                return Vector2.zero;

            Vector3Int currentCell = grid.WorldToCell(rb.position);
            Vector2 totalPushDirection = Vector2.zero;
            int groundCount = 0;

            // 5x5 grid kontrol et - tüm yakındaki Ground'lardan uzaklaş
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    Vector3Int checkCell = currentCell + new Vector3Int(x, y, 0);

                    if (groundTilemap.HasTile(checkCell))
                    {
                        // Bu Ground'dan uzaklaşma yönü
                        Vector2 groundCenter = grid.GetCellCenterWorld(checkCell);
                        Vector2 pushDir = (rb.position - groundCenter).normalized;

                        // Mesafeye göre ağırlıklandır - yakındaki Ground'lar daha önemli
                        float distance = Vector2.Distance(rb.position, groundCenter);
                        float weight = 1f / Mathf.Max(distance, 0.1f);

                        totalPushDirection += pushDir * weight;
                        groundCount++;
                    }
                }
            }

            if (groundCount > 0)
            {
                // Normalize et - tüm Ground'lardan uzaklaşma yönü
                Vector2 escapeDirection = totalPushDirection.normalized;

                if (showDebugLogs)
                {
                    Debug.Log($"[BoatController] Escape direction calculated from {groundCount} nearby Ground tiles: {escapeDirection}");
                }

                return escapeDirection;
            }

            return Vector2.zero;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Hareket input'unu ayarla (BoatInteraction tarafından çağrılır)
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            moveInput = input;
        }

        /// <summary>
        /// Dönüş input'unu ayarla (BoatInteraction tarafından çağrılır)
        /// </summary>
        public void SetRotationInput(float input)
        {
            rotationInput = input;
        }

        /// <summary>
        /// Player bota bindi
        /// </summary>
        public void PlayerBoarded()
        {
            isPlayerOnBoard = true;
            Log("Player boarded the boat");
        }

        /// <summary>
        /// Player bottan indi
        /// </summary>
        public void PlayerDisembarked()
        {
            isPlayerOnBoard = false;
            moveInput = Vector2.zero;
            rotationInput = 0f;
            Log("Player left the boat");
        }

        /// <summary>
        /// Bot'un mevcut hızını al
        /// </summary>
        public float GetCurrentSpeed()
        {
            return rb.linearVelocity.magnitude;
        }

        /// <summary>
        /// Bot'un baktığı yönü al
        /// </summary>
        public Vector2 GetForwardDirection()
        {
            return transform.up;
        }

        #endregion

        #region Debug

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[BoatController] {message}");
            }
        }

        private void OnDrawGizmos()
        {
            if (!isPlayerOnBoard) return;

            // Botun baktığı yön
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.up * 2f);

            // Hareket yönü
            if (rb != null && rb.linearVelocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 2f);
            }
        }

        #endregion
    }
}
