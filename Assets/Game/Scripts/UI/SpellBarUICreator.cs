using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Spell Bar UI Creator - Creates visible spell slots at screen bottom
/// </summary>
public class SpellBarUICreator : MonoBehaviour
{
    [Header("═══ SETTINGS ═══")]
    [SerializeField] private Vector2 slotSize = new Vector2(45, 45); // Smaller slots
    [SerializeField] private float spacing = 6f;
    [SerializeField] private float bottomMargin = 90f; // Above inventory bar

    [Header("═══ COLORS ═══")]
    [SerializeField] private Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    [SerializeField] private Color borderColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.8f, 0.2f, 1f);

    private Canvas rootCanvas;
    private RectTransform spellBarRect;
    private SpellSlotUI.SlotUI[] createdSlots;

    void Start()
    {
        Debug.Log("[SpellBarUICreator] === START CALLED ===");
        
        // Find root canvas
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogError("[SpellBarUICreator] ERROR: No Canvas found!");
            return;
        }
        
        Debug.Log($"[SpellBarUICreator] Found canvas: {rootCanvas.name}");
        
        // Delete any existing SpellBar first
        Transform existing = rootCanvas.transform.Find("SpellBar_Runtime");
        if (existing != null)
        {
            Debug.Log("[SpellBarUICreator] Deleting old SpellBar_Runtime");
            Destroy(existing.gameObject);
        }

        CreateUI();
    }

    void CreateUI()
    {
        // Create main container directly on Canvas
        GameObject container = new GameObject("SpellBar_Runtime");
        container.transform.SetParent(rootCanvas.transform, false);
        
        spellBarRect = container.AddComponent<RectTransform>();
        
        // Calculate size
        float totalWidth = (slotSize.x * 4) + (spacing * 3) + 20; // +20 for padding
        float totalHeight = slotSize.y + 10;
        
        // FORCE bottom center positioning
        spellBarRect.anchorMin = new Vector2(0.5f, 0f);
        spellBarRect.anchorMax = new Vector2(0.5f, 0f);
        spellBarRect.pivot = new Vector2(0.5f, 0f);
        spellBarRect.sizeDelta = new Vector2(totalWidth, totalHeight);
        spellBarRect.anchoredPosition = new Vector2(0, bottomMargin);
        
        Debug.Log($"[SpellBarUICreator] Bar rect: size={spellBarRect.sizeDelta}, pos={spellBarRect.anchoredPosition}");

        // Add background image to container
        Image bgImage = container.AddComponent<Image>();
        bgImage.color = backgroundColor;

        // Add layout
        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        // Create 4 slots
        createdSlots = new SpellSlotUI.SlotUI[4];
        for (int i = 0; i < 4; i++)
        {
            createdSlots[i] = CreateSlot(container.transform, i);
        }

        // Setup the SpellSlotUI component
        SpellSlotUI slotUI = container.AddComponent<SpellSlotUI>();
        slotUI.RuntimeSetup(createdSlots[0], createdSlots[1], createdSlots[2], createdSlots[3]);

        Debug.Log("[SpellBarUICreator] ✅ UI Creation complete!");
    }

    SpellSlotUI.SlotUI CreateSlot(Transform parent, int index)
    {
        // Slot root
        GameObject slotObj = new GameObject($"Slot_{index + 1}");
        slotObj.transform.SetParent(parent, false);
        
        RectTransform slotRect = slotObj.AddComponent<RectTransform>();
        slotRect.sizeDelta = slotSize;

        // Background
        Image bg = slotObj.AddComponent<Image>();
        bg.color = borderColor;

        // Icon area (child)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.sizeDelta = Vector2.zero;
        iconRect.anchoredPosition = Vector2.zero;
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.color = new Color(1, 1, 1, 0.3f);
        iconImg.raycastTarget = false;

        // Cooldown overlay
        GameObject cdObj = new GameObject("Cooldown");
        cdObj.transform.SetParent(slotObj.transform, false);
        RectTransform cdRect = cdObj.AddComponent<RectTransform>();
        cdRect.anchorMin = Vector2.zero;
        cdRect.anchorMax = Vector2.one;
        cdRect.sizeDelta = Vector2.zero;
        Image cdImg = cdObj.AddComponent<Image>();
        cdImg.color = new Color(0, 0, 0, 0.6f);
        cdImg.type = Image.Type.Filled;
        cdImg.fillMethod = Image.FillMethod.Vertical;
        cdImg.fillAmount = 0;
        cdImg.raycastTarget = false;

        // Key hint text
        GameObject keyObj = new GameObject("KeyHint");
        keyObj.transform.SetParent(slotObj.transform, false);
        TextMeshProUGUI keyText = keyObj.AddComponent<TextMeshProUGUI>();
        keyText.text = (index + 1).ToString();
        keyText.fontSize = 14;
        keyText.alignment = TextAlignmentOptions.BottomLeft;
        keyText.color = Color.white;
        keyText.raycastTarget = false;
        RectTransform keyRect = keyText.rectTransform;
        keyRect.anchorMin = Vector2.zero;
        keyRect.anchorMax = new Vector2(0.4f, 0.4f);
        keyRect.sizeDelta = Vector2.zero;
        keyRect.anchoredPosition = new Vector2(4, 2);

        // Cooldown text
        GameObject cdTextObj = new GameObject("CooldownText");
        cdTextObj.transform.SetParent(slotObj.transform, false);
        TextMeshProUGUI cdText = cdTextObj.AddComponent<TextMeshProUGUI>();
        cdText.text = "";
        cdText.fontSize = 16;
        cdText.alignment = TextAlignmentOptions.Center;
        cdText.color = Color.white;
        cdText.fontStyle = FontStyles.Bold;
        cdText.raycastTarget = false;
        RectTransform cdTextRect = cdText.rectTransform;
        cdTextRect.anchorMin = Vector2.zero;
        cdTextRect.anchorMax = Vector2.one;
        cdTextRect.sizeDelta = Vector2.zero;

        // Border/selection
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(slotObj.transform, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = index == 0 ? selectedColor : new Color(0.5f, 0.5f, 0.5f, 0.3f);
        borderImg.raycastTarget = false;

        // Button
        Button btn = slotObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        return new SpellSlotUI.SlotUI
        {
            root = slotRect,
            icon = iconImg,
            cooldownOverlay = cdImg,
            cooldownText = cdText,
            keyHintText = keyText,
            selectionBorder = borderImg,
            selectButton = btn
        };
    }
}
