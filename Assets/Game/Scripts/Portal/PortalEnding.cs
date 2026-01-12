using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Portal ile etkileşim - F tuşuna basınca bitiş ekranını gösterir
/// </summary>
public class PortalEnding : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Etkileşim tuşu")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    
    [Tooltip("Etkileşim mesajı (opsiyonel)")]
    [SerializeField] private string promptText = "Press F";
    
    [Tooltip("Manuel algılama yarıçapı (Trigger çalışmazsa kullanılır)")]
    [SerializeField] private float detectionRadius = 2f;

    [Header("Ending Screen")]
    [Tooltip("Bitiş ekranında gösterilecek resim")]
    [SerializeField] private Sprite endingImage;
    
    [Tooltip("Bitiş müziği (Quit'e basana kadar döngüde çalar)")]
    [SerializeField] private AudioClip endingMusic;
    
    [Tooltip("Fade-in süresi (saniye)")]
    [SerializeField] private float fadeInDuration = 2f;

    private bool playerInRange = false;
    private bool isActive = true;
    private GameObject promptUI;
    private GameObject endingUI;
    private bool showingEnding = false;
    private bool showingThankYou = false;
    private Transform playerTransform;

    private void Start()
    {
        CreatePromptUI();
        
        // Player'ı bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
        
        // Rigidbody2D kontrolü - Trigger için gerekli
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            Debug.Log("[PortalEnding] ⚙️ Rigidbody2D (Static) eklendi - Trigger için gerekli");
        }
        
        Debug.Log($"[PortalEnding] ✅ Portal hazır. Player: {(playerTransform != null ? playerTransform.name : "NOT FOUND")}");
    }

    private void Update()
    {
        if (!isActive) return;

        // MANUEL MESAFE KONTROLÜ (Trigger çalışmazsa yedek)
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        if (playerTransform != null)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            bool wasInRange = playerInRange;
            playerInRange = distance <= detectionRadius;
            
            if (playerInRange && !wasInRange)
            {
                Debug.Log($"[PortalEnding] ✅ Player yaklaştı (Mesafe: {distance:F1})");
            }
        }

        // Prompt göster/gizle
        if (promptUI != null)
            promptUI.SetActive(playerInRange && !showingEnding);

        // F tuşu kontrolü
        if (playerInRange && !showingEnding && Input.GetKeyDown(interactKey))
        {
            Debug.Log("[PortalEnding] 🎮 F tuşuna basıldı!");
            ShowEndingScreen();
        }

        // Bitiş ekranında tıklama kontrolü
        if (showingEnding && !showingThankYou && Input.GetMouseButtonDown(0))
        {
            ShowThankYou();
        }
    }

    private void CreatePromptUI()
    {
        Canvas canvas = FindOrCreateCanvas();

        // Prompt Text
        promptUI = new GameObject("PortalPrompt");
        promptUI.transform.SetParent(canvas.transform, false);

        RectTransform rt = promptUI.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.3f);
        rt.anchorMax = new Vector2(0.5f, 0.3f);
        rt.sizeDelta = new Vector2(200, 50);

        TextMeshProUGUI tmp = promptUI.AddComponent<TextMeshProUGUI>();
        tmp.text = promptText;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        promptUI.SetActive(false);
    }

    private void ShowEndingScreen()
    {
        showingEnding = true;
        
        // Ending sahnesine geç
        Debug.Log("[PortalEnding] 🎬 Ending sahnesine geçiliyor...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Ending");
    }

    private System.Collections.IEnumerator FadeInEndingScreen(Image img, TextMeshProUGUI text)
    {
        float elapsed = 0f;
        
        // UNSCALED time kullan çünkü oyun duraklatılmış olabilir
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            
            if (img != null) img.color = new Color(1, 1, 1, alpha);
            if (text != null) text.color = new Color(1, 1, 1, alpha * 0.7f);
            
            yield return null;
        }
        
        // Tam opaklık
        if (img != null) img.color = Color.white;
        if (text != null) text.color = new Color(1, 1, 1, 0.7f);
    }

    private void ShowThankYou()
    {
        showingThankYou = true;

        // Mevcut içeriği temizle
        foreach (Transform child in endingUI.transform)
        {
            Destroy(child.gameObject);
        }

        // "Thank You For Playing" text
        GameObject thankYouObj = new GameObject("ThankYouText");
        thankYouObj.transform.SetParent(endingUI.transform, false);

        RectTransform tyRT = thankYouObj.AddComponent<RectTransform>();
        tyRT.anchorMin = new Vector2(0.5f, 0.6f);
        tyRT.anchorMax = new Vector2(0.5f, 0.6f);
        tyRT.sizeDelta = new Vector2(600, 80);

        TextMeshProUGUI tyTMP = thankYouObj.AddComponent<TextMeshProUGUI>();
        tyTMP.text = "Thank You For Playing!";
        tyTMP.fontSize = 48;
        tyTMP.alignment = TextAlignmentOptions.Center;
        tyTMP.color = Color.white;

        // Quit Button
        GameObject quitBtn = new GameObject("QuitButton");
        quitBtn.transform.SetParent(endingUI.transform, false);

        RectTransform btnRT = quitBtn.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.3f);
        btnRT.anchorMax = new Vector2(0.5f, 0.3f);
        btnRT.sizeDelta = new Vector2(200, 60);

        Image btnImg = quitBtn.AddComponent<Image>();
        btnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

        Button btn = quitBtn.AddComponent<Button>();
        btn.onClick.AddListener(QuitGame);

        // Button Text
        GameObject btnTextObj = new GameObject("ButtonText");
        btnTextObj.transform.SetParent(quitBtn.transform, false);

        RectTransform btnTextRT = btnTextObj.AddComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = Vector2.zero;
        btnTextRT.offsetMax = Vector2.zero;

        TextMeshProUGUI btnTMP = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "QUIT";
        btnTMP.fontSize = 28;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = Color.white;

        Debug.Log("[PortalEnding] 🎬 Thank You ekranı gösterildi");
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[PortalEnding] 👋 Oyundan çıkılıyor...");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private Canvas FindOrCreateCanvas()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("EndingCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        return canvas;
    }

    /// <summary>
    /// Tüm diğer UI elemanlarını gizle (toolbars, menus, HUD)
    /// </summary>
    private void HideAllOtherUI()
    {
        int hiddenCount = 0;
        
        // Tüm Canvas'ları bul
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        foreach (Canvas canvas in allCanvases)
        {
            // Her canvas'ın çocuklarını gizle
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                Transform child = canvas.transform.GetChild(i);
                
                // Ending UI hariç hepsini gizle
                if (child.gameObject != endingUI && child.gameObject != promptUI)
                {
                    child.gameObject.SetActive(false);
                    hiddenCount++;
                }
            }
        }
        
        // Ayrıca Player'ı da gizle (isteğe bağlı)
        if (playerTransform != null)
        {
            SpriteRenderer playerSprite = playerTransform.GetComponent<SpriteRenderer>();
            if (playerSprite != null) playerSprite.enabled = false;
        }
        
        Debug.Log($"[PortalEnding] 🔒 {hiddenCount} UI elemanı gizlendi");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[PortalEnding] Player portal alanına girdi");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    /// <summary>
    /// Portal'ı aktifleştir (Boss öldüğünde çağrılabilir)
    /// </summary>
    public void ActivatePortal()
    {
        isActive = true;
        gameObject.SetActive(true);
        Debug.Log("[PortalEnding] ✅ Portal aktifleştirildi!");
    }

    /// <summary>
    /// Portal'ı devre dışı bırak
    /// </summary>
    public void DeactivatePortal()
    {
        isActive = false;
    }
}
