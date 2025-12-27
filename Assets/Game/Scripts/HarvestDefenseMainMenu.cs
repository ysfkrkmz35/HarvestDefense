
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// HARVEST DEFENSE - PREMIUM DARK FANTASY FARM MENU
/// </summary>
public class HarvestDefenseMainMenu : MonoBehaviour
{
    [Header("=== SCENE SETTINGS ===")]
    public string gameSceneName = "GameScene";

    [Header("=== AUDIO ===")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.4f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    private readonly Color bgDark = new Color32(0x1A, 0x0F, 0x08, 255);
    private readonly Color bgMedium = new Color32(0x2D, 0x18, 0x10, 255);
    private readonly Color goldLight = new Color32(0xFF, 0xD9, 0x66, 255);
    private readonly Color goldDark = new Color32(0xCC, 0x7A, 0x00, 255);
    private readonly Color redTitle = new Color32(0xD9, 0x44, 0x44, 255);
    private readonly Color cream = new Color32(0xF5, 0xF0, 0xE6, 255);
    private readonly Color brownDark = new Color32(0x3A, 0x22, 0x12, 255);

    private Canvas mainCanvas;
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private readonly List<ButtonData> buttons = new List<ButtonData>();
    private RectTransform titleRect;
    private readonly List<ParticleInfo> particles = new List<ParticleInfo>();
    private bool ready;

    private Sprite leafSprite;
    private Sprite wheatSprite;
    private Sprite sparkleSprite;
    private Sprite swordIcon;
    private Sprite bookIcon;
    private Sprite gearIcon;
    private Sprite peopleIcon;
    private Sprite exitIcon;
    private Sprite buttonBgSprite;
    private Sprite buttonShadowSprite;
    private Sprite buttonLeftSprite;

    private class ButtonData
    {
        public RectTransform rect;
        public Image bg;
        public Image iconBg;
        public TextMeshProUGUI label;
        public TextMeshProUGUI arrow;
        public CanvasGroup cg;
        public Vector2 basePos;
        public bool hovered;
    }

    private class ParticleInfo
    {
        public RectTransform rect;
        public float speed;
        public float sway;
        public float swaySpeed;
        public float rot;
        public float baseX;
        public float phase;
        public int type;
    }

    void Start()
    {
        SetupAudio();
        CreateCanvas();
        CacheSprites();
        CreateBackground();
        CreateCornerDecorations();
        CreateParticles();
        CreateTitle();
        CreateButtons();
        CreateVersionText();

        ready = true;
        StartCoroutine(EntranceAnimation());
    }

    void Update()
    {
        if (!ready) return;
        AnimateParticles();
    }

    void SetupAudio()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.volume = sfxVolume;
    }

