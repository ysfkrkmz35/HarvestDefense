using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Game Over ekranı yönetimi
/// - Ölüm sonrası gösterilir
/// - Restart butonu ile sahneyi yeniden başlatır
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("═══ UI REFERENCES ═══")]
    [Tooltip("Game Over paneli (Canvas altındaki ana panel)")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Restart butonu")]
    [SerializeField] private Button restartButton;

    [Tooltip("Game Over başlığı (opsiyonel)")]
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("═══ SETTINGS ═══")]
    [Tooltip("Panel açılırken animasyon süresi (saniye)")]
    [SerializeField] private float fadeInDuration = 0.5f;

    [Tooltip("Panel açılırken scale animasyonu kullansın mı?")]
    [SerializeField] private bool useScaleAnimation = true;

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    private CanvasGroup canvasGroup;
    private bool isShowing = false;

    private void Awake()
    {
        // Panelin null olmadığından emin ol
        if (gameOverPanel == null)
        {
            Debug.LogError("[GameOverUI] ❌ Game Over Panel atanmadı! Inspector'dan ata.");
            return;
        }

        // CanvasGroup component'i ekle (yoksa)
        canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
        }

        // Başlangıçta paneli gizle
        HideImmediate();

        // Restart butonuna listener ekle
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
            if (showDebugLogs)
                Debug.Log("[GameOverUI] ✅ Restart button listener eklendi.");
        }
        else
        {
            Debug.LogError("[GameOverUI] ❌ Restart Button atanmadı! Inspector'dan ata.");
        }
    }

    private void Start()
    {
        // Oyun başladığında paneli kesinlikle gizle
        HideImmediate();

        if (showDebugLogs)
            Debug.Log("[GameOverUI] ✅ Game Over UI hazır. Panel gizli.");
    }

    private void OnEnable()
    {
        // Sahne her yüklendiğinde paneli gizle
        if (gameOverPanel != null)
        {
            HideImmediate();
        }
    }

    /// <summary>
    /// Game Over ekranını göster (animasyonlu)
    /// </summary>
    public void Show()
    {
        if (isShowing) return;

        if (showDebugLogs)
            Debug.Log("[GameOverUI] 💀 Game Over ekranı gösteriliyor...");

        isShowing = true;
        gameObject.SetActive(true);
        gameOverPanel.SetActive(true);

        // Animasyon başlat
        StartCoroutine(ShowAnimation());

        // Zamanı durdur (opsiyonel)
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Game Over ekranını hemen gizle
    /// </summary>
    public void HideImmediate()
    {
        isShowing = false;
        gameOverPanel.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (useScaleAnimation)
        {
            gameOverPanel.transform.localScale = Vector3.zero;
        }

        // Zamanı normale döndür
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Gösterme animasyonu
    /// </summary>
    private System.Collections.IEnumerator ShowAnimation()
    {
        float elapsedTime = 0f;

        // Başlangıç değerleri
        canvasGroup.alpha = 0f;
        if (useScaleAnimation)
        {
            gameOverPanel.transform.localScale = Vector3.zero;
        }

        // Animasyon
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // unscaledDeltaTime = Time.timeScale'den etkilenmez
            float t = elapsedTime / fadeInDuration;

            // Fade in
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            // Scale animation
            if (useScaleAnimation)
            {
                // Elastic ease-out effect için
                float scale = EaseOutElastic(t);
                gameOverPanel.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        // Final değerleri
        canvasGroup.alpha = 1f;
        if (useScaleAnimation)
        {
            gameOverPanel.transform.localScale = Vector3.one;
        }

        if (showDebugLogs)
            Debug.Log("[GameOverUI] ✅ Game Over animasyonu tamamlandı.");
    }

    /// <summary>
    /// Restart butonuna tıklandığında
    /// </summary>
    private void OnRestartButtonClicked()
    {
        if (showDebugLogs)
            Debug.Log("[GameOverUI] 🔄 Restart butonuna tıklandı!");

        // Zamanı normale döndür
        Time.timeScale = 1f;

        // DontDestroyOnLoad player'ı yok et (temiz restart için)
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Destroy(player);
            if (showDebugLogs)
                Debug.Log("[GameOverUI] 🗑️ Player destroyed for clean restart.");
        }

        // Sahneyi yeniden yükle
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// Elastic ease-out animasyon fonksiyonu
    /// </summary>
    private float EaseOutElastic(float t)
    {
        const float c4 = (2f * Mathf.PI) / 3f;

        if (t == 0f) return 0f;
        if (t == 1f) return 1f;

        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    /// <summary>
    /// Dışarıdan erişim için singleton pattern (opsiyonel)
    /// </summary>
    private static GameOverUI instance;
    public static GameOverUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameOverUI>();
            }
            return instance;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        // Zamanı normale döndür
        Time.timeScale = 1f;
    }
}
