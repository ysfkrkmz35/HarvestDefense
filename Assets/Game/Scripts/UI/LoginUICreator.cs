using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Login UI Creator
/// Programmatically creates the login screen UI
/// - Username input field
/// - Play button
/// - Error/Status text
/// - Dark fantasy themed design
/// </summary>
public class LoginUICreator : MonoBehaviour
{
    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ SCENE SETTINGS ═══")]
    [Tooltip("Scene to load after login (by name)")]
    [SerializeField] private string gameSceneName = "Game_Main_Scene";

    [Tooltip("Scene to load after login (by index, -1 to use name)")]
    [SerializeField] private int gameSceneIndex = -1;

    [Header("═══ VISUAL SETTINGS ═══")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    [SerializeField] private Color panelColor = new Color(0.12f, 0.12f, 0.18f, 0.98f);
    [SerializeField] private Color accentColor = new Color(0.4f, 0.7f, 0.3f, 1f); // Green accent
    [SerializeField] private Color textColor = new Color(0.9f, 0.85f, 0.7f, 1f); // Warm text
    [SerializeField] private Color inputBgColor = new Color(0.15f, 0.15f, 0.2f, 1f);

    [Header("═══ AUTO CREATE ═══")]
    [SerializeField] private bool createOnStart = true;

    #endregion

    #region ═══════ RUNTIME REFERENCES ═══════

    private Canvas canvas;
    private GameObject loginPanel;
    private TMP_InputField usernameInput;
    private Button playButton;
    private TextMeshProUGUI errorText;
    private TextMeshProUGUI statusText;
    private LoginScreenController controller;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    private void Start()
    {
        if (createOnStart)
        {
            CreateLoginUI();
        }
    }

    #endregion

    #region ═══════ UI CREATION ═══════

    /// <summary>
    /// Create the complete login UI
    /// </summary>
    [ContextMenu("Create Login UI")]
    public void CreateLoginUI()
    {
        // Ensure EventSystem exists
        EnsureEventSystem();

        // Create canvas
        CreateCanvas();

        // Create background overlay
        CreateBackground();

        // Create login panel
        CreateLoginPanel();

        // Add controller and wire references
        WireController();

        Debug.Log("[LoginUICreator] ✅ Login UI created successfully");
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
    }

    private void CreateCanvas()
    {
        // Create canvas GameObject
        var canvasObj = new GameObject("LoginCanvas");
        canvasObj.transform.SetParent(transform);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Above other UI

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void CreateBackground()
    {
        // Full screen dark background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvas.transform, false);

        var bgRect = bgObj.AddComponent<RectTransform>();
        StretchFull(bgRect);

        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = backgroundColor;
    }

    private void CreateLoginPanel()
    {
        // Center panel
        loginPanel = new GameObject("LoginPanel");
        loginPanel.transform.SetParent(canvas.transform, false);

        var panelRect = loginPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500, 400);

        var panelImage = loginPanel.AddComponent<Image>();
        panelImage.color = panelColor;

        // Add rounded corners effect via child
        AddPanelBorder(loginPanel.transform);

        // Vertical layout
        var layout = loginPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Title
        CreateTitle(loginPanel.transform);

        // Subtitle
        CreateSubtitle(loginPanel.transform);

        // Spacer
        CreateSpacer(loginPanel.transform, 20);

        // Username input
        CreateUsernameInput(loginPanel.transform);

        // Status text
        CreateStatusText(loginPanel.transform);

        // Spacer
        CreateSpacer(loginPanel.transform, 10);

        // Play button
        CreatePlayButton(loginPanel.transform);

        // Error text
        CreateErrorText(loginPanel.transform);
    }

    private void AddPanelBorder(Transform parent)
    {
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(parent, false);

        var borderRect = borderObj.AddComponent<RectTransform>();
        StretchFull(borderRect);
        borderRect.offsetMin = new Vector2(-3, -3);
        borderRect.offsetMax = new Vector2(3, 3);

        var borderImage = borderObj.AddComponent<Image>();
        borderImage.color = accentColor * 0.5f;

        // Move behind panel
        borderObj.transform.SetAsFirstSibling();
    }

    private void CreateTitle(Transform parent)
    {
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);

        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "HARVEST DEFENSE";
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = accentColor;
        titleText.alignment = TextAlignmentOptions.Center;

        var titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 60;
    }