    void CreateCanvas()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvasObj = new GameObject("MenuCanvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.6f;
        scaler.referencePixelsPerUnit = 100;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    void CacheSprites()
    {
        buttonBgSprite = CreateRoundedRect(1024, 256, 96);
        buttonShadowSprite = CreateRoundedRect(1024, 256, 96);
        buttonLeftSprite = CreateLeftRoundedRect(256, 256, 96);
        leafSprite = CreateLeafSprite(512);
        wheatSprite = CreateWheatGrain(512);
        sparkleSprite = CreateSparkle(256);
        swordIcon = CreateSwordIcon(512);
        bookIcon = CreateBookIcon(512);
        gearIcon = CreateGearIcon(512);
        peopleIcon = CreatePeopleIcon(512);
        exitIcon = CreateExitIcon(512);
    }

    void CreateBackground()
    {
        var bgObj = CreateUI("Background", mainCanvas.transform);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = CreateGradientSprite(4, 1024, bgDark, bgMedium);
        Stretch(bgObj.GetComponent<RectTransform>());

        var vignetteObj = CreateUI("Vignette", mainCanvas.transform);
        var vignetteImg = vignetteObj.AddComponent<Image>();
        vignetteImg.sprite = CreateVignetteSprite(1024);
        vignetteImg.raycastTarget = false;
        Stretch(vignetteObj.GetComponent<RectTransform>());
    }

    void CreateCornerDecorations()
    {
        float offset = 28f;
        float size = 120f;
        float thick = 4f;
        Color c = new Color(goldDark.r, goldDark.g, goldDark.b, 0.55f);

        CreateCorner("TL", new Vector2(0, 1), new Vector2(offset, -offset), size, thick, c, true, true);
        CreateCorner("TR", new Vector2(1, 1), new Vector2(-offset, -offset), size, thick, c, false, true);
        CreateCorner("BL", new Vector2(0, 0), new Vector2(offset, offset), size, thick, c, true, false);
        CreateCorner("BR", new Vector2(1, 0), new Vector2(-offset, offset), size, thick, c, false, false);
    }

    void CreateCorner(string name, Vector2 anchor, Vector2 pos, float size, float thick, Color color, bool left, bool top)
    {
        var container = CreateUI(name, mainCanvas.transform);
        var cRect = container.GetComponent<RectTransform>();
        cRect.anchorMin = cRect.anchorMax = anchor;
        cRect.anchoredPosition = pos;
        cRect.sizeDelta = new Vector2(size, size);

        var hLine = CreateUI("H", container.transform);
        var hImg = hLine.AddComponent<Image>();
        hImg.sprite = CreateSmoothRect(256, 16);
        hImg.color = color;
        hImg.raycastTarget = false;
        var hRect = hLine.GetComponent<RectTransform>();
        hRect.anchorMin = hRect.anchorMax = new Vector2(left ? 0 : 1, top ? 1 : 0);
        hRect.pivot = new Vector2(left ? 0 : 1, 0.5f);
        hRect.sizeDelta = new Vector2(size, thick);

        var vLine = CreateUI("V", container.transform);
        var vImg = vLine.AddComponent<Image>();
        vImg.sprite = CreateSmoothRect(16, 256);
        vImg.color = color;
        vImg.raycastTarget = false;
        var vRect = vLine.GetComponent<RectTransform>();
        vRect.anchorMin = vRect.anchorMax = new Vector2(left ? 0 : 1, top ? 1 : 0);
        vRect.pivot = new Vector2(0.5f, top ? 1 : 0);
        vRect.sizeDelta = new Vector2(thick, size);
    }

    void CreateParticles()
    {
        for (int i = 0; i < 28; i++)
        {
            var obj = CreateUI($"P{i}", mainCanvas.transform);
            var img = obj.AddComponent<Image>();

            int type = Random.Range(0, 3);
            var info = new ParticleInfo { type = type };

            if (type == 0)
            {
                img.sprite = leafSprite;
                img.color = new Color(Random.Range(0.55f, 0.75f), Random.Range(0.35f, 0.55f), Random.Range(0.15f, 0.3f), Random.Range(0.35f, 0.65f));
            }
            else if (type == 1)
            {
                img.sprite = wheatSprite;
                img.color = new Color(Random.Range(0.85f, 1f), Random.Range(0.65f, 0.8f), Random.Range(0.25f, 0.45f), Random.Range(0.4f, 0.7f));
            }
            else
            {
                img.sprite = sparkleSprite;
                img.color = new Color(1f, 0.92f, 0.65f, Random.Range(0.15f, 0.4f));
            }

            img.raycastTarget = false;

            var rect = obj.GetComponent<RectTransform>();
            float x = Random.Range(0.05f, 0.95f);
            float y = Random.Range(0f, 1.2f);
            rect.anchorMin = rect.anchorMax = new Vector2(x, y);
            float scale = type == 2 ? Random.Range(0.25f, 0.5f) : Random.Range(0.35f, 0.8f);
            rect.sizeDelta = type == 0 ? new Vector2(36 * scale, 22 * scale) : type == 1 ? new Vector2(16 * scale, 26 * scale) : new Vector2(10 * scale, 10 * scale);

            info.rect = rect;
            info.speed = Random.Range(10f, 26f);
            info.sway = Random.Range(18f, 42f);
            info.swaySpeed = Random.Range(0.6f, 1.6f);
            info.rot = Random.Range(-60f, 60f);
            info.baseX = x;
            info.phase = Random.Range(0f, Mathf.PI * 2);
            particles.Add(info);
        }
    }

    void CreateTitle()
    {
        var container = CreateUI("Title", mainCanvas.transform);
        titleRect = container.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.84f);
        titleRect.sizeDelta = new Vector2(700, 220);

        CreateTitleDecoration(container.transform);

        var harvestObj = CreateUI("Harvest", container.transform);
        var harvestTxt = harvestObj.AddComponent<TextMeshProUGUI>();
        harvestTxt.text = "HARVEST";
        harvestTxt.fontSize = 80;
        harvestTxt.fontStyle = FontStyles.Bold;
        harvestTxt.alignment = TextAlignmentOptions.Center;
        harvestTxt.enableVertexGradient = true;
        harvestTxt.colorGradient = new VertexGradient(goldLight, goldLight, goldDark, goldDark);
        harvestTxt.outlineWidth = 0.12f;
        harvestTxt.outlineColor = new Color(0.25f, 0.12f, 0.04f);
        var harvestRect = harvestObj.GetComponent<RectTransform>();
        harvestRect.anchorMin = new Vector2(0, 0.4f);
        harvestRect.anchorMax = new Vector2(1, 0.9f);
        harvestRect.offsetMin = harvestRect.offsetMax = Vector2.zero;

        var defenseObj = CreateUI("Defense", container.transform);
        var defenseTxt = defenseObj.AddComponent<TextMeshProUGUI>();
        defenseTxt.text = "DEFENSE";
        defenseTxt.fontSize = 48;
        defenseTxt.fontStyle = FontStyles.Bold;
        defenseTxt.alignment = TextAlignmentOptions.Center;
        defenseTxt.color = redTitle;
        defenseTxt.outlineWidth = 0.1f;
        defenseTxt.outlineColor = new Color(0.18f, 0.04f, 0.04f);
        var defenseRect = defenseObj.GetComponent<RectTransform>();
        defenseRect.anchorMin = new Vector2(0, 0.08f);
        defenseRect.anchorMax = new Vector2(1, 0.45f);
        defenseRect.offsetMin = defenseRect.offsetMax = Vector2.zero;

        var subObj = CreateUI("Sub", container.transform);
        var subTxt = subObj.AddComponent<TextMeshProUGUI>();
        subTxt.text = "PROTECT YOUR HARVEST • DESTROY ENEMIES";
        subTxt.fontSize = 16;
        subTxt.characterSpacing = 6;
        subTxt.alignment = TextAlignmentOptions.Center;
        subTxt.color = new Color(cream.r, cream.g, cream.b, 0.6f);
        var subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = subRect.anchorMax = new Vector2(0.5f, 0);
        subRect.anchoredPosition = new Vector2(0, -26);
        subRect.sizeDelta = new Vector2(560, 26);
    }

