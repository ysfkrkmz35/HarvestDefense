#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Profesyonel Health & Mana Bar Oluşturucu
/// Özellikler: Delayed damage bar, glow efektleri, ikonlar, segment çizgileri
/// Kullanım: Tools > Create Pro Health Bars
/// </summary>
public class HealthBarCreator : Editor
{
    // Renk paleti
    private static readonly Color FRAME_DARK = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    private static readonly Color FRAME_LIGHT = new Color(0.25f, 0.25f, 0.3f, 1f);
    private static readonly Color FRAME_GOLD = new Color(0.85f, 0.7f, 0.4f, 1f);
    
    private static readonly Color HEALTH_FULL = new Color(0.3f, 0.95f, 0.4f, 1f);
    private static readonly Color HEALTH_GLOW = new Color(0.4f, 1f, 0.5f, 0.6f);
    private static readonly Color DAMAGE_BAR = new Color(1f, 0.3f, 0.2f, 0.8f);
    
    private static readonly Color MANA_FILL = new Color(0.3f, 0.6f, 1f, 1f);
    private static readonly Color MANA_GLOW = new Color(0.4f, 0.7f, 1f, 0.6f);

    [MenuItem("Tools/Create Pro Health Bars")]
    public static void CreateHealthManaBars()
    {
        // Canvas bul veya oluştur
        Canvas canvas = FindOrCreateCanvas();

        // Ana container
        GameObject container = CreateContainer(canvas.transform);
        
        // Health Bar
        GameObject healthBar = CreateProBar(container.transform, "HealthBar", 0, true);
        
        // Mana Bar  
        GameObject manaBar = CreateProBar(container.transform, "ManaBar", -50, false);

        // UI Manager ekle
        ProHealthManaUI uiManager = container.AddComponent<ProHealthManaUI>();
        AssignReferences(uiManager, healthBar, manaBar);

        Selection.activeGameObject = container;
        Undo.RegisterCreatedObjectUndo(container, "Create Pro Health Bars");

        Debug.Log("✨ Profesyonel Health & Mana Bars oluşturuldu!");
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
        return canvas;
    }

    private static GameObject CreateContainer(Transform parent)
    {
        GameObject container = new GameObject("ProPlayerBarsUI");
        container.transform.SetParent(parent, false);
        
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(30, -30);
        rect.sizeDelta = new Vector2(320, 120);

        return container;
    }

    private static GameObject CreateProBar(Transform parent, string name, float yOffset, bool isHealth)
    {
        // Ana bar container
        GameObject bar = new GameObject(name);
        bar.transform.SetParent(parent, false);
        
        RectTransform barRect = bar.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 1);
        barRect.anchorMax = new Vector2(1, 1);
        barRect.pivot = new Vector2(0, 1);
        barRect.anchoredPosition = new Vector2(0, yOffset);
        barRect.sizeDelta = new Vector2(0, 44);

        // === ICON ===
        GameObject iconBg = CreateElement("IconBg", bar.transform);
        Image iconBgImg = iconBg.AddComponent<Image>();
        iconBgImg.color = FRAME_DARK;
        SetRect(iconBg, 0, 0.5f, 0, 0.5f, 0, 0.5f, Vector2.zero, new Vector2(44, 44));
        
        // Icon glow
        GameObject iconGlow = CreateElement("IconGlow", iconBg.transform);
        Image iconGlowImg = iconGlow.AddComponent<Image>();
        iconGlowImg.color = isHealth ? new Color(1f, 0.4f, 0.4f, 0.4f) : new Color(0.4f, 0.6f, 1f, 0.4f);
        SetRectStretch(iconGlow, 4, 4, -4, -4);
        
        // Icon symbol (TextMeshPro ile)
        GameObject iconSymbol = CreateElement("IconSymbol", iconBg.transform);
        TextMeshProUGUI iconText = iconSymbol.AddComponent<TextMeshProUGUI>();
        iconText.text = isHealth ? "♥" : "✦";
        iconText.fontSize = 24;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = isHealth ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(0.5f, 0.7f, 1f, 1f);
        SetRectStretch(iconSymbol, 0, 0, 0, 0);

        // === OUTER FRAME ===
        GameObject outerFrame = CreateElement("OuterFrame", bar.transform);
        Image outerImg = outerFrame.AddComponent<Image>();
        outerImg.color = FRAME_LIGHT;
        SetRect(outerFrame, 0, 0, 1, 1, 0, 0.5f, new Vector2(50, 0), new Vector2(-8, 0));
        
        // === INNER FRAME (Dark background) ===
        GameObject innerFrame = CreateElement("InnerFrame", outerFrame.transform);
        Image innerImg = innerFrame.AddComponent<Image>();
        innerImg.color = FRAME_DARK;
        SetRectStretch(innerFrame, 2, 2, -2, -2);