    private void CreateSubtitle(Transform parent)
    {
        var subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(parent, false);

        var subText = subObj.AddComponent<TextMeshProUGUI>();
        subText.text = "Enter your name to begin";
        subText.fontSize = 18;
        subText.color = textColor * 0.7f;
        subText.alignment = TextAlignmentOptions.Center;

        var subLayout = subObj.AddComponent<LayoutElement>();
        subLayout.preferredHeight = 30;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);

        var layout = spacer.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
    }

    private void CreateUsernameInput(Transform parent)
    {
        // Input container
        var inputContainer = new GameObject("InputContainer");
        inputContainer.transform.SetParent(parent, false);

        var containerRect = inputContainer.AddComponent<RectTransform>();
        var containerLayout = inputContainer.AddComponent<LayoutElement>();
        containerLayout.preferredHeight = 60;

        var containerImage = inputContainer.AddComponent<Image>();
        containerImage.color = inputBgColor;

        // Input field
        var inputObj = new GameObject("UsernameInput");
        inputObj.transform.SetParent(inputContainer.transform, false);

        var inputRect = inputObj.AddComponent<RectTransform>();
        StretchFull(inputRect);
        inputRect.offsetMin = new Vector2(15, 5);
        inputRect.offsetMax = new Vector2(-15, -5);

        usernameInput = inputObj.AddComponent<TMP_InputField>();
        usernameInput.characterLimit = 15;

        // Text area
        var textArea = new GameObject("TextArea");
        textArea.transform.SetParent(inputObj.transform, false);
        var textAreaRect = textArea.AddComponent<RectTransform>();
        StretchFull(textAreaRect);

        // Placeholder
        var placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform, false);
        var placeholderRect = placeholderObj.AddComponent<RectTransform>();
        StretchFull(placeholderRect);

        var placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Enter username...";
        placeholder.fontSize = 24;
        placeholder.color = textColor * 0.4f;
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;

        // Input text
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        StretchFull(textRect);

        var inputText = textObj.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = 24;
        inputText.color = textColor;
        inputText.alignment = TextAlignmentOptions.MidlineLeft;

        // Wire input field
        usernameInput.textViewport = textAreaRect;
        usernameInput.textComponent = inputText;
        usernameInput.placeholder = placeholder;
    }

    private void CreateStatusText(Transform parent)
    {
        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(parent, false);

        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "";
        statusText.fontSize = 16;
        statusText.color = new Color(0.5f, 0.8f, 1f, 1f);
        statusText.alignment = TextAlignmentOptions.Center;

        var statusLayout = statusObj.AddComponent<LayoutElement>();
        statusLayout.preferredHeight = 25;
    }

    private void CreatePlayButton(Transform parent)
    {
        var buttonObj = new GameObject("PlayButton");
        buttonObj.transform.SetParent(parent, false);

        var buttonRect = buttonObj.AddComponent<RectTransform>();
        var buttonLayout = buttonObj.AddComponent<LayoutElement>();
        buttonLayout.preferredHeight = 60;

        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = accentColor;

        playButton = buttonObj.AddComponent<Button>();
        playButton.targetGraphic = buttonImage;

        // Button colors
        var colors = playButton.colors;
        colors.normalColor = accentColor;
        colors.highlightedColor = accentColor * 1.2f;
        colors.pressedColor = accentColor * 0.8f;
        colors.selectedColor = accentColor;
        playButton.colors = colors;

        // Button text
        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(buttonObj.transform, false);

        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        StretchFull(btnTextRect);

        var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "PLAY";
        btnText.fontSize = 28;
        btnText.fontStyle = FontStyles.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
    }

    private void CreateErrorText(Transform parent)
    {
        var errorObj = new GameObject("ErrorText");
        errorObj.transform.SetParent(parent, false);

        errorText = errorObj.AddComponent<TextMeshProUGUI>();
        errorText.text = "";
        errorText.fontSize = 16;
        errorText.color = new Color(1f, 0.3f, 0.3f, 1f);
        errorText.alignment = TextAlignmentOptions.Center;

        var errorLayout = errorObj.AddComponent<LayoutElement>();
        errorLayout.preferredHeight = 25;

        errorObj.SetActive(false);
    }

    private void WireController()
    {
        // Add LoginScreenController
        controller = loginPanel.AddComponent<LoginScreenController>();

        // Use reflection to set serialized fields, or expose public setters
        // For now, the controller should find these on Start
        // We'll add a setup method to LoginScreenController
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

    /// <summary>
    /// Show the login UI
    /// </summary>
    public void Show()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Hide the login UI
    /// </summary>
    public void Hide()
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }
    }

    #endregion
}
