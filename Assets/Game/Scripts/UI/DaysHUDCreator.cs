using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Days HUD Creator
/// Programmatically creates the Days Survived HUD element
/// - Day counter display
/// - Animated on day change
/// - Positioned in top-left corner
/// </summary>
public class DaysHUDCreator : MonoBehaviour
{
    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ POSITION ═══")]
    [SerializeField] private Vector2 anchorPosition = new Vector2(0, 1); // Top-left
    [SerializeField] private Vector2 offset = new Vector2(20, -20);

    [Header("═══ VISUAL SETTINGS ═══")]
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.85f);
    [SerializeField] private Color textColor = new Color(1f, 0.9f, 0.6f, 1f);
    [SerializeField] private Color iconColor = new Color(1f, 0.8f, 0.3f, 1f);
    [SerializeField] private float fontSize = 24f;

    [Header("═══ AUTO CREATE ═══")]
    [SerializeField] private bool createOnStart = true;

    #endregion

    #region ═══════ RUNTIME REFERENCES ═══════

    private Canvas canvas;
    private GameObject hudPanel;
    private TextMeshProUGUI dayText;
    private Image sunIcon;
    private DaysSurvivedUI uiController;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    private void Start()
    {
        if (createOnStart)
        {
            CreateDaysHUD();
        }
    }

    #endregion

    #region ═══════ UI CREATION ═══════

    /// <summary>
    /// Create the Days HUD
    /// </summary>
    [ContextMenu("Create Days HUD")]
    public void CreateDaysHUD()
    {
        // Find or create canvas
        canvas = FindOrCreateHUDCanvas();

        // Create HUD panel
        CreateHUDPanel();

        // Add controller
        WireController();

        Debug.Log("[DaysHUDCreator] ✅ Days HUD created successfully");
    }

    private Canvas FindOrCreateHUDCanvas()
    {
        // Try to find existing HUD canvas
        var existingCanvas = GameObject.Find("GameHUDCanvas");
        if (existingCanvas != null)
        {
            return existingCanvas.GetComponent<Canvas>();
        }

        // Create new canvas
        var canvasObj = new GameObject("GameHUDCanvas");
        canvasObj.transform.SetParent(transform);

        var newCanvas = canvasObj.AddComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        newCanvas.sortingOrder = 50;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        return newCanvas;
    }

    private void CreateHUDPanel()
    {
        // Panel container
        hudPanel = new GameObject("DaysHUD");
        hudPanel.transform.SetParent(canvas.transform, false);

        var panelRect = hudPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = anchorPosition;
        panelRect.anchorMax = anchorPosition;
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = offset;
        panelRect.sizeDelta = new Vector2(160, 50);

        var panelImage = hudPanel.AddComponent<Image>();
        panelImage.color = backgroundColor;

        // Horizontal layout
        var layout = hudPanel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        // Sun icon
        CreateSunIcon(hudPanel.transform);

        // Day text
        CreateDayText(hudPanel.transform);
    }

    private void CreateSunIcon(Transform parent)
    {
        var iconObj = new GameObject("SunIcon");
        iconObj.transform.SetParent(parent, false);

        var iconRect = iconObj.AddComponent<RectTransform>();
        var iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 32;
        iconLayout.preferredHeight = 32;

        sunIcon = iconObj.AddComponent<Image>();
        sunIcon.color = iconColor;

        // Create simple sun sprite
        sunIcon.sprite = CreateSunSprite();
    }

    private void CreateDayText(Transform parent)
    {
        var textObj = new GameObject("DayText");
        textObj.transform.SetParent(parent, false);

        var textRect = textObj.AddComponent<RectTransform>();
        var textLayout = textObj.AddComponent<LayoutElement>();
        textLayout.preferredWidth = 90;
        textLayout.flexibleWidth = 1;

        dayText = textObj.AddComponent<TextMeshProUGUI>();
        dayText.text = "Day 1";
        dayText.fontSize = fontSize;
        dayText.fontStyle = FontStyles.Bold;
        dayText.color = textColor;
        dayText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private Sprite CreateSunSprite()
    {
        // Create simple sun texture
        int size = 64;
        var texture = new Texture2D(size, size);
        Color transparent = new Color(0, 0, 0, 0);

        // Clear
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                texture.SetPixel(x, y, transparent);

        // Draw circle (sun body)
        int centerX = size / 2;
        int centerY = size / 2;
        int radius = size / 3;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (dist <= radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
        }

        // Draw rays
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4;
            int startR = radius + 4;
            int endR = radius + 12;

            for (int r = startR; r < endR; r++)
            {
                int px = centerX + Mathf.RoundToInt(Mathf.Cos(angle) * r);
                int py = centerY + Mathf.RoundToInt(Mathf.Sin(angle) * r);

                if (px >= 0 && px < size && py >= 0 && py < size)
                {
                    texture.SetPixel(px, py, Color.white);
                    // Thicken rays
                    if (px + 1 < size) texture.SetPixel(px + 1, py, Color.white);
                    if (py + 1 < size) texture.SetPixel(px, py + 1, Color.white);
                }
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void WireController()
    {
        // Add DaysSurvivedUI controller
        uiController = hudPanel.AddComponent<DaysSurvivedUI>();

        // The controller will find the text component automatically
        // Or we can use reflection/setup method
    }

    #endregion

    #region ═══════ PUBLIC ACCESS ═══════

    public TextMeshProUGUI GetDayText() => dayText;
    public Image GetSunIcon() => sunIcon;
    public DaysSurvivedUI GetController() => uiController;

    #endregion
}
