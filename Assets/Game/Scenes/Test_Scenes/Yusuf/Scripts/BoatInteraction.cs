using UnityEngine;
using UnityEngine.InputSystem;

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
        [Tooltip("Trigger kullan (true) veya mesafe kontrolü (false)")]
        [SerializeField] private bool useTrigger = false; // FALSE - Mesafe kontrolü kullan

        [Tooltip("Bota binmek için gereken mesafe")]
        [SerializeField] private float interactionRange = 17f; // 8.5 * 2 = 17

        [Tooltip("Binme tuşu (varsayılan: F)")]
        [SerializeField] private Key boardKey = Key.F;

        [Header("Player Positioning")]
        [Tooltip("Player bot üzerinde nerede duracak (local position)")]
        [SerializeField] private Vector3 playerPositionOnBoat = new Vector3(0f, 0f, 0f);

        [Tooltip("Player bottan inerken ne kadar uzakta spawn olacak")]
        [SerializeField] private float disembarkDistance = 2f;

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
        [SerializeField] private float boatCameraSize = 15f;

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
        private float originalCameraSize;

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

            // Kamera referansını al - Hem Camera.main hem FindObjectOfType dene
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
                Log("Camera.main null, FindObjectOfType ile arandı");
            }

            if (mainCamera != null)
            {
                originalCameraSize = mainCamera.orthographicSize;
                Log($"✅ Camera bulundu! Original size: {originalCameraSize}");
            }
            else
            {
                LogError("❌ Main camera bulunamadı!");
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

            float distance = Vector2.Distance(playerTransform.position, transform.position);
            bool wasInRange = isPlayerInRange;
            isPlayerInRange = distance <= interactionRange;

            // Debug: Mesafe değişikliğini logla
            if (wasInRange != isPlayerInRange)
            {
                if (isPlayerInRange)
                {
                    Log($"✅ Player menzile GİRDİ! Mesafe: {distance:F2} (Max: {interactionRange})");
                }
                else
                {
                    Log($"❌ Player menzilden ÇIKTI! Mesafe: {distance:F2} (Max: {interactionRange})");
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
                Log($"🎮 {boardKey} tuşuna basıldı!");
                Log($"  - isPlayerOnBoard: {isPlayerOnBoard}");
                Log($"  - isPlayerInRange: {isPlayerInRange}");

                if (isPlayerOnBoard)
                {
                    Log("-> Bottan iniliyor...");
                    DisembarkBoat();
                }
                else if (isPlayerInRange)
                {
                    Log("-> Bota biniliyor...");
                    BoardBoat();
                }
                else
                {
                    Log("-> Player yakın değil, hiçbir şey yapılmadı");
                }
            }
        }

        /// <summary>
        /// Bota bin
        /// </summary>
        private void BoardBoat()
        {
            if (playerTransform == null) return;

            Log("Player is boarding the boat...");

            // Player'ın orijinal durumunu kaydet
            originalPlayerParent = playerTransform.parent;

            // Player kontrolünü devre dışı bırak
            if (playerController != null)
            {
                playerController.ToggleControl(false);
            }

            // Player rigidbody'sini kinematic yap
            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector2.zero;
                playerRigidbody.isKinematic = true;
            }

            // Player'ı bot üzerine yerleştir (Parent yapmıyoruz, manual olarak takip edeceğiz)
            playerTransform.position = transform.TransformPoint(playerPositionOnBoat);

            // Player'ın TÜM sprite'larını gizle
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
            else
            {
                LogError("⚠️ Hiçbir sprite renderer bulunamadı!");
            }

            // Kamerayı UZAKLAŞTIR - Orthographic size'ı artır
            if (mainCamera != null)
            {
                mainCamera.orthographicSize = boatCameraSize;
                Log($"🎥 KAMERA UZAKLAŞTIRILDI!");
                Log($"  - Eski Size: {originalCameraSize}");
                Log($"  - Yeni Size: {boatCameraSize}");
                Log($"  - Gerçek Size: {mainCamera.orthographicSize}");
            }
            else
            {
                LogError("❌ Kamera null!");
            }

            // Boat controller'a bildir
            boatController.PlayerBoarded();

            isPlayerOnBoard = true;

            Log("Player boarded successfully!");
        }

        /// <summary>
        /// Bottan in
        /// </summary>
        private void DisembarkBoat()
        {
            if (playerTransform == null) return;

            Log("Player is leaving the boat...");

            // İniş pozisyonunu hesapla (botun sağ tarafı)
            Vector3 disembarkPosition = transform.position + transform.right * disembarkDistance;

            // Player'ı pozisyonla
            playerTransform.position = disembarkPosition;

            // Player kontrolünü geri ver
            if (playerController != null)
            {
                playerController.ToggleControl(true);
            }

            // Player rigidbody'sini dinamik yap
            if (playerRigidbody != null)
            {
                playerRigidbody.isKinematic = false;
                playerRigidbody.linearVelocity = Vector2.zero;
            }

            // Player'ın TÜM sprite'larını tekrar göster
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

            // Kamerayı zoom out olarak bırak (orijinal haline GETİRME)
            // Kamera uzak kalacak
            Log("Kamera zoom out olarak kalıyor (reset yapılmadı)");

            // Boat controller'a bildir
            boatController.PlayerDisembarked();

            isPlayerOnBoard = false;

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

            // Player yakındaysa ve botta değilse göster
            if (isPlayerInRange && !isPlayerOnBoard)
            {
                promptTextObject.SetActive(true);
                promptText.text = "Press F to Enter Boat";
            }
            // Player bottaysa farklı mesaj göster
            else if (isPlayerOnBoard)
            {
                promptTextObject.SetActive(true);
                promptText.text = "Press F to Exit Boat";
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

            // Disembark position
            Gizmos.color = Color.red;
            Vector3 disembarkPos = transform.position + transform.right * disembarkDistance;
            Gizmos.DrawWireSphere(disembarkPos, 0.3f);
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
