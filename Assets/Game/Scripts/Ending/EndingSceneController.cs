using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Ending sahnesinde çalışır
/// - Açılışta fade-in efekti
/// - Arka plan müziği (loop)
/// - Fotoğraf gösterimi
/// - Click to continue -> Thank You + Quit butonu
/// </summary>
public class EndingSceneController : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Gösterilecek bitiş görseli")]
    [SerializeField] private Sprite endingImage;
    
    [Tooltip("Fade-in süresi (saniye)")]
    [SerializeField] private float fadeInDuration = 2f;

    [Header("Audio")]
    [Tooltip("Bitiş müziği")]
    [SerializeField] private AudioClip endingMusic;
    
    [Tooltip("Müzik ses seviyesi")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.7f;

    private Canvas canvas;
    private GameObject endingUI;
    private AudioSource audioSource;
    private bool showingThankYou = false;

    private void Start()
    {
        // Müziği başlat
        StartMusic();
        
        // UI oluştur
        CreateCanvas();
        ShowEndingImage();
    }

    private void Update()
    {
        // Click to continue
        if (!showingThankYou && Input.GetMouseButtonDown(0))
        {
            ShowThankYou();
        }
    }

    private void StartMusic()
    {
        if (endingMusic == null) return;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = endingMusic;
        audioSource.loop = true;
        audioSource.volume = musicVolume;
        audioSource.Play();
        
        Debug.Log("[EndingScene] 🎵 Müzik başladı");
    }

    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("EndingCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void ShowEndingImage()
    {
        endingUI = new GameObject("EndingScreen");
        endingUI.transform.SetParent(canvas.transform, false);

        // Tam ekran
        RectTransform rt = endingUI.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Siyah arkaplan
        Image bg = endingUI.AddComponent<Image>();
        bg.color = Color.black;

        // Görsel
        Image imgComponent = null;
        if (endingImage != null)
        {
            GameObject imgObj = new GameObject("EndingImage");
            imgObj.transform.SetParent(endingUI.transform, false);

            RectTransform imgRT = imgObj.AddComponent<RectTransform>();
            imgRT.anchorMin = Vector2.zero;
            imgRT.anchorMax = Vector2.one;
            imgRT.offsetMin = Vector2.zero;
            imgRT.offsetMax = Vector2.zero;

            imgComponent = imgObj.AddComponent<Image>();
            imgComponent.sprite = endingImage;
            imgComponent.preserveAspect = true;
            imgComponent.color = new Color(1, 1, 1, 0); // Şeffaf başla
        }

        // "Click to continue" text
        GameObject clickText = new GameObject("ClickText");
        clickText.transform.SetParent(endingUI.transform, false);

        RectTransform clickRT = clickText.AddComponent<RectTransform>();
        clickRT.anchorMin = new Vector2(0.5f, 0.05f);
        clickRT.anchorMax = new Vector2(0.5f, 0.05f);
        clickRT.sizeDelta = new Vector2(400, 50);

        TextMeshProUGUI clickTMP = clickText.AddComponent<TextMeshProUGUI>();
        clickTMP.text = "Click to continue...";
        clickTMP.fontSize = 36; // Daha büyük font
        clickTMP.alignment = TextAlignmentOptions.Center;
        clickTMP.color = new Color(1, 1, 1, 0); // Şeffaf başla

        // Fade-in
        StartCoroutine(FadeIn(imgComponent, clickTMP));
    }

    private System.Collections.IEnumerator FadeIn(Image img, TextMeshProUGUI text)
    {
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            
            if (img != null) img.color = new Color(1, 1, 1, alpha);
            if (text != null) text.color = new Color(1, 1, 1, alpha * 0.7f);
            
            yield return null;
        }
        
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

        Debug.Log("[EndingScene] 🎬 Thank You ekranı gösterildi");
    }

    private void QuitGame()
    {
        Debug.Log("[EndingScene] 👋 Oyundan çıkılıyor...");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
