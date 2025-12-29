using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace YusufTest
{
    /// <summary>
    /// Bota binme/inme sistemi ve bot kontrolü yöneticisi
    /// - Player E tuşu ile bota biner/iner
    /// - Botta iken WASD ile bot kontrolü
    /// - Player'ı bot pozisyonuna bağlar
    /// </summary>
    [RequireComponent(typeof(BoatController))]
    public class BoatInteraction : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Interaction Settings")]
        [Tooltip("Trigger kullan (true) veya mesafe kontrolü (false). Trigger kullanırsanız Circle Collider'ı 'Is Trigger' yapın!")]
        [SerializeField] private bool useTrigger = true; // TRUE - Circle Collider trigger sistemi kullan

        [Tooltip("Bota binmek için gereken mesafe (grid cell cinsinden - SADECE useTrigger=false ise kullanılır)")]
        [SerializeField] private float interactionRange = 3f; // Grid cell cinsinden - 3 cell mesafe

        [Tooltip("Binme tuşu (varsayılan: F)")]
        [SerializeField] private Key boardKey = Key.F;

        [Header("Player Positioning")]
        [Tooltip("Player bot üzerinde nerede duracak (local position)")]
        [SerializeField] private Vector3 playerPositionOnBoat = new Vector3(0f, 0f, 0f);

        [Tooltip("Player bottan inerken ne kadar uzakta spawn olacak (en yakın Ground araması için max mesafe)")]
        [SerializeField] private float disembarkDistance = 10f;

        [Header("Visual Feedback")]
        [Tooltip("Etkileşim göstergesi (opsiyonel - 3D dünyada GameObject)")]
        [SerializeField] private GameObject interactionPrompt;

        [Tooltip("Etkileşim göstergesinin offset'i")]
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.5f, 0f);

        [Header("UI Text Prompt")]
        [Tooltip("'Press E to Enter Boat' yazısını göster")]
        [SerializeField] private bool showTextPrompt = true;

        [Tooltip("Text prompt için Canvas (yoksa otomatik oluşturulur)")]
        [SerializeField] private Canvas promptCanvas;

        [Tooltip("Prompt text'in rengi")]
        [SerializeField] private Color promptTextColor = Color.white;

        [Tooltip("Prompt text'in boyutu")]
        [SerializeField] private int promptTextSize = 16;

        [Header("References")]
        [Tooltip("Player GameObject tag'i")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Player GameObject ismi (tag yoksa kullanılır)")]
        [SerializeField] private string playerName = "Character";

        [Header("Camera Settings")]
        [Tooltip("Gemideyken kamera orthographic size (ne kadar büyük o kadar uzak)")]
        [SerializeField] private float boatCameraSize = 25f;

        [Tooltip("Kamera zoom geçiş süresi (saniye)")]
        [SerializeField] private float cameraTransitionDuration = 1.5f;

        [Header("Fade Settings")]
        [Tooltip("Ekran kararması efekti aktif olsun mu?")]
        [SerializeField] private bool useFadeEffect = true;

        [Tooltip("Fade in/out süresi (saniye)")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Tooltip("Fade rengi (genelde siyah)")]
        [SerializeField] private Color fadeColor = Color.black;

        [Header("Repair System")]
        [Tooltip("Repair cost in gold")]
        [SerializeField] private int repairCost = 10;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showGizmos = true;

        #endregion

        #region Private Fields

        private BoatController boatController;
        private Transform playerTransform;
        private Rigidbody2D playerRigidbody;
        private bool isPlayerOnBoard = false;
        private bool isPlayerInRange = false;

        // Player'ın orijinal parent'ı (indiğinde geri koymak için)
        private Transform originalPlayerParent;

        // Player'ın orijinal control durumu
        private HappyHarvest.PlayerController playerController;
        private bool playerOriginalControlState;

        // Input System
        private Keyboard keyboard;

        // UI Elements
        private UnityEngine.UI.Text promptText;
        private GameObject promptTextObject;

        // Player visibility
        private SpriteRenderer playerSpriteRenderer;
        private SpriteRenderer[] allPlayerSpriteRenderers; // Tüm sprite renderer'lar

        // Camera zoom
        private Camera mainCamera;
        private CinemachineCamera cinemachineCamera;
        private float originalCameraSize;
        private bool useCinemachine;

        // Fade effect
        private Canvas fadeCanvas;
        private UnityEngine.UI.Image fadeImage;
        private Coroutine currentFadeCoroutine;
        private Coroutine currentCameraZoomCoroutine;

        // Repair system state
        private bool isRepaired = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            boatController = GetComponent<BoatController>();
            keyboard = Keyboard.current;

            // Debug: Boat setup kontrolü
            Log("=== BOAT INTERACTION AWAKE ===");
            Log($"Boat Controller: {(boatController != null ? "OK" : "MISSING!")}");
            Log($"Keyboard: {(keyboard != null ? "OK" : "MISSING!")}");
        }

        private void Start()
        {
            Log("=== BOAT INTERACTION START ===");

            FindPlayer();

            // Kamera referansını al - Önce Cinemachine, sonra normal Camera
            cinemachineCamera = FindObjectOfType<CinemachineCamera>();

            if (cinemachineCamera != null)
            {
                useCinemachine = true;
                originalCameraSize = cinemachineCamera.Lens.OrthographicSize;
                Log($"✅ Cinemachine Camera bulundu! Original size: {originalCameraSize}");
            }
            else
            {
                // Cinemachine yoksa normal Camera kullan
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                    Log("Camera.main null, FindObjectOfType ile arandı");
                }

                if (mainCamera != null)
                {
                    useCinemachine = false;
                    originalCameraSize = mainCamera.orthographicSize;
                    Log($"✅ Normal Camera bulundu! Original size: {originalCameraSize}");
                }
                else
                {
                    LogError("❌ Hiçbir kamera bulunamadı!");
                }
            }

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }

            // UI Text prompt oluştur
            if (showTextPrompt)
            {
                Log("Creating text prompt...");
                CreateTextPrompt();
            }

            // Fade Canvas oluştur
            if (useFadeEffect)
            {
                Log("Creating fade canvas...");
                CreateFadeCanvas();
            }

            // Trigger kullanılıyorsa Collider2D'nin trigger olduğundan emin ol
            if (useTrigger)
            {
                Collider2D col = GetComponent<Collider2D>();
                if (col == null)
                {
                    LogError("❌ COLLIDER2D YOK! Boat'a Collider2D ekleyin ve Is Trigger = true yapın!");
                }
                else
                {
                    Log($"Collider2D bulundu: {col.GetType().Name}");
                    if (!col.isTrigger)
                    {
                        Log("⚠️ Collider trigger değildi, trigger yapılıyor...");
                        col.isTrigger = true;
                    }
                    else
                    {
                        Log("✅ Collider zaten trigger");
                    }
                }
            }

            Log($"Use Trigger: {useTrigger}");
            Log($"Show Text Prompt: {showTextPrompt}");
            Log($"Player Tag: '{playerTag}'");
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            // HER ZAMAN mesafe kontrolü yap (trigger sistemi çalışmıyor)
            CheckPlayerProximity();

            HandleInteractionInput();
            UpdateInteractionPrompt();
            UpdateTextPrompt();

            if (isPlayerOnBoard)
            {
                HandleBoatControl();
                UpdatePlayerPosition(); // Player'ı bot ile birlikte hareket ettir
            }
        }

        private void LateUpdate()
        {
            // Player'ın pozisyonunu bot ile senkronize tut
            if (isPlayerOnBoard && playerTransform != null)
            {
                playerTransform.position = transform.TransformPoint(playerPositionOnBoat);
            }
        }

        /// <summary>
        /// Trigger'a giriş - Player bota yaklaştı
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            Log($"🔔 Trigger Enter: {other.gameObject.name} (Tag: '{other.tag}')");

            if (!useTrigger)
            {
                Log("  -> useTrigger=false, atlanıyor");
                return;
            }

            // Tag veya isim ile kontrol et
            bool isPlayer = other.CompareTag(playerTag) || other.gameObject.name == playerName;

            if (isPlayer)
            {
                isPlayerInRange = true;
                Log($"✅ PLAYER TRIGGER'A GİRDİ! isPlayerInRange = true");
            }
            else
            {
                Log($"  -> Eşleşmedi. Aranan Tag: '{playerTag}' veya İsim: '{playerName}'");
                Log($"  -> Bulunan Tag: '{other.tag}', İsim: '{other.gameObject.name}'");
            }
        }

        /// <summary>
        /// Trigger'dan çıkış - Player bottan uzaklaştı
        /// </summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!useTrigger) return;

            // Tag veya isim ile kontrol et
            bool isPlayer = other.CompareTag(playerTag) || other.gameObject.name == playerName;

            if (isPlayer)
            {
                isPlayerInRange = false;
                Log("Player left boat trigger zone");
            }
        }

        #endregion

        #region Player Detection

        /// <summary>
        /// Player'ı bul - Önce tag ile, sonra isim ile arar
        /// </summary>
        private void FindPlayer()
        {
            GameObject playerObj = null;

            // Önce tag ile ara
            try
            {
                playerObj = GameObject.FindGameObjectWithTag(playerTag);
            }
            catch
            {
                Log($"Tag '{playerTag}' bulunamadı, isim ile aranacak...");
            }

            // Tag ile bulamadıysa isim ile ara
            if (playerObj == null && !string.IsNullOrEmpty(playerName))
            {
                playerObj = GameObject.Find(playerName);
                if (playerObj != null)
                {
                    Log($"Player isim ile bulundu: '{playerName}'");
                }
            }

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerRigidbody = playerObj.GetComponent<Rigidbody2D>();
                playerController = playerObj.GetComponent<HappyHarvest.PlayerController>();
                playerSpriteRenderer = playerObj.GetComponentInChildren<SpriteRenderer>();
                allPlayerSpriteRenderers = playerObj.GetComponentsInChildren<SpriteRenderer>();

                Log($"✅ Player bulundu: {playerObj.name}");
                Log($"  - Transform: {(playerTransform != null ? "OK" : "NULL")}");
                Log($"  - Rigidbody2D: {(playerRigidbody != null ? "OK" : "⚠️ MISSING - Trigger çalışmaz!")}");
                Log($"  - PlayerController: {(playerController != null ? "OK" : "⚠️ MISSING")}");
                Log($"  - SpriteRenderer: {(playerSpriteRenderer != null ? "OK" : "⚠️ MISSING")}");
                Log($"  - Total SpriteRenderers found: {allPlayerSpriteRenderers.Length}");
                Log($"  - Tag: '{playerObj.tag}'");
                Log($"  - Name: '{playerObj.name}'");
            }
            else
            {
                LogError($"❌ PLAYER BULUNAMADI!");
                LogError($"  Tag '{playerTag}' ile bulunamadı");
                LogError($"  İsim '{playerName}' ile bulunamadı");
                LogError("Player GameObject'ine 'Player' tag'ini ekleyin veya ismini kontrol edin!");
            }
        }

        /// <summary>
        /// Player'ın bot ile olan mesafesini kontrol et - HER FRAME
        /// </summary>
        private void CheckPlayerProximity()
        {
            if (isPlayerOnBoard)
            {
                isPlayerInRange = false;
                return;
            }

            if (playerTransform == null)
            {
                isPlayerInRange = false;
                return;
            }

            // Grid cell size'a göre mesafe hesapla (daha tutarlı)
            float actualInteractionDistance = interactionRange;

            // Eğer terrain varsa, grid cell size ile çarp
            if (HappyHarvest.GameManager.Instance.Terrain != null &&
                HappyHarvest.GameManager.Instance.Terrain.Grid != null)
            {
                float cellSize = HappyHarvest.GameManager.Instance.Terrain.Grid.cellSize.x;
                actualInteractionDistance = interactionRange * cellSize;
            }

            float distance = Vector2.Distance(playerTransform.position, transform.position);
            bool wasInRange = isPlayerInRange;
            isPlayerInRange = distance <= actualInteractionDistance;

            // Debug: Mesafe değişikliğini logla
            if (wasInRange != isPlayerInRange)
            {
                if (isPlayerInRange)
                {
                    Log($"✅ Player menzile GİRDİ! Mesafe: {distance:F2} (Max: {actualInteractionDistance:F2})");
                }
                else
                {
                    Log($"❌ Player menzilden ÇIKTI! Mesafe: {distance:F2} (Max: {actualInteractionDistance:F2})");
                }
            }

            // Debug: Her 60 frame'de bir mesafe logla
            if (showDebugLogs && Time.frameCount % 120 == 0)
            {
                Log($"[Distance Check] Mesafe: {distance:F2} / {interactionRange} - InRange: {isPlayerInRange}");
            }
        }

        #endregion

        #region Interaction

        /// <summary>
        /// Binme/inme input kontrolü
        /// </summary>
        private void HandleInteractionInput()
        {
            if (keyboard == null)
            {
                // Keyboard yoksa yeniden dene
                keyboard = Keyboard.current;
                if (keyboard == null)
                {
                    LogError("⚠️ Keyboard.current NULL! Input System çalışmıyor olabilir.");
                    return;
                }
            }

            if (keyboard[boardKey].wasPressedThisFrame)
            {
                Log($"🎮 {boardKey} key pressed!");
                Log($"  - isPlayerOnBoard: {isPlayerOnBoard}");
                Log($"  - isPlayerInRange: {isPlayerInRange}");
                Log($"  - isRepaired: {isRepaired}");

                if (isPlayerOnBoard)
                {
                    Log("-> Disembarking boat...");
                    DisembarkBoat();
                }
                else if (isPlayerInRange)
                {
                    // Repair system check
                    if (!isRepaired)
                    {
                        Log("-> Boat not repaired, attempting repair...");
                        TryRepairBoat();
                    }
                    else
                    {
                        Log("-> Boarding boat...");
                        BoardBoat();
                    }
                }
                else
                {
                    Log("-> Player not in range, no action taken");
                }
            }
        }

        /// <summary>
        /// Try to repair the boat
        /// </summary>
        private void TryRepairBoat()
        {
            if (playerController == null)
            {
                LogError("PlayerController not found!");
                return;
            }

            if (playerController.Coins >= repairCost)
            {
                playerController.Coins -= repairCost;
                isRepaired = true;
                Log($"✅ Boat repaired! Paid {repairCost} gold.");

                // After repair, board automatically
                BoardBoat();
            }
            else
            {
                LogError($"❌ Not enough gold! Repair costs {repairCost} gold. (Current: {playerController.Coins})");
            }
        }

        /// <summary>
        /// Board the boat
        /// </summary>
        private void BoardBoat()
        {
            if (playerTransform == null) return;

            Log("Player is boarding the boat...");

            // Start coroutine
            StartCoroutine(BoardBoatCoroutine());
        }

        /// <summary>
        /// Bota binme animasyonu (fade + kamera geçişi)
        /// </summary>
        private System.Collections.IEnumerator BoardBoatCoroutine()
        {
            // 0. BİNMEDEN ÖNCE - Hem player hem boat yakınında Ground var mı kontrol et
            Vector3? playerGroundCheck = FindNearestGroundPosition(playerTransform.position, disembarkDistance);
            Vector3? boatGroundCheck = FindNearestGroundPosition(transform.position, disembarkDistance);

            if (!playerGroundCheck.HasValue)
            {
                LogError("❌ Player yakınında Ground yok, gemiye binilemez!");
                yield break; // Binmeyi iptal et
            }

            if (!boatGroundCheck.HasValue)
            {
                LogError("❌ Gemi yakınında Ground yok, gemiye binilemez (inebilmek için Ground gerekli)!");
                yield break; // Binmeyi iptal et
            }

            Log($"✅ Binilebilir: Player Ground={playerGroundCheck.Value}, Boat Ground={boatGroundCheck.Value}");

            // 1. Fade to black (ekranı karart)
            if (useFadeEffect && fadeImage != null)
            {
                Log("Fading to black...");
                yield return StartCoroutine(FadeToBlack());
            }

            // 2. Player'ın orijinal durumunu kaydet
            originalPlayerParent = playerTransform.parent;

            // 3. Player kontrolünü devre dışı bırak
            if (playerController != null)
            {
                playerController.ToggleControl(false);
            }

            // 4. Player rigidbody'sini kinematic yap
            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector2.zero;
                playerRigidbody.isKinematic = true;
            }

            // 5. Player'ı bot üzerine yerleştir
            playerTransform.position = transform.TransformPoint(playerPositionOnBoat);

            // 6. Player'ın TÜM sprite'larını gizle
            if (allPlayerSpriteRenderers != null && allPlayerSpriteRenderers.Length > 0)
            {
                foreach (var sprite in allPlayerSpriteRenderers)
                {
                    sprite.enabled = false;
                }
                Log($"Player'ın {allPlayerSpriteRenderers.Length} sprite'ı gizlendi");
            }
            else if (playerSpriteRenderer != null)
            {
                playerSpriteRenderer.enabled = false;
                Log("Player sprite gizlendi (fallback)");
            }

            // 7. Kamerayı smooth olarak UZAKLAŞTIR
            if (useCinemachine ? cinemachineCamera != null : mainCamera != null)
            {
                Log($"🎥 Kamera uzaklaştırılıyor... ({originalCameraSize} -> {boatCameraSize})");
                if (currentCameraZoomCoroutine != null)
                {
                    StopCoroutine(currentCameraZoomCoroutine);
                }
                currentCameraZoomCoroutine = StartCoroutine(SmoothCameraZoom(originalCameraSize, boatCameraSize, cameraTransitionDuration));
            }
            else
            {
                LogError("❌ Kamera bulunamadı, zoom yapılamıyor!");
            }

            // 8. Boat controller'a bildir
            boatController.PlayerBoarded();
            isPlayerOnBoard = true;

            // 9. Fade from black (ekranı aç)
            if (useFadeEffect && fadeImage != null)
            {
                Log("Fading from black...");
                yield return StartCoroutine(FadeFromBlack());
            }

            Log("Player boarded successfully!");
        }

        /// <summary>
        /// Bottan in
        /// </summary>
        private void DisembarkBoat()
        {
            if (playerTransform == null) return;

            Log("Player is leaving the boat...");

            // Coroutine başlat
            StartCoroutine(DisembarkBoatCoroutine());
        }

        /// <summary>
        /// Bottan inme animasyonu (fade + kamera geçişi)
        /// </summary>
        private System.Collections.IEnumerator DisembarkBoatCoroutine()
        {
            // 1. En yakın Ground tile'ını kontrol et - İNMEDEN ÖNCE!
            Vector3? disembarkPositionNullable = FindNearestGroundPosition(transform.position, disembarkDistance);

            if (!disembarkPositionNullable.HasValue)
            {
                LogError("❌ Yakında Ground yok, gemiden inilemez!");
                yield break; // İnmeyi iptal et
            }

            Vector3 disembarkPosition = disembarkPositionNullable.Value;
            Log($"✅ İnilebilir Ground bulundu: {disembarkPosition}");

            // 2. Fade to black (ekranı karart)
            if (useFadeEffect && fadeImage != null)
            {
                Log("Fading to black...");
                yield return StartCoroutine(FadeToBlack());
            }

            // 3. Player'ı pozisyonla
            playerTransform.position = disembarkPosition;

            // 4. Player kontrolünü geri ver
            if (playerController != null)
            {
                playerController.ToggleControl(true);
            }

            // 5. Player rigidbody'sini dinamik yap
            if (playerRigidbody != null)
            {
                playerRigidbody.isKinematic = false;
                playerRigidbody.linearVelocity = Vector2.zero;
            }

            // 6. Player'ın TÜM sprite'larını tekrar göster
            if (allPlayerSpriteRenderers != null && allPlayerSpriteRenderers.Length > 0)
            {
                foreach (var sprite in allPlayerSpriteRenderers)
                {
                    sprite.enabled = true;
                }
                Log($"Player'ın {allPlayerSpriteRenderers.Length} sprite'ı gösterildi");
            }
            else if (playerSpriteRenderer != null)
            {
                playerSpriteRenderer.enabled = true;
                Log("Player sprite gösterildi (fallback)");
            }

            // 7. Kamerayı smooth olarak YAKLAŞTIR (orijinal boyuta döndür)
            if (useCinemachine ? cinemachineCamera != null : mainCamera != null)
            {
                Log($"🎥 Kamera yakınlaştırılıyor... ({boatCameraSize} -> {originalCameraSize})");
                if (currentCameraZoomCoroutine != null)
                {
                    StopCoroutine(currentCameraZoomCoroutine);
                }
                currentCameraZoomCoroutine = StartCoroutine(SmoothCameraZoom(boatCameraSize, originalCameraSize, cameraTransitionDuration));
            }
            else
            {
                LogError("❌ Kamera bulunamadı, zoom yapılamıyor!");
            }

            // 8. Boat controller'a bildir
            boatController.PlayerDisembarked();
            isPlayerOnBoard = false;

            // 9. Fade from black (ekranı aç)
            if (useFadeEffect && fadeImage != null)
            {
                Log("Fading from black...");
                yield return StartCoroutine(FadeFromBlack());
            }

            Log("Player disembarked successfully!");
        }

        #endregion

        #region Boat Control

        /// <summary>
        /// Botta iken bot kontrolünü yönet - X/Y ekseninde hareket
        /// </summary>
        private void HandleBoatControl()
        {
            if (keyboard == null) return;

            // WASD input al - X ve Y eksenlerinde
            Vector2 moveInput = Vector2.zero;

            // Yukarı/aşağı (W/S)
            if (keyboard[Key.W].isPressed)
            {
                moveInput.y = 1f;
            }
            else if (keyboard[Key.S].isPressed)
            {
                moveInput.y = -1f;
            }

            // Sağa/sola hareket (A/D)
            if (keyboard[Key.A].isPressed)
            {
                moveInput.x = -1f; // Sol
            }
            else if (keyboard[Key.D].isPressed)
            {
                moveInput.x = 1f; // Sağ
            }

            // Boat controller'a input gönder (rotation artık kullanılmıyor)
            boatController.SetMoveInput(moveInput);
            boatController.SetRotationInput(0f); // Rotasyon her zaman 0
        }

        /// <summary>
        /// Player'ın pozisyonunu bot ile senkronize et
        /// </summary>
        private void UpdatePlayerPosition()
        {
            if (playerTransform != null)
            {
                // Player'ı botun üzerinde tut
                playerTransform.position = transform.TransformPoint(playerPositionOnBoat);
            }
        }

        #endregion

        #region Ground Finding

        /// <summary>
        /// En yakın Ground tile pozisyonunu bulur
        /// </summary>
        /// <returns>Ground pozisyonu, bulunamazsa null</returns>
        private Vector3? FindNearestGroundPosition(Vector3 startPosition, float maxSearchDistance)
        {
            if (HappyHarvest.GameManager.Instance.Terrain == null)
            {
                LogError("Terrain null!");
                return null;
            }

            var grid = HappyHarvest.GameManager.Instance.Terrain.Grid;
            var groundTilemap = HappyHarvest.GameManager.Instance.Terrain.GroundTilemap;

            if (grid == null || groundTilemap == null)
            {
                LogError("Grid veya GroundTilemap null!");
                return null;
            }

            // Başlangıç cell'i
            Vector3Int startCell = grid.WorldToCell(startPosition);

            // Spiral arama - merkez noktadan başlayıp giderek genişleyen daireler çiz
            int maxRadius = Mathf.CeilToInt(maxSearchDistance / grid.cellSize.x);

            for (int radius = 1; radius <= maxRadius; radius++)
            {
                // Her radius için etrafındaki cell'leri kontrol et
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        // Sadece mevcut radius sınırındaki cell'lere bak
                        if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                            continue;

                        Vector3Int checkCell = startCell + new Vector3Int(x, y, 0);

                        // Bu cell'de Ground tile var mı?
                        if (groundTilemap.HasTile(checkCell))
                        {
                            // Bulundu! Dünya pozisyonunu döndür
                            Vector3 groundPosition = grid.GetCellCenterWorld(checkCell);
                            Log($"✅ En yakın Ground bulundu: {checkCell}, Dünya pozisyonu: {groundPosition}");
                            return groundPosition;
                        }
                    }
                }
            }

            // Hiç Ground bulunamadı
            LogError($"❌ {maxSearchDistance} birim içinde Ground bulunamadı!");
            return null;
        }

        #endregion

        #region Fade & Camera Effects

        /// <summary>
        /// Fade Canvas oluştur - Ekran kararması için
        /// </summary>
        private void CreateFadeCanvas()
        {
            // Canvas oluştur
            GameObject canvasObj = new GameObject("BoatFadeCanvas");
            fadeCanvas = canvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999; // En üstte olsun

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Fade Image oluştur (tam ekran siyah)
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(fadeCanvas.transform);

            fadeImage = imageObj.AddComponent<UnityEngine.UI.Image>();
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // Başlangıçta görünmez
            fadeImage.raycastTarget = false; // ÖNEMLİ: Mouse tıklamalarını engellemez

            // RectTransform - Tam ekran
            RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            Log("Fade canvas created successfully");
        }

        /// <summary>
        /// Ekranı karart (Fade to black)
        /// </summary>
        private System.Collections.IEnumerator FadeToBlack()
        {
            if (fadeImage == null)
            {
                LogError("Fade image null!");
                yield break;
            }

            float elapsed = 0f;
            Color startColor = fadeImage.color;
            Color targetColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                fadeImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            fadeImage.color = targetColor;
        }

        /// <summary>
        /// Ekranı aç (Fade from black)
        /// </summary>
        private System.Collections.IEnumerator FadeFromBlack()
        {
            if (fadeImage == null)
            {
                LogError("Fade image null!");
                yield break;
            }

            float elapsed = 0f;
            Color startColor = fadeImage.color;
            Color targetColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                fadeImage.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            fadeImage.color = targetColor;
        }

        /// <summary>
        /// Kamerayı smooth olarak zoom yap
        /// </summary>
        private System.Collections.IEnumerator SmoothCameraZoom(float fromSize, float toSize, float duration)
        {
            if (useCinemachine && cinemachineCamera == null)
            {
                LogError("Cinemachine camera null!");
                yield break;
            }
            else if (!useCinemachine && mainCamera == null)
            {
                LogError("Main camera null!");
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Smooth easing (ease in-out)
                t = t * t * (3f - 2f * t);

                float newSize = Mathf.Lerp(fromSize, toSize, t);

                if (useCinemachine)
                {
                    // Cinemachine için Lens.OrthographicSize değiştir
                    var lens = cinemachineCamera.Lens;
                    lens.OrthographicSize = newSize;
                    cinemachineCamera.Lens = lens;
                }
                else
                {
                    // Normal kamera için orthographicSize değiştir
                    mainCamera.orthographicSize = newSize;
                }

                yield return null;
            }

            // Final değeri ayarla
            if (useCinemachine)
            {
                var lens = cinemachineCamera.Lens;
                lens.OrthographicSize = toSize;
                cinemachineCamera.Lens = lens;
            }
            else
            {
                mainCamera.orthographicSize = toSize;
            }

            Log($"Camera zoom completed: {toSize} (Cinemachine: {useCinemachine})");
        }

        #endregion

        #region Visual Feedback

        /// <summary>
        /// UI Text prompt oluştur
        /// </summary>
        private void CreateTextPrompt()
        {
            // Canvas bul veya oluştur
            if (promptCanvas == null)
            {
                promptCanvas = FindObjectOfType<Canvas>();

                if (promptCanvas == null)
                {
                    GameObject canvasObj = new GameObject("BoatPromptCanvas");
                    promptCanvas = canvasObj.AddComponent<Canvas>();
                    promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                    canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
            }

            // Text GameObject oluştur
            promptTextObject = new GameObject("BoatPromptText");
            promptTextObject.transform.SetParent(promptCanvas.transform);

            // Text component ekle
            promptText = promptTextObject.AddComponent<UnityEngine.UI.Text>();
            promptText.text = "Press F to Enter Boat";
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = promptTextSize;
            promptText.color = promptTextColor;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.raycastTarget = false; // Mouse tıklamalarını engellemez

            // RectTransform ayarları - Ekranın üst ortasında
            RectTransform rectTransform = promptTextObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(400f, 50f);
            rectTransform.anchoredPosition = Vector2.zero;

            // Başlangıçta gizle
            promptTextObject.SetActive(false);

            Log("Text prompt created successfully");
        }

        /// <summary>
        /// Text prompt'u güncelle
        /// </summary>
        private void UpdateTextPrompt()
        {
            if (!showTextPrompt || promptText == null) return;

            // Show prompt if player is in range and not on board
            if (isPlayerInRange && !isPlayerOnBoard)
            {
                promptTextObject.SetActive(true);

                // Show message based on repair state
                if (!isRepaired)
                {
                    promptText.text = $"Press F to Repair Boat ({repairCost} Gold)";
                }
                else
                {
                    promptText.text = "Press F to Enter Boat";
                }
            }
            // Player bottaysa - SADECE yakında Ground varsa "Exit" göster
            else if (isPlayerOnBoard)
            {
                // Yakında Ground var mı kontrol et
                Vector3? nearestGround = FindNearestGroundPosition(transform.position, disembarkDistance);

                if (nearestGround.HasValue)
                {
                    promptTextObject.SetActive(true);
                    promptText.text = "Press F to Exit Boat";
                }
                else
                {
                    // Yakında Ground yok, inme mümkün değil
                    promptTextObject.SetActive(false);
                }
            }
            else
            {
                promptTextObject.SetActive(false);
            }
        }

        /// <summary>
        /// Etkileşim göstergesini güncelle
        /// </summary>
        private void UpdateInteractionPrompt()
        {
            if (interactionPrompt == null) return;

            if (isPlayerInRange && !isPlayerOnBoard)
            {
                interactionPrompt.SetActive(true);
                interactionPrompt.transform.position = transform.position + promptOffset;
            }
            else
            {
                interactionPrompt.SetActive(false);
            }
        }

        #endregion

        #region Debug & Gizmos

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[BoatInteraction] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[BoatInteraction] {message}");
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            // Interaction range
            Gizmos.color = isPlayerInRange ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            // Player position on boat
            if (isPlayerOnBoard)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.TransformPoint(playerPositionOnBoat), 0.3f);
            }

            // Disembark search area - En yakın Ground arama alanı
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Yeşil yarı saydam
            Gizmos.DrawWireSphere(transform.position, disembarkDistance);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            // Player position on boat (local)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.TransformPoint(playerPositionOnBoat));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Player botta mı?
        /// </summary>
        public bool IsPlayerOnBoard()
        {
            return isPlayerOnBoard;
        }

        /// <summary>
        /// Player yakın mı?
        /// </summary>
        public bool IsPlayerInRange()
        {
            return isPlayerInRange;
        }

        #endregion
    }
}
