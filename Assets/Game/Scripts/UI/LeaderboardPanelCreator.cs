using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Leaderboard Panel Creator
/// Programmatically creates the leaderboard UI
/// - Rankings sorted by days survived
/// - Current player highlighted
/// - Toggle with Tab key
/// </summary>
public class LeaderboardPanelCreator : MonoBehaviour
{
    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ VISUAL SETTINGS ═══")]
    [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.7f);
    [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.98f);
    [SerializeField] private Color headerColor = new Color(0.08f, 0.08f, 0.12f, 1f);
    [SerializeField] private Color accentColor = new Color(0.4f, 0.7f, 0.3f, 1f);
    [SerializeField] private Color textColor = new Color(0.9f, 0.85f, 0.7f, 1f);
    [SerializeField] private Color rowColor1 = new Color(0.12f, 0.12f, 0.16f, 0.9f);
    [SerializeField] private Color rowColor2 = new Color(0.14f, 0.14f, 0.18f, 0.9f);
    [SerializeField] private Color currentUserColor = new Color(0.2f, 0.35f, 0.2f, 0.95f);

    [Header("═══ SIZE ═══")]
    [SerializeField] private Vector2 panelSize = new Vector2(800, 600);

    [Header("═══ AUTO CREATE ═══")]
    [SerializeField] private bool createOnStart = true;

    #endregion

    #region ═══════ RUNTIME REFERENCES ═══════

    private Canvas canvas;
    private GameObject overlayObj;
    private GameObject panelObj;
    private GameObject contentObj;
    private Transform entriesContainer;
    private TextMeshProUGUI currentRankText;
    private Button closeButton;
    private Button refreshButton;
    private LeaderboardUI uiController;
    private GameObject entryPrefab;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    private void Start()
    {
        if (createOnStart)
        {
            CreateLeaderboardPanel();
        }
    }

    #endregion

    #region ═══════ UI CREATION ═══════

    /// <summary>
    /// Create the leaderboard panel
    /// </summary>
    [ContextMenu("Create Leaderboard Panel")]
    public void CreateLeaderboardPanel()
    {
        // Create canvas
        CreateCanvas();

        // Create overlay
        CreateOverlay();

        // Create main panel
        CreatePanel();

        // Create entry prefab
        CreateEntryPrefab();

        // Add controller
        WireController();

        // Start hidden
        overlayObj.SetActive(false);

        Debug.Log("[LeaderboardPanelCreator] ✅ Leaderboard panel created successfully");
    }

    private void CreateCanvas()
    {
        var canvasObj = new GameObject("LeaderboardCanvas");
        canvasObj.transform.SetParent(transform);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void CreateOverlay()
    {
        overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(canvas.transform, false);

        var overlayRect = overlayObj.AddComponent<RectTransform>();
        StretchFull(overlayRect);

        var overlayImage = overlayObj.AddComponent<Image>();
        overlayImage.color = overlayColor;

        // Click overlay to close
        var overlayBtn = overlayObj.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
    }

    private void CreatePanel()
    {
        panelObj = new GameObject("LeaderboardPanel");
        panelObj.transform.SetParent(overlayObj.transform, false);

        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = panelSize;

        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = panelColor;

        // Vertical layout for panel contents
        var layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 0;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Header
        CreateHeader(panelObj.transform);

        // Column headers
        CreateColumnHeaders(panelObj.transform);

        // Scrollable content
        CreateScrollContent(panelObj.transform);

        // Footer with current rank
        CreateFooter(panelObj.transform);
    }

    private void CreateHeader(Transform parent)
    {
        var headerObj = new GameObject("Header");
        headerObj.transform.SetParent(parent, false);

        var headerRect = headerObj.AddComponent<RectTransform>();
        var headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 60;

        var headerImage = headerObj.AddComponent<Image>();
        headerImage.color = headerColor;

        // Horizontal layout
        var hLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding = new RectOffset(20, 20, 10, 10);
        hLayout.childAlignment = TextAnchor.MiddleCenter;

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(headerObj.transform, false);

        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "🏆 LEADERBOARD";
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = accentColor;
        titleText.alignment = TextAlignmentOptions.Center;

        var titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1;

        // Refresh button
        CreateHeaderButton(headerObj.transform, "↻", out refreshButton);

        // Close button
        CreateHeaderButton(headerObj.transform, "✕", out closeButton);
    }

    private void CreateHeaderButton(Transform parent, string label, out Button button)
    {
        var btnObj = new GameObject($"Button_{label}");
        btnObj.transform.SetParent(parent, false);

        var btnRect = btnObj.AddComponent<RectTransform>();
        var btnLayout = btnObj.AddComponent<LayoutElement>();
        btnLayout.preferredWidth = 40;
        btnLayout.preferredHeight = 40;

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);

        button = btnObj.AddComponent<Button>();
        button.targetGraphic = btnImage;

        var colors = button.colors;
        colors.highlightedColor = accentColor * 0.7f;
        colors.pressedColor = accentColor * 0.5f;
        button.colors = colors;

        // Button text
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        StretchFull(textRect);

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 24;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
    }

