using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class StoryCutsceneManager : MonoBehaviour
{
    public static StoryCutsceneManager Instance { get; private set; }

    [Header("UI Componentleri (Otomatik Oluşturulur)")]
    [SerializeField] private Canvas storyCanvas;
    [SerializeField] private Image slideImage;
    [SerializeField] private Image fadeOverlay;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private bool isPlaying = false;
    private bool userClicked = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (isPlaying)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                userClicked = true;
            }
        }
    }

    /// <summary>
    /// Hikayeyi başlatır
    /// </summary>
    public void PlayStory(StoryData data, Action onComplete = null)
    {
        if (isPlaying) return;
        if (data == null || data.slides.Count == 0)
        {
            Debug.LogError("[StoryCutsceneManager] Data boş veya resim yok!");
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(PlayRoutine(data, onComplete));
    }

    private IEnumerator PlayRoutine(StoryData data, Action onComplete)
    {
        isPlaying = true;
        
        // Oyunu durdur
        Time.timeScale = 0f;

        // UI aç
        storyCanvas.gameObject.SetActive(true);
        fadeOverlay.color = Color.black; // Siyah başla
        slideImage.color = Color.white;

        // Müzik başlat
        if (data.backgroundMusic != null)
        {
            audioSource.clip = data.backgroundMusic;
            audioSource.volume = 0;
            audioSource.Play();
            StartCoroutine(FadeMusic(data.musicVolume, 1.0f));
        }

        // --- SLIDE AKIŞI ---
        foreach (Sprite slide in data.slides)
        {
            slideImage.sprite = slide;
            userClicked = false;

            // Fade In (Siyahtan Resme)
            yield return Fade(1f, 0f, data.transitionDuration / 2);

            // Bekle (Tıklama veya Süre)
            float timer = 0f;
            while (!userClicked)
            {
                if (data.autoAdvance)
                {
                    timer += Time.unscaledDeltaTime;
                    if (timer >= data.slideDisplayTime) break;
                }
                yield return null;
            }

            // Fade Out (Resimden Siyaha) - Sadece son resim değilse veya son resimden çıkarken
            yield return Fade(0f, 1f, data.transitionDuration / 2);
        }

        // --- BİTİŞ ---
        
        // Müzik sustur
        if (audioSource.isPlaying)
        {
            StartCoroutine(FadeMusic(0f, 1.0f));
        }

        // UI kapat
        storyCanvas.gameObject.SetActive(false);
        
        // Oyunu devam ettir
        Time.timeScale = 1f;
        isPlaying = false;
        
        onComplete?.Invoke();
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = fadeOverlay.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            c.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeOverlay.color = c;
            yield return null;
        }
        c.a = endAlpha;
        fadeOverlay.color = c;
    }

    private IEnumerator FadeMusic(float targetVolume, float duration)
    {
        float startVol = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            audioSource.volume = Mathf.Lerp(startVol, targetVolume, t);
            yield return null;
        }
        audioSource.volume = targetVolume;
    }

    private void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("StoryCanvas");
        storyCanvas = canvasObj.AddComponent<Canvas>();
        storyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        storyCanvas.sortingOrder = 999; // En üstte
        DontDestroyOnLoad(canvasObj);
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background (Black BG container)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = Color.black;
        Stretch(bgObj.GetComponent<RectTransform>());

        // Slide Image
        GameObject slideObj = new GameObject("SlideImage");
        slideObj.transform.SetParent(bgObj.transform, false);
        slideImage = slideObj.AddComponent<Image>();
        slideImage.preserveAspect = true; // Resmi bozma
        Stretch(slideObj.GetComponent<RectTransform>());

        // Fade Overlay
        GameObject fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(canvasObj.transform, false);
        fadeOverlay = fadeObj.AddComponent<Image>();
        fadeOverlay.color = Color.black;
        fadeOverlay.raycastTarget = false; // Tıklamayı engellemesin
        Stretch(fadeObj.GetComponent<RectTransform>());

        // Audio Source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;

        // Başlangıçta gizli
        storyCanvas.gameObject.SetActive(false);
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
