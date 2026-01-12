using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Leaderboard UI
/// - Displays player rankings sorted by days survived
/// - Toggle with Tab key
/// - Highlights current player
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ PANEL REFERENCES ═══")]
    [Tooltip("Main leaderboard panel")]
    [SerializeField] private GameObject leaderboardPanel;

    [Tooltip("Close button")]
    [SerializeField] private Button closeButton;

    [Tooltip("Refresh button")]
    [SerializeField] private Button refreshButton;

    [Header("═══ CONTENT ═══")]
    [Tooltip("Content container for entries")]
    [SerializeField] private Transform entriesContainer;

    [Tooltip("Entry prefab (or template to clone)")]
    [SerializeField] private GameObject entryPrefab;

    [Header("═══ CURRENT USER INFO ═══")]
    [Tooltip("Text showing current user's rank")]
    [SerializeField] private TextMeshProUGUI currentRankText;

    [Header("═══ INPUT ═══")]
    [Tooltip("Key to toggle leaderboard")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Tooltip("Alternative key to toggle")]
    [SerializeField] private KeyCode altToggleKey = KeyCode.L;

    [Header("═══ VISUAL SETTINGS ═══")]
    [SerializeField] private Color normalRowColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
    [SerializeField] private Color alternateRowColor = new Color(0.25f, 0.25f, 0.3f, 0.9f);
    [SerializeField] private Color currentUserRowColor = new Color(0.3f, 0.5f, 0.3f, 0.95f);
    [SerializeField] private Color headerColor = new Color(0.15f, 0.15f, 0.2f, 1f);

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region ═══════ RUNTIME STATE ═══════

    private List<GameObject> spawnedEntries = new List<GameObject>();
    private bool isVisible = false;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    private void Start()
    {
        // Auto-discover components if not assigned
        AutoDiscoverComponents();

        // Setup buttons
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshLeaderboard);
        }

        // Start hidden
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }

    private void AutoDiscoverComponents()
    {
        // Auto-find leaderboardPanel
        if (leaderboardPanel == null)
        {
            leaderboardPanel = gameObject;
        }

        // Auto-find entriesContainer
        if (entriesContainer == null)
        {
            var content = transform.Find("ScrollView/Viewport/Content");
            if (content != null) entriesContainer = content;
        }

        // Auto-find currentRankText
        if (currentRankText == null)
        {
            var footer = transform.Find("Footer/CurrentRank");
            if (footer != null) currentRankText = footer.GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(RefreshLeaderboard);
        }
    }

    private void Update()
    {
        // Toggle with key press
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(altToggleKey))
        {
            Toggle();
        }
    }

    #endregion

    #region ═══════ VISIBILITY ═══════

    /// <summary>
    /// Toggle leaderboard visibility
    /// </summary>
    public void Toggle()
    {
        if (isVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <summary>
    /// Show the leaderboard
    /// </summary>
    public void Show()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
        }

        isVisible = true;
        RefreshLeaderboard();

        if (showDebugLogs)
        {
            Debug.Log("[LeaderboardUI] 🏆 Leaderboard opened");
        }
    }

    /// <summary>
    /// Hide the leaderboard
    /// </summary>
    public void Hide()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }

        isVisible = false;

        if (showDebugLogs)
        {
            Debug.Log("[LeaderboardUI] 📋 Leaderboard closed");
        }
    }

    #endregion

    #region ═══════ DATA DISPLAY ═══════

    /// <summary>
    /// Refresh and rebuild the leaderboard display
    /// </summary>
    public void RefreshLeaderboard()
    {
        // Clear existing entries
        ClearEntries();

        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning("[LeaderboardUI] ⚠️ LeaderboardManager not available");
            return;
        }

        // Get leaderboard data
        var leaderboard = LeaderboardManager.Instance.GetLeaderboard(true);
        string currentUsername = UserDataManager.Instance?.CurrentUsername;

        // Create entries
        for (int i = 0; i < leaderboard.Count; i++)
        {
            CreateEntry(i + 1, leaderboard[i], currentUsername);
        }

        // Update current user rank text
        UpdateCurrentRankText();

        if (showDebugLogs)
        {
            Debug.Log($"[LeaderboardUI] 📊 Refreshed with {leaderboard.Count} entries");
        }
    }

    private void CreateEntry(int rank, LeaderboardEntry entry, string currentUsername)
    {
        if (entriesContainer == null) return;

        GameObject entryObj;

        if (entryPrefab != null)
        {
            entryObj = Instantiate(entryPrefab, entriesContainer);
        }
        else
        {
            // Create a simple entry dynamically if no prefab
            entryObj = CreateDefaultEntry();
            entryObj.transform.SetParent(entriesContainer, false);
        }

        entryObj.SetActive(true);
        spawnedEntries.Add(entryObj);

        // Check if this is the current user
        bool isCurrentUser = !string.IsNullOrEmpty(currentUsername) &&
                             entry.username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase);

        // Set values
        SetEntryValues(entryObj, rank, entry, isCurrentUser);

        // Set background color
        var bg = entryObj.GetComponent<Image>();
        if (bg != null)
        {
            if (isCurrentUser)
            {
                bg.color = currentUserRowColor;
            }
            else
            {
                bg.color = (rank % 2 == 0) ? alternateRowColor : normalRowColor;
            }
        }
    }

    private void SetEntryValues(GameObject entryObj, int rank, LeaderboardEntry entry, bool isCurrentUser)
    {
        // Find and set text components
        // Expected structure: Rank, Username, Level, Gold, Spells, Days
        var texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length >= 6)
        {
            texts[0].text = $"#{rank}";
            texts[1].text = isCurrentUser ? $"► {entry.username}" : entry.username;
            texts[2].text = $"Lv.{entry.level}";
            texts[3].text = FormatGold(entry.gold);
            texts[4].text = $"{entry.spellCount}";
            texts[5].text = $"Day {entry.daysSurvived}";
        }
        else if (texts.Length >= 1)
        {
            // Fallback: single text with all info
            string marker = isCurrentUser ? "► " : "";
            texts[0].text = $"{marker}#{rank} {entry.username} - Lv.{entry.level} - {FormatGold(entry.gold)} Gold - {entry.spellCount} Spells - Day {entry.daysSurvived}";
        }

        // Highlight current user text if needed
        if (isCurrentUser)
        {
            foreach (var text in texts)
            {
                text.fontStyle = FontStyles.Bold;
            }
        }
    }

    private string FormatGold(int gold)
    {
        if (gold >= 1000000)
            return (gold / 1000000f).ToString("0.#") + "M";
        if (gold >= 1000)
            return (gold / 1000f).ToString("0.#") + "K";
        return gold.ToString();
    }

    private GameObject CreateDefaultEntry()
    {
        // Create a simple horizontal layout entry
        var entryObj = new GameObject("LeaderboardEntry");

        // Add background image
        var bg = entryObj.AddComponent<Image>();
        bg.color = normalRowColor;

        // Add horizontal layout
        var layout = entryObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Add rect transform sizing
        var rect = entryObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 40);

        // Add layout element
        var layoutElement = entryObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 40;
        layoutElement.flexibleWidth = 1;

        // Create text fields
        string[] labels = { "Rank", "Username", "Level", "Gold", "Spells", "Days" };
        float[] widths = { 50, 150, 60, 80, 60, 80 };

        for (int i = 0; i < labels.Length; i++)
        {
            var textObj = new GameObject(labels[i]);
            textObj.transform.SetParent(entryObj.transform, false);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = labels[i];
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = Color.white;

            var textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.preferredWidth = widths[i];
        }

        return entryObj;
    }

    private void ClearEntries()
    {
        foreach (var entry in spawnedEntries)
        {
            if (entry != null)
            {
                Destroy(entry);
            }
        }
        spawnedEntries.Clear();
    }

    private void UpdateCurrentRankText()
    {
        if (currentRankText == null) return;

        if (LeaderboardManager.Instance == null || UserDataManager.Instance == null)
        {
            currentRankText.text = "";
            return;
        }

        int rank = LeaderboardManager.Instance.GetCurrentUserRank();
        if (rank > 0)
        {
            currentRankText.text = $"Your Rank: #{rank}";
        }
        else
        {
            currentRankText.text = "Not Ranked";
        }
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("🏆 Test: Show Leaderboard")]
    private void TestShow()
    {
        Show();
    }

    [ContextMenu("📋 Test: Hide Leaderboard")]
    private void TestHide()
    {
        Hide();
    }

    #endregion
}