    void CreateTitleDecoration(Transform parent)
    {
        var swordsObj = CreateUI("Swords", parent);
        var swordsImg = swordsObj.AddComponent<Image>();
        swordsImg.sprite = CreateCrossedSwords(512);
        swordsImg.color = goldLight;
        swordsImg.raycastTarget = false;
        var swordsRect = swordsObj.GetComponent<RectTransform>();
        swordsRect.anchorMin = swordsRect.anchorMax = new Vector2(0.5f, 1f);
        swordsRect.anchoredPosition = new Vector2(0, 36);
        swordsRect.sizeDelta = new Vector2(64, 64);

        var lw = CreateUI("LW", parent);
        var lwImg = lw.AddComponent<Image>();
        lwImg.sprite = CreateWheatBundle(512);
        lwImg.color = goldLight;
        lwImg.raycastTarget = false;
        var lwRect = lw.GetComponent<RectTransform>();
        lwRect.anchorMin = lwRect.anchorMax = new Vector2(0.5f, 1f);
        lwRect.anchoredPosition = new Vector2(-70, 30);
        lwRect.sizeDelta = new Vector2(52, 68);
        lwRect.localRotation = Quaternion.Euler(0, 0, 12);

        var rw = CreateUI("RW", parent);
        var rwImg = rw.AddComponent<Image>();
        rwImg.sprite = CreateWheatBundle(512);
        rwImg.color = goldLight;
        rwImg.raycastTarget = false;
        var rwRect = rw.GetComponent<RectTransform>();
        rwRect.anchorMin = rwRect.anchorMax = new Vector2(0.5f, 1f);
        rwRect.anchoredPosition = new Vector2(70, 30);
        rwRect.sizeDelta = new Vector2(52, 68);
        rwRect.localRotation = Quaternion.Euler(0, 0, -12);
        rwRect.localScale = new Vector3(-1, 1, 1);
    }
    void CreateButtons()
    {
        var container = CreateUI("Buttons", mainCanvas.transform);
        var cRect = container.GetComponent<RectTransform>();
        cRect.anchorMin = cRect.anchorMax = new Vector2(0.5f, 0.47f);
        cRect.sizeDelta = new Vector2(560, 520);

        var btnData = new (string label, string icon, System.Action action)[]
        {
            ("PLAY", "sword", OnPlay),
            ("CONTINUE", "book", OnContinue),
            ("SETTINGS", "gear", OnSettings),
            ("CREDITS", "people", OnCredits),
            ("QUIT", "exit", OnQuit)
        };

        float startY = 190f;
        float spacing = 90f;
        for (int i = 0; i < btnData.Length; i++)
        {
            var b = btnData[i];
            CreateButton(container.transform, b.label, b.icon, b.action, new Vector2(0, startY - i * spacing), i);
        }
    }

