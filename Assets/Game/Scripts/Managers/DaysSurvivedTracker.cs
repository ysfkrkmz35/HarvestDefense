using System;
using UnityEngine;
using HappyHarvest;

/// <summary>
/// Days Survived Tracker
/// - Tracks total days the player has survived
/// - Subscribes to GameManager.OnDayStart to increment counter
/// - Broadcasts events for UI updates
/// </summary>
public class DaysSurvivedTracker : MonoBehaviour
{
    #region ═══════ SINGLETON ═══════

    public static DaysSurvivedTracker Instance { get; private set; }

    #endregion

    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ SETTINGS ═══")]
    [Tooltip("Starting day number (usually 1)")]
    [SerializeField] private int startingDay = 1;

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region ═══════ RUNTIME STATE ═══════

    private int daysSurvived;

    /// <summary>Total days survived by the player</summary>
    public int DaysSurvived => daysSurvived;

    #endregion

    #region ═══════ EVENTS ═══════

    /// <summary>Fired when day count changes. Parameters: new day count, delta</summary>
    public static event Action<int, int> OnDaysChanged;

    /// <summary>Fired at the start of each new day. Parameter: new day number</summary>
    public static event Action<int> OnNewDayStarted;

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

        // Initialize
        daysSurvived = startingDay;
    }

    private void Start()
    {
        // Subscribe to day start event
        GameManager.OnDayStart += HandleDayStart;

        if (showDebugLogs)
        {
            Debug.Log($"[DaysSurvivedTracker] ✅ Initialized at Day {daysSurvived}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            GameManager.OnDayStart -= HandleDayStart;
            Instance = null;
        }
    }

    #endregion

    #region ═══════ DAY TRACKING ═══════

    /// <summary>
    /// Handle the start of a new day
    /// Increments the day counter
    /// </summary>
    private void HandleDayStart()
    {
        IncrementDay();
    }

    /// <summary>
    /// Increment the day counter by 1
    /// </summary>
    public void IncrementDay()
    {
        int previousDay = daysSurvived;
        daysSurvived++;

        if (showDebugLogs)
        {
            Debug.Log($"[DaysSurvivedTracker] 🌅 New day! Day {daysSurvived}");
        }

        OnDaysChanged?.Invoke(daysSurvived, 1);
        OnNewDayStarted?.Invoke(daysSurvived);
    }

    /// <summary>
    /// Set the days survived directly (for loading saved data)
    /// </summary>
    public void SetDaysSurvived(int days)
    {
        if (days < 1) days = 1;

        int previousDay = daysSurvived;
        int delta = days - previousDay;
        daysSurvived = days;

        if (showDebugLogs)
        {
            Debug.Log($"[DaysSurvivedTracker] 📂 Days set to {daysSurvived} (was {previousDay})");
        }

        if (delta != 0)
        {
            OnDaysChanged?.Invoke(daysSurvived, delta);
        }
    }

    /// <summary>
    /// Reset days to starting value
    /// </summary>
    public void Reset()
    {
        SetDaysSurvived(startingDay);
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("🌅 Test: Increment Day")]
    private void TestIncrementDay()
    {
        IncrementDay();
    }

    [ContextMenu("🔄 Test: Reset Days")]
    private void TestReset()
    {
        Reset();
    }

    [ContextMenu("📊 Test: Set to Day 10")]
    private void TestSetDay10()
    {
        SetDaysSurvived(10);
    }

    #endregion
}
