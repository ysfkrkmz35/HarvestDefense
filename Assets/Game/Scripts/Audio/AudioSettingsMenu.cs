using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Oyun içi ses ayarları menüsü
/// - ESC tuşu ile açılır/kapanır
/// - Master Volume slider ile ses ayarlanır
/// - Oyun duraklar (opsiyonel)
/// </summary>
public class AudioSettingsMenu : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Menü açma/kapama tuşu")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;
    
    [Tooltip("Menü açıkken oyun duraklasın mı?")]
    [SerializeField] private bool pauseWhenOpen = true;

    [Header("UI References (Auto-created if null)")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeLabel;

    private bool isOpen = false;
    private float savedTimeScale = 1f;

    private void Start()
    {
        if (menuPanel == null)
        {
            CreateMenuUI();
        }
        else
        {
            menuPanel.SetActive(false);
        }

        // Kayıtlı ses seviyesini yükle
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        AudioListener.volume = savedVolume;
        if (volumeSlider != null) volumeSlider.value = savedVolume;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isOpen = !isOpen;
        menuPanel.SetActive(isOpen);

        if (pauseWhenOpen)
        {
            if (isOpen)
            {
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = savedTimeScale;
            }
        }
    }

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        
        if (volumeLabel != null)
            volumeLabel.text = $"Volume: {Mathf.RoundToInt(value * 100)}%";

        // SceneBGM varsa onu da güncelle
        SceneBGM bgm = FindFirstObjectByType<SceneBGM>();
        if (bgm != null)
        {
            bgm.SetVolume(value);
        }
    }

    public void CloseMenu()
    {
        isOpen = false;
        menuPanel.SetActive(false);
        
        if (pauseWhenOpen)
        {
            Time.timeScale = savedTimeScale;
        }
    }

    private void CreateMenuUI()
    {
        // Canvas bul veya oluştur
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("SettingsCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Panel
        menuPanel = new GameObject("AudioSettingsPanel");
        menuPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRT = menuPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(400, 200);

        Image panelImg = menuPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(menuPanel.transform, false);

        RectTransform titleRT = titleObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0, -30);
        titleRT.sizeDelta = new Vector2(300, 40);

        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "SETTINGS";
        titleTMP.fontSize = 28;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;

        // Volume Label
        GameObject labelObj = new GameObject("VolumeLabel");
        labelObj.transform.SetParent(menuPanel.transform, false);

        RectTransform labelRT = labelObj.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.5f, 0.5f);
        labelRT.anchorMax = new Vector2(0.5f, 0.5f);
        labelRT.anchoredPosition = new Vector2(0, 20);
        labelRT.sizeDelta = new Vector2(200, 30);

        volumeLabel = labelObj.AddComponent<TextMeshProUGUI>();
        volumeLabel.text = "Volume: 50%";
        volumeLabel.fontSize = 18;
        volumeLabel.alignment = TextAlignmentOptions.Center;
        volumeLabel.color = Color.white;

        // Volume Slider
        GameObject sliderObj = new GameObject("VolumeSlider");
        sliderObj.transform.SetParent(menuPanel.transform, false);

        RectTransform sliderRT = sliderObj.AddComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRT.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRT.anchoredPosition = new Vector2(0, -20);
        sliderRT.sizeDelta = new Vector2(300, 20);

        // Slider Background
        Image sliderBg = sliderObj.AddComponent<Image>();
        sliderBg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(5, 5);
        fillAreaRT.offsetMax = new Vector2(-5, -5);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.7f, 0.3f, 1f);

        // Handle Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRT = handleArea.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10, 0);
        handleAreaRT.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRT = handle.AddComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20, 20);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        // Slider Component
        volumeSlider = sliderObj.AddComponent<Slider>();
        volumeSlider.fillRect = fillRT;
        volumeSlider.handleRect = handleRT;
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = AudioListener.volume;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        // Close Button
        GameObject closeBtn = new GameObject("CloseButton");
        closeBtn.transform.SetParent(menuPanel.transform, false);

        RectTransform closeBtnRT = closeBtn.AddComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(0.5f, 0f);
        closeBtnRT.anchorMax = new Vector2(0.5f, 0f);
        closeBtnRT.anchoredPosition = new Vector2(0, 30);
        closeBtnRT.sizeDelta = new Vector2(120, 40);

        Image closeBtnImg = closeBtn.AddComponent<Image>();
        closeBtnImg.color = new Color(0.5f, 0.5f, 0.5f, 1f);

        Button closeButton = closeBtn.AddComponent<Button>();
        closeButton.onClick.AddListener(CloseMenu);

        GameObject closeBtnText = new GameObject("Text");
        closeBtnText.transform.SetParent(closeBtn.transform, false);
        RectTransform closeBtnTextRT = closeBtnText.AddComponent<RectTransform>();
        closeBtnTextRT.anchorMin = Vector2.zero;
        closeBtnTextRT.anchorMax = Vector2.one;
        closeBtnTextRT.offsetMin = Vector2.zero;
        closeBtnTextRT.offsetMax = Vector2.zero;

        TextMeshProUGUI closeBtnTMP = closeBtnText.AddComponent<TextMeshProUGUI>();
        closeBtnTMP.text = "CLOSE";
        closeBtnTMP.fontSize = 18;
        closeBtnTMP.alignment = TextAlignmentOptions.Center;
        closeBtnTMP.color = Color.white;

        menuPanel.SetActive(false);
        Debug.Log("[AudioSettingsMenu] ✅ Menü oluşturuldu. ESC ile açılır.");
    }
}
