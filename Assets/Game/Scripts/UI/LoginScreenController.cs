using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Login Screen Controller
/// - Username input and validation
/// - User detection (existing vs new)
/// - Game start flow
/// </summary>
public class LoginScreenController : MonoBehaviour
{
    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ UI REFERENCES ═══")]
    [Tooltip("Username input field")]
    [SerializeField] private TMP_InputField usernameInput;

    [Tooltip("Play/Submit button")]
    [SerializeField] private Button playButton;

    [Tooltip("Error message text")]
    [SerializeField] private TextMeshProUGUI errorText;

    [Tooltip("Status text (New User / Welcome Back)")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Tooltip("Login panel container")]
    [SerializeField] private GameObject loginPanel;

    [Header("═══ SCENE SETTINGS ═══")]
    [Tooltip("Scene to load after login (by name)")]
    [SerializeField] private string gameSceneName = "Game_Main_Scene";

    [Tooltip("Scene to load after login (by index, -1 to use name)")]
    [SerializeField] private int gameSceneIndex = -1;

    [Header("═══ VISUAL SETTINGS ═══")]
    [SerializeField] private Color errorColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color successColor = new Color(0.3f, 1f, 0.5f, 1f);
    [SerializeField] private Color newUserColor = new Color(0.5f, 0.8f, 1f, 1f);
    [SerializeField] private Color returningUserColor = new Color(1f, 0.9f, 0.5f, 1f);

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region ═══════ RUNTIME STATE ═══════

    private bool isProcessing = false;

    #endregion

    #region ═══════ EVENTS ═══════

    /// <summary>Fired when login is successful. Parameter: user data</summary>
    public static event Action<UserSaveData> OnLoginSuccess;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    private void Start()
    {
        // Auto-discover components if not assigned (for programmatic creation)
        AutoDiscoverComponents();

        // Setup button listener
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        // Setup input field
        if (usernameInput != null)
        {
            usernameInput.onValueChanged.AddListener(OnUsernameChanged);
            usernameInput.onSubmit.AddListener(OnUsernameSubmit);
        }

        // Clear initial states
        ClearError();
        ClearStatus();

        // Focus input field
        if (usernameInput != null)
        {
            usernameInput.Select();
            usernameInput.ActivateInputField();
        }
    }

    private void AutoDiscoverComponents()
    {
        // Auto-find components by name if not assigned
        if (usernameInput == null)
        {
            usernameInput = GetComponentInChildren<TMP_InputField>(true);
        }

        if (playButton == null)
        {
            var btn = transform.Find("PlayButton");
            if (btn != null) playButton = btn.GetComponent<Button>();
            // Fallback: find first button
            if (playButton == null) playButton = GetComponentInChildren<Button>(true);
        }

        if (errorText == null)
        {
            var err = transform.Find("ErrorText");
            if (err != null) errorText = err.GetComponent<TextMeshProUGUI>();
        }

        if (statusText == null)
        {
            var status = transform.Find("StatusText");
            if (status != null) statusText = status.GetComponent<TextMeshProUGUI>();
        }

        if (loginPanel == null)
        {
            loginPanel = gameObject;
        }
    }

    private void OnDestroy()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
        }

        if (usernameInput != null)
        {
            usernameInput.onValueChanged.RemoveListener(OnUsernameChanged);
            usernameInput.onSubmit.RemoveListener(OnUsernameSubmit);
        }
    }

    #endregion

    #region ═══════ INPUT HANDLERS ═══════

    private void OnUsernameChanged(string username)
    {
        ClearError();
        UpdateStatus(username);
    }

    private void OnUsernameSubmit(string username)
    {
        AttemptLogin();
    }

    private void OnPlayClicked()
    {
        AttemptLogin();
    }

    #endregion

    #region ═══════ STATUS UPDATE ═══════

    private void UpdateStatus(string username)
    {
        if (statusText == null) return;
        if (string.IsNullOrWhiteSpace(username))
        {
            ClearStatus();
            return;
        }

        username = username.Trim();

        // Check validation first
        if (UserDataManager.Instance != null)
        {
            var validation = UserDataManager.Instance.ValidateUsername(username);
            if (!validation.isValid)
            {
                ClearStatus();
                return;
            }

            // Check if user exists
            if (UserDataManager.Instance.UserExists(username))
            {
                statusText.text = "Welcome back!";
                statusText.color = returningUserColor;
            }
            else
            {
                statusText.text = "New adventurer!";
                statusText.color = newUserColor;
            }
        }
    }

    private void ClearStatus()
    {
        if (statusText != null)
        {
            statusText.text = "";
        }
    }

    #endregion

    #region ═══════ LOGIN LOGIC ═══════

    private void AttemptLogin()
    {
        if (isProcessing) return;

        string username = usernameInput?.text?.Trim();

        // Validate
        if (string.IsNullOrEmpty(username))
        {
            ShowError("Please enter a username");
            return;
        }

        if (UserDataManager.Instance == null)
        {
            ShowError("System not ready, please try again");
            Debug.LogError("[LoginScreenController] UserDataManager not found!");
            return;
        }

        // Validate username
        var validation = UserDataManager.Instance.ValidateUsername(username);
        if (!validation.isValid)
        {
            ShowError(validation.errorMessage);
            return;
        }

        // Attempt login
        isProcessing = true;
        StartCoroutine(LoginCoroutine(username));
    }

    private IEnumerator LoginCoroutine(string username)
    {
        // Small delay for visual feedback
        yield return new WaitForSeconds(0.1f);

        // Login (creates or loads user)
        var userData = UserDataManager.Instance.Login(username);

        if (userData == null)
        {
            ShowError("Login failed, please try again");
            isProcessing = false;
            yield break;
        }

        // Success!
        if (showDebugLogs)
        {
            Debug.Log($"[LoginScreenController] ✅ Login successful: {userData}");
        }

        ShowSuccess($"Welcome, {username}!");

        OnLoginSuccess?.Invoke(userData);

        // Wait briefly then load game
        yield return new WaitForSeconds(0.5f);

        LoadGameScene();
    }

    private void LoadGameScene()
    {
        if (gameSceneIndex >= 0)
        {
            SceneManager.LoadScene(gameSceneIndex);
        }
        else if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // Load next scene in build order
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    #endregion

    #region ═══════ ERROR/SUCCESS DISPLAY ═══════

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = errorColor;
            errorText.gameObject.SetActive(true);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[LoginScreenController] ❌ Error: {message}");
        }
    }

    private void ShowSuccess(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = successColor;
            errorText.gameObject.SetActive(true);
        }
    }

    private void ClearError()
    {
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
    }

    #endregion

    #region ═══════ PUBLIC METHODS ═══════

    /// <summary>
    /// Show the login panel
    /// </summary>
    public void Show()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }
        gameObject.SetActive(true);

        // Clear and focus
        if (usernameInput != null)
        {
            usernameInput.text = "";
            usernameInput.Select();
            usernameInput.ActivateInputField();
        }

        ClearError();
        ClearStatus();
        isProcessing = false;
    }

    /// <summary>
    /// Hide the login panel
    /// </summary>
    public void Hide()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("🔓 Test: Login as TestUser")]
    private void TestLogin()
    {
        if (usernameInput != null)
        {
            usernameInput.text = "TestUser";
        }
        AttemptLogin();
    }

    #endregion
}
