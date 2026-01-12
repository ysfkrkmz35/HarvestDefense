using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Leaderboard Manager
/// Handles retrieving and ranking leaderboard data
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    #region ═══════ SINGLETON ═══════

    public static LeaderboardManager Instance { get; private set; }

    #endregion

    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ SETTINGS ═══")]
    [Tooltip("Maximum entries to display in leaderboard")]
    [SerializeField] private int maxEntries = 100;

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region ═══════ CACHED DATA ═══════

    private List<LeaderboardEntry> cachedLeaderboard = new List<LeaderboardEntry>();
    private bool isDirty = true;

    #endregion

    #region ═══════ EVENTS ═══════

    /// <summary>Fired when leaderboard data is refreshed</summary>
    public static event Action<List<LeaderboardEntry>> OnLeaderboardRefreshed;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Subscribe to save events to mark cache dirty
        UserDataManager.OnGameSaved += OnGameSaved;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            UserDataManager.OnGameSaved -= OnGameSaved;
            Instance = null;
        }
    }

    #endregion

    #region ═══════ LEADERBOARD DATA ═══════

    /// <summary>
    /// Get the sorted leaderboard entries
    /// </summary>
    public List<LeaderboardEntry> GetLeaderboard(bool forceRefresh = false)
    {
        if (isDirty || forceRefresh)
        {
            RefreshLeaderboard();
        }

        return new List<LeaderboardEntry>(cachedLeaderboard);
    }

    /// <summary>
    /// Refresh leaderboard data from storage
    /// </summary>
    public void RefreshLeaderboard()
    {
        cachedLeaderboard.Clear();

        if (UserDataManager.Instance == null)
        {
            Debug.LogWarning("[LeaderboardManager] ⚠️ UserDataManager not available");
            isDirty = false;
            return;
        }

        // Get all users
        var allUsers = UserDataManager.Instance.GetAllUsers();

        // Convert to leaderboard entries
        foreach (var userData in allUsers)
        {
            var entry = LeaderboardEntry.FromUserSaveData(userData);
            if (entry != null)
            {
                cachedLeaderboard.Add(entry);
            }
        }

        // Sort by days survived (descending)
        cachedLeaderboard.Sort();

        // Limit entries
        if (cachedLeaderboard.Count > maxEntries)
        {
            cachedLeaderboard.RemoveRange(maxEntries, cachedLeaderboard.Count - maxEntries);
        }

        isDirty = false;

        if (showDebugLogs)
        {
            Debug.Log($"[LeaderboardManager] 🏆 Refreshed leaderboard: {cachedLeaderboard.Count} entries");
        }

        OnLeaderboardRefreshed?.Invoke(cachedLeaderboard);
    }

    /// <summary>
    /// Get the rank of the current user (1-based)
    /// Returns -1 if user not found
    /// </summary>
    public int GetCurrentUserRank()
    {
        if (UserDataManager.Instance == null || !UserDataManager.Instance.IsLoggedIn)
        {
            return -1;
        }

        string currentUsername = UserDataManager.Instance.CurrentUsername;
        var leaderboard = GetLeaderboard();

        for (int i = 0; i < leaderboard.Count; i++)
        {
            if (leaderboard[i].username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                return i + 1; // 1-based rank
            }
        }

        return -1;
    }

    /// <summary>
    /// Get the current user's leaderboard entry
    /// </summary>
    public LeaderboardEntry GetCurrentUserEntry()
    {
        if (UserDataManager.Instance == null || !UserDataManager.Instance.IsLoggedIn)
        {
            return null;
        }

        string currentUsername = UserDataManager.Instance.CurrentUsername;
        var leaderboard = GetLeaderboard();

        foreach (var entry in leaderboard)
        {
            if (entry.username.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }

    #endregion

    #region ═══════ EVENT HANDLERS ═══════

    private void OnGameSaved(UserSaveData userData)
    {
        // Mark cache as dirty when game saves
        isDirty = true;
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("🏆 Test: Print Leaderboard")]
    private void TestPrintLeaderboard()
    {
        var leaderboard = GetLeaderboard(true);
        Debug.Log($"[LeaderboardManager] === LEADERBOARD ({leaderboard.Count} entries) ===");
        for (int i = 0; i < leaderboard.Count; i++)
        {
            Debug.Log($"  #{i + 1}: {leaderboard[i]}");
        }
    }

    [ContextMenu("📊 Test: Get Current User Rank")]
    private void TestCurrentUserRank()
    {
        int rank = GetCurrentUserRank();
        Debug.Log($"[LeaderboardManager] Current user rank: {rank}");
    }

    #endregion
}