    private void CreateColumnHeaders(Transform parent)
    {
        var headerObj = new GameObject("ColumnHeaders");
        headerObj.transform.SetParent(parent, false);

        var headerRect = headerObj.AddComponent<RectTransform>();
        var headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 35;

        var headerImage = headerObj.AddComponent<Image>();
        headerImage.color = headerColor * 0.8f;

        // Horizontal layout
        var hLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding = new RectOffset(15, 15, 5, 5);
        hLayout.spacing = 10;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childControlWidth = false;
        hLayout.childForceExpandWidth = false;

        // Create column headers
        CreateColumnLabel(headerObj.transform, "RANK", 60);
        CreateColumnLabel(headerObj.transform, "PLAYER", 180);
        CreateColumnLabel(headerObj.transform, "LEVEL", 70);
        CreateColumnLabel(headerObj.transform, "GOLD", 100);
        CreateColumnLabel(headerObj.transform, "SPELLS", 70);
        CreateColumnLabel(headerObj.transform, "DAYS", 80);
    }

    private void CreateColumnLabel(Transform parent, string text, float width)
    {
        var labelObj = new GameObject(text);
        labelObj.transform.SetParent(parent, false);

        var layout = labelObj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;

        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 14;
        label.fontStyle = FontStyles.Bold;
        label.color = textColor * 0.6f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void CreateScrollContent(Transform parent)
    {
        // Scroll view container
        var scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(parent, false);

        var scrollRect = scrollObj.AddComponent<RectTransform>();
        var scrollLayout = scrollObj.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1;
        scrollLayout.preferredHeight = 400;

        var scrollView = scrollObj.AddComponent<ScrollRect>();
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        var viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);

        var viewportRect = viewportObj.AddComponent<RectTransform>();
        StretchFull(viewportRect);

        var viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = Color.clear;

        var mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        scrollView.viewport = viewportRect;

        // Content
        contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);

        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;

        var contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 2;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        var contentFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollView.content = contentRect;
        entriesContainer = contentObj.transform;
    }

    private void CreateFooter(Transform parent)
    {
        var footerObj = new GameObject("Footer");
        footerObj.transform.SetParent(parent, false);

        var footerRect = footerObj.AddComponent<RectTransform>();
        var footerLayout = footerObj.AddComponent<LayoutElement>();
        footerLayout.preferredHeight = 50;

        var footerImage = footerObj.AddComponent<Image>();
        footerImage.color = headerColor;

        // Current rank text
        var rankObj = new GameObject("CurrentRank");
        rankObj.transform.SetParent(footerObj.transform, false);

        var rankRect = rankObj.AddComponent<RectTransform>();
        StretchFull(rankRect);

        currentRankText = rankObj.AddComponent<TextMeshProUGUI>();
        currentRankText.text = "Your Rank: #1";
        currentRankText.fontSize = 20;
        currentRankText.fontStyle = FontStyles.Bold;
        currentRankText.color = accentColor;
        currentRankText.alignment = TextAlignmentOptions.Center;
    }

    private void CreateEntryPrefab()
    {
        // Create a template entry (will be cloned)
        entryPrefab = new GameObject("EntryTemplate");
        entryPrefab.transform.SetParent(transform, false);

        var entryRect = entryPrefab.AddComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(0, 40);

        var entryImage = entryPrefab.AddComponent<Image>();
        entryImage.color = rowColor1;

        var entryLayout = entryPrefab.AddComponent<LayoutElement>();
        entryLayout.preferredHeight = 40;

        // Horizontal layout
        var hLayout = entryPrefab.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding = new RectOffset(15, 15, 5, 5);
        hLayout.spacing = 10;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childControlWidth = false;
        hLayout.childForceExpandWidth = false;

        // Create entry columns
        CreateEntryText(entryPrefab.transform, "Rank", 60, "#1");
        CreateEntryText(entryPrefab.transform, "Username", 180, "Player");
        CreateEntryText(entryPrefab.transform, "Level", 70, "Lv.1");
        CreateEntryText(entryPrefab.transform, "Gold", 100, "0");
        CreateEntryText(entryPrefab.transform, "Spells", 70, "0");
        CreateEntryText(entryPrefab.transform, "Days", 80, "Day 1");

        entryPrefab.SetActive(false);
    }

    private void CreateEntryText(Transform parent, string name, float width, string defaultText)
    {
        var textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        var layout = textObj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = defaultText;
        text.fontSize = 16;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void WireController()
    {
        // Add LeaderboardUI controller to the panel
        uiController = panelObj.AddComponent<LeaderboardUI>();

        // Wire close button to hide overlay
        closeButton.onClick.AddListener(() => overlayObj.SetActive(false));
        refreshButton.onClick.AddListener(() => uiController.RefreshLeaderboard());

        // Wire overlay click to close
        overlayObj.GetComponent<Button>().onClick.AddListener(() => overlayObj.SetActive(false));
    }

    #endregion

    #region ═══════ HELPERS ═══════

    private void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void Show()
    {
        if (overlayObj != null)
        {
            overlayObj.SetActive(true);
            uiController?.RefreshLeaderboard();
        }
    }

    public void Hide()
    {
        if (overlayObj != null)
        {
            overlayObj.SetActive(false);
        }
    }

    public void Toggle()
    {
        if (overlayObj != null)
        {
            if (overlayObj.activeSelf)
                Hide();
            else
                Show();
        }
    }

    #endregion

    #region ═══════ PUBLIC ACCESS ═══════

    public Transform GetEntriesContainer() => entriesContainer;
    public GameObject GetEntryPrefab() => entryPrefab;
    public TextMeshProUGUI GetCurrentRankText() => currentRankText;
    public LeaderboardUI GetController() => uiController;

    #endregion
}