        // === DAMAGE BAR (Delayed damage göstergesi - sadece health için) ===
        if (isHealth)
        {
            GameObject damageBar = CreateElement("DamageBar", innerFrame.transform);
            Image damageImg = damageBar.AddComponent<Image>();
            damageImg.color = DAMAGE_BAR;
            damageImg.raycastTarget = false;
            RectTransform dmgRect = damageBar.GetComponent<RectTransform>();
            dmgRect.anchorMin = Vector2.zero;
            dmgRect.anchorMax = Vector2.one;
            dmgRect.pivot = new Vector2(0, 0.5f);
            dmgRect.offsetMin = new Vector2(4, 4);
            dmgRect.offsetMax = new Vector2(-4, -4);
        }

        // === FILL BAR ===
        GameObject fill = CreateElement("Fill", innerFrame.transform);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = isHealth ? HEALTH_FULL : MANA_FILL;
        fillImg.raycastTarget = false;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.offsetMin = new Vector2(4, 4);
        fillRect.offsetMax = new Vector2(-4, -4);

        // === GLOW OVERLAY ===
        GameObject glow = CreateElement("Glow", innerFrame.transform);
        Image glowImg = glow.AddComponent<Image>();
        glowImg.color = isHealth ? HEALTH_GLOW : MANA_GLOW;
        glowImg.raycastTarget = false;
        RectTransform glowRect = glow.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.pivot = new Vector2(0, 0.5f);
        glowRect.offsetMin = new Vector2(4, 4);
        glowRect.offsetMax = new Vector2(-4, -4);

        // === SHINE (Üst parlama) ===
        GameObject shine = CreateElement("Shine", innerFrame.transform);
        Image shineImg = shine.AddComponent<Image>();
        shineImg.color = new Color(1f, 1f, 1f, 0.12f);
        shineImg.raycastTarget = false;
        RectTransform shineRect = shine.GetComponent<RectTransform>();
        shineRect.anchorMin = new Vector2(0, 0.55f);
        shineRect.anchorMax = new Vector2(1, 1);
        shineRect.offsetMin = new Vector2(4, 0);
        shineRect.offsetMax = new Vector2(-4, -4);

        // === SEGMENT LINES ===
        CreateSegmentLines(innerFrame.transform);

        // === ACCENT LINE (Alt gold çizgi) ===
        GameObject accent = CreateElement("AccentLine", outerFrame.transform);
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = isHealth ? new Color(0.9f, 0.75f, 0.3f, 0.8f) : new Color(0.5f, 0.7f, 1f, 0.6f);
        accentImg.raycastTarget = false;
        SetRect(accent, 0, 0, 1, 0, 0.5f, 0, new Vector2(10, -1), new Vector2(-10, 2));

        // === VALUE TEXT ===
        GameObject valueText = CreateElement("ValueText", bar.transform);
        TextMeshProUGUI tmpText = valueText.AddComponent<TextMeshProUGUI>();
        tmpText.text = "100";
        tmpText.fontSize = 14;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.alignment = TextAlignmentOptions.Right;
        tmpText.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        SetRect(valueText, 1, 0, 1, 1, 1, 0.5f, new Vector2(-12, 0), new Vector2(50, 0));

        return bar;
    }

    private static void CreateSegmentLines(Transform parent)
    {
        float[] positions = { 0.25f, 0.5f, 0.75f };
        
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject line = CreateElement($"Segment_{i}", parent);
            Image lineImg = line.AddComponent<Image>();
            lineImg.color = new Color(0f, 0f, 0f, 0.25f);
            lineImg.raycastTarget = false;
            
            RectTransform lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(positions[i], 0);
            lineRect.anchorMax = new Vector2(positions[i], 1);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(1, 0);
            lineRect.offsetMin = new Vector2(-0.5f, 6);
            lineRect.offsetMax = new Vector2(0.5f, -6);
        }
    }

    private static void AssignReferences(ProHealthManaUI ui, GameObject healthBar, GameObject manaBar)
    {
        Transform hInner = healthBar.transform.Find("OuterFrame/InnerFrame");
        Transform mInner = manaBar.transform.Find("OuterFrame/InnerFrame");

        ui.healthFill = hInner.Find("Fill").GetComponent<Image>();
        ui.healthGlow = hInner.Find("Glow").GetComponent<Image>();
        ui.healthDamageBar = hInner.Find("DamageBar").GetComponent<Image>();
        ui.healthText = healthBar.transform.Find("ValueText").GetComponent<TextMeshProUGUI>();
        ui.healthIconGlow = healthBar.transform.Find("IconBg/IconGlow").GetComponent<Image>();

        ui.manaFill = mInner.Find("Fill").GetComponent<Image>();
        ui.manaGlow = mInner.Find("Glow").GetComponent<Image>();
        ui.manaText = manaBar.transform.Find("ValueText").GetComponent<TextMeshProUGUI>();
        ui.manaIconGlow = manaBar.transform.Find("IconBg/IconGlow").GetComponent<Image>();
    }

    #region Helper Methods
    
    private static GameObject CreateElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private static void SetRect(GameObject obj, float anchorMinX, float anchorMinY, 
        float anchorMaxX, float anchorMaxY, float pivotX, float pivotY,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rect.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rect.pivot = new Vector2(pivotX, pivotY);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
    }

    private static void SetRectStretch(GameObject obj, float left, float bottom, float right, float top)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }
    
    #endregion
}
#endif