    void CreateButton(Transform parent, string label, string iconType, System.Action onClick, Vector2 pos, int idx)
    {
        var data = new ButtonData { basePos = pos };

        var btnObj = CreateUI($"Btn_{label}", parent);
        data.rect = btnObj.GetComponent<RectTransform>();
        data.rect.anchorMin = data.rect.anchorMax = new Vector2(0.5f, 0.5f);
        data.rect.sizeDelta = new Vector2(520, 76);
        data.rect.anchoredPosition = pos;

        data.cg = btnObj.AddComponent<CanvasGroup>();
        data.cg.alpha = 0;

        var shadowObj = CreateUI("Shadow", btnObj.transform);
        var shadowImg = shadowObj.AddComponent<Image>();
        shadowImg.sprite = buttonShadowSprite;
        shadowImg.type = Image.Type.Sliced;
        shadowImg.color = new Color(0, 0, 0, 0.45f);
        shadowImg.raycastTarget = false;
        var shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.offsetMin = new Vector2(-2, -5);
        shadowRect.offsetMax = new Vector2(6, 3);

        var bgObj = CreateUI("Bg", btnObj.transform);
        data.bg = bgObj.AddComponent<Image>();
        data.bg.sprite = buttonBgSprite;
        data.bg.type = Image.Type.Sliced;
        data.bg.color = cream;
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        var iconSection = CreateUI("IconSec", btnObj.transform);
        data.iconBg = iconSection.AddComponent<Image>();
        data.iconBg.sprite = buttonLeftSprite;
        data.iconBg.type = Image.Type.Sliced;
        data.iconBg.color = brownDark;
        var iconSecRect = iconSection.GetComponent<RectTransform>();
        iconSecRect.anchorMin = new Vector2(0, 0);
        iconSecRect.anchorMax = new Vector2(0, 1);
        iconSecRect.offsetMin = Vector2.zero;
        iconSecRect.offsetMax = new Vector2(86, 0);

        var iconObj = CreateUI("Icon", iconSection.transform);
        var iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = GetIconSprite(iconType);
        iconImg.color = goldLight;
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;
        var iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(30, 30);

        var labelObj = CreateUI("Label", btnObj.transform);
        data.label = labelObj.AddComponent<TextMeshProUGUI>();
        data.label.text = label;
        data.label.fontSize = 26;
        data.label.fontStyle = FontStyles.Bold;
        data.label.alignment = TextAlignmentOptions.Left;
        data.label.color = brownDark;
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(108, 0);
        labelRect.offsetMax = new Vector2(-54, 0);

        var arrowObj = CreateUI("Arrow", btnObj.transform);
        data.arrow = arrowObj.AddComponent<TextMeshProUGUI>();
        data.arrow.text = "▶";
        data.arrow.fontSize = 20;
        data.arrow.alignment = TextAlignmentOptions.Center;
        data.arrow.color = new Color(brownDark.r, brownDark.g, brownDark.b, 0.4f);
        var arrowRect = arrowObj.GetComponent<RectTransform>();
        arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-28, 0);
        arrowRect.sizeDelta = new Vector2(32, 32);

        var btn = btnObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() =>
        {
            PlaySFX(clickSound);
            StartCoroutine(ClickAnim(data, onClick));
        });

        var trigger = btnObj.AddComponent<EventTrigger>();
        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => OnBtnEnter(data));
        trigger.triggers.Add(enterEntry);
        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => OnBtnExit(data));
        trigger.triggers.Add(exitEntry);

        buttons.Add(data);
    }

    Sprite GetIconSprite(string type)
    {
        switch (type)
        {
            case "sword": return swordIcon;
            case "book": return bookIcon;
            case "gear": return gearIcon;
            case "people": return peopleIcon;
            case "exit": return exitIcon;
            default: return CreateCircle(256);
        }
    }

    void OnBtnEnter(ButtonData btn)
    {
        if (btn.hovered) return;
        btn.hovered = true;
        PlaySFX(hoverSound);
        StartCoroutine(HoverAnim(btn, true));
    }

    void OnBtnExit(ButtonData btn)
    {
        if (!btn.hovered) return;
        btn.hovered = false;
        StartCoroutine(HoverAnim(btn, false));
    }

    IEnumerator HoverAnim(ButtonData btn, bool enter)
    {
        float dur = 0.12f;
        float t = 0f;
        Vector3 fromScale = btn.rect.localScale;
        Vector3 toScale = enter ? Vector3.one * 1.03f : Vector3.one;
        Vector2 fromPos = btn.rect.anchoredPosition;
        Vector2 toPos = enter ? btn.basePos + new Vector2(8, 0) : btn.basePos;
        Color fromArrow = btn.arrow.color;
        Color toArrow = enter ? brownDark : new Color(brownDark.r, brownDark.g, brownDark.b, 0.4f);

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = EaseOutBack(t / dur);
            btn.rect.localScale = Vector3.Lerp(fromScale, toScale, p);
            btn.rect.anchoredPosition = Vector2.Lerp(fromPos, toPos, p);
            btn.arrow.color = Color.Lerp(fromArrow, toArrow, p);
            yield return null;
        }
        btn.rect.localScale = toScale;
        btn.rect.anchoredPosition = toPos;
        btn.arrow.color = toArrow;
    }

    IEnumerator ClickAnim(ButtonData btn, System.Action callback)
    {
        float t = 0f;
        float dur = 0.05f;
        Vector3 from = btn.rect.localScale;
        while (t < dur)
        {
            t += Time.deltaTime;
            btn.rect.localScale = Vector3.Lerp(from, Vector3.one * 0.92f, t / dur);
            yield return null;
        }
        t = 0f;
        dur = 0.08f;
        from = btn.rect.localScale;
        while (t < dur)
        {
            t += Time.deltaTime;
            btn.rect.localScale = Vector3.Lerp(from, Vector3.one, EaseOutBack(t / dur));
            yield return null;
        }
        callback?.Invoke();
    }

    void CreateVersionText()
    {
        var obj = CreateUI("Version", mainCanvas.transform);
        var txt = obj.AddComponent<TextMeshProUGUI>();
        txt.text = "v1.0.0 • © 2024 Studio Name";
        txt.fontSize = 14;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(cream.r, cream.g, cream.b, 0.35f);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 40);
        rect.sizeDelta = new Vector2(520, 24);
    }

    IEnumerator EntranceAnimation()
    {
        titleRect.localScale = Vector3.zero;
        float t = 0f;
        float dur = 0.45f;
        while (t < dur)
        {
            t += Time.deltaTime;
            titleRect.localScale = Vector3.one * EaseOutBack(t / dur);
            yield return null;
        }
        titleRect.localScale = Vector3.one;
        yield return new WaitForSeconds(0.04f);
        for (int i = 0; i < buttons.Count; i++)
            StartCoroutine(ButtonEnterAnim(buttons[i], i * 0.07f));
    }

    IEnumerator ButtonEnterAnim(ButtonData btn, float delay)
    {
        yield return new WaitForSeconds(delay);
        float t = 0f;
        float dur = 0.25f;
        Vector2 startPos = btn.basePos + new Vector2(-70, 0);
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = EaseOutBack(t / dur);
            btn.rect.localScale = Vector3.one * p;
            btn.rect.anchoredPosition = Vector2.Lerp(startPos, btn.basePos, p);
            btn.cg.alpha = p;
            yield return null;
        }
        btn.rect.localScale = Vector3.one;
        btn.rect.anchoredPosition = btn.basePos;
        btn.cg.alpha = 1f;
    }

    void AnimateParticles()
    {
        float dt = Time.deltaTime;
        float time = Time.time;
        foreach (var p in particles)
        {
            var anchor = p.rect.anchorMin;
            anchor.y -= p.speed * dt * 0.00022f;
            anchor.x = p.baseX + Mathf.Sin(time * p.swaySpeed + p.phase) * p.sway * 0.0005f;
            p.rect.Rotate(0f, 0f, p.rot * dt);
            if (anchor.y < -0.06f)
            {
                anchor.y = 1.1f;
                anchor.x = Random.Range(0.05f, 0.95f);
                p.baseX = anchor.x;
            }
            p.rect.anchorMin = p.rect.anchorMax = anchor;
        }
    }
    Sprite CreateGradientSprite(int w, int h, Color bottom, Color top)
    {
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bottom, top, (float)y / h);
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 100);
    }

    Sprite CreateVignetteSprite(int size)
    {
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
        float r = size / 2f;
        Vector2 c = Vector2.one * r;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / r;
                tex.SetPixel(x, y, new Color(0, 0, 0, Mathf.Clamp01(d * d * 0.55f)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100);
    }

    Sprite CreateRoundedRect(int w, int h, int rad)
    {
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, new Color(1, 1, 1, GetRRA(x, y, w, h, rad)));
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, new Vector4(rad, rad, rad, rad));
    }

    Sprite CreateLeftRoundedRect(int w, int h, int rad)
    {
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float a = 1f;
                if (x < rad && y < rad) a = CA(x, y, rad, rad, rad);
                else if (x < rad && y >= h - rad) a = CA(x, y, rad, h - rad - 1, rad);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 100, 0, SpriteMeshType.FullRect, new Vector4(rad, 0, rad, 0));
    }

    float GetRRA(int x, int y, int w, int h, int rad)
    {
        if (x < rad && y < rad) return CA(x, y, rad, rad, rad);
        if (x >= w - rad && y < rad) return CA(x, y, w - rad - 1, rad, rad);
        if (x < rad && y >= h - rad) return CA(x, y, rad, h - rad - 1, rad);
        if (x >= w - rad && y >= h - rad) return CA(x, y, w - rad - 1, h - rad - 1, rad);
        return 1f;
    }

    float CA(int x, int y, int cx, int cy, int r) =>
        Mathf.Clamp01(r - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) + 1.5f);

    Sprite CreateSmoothRect(int w, int h)
    {
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 100);
    }

    Sprite CreateCircle(int size)
    {
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
        float r = size / 2f;
        Vector2 c = Vector2.one * r;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((r - Vector2.Distance(new Vector2(x, y), c) + 1f) * 2f / size * size)));
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100);
    }
    Sprite CreateLeafSprite(int size)
    {
        int w = size;
        int h = (int)(size * 0.6f);
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear };
        ClearTex(tex);
        for (int x = 0; x < w; x++)
        {
            float xN = (float)x / w;
            float leafW = Mathf.Sin(xN * Mathf.PI) * h * 0.45f;
            int cy = h / 2;
            for (int y = cy - (int)leafW; y <= cy + (int)leafW; y++)
                if (y >= 0 && y < h)
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - Mathf.Abs(y - cy) / leafW) * 1.8f)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 100);
    }

    Sprite CreateWheatGrain(int size)
    {
        int w = (int)(size * 0.45f);
        int h = size;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear };
        ClearTex(tex);
        float cx = w / 2f;
        for (int y = 0; y < h; y++)
        {
            float yN = (float)y / h;
            float width = Mathf.Sin(yN * Mathf.PI) * w * 0.42f;
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - cx);
                if (dx < width) tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((width - dx + 1f) * 0.7f)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 100);
    }

    Sprite CreateSparkle(int size)
    {
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
        float c = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d * d)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100);
    }

    Sprite CreateWheatBundle(int size)
    {
        int w = (int)(size * 0.65f);
        int h = size;
        var tex = new Texture2D(w, h) { filterMode = FilterMode.Bilinear };
        ClearTex(tex);
        int cx = w / 2;
        for (int y = 0; y < h * 0.55f; y++)
            for (int x = cx - 2; x <= cx + 2; x++)
                if (x >= 0 && x < w) tex.SetPixel(x, y, Color.white);
        DrawOval(tex, cx - 7, (int)(h * 0.68f), 5, 9);
        DrawOval(tex, cx + 7, (int)(h * 0.62f), 5, 9);
        DrawOval(tex, cx - 4, (int)(h * 0.8f), 4, 8);
        DrawOval(tex, cx + 4, (int)(h * 0.75f), 4, 8);
        DrawOval(tex, cx, (int)(h * 0.88f), 4, 7);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0), 100);
    }

    Sprite CreateCrossedSwords(int size)
    {
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
        ClearTex(tex);
        int thick = size / 10;
        for (int i = 0; i < size; i++)
        {
            DrawTP(tex, i, i, thick);
            DrawTP(tex, i, size - 1 - i, thick);
        }
        DrawRect(tex, 0, 0, size / 5, size / 5);
        DrawRect(tex, size - size / 5, size - size / 5, size / 5, size / 5);
        DrawRect(tex, size - size / 5, 0, size / 5, size / 5);
        DrawRect(tex, 0, size - size / 5, size / 5, size / 5);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100);
    }

    Sprite CreateSwordIcon(int size)
    {
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
        ClearTex(tex);
        int cx = size / 2;
        int thick = size / 9;
        for (int y = size / 4; y < size; y++)
        {
            int w = thick;
            if (y > size * 0.85f)
                w = (int)(thick * (1f - (y - size * 0.85f) / (size * 0.15f)));
            for (int x = cx - w; x <= cx + w; x++)
                if (x >= 0 && x < size) tex.SetPixel(x, y, Color.white);
        }
        for (int x = cx - size / 4; x <= cx + size / 4; x++)
            for (int y = size / 4 - thick; y <= size / 4 + thick; y++)
                if (x >= 0 && x < size && y >= 0 && y < size) tex.SetPixel(x, y, Color.white);
        for (int y = 0; y < size / 4; y++)
            for (int x = cx - thick / 2; x <= cx + thick / 2; x++)
                if (x >= 0 && x < size) tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100);
    }

    Sprite CreateBookIcon(int size)
    {
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
        ClearTex(tex);
        int m = size / 7;
        for (int y = m; y < size - m; y++)
            for (int x = m; x < size - m; x++) tex.SetPixel(x, y, Color.white);
        for (int y = m; y < size - m; y++)
            for (int x = m; x < m + size / 9; x++) tex.SetPixel(x, y, new Color(0.65f, 0.65f, 0.65f, 1));
        for (int i = 1; i <= 3; i++)
        {
            int ly = m + i * (size - 2 * m) / 4;
            for (int x = m + size / 7; x < size - m - size / 7; x++)
                tex.SetPixel(x, ly, new Color(0.75f, 0.75f, 0.75f, 1));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100);
    }

    Sprite CreateGearIcon(int size)
    {
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
        ClearTex(tex);
        float c = size / 2f;
        float outerR = size * 0.42f;
        float innerR = size * 0.22f;
        float holeR = size * 0.1f;
        int teeth = 8;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);
                float toothOffset = Mathf.Sin(angle * teeth) * 0.5f + 0.5f;
                float r = Mathf.Lerp(innerR, outerR, toothOffset);
                if (d < r && d > holeR) tex.SetPixel(x, y, Color.white);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100);
    }

    Sprite CreatePeopleIcon(int size)
    {
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
        ClearTex(tex);
        DrawOval(tex, size / 3, (int)(size * 0.72f), size / 9, size / 9);
        DrawOval(tex, size / 3, (int)(size * 0.38f), size / 7, size / 5);
        DrawOval(tex, 2 * size / 3, (int)(size * 0.72f), size / 9, size / 9);
        DrawOval(tex, 2 * size / 3, (int)(size * 0.38f), size / 7, size / 5);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100);
    }

    Sprite CreateExitIcon(int size)
    {
        var tex = new Texture2D(size, size) { filterMode = FilterMode.Bilinear };
        ClearTex(tex);
        int thick = size / 7;
        int m = size / 5;
        for (int i = 0; i < size - 2 * m; i++)
        {
            DrawTP(tex, m + i, m + i, thick);
            DrawTP(tex, m + i, size - m - 1 - i, thick);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100);
    }
    void ClearTex(Texture2D tex)
    {
        Color c = new Color(0, 0, 0, 0);
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
                tex.SetPixel(x, y, c);
    }

    void DrawOval(Texture2D tex, int cx, int cy, int rx, int ry)
    {
        for (int y = cy - ry; y <= cy + ry; y++)
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;
                float dx = (float)(x - cx) / rx;
                float dy = (float)(y - cy) / ry;
                if (dx * dx + dy * dy <= 1) tex.SetPixel(x, y, Color.white);
            }
    }

    void DrawRect(Texture2D tex, int sx, int sy, int w, int h)
    {
        for (int y = sy; y < sy + h && y < tex.height; y++)
            for (int x = sx; x < sx + w && x < tex.width; x++)
                if (x >= 0 && y >= 0) tex.SetPixel(x, y, Color.white);
    }

    void DrawTP(Texture2D tex, int cx, int cy, int r)
    {
        for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
                if (x >= 0 && x < tex.width && y >= 0 && y < tex.height) tex.SetPixel(x, y, Color.white);
    }

    GameObject CreateUI(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1;
        return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
    }

    void PlaySFX(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
    }

    void OnPlay() => StartCoroutine(TransitionTo(gameSceneName));
    void OnContinue() => Debug.Log("Continue");
    void OnSettings() => Debug.Log("Settings");
    void OnCredits() => Debug.Log("Credits");
    void OnQuit() => StartCoroutine(QuitAnim());

    IEnumerator TransitionTo(string scene)
    {
        var fade = CreateUI("Fade", mainCanvas.transform);
        var fadeImg = fade.AddComponent<Image>();
        fadeImg.color = new Color(0, 0, 0, 0);
        Stretch(fade.GetComponent<RectTransform>());
        float t = 0f;
        float dur = 0.35f;
        while (t < dur)
        {
            t += Time.deltaTime;
            fadeImg.color = new Color(0, 0, 0, t / dur);
            musicSource.volume = musicVolume * (1 - t / dur);
            yield return null;
        }
        SceneManager.LoadScene(scene);
    }

    IEnumerator QuitAnim()
    {
        var fade = CreateUI("Fade", mainCanvas.transform);
        var fadeImg = fade.AddComponent<Image>();
        fadeImg.color = new Color(0, 0, 0, 0);
        Stretch(fade.GetComponent<RectTransform>());
        float t = 0f;
        float dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            fadeImg.color = new Color(0, 0, 0, t / dur);
            yield return null;
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
