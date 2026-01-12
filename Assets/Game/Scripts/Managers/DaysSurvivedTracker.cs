using System;
using UnityEngine;
using HappyHarvest;

/// <summary>
/// Days Survived Tracker
/// - Tracks total days the player has survived
/// - Subscribes to TimeManager's GameManager.OnDayStart event
/// - Skips the first call (game initialization) and increments on subsequent day starts
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
            Debug.Log($"[DaysSurvivedTracker] 🏗️ Instance Created on '{gameObject.name}' (ID: {GetInstanceID()})");
        }
        else
        {
            Debug.Log($"[DaysSurvivedTracker] ⚠️ DUPLICATE detected on '{gameObject.name}' (ID: {GetInstanceID()}). Destroying it.");
            Destroy(gameObject);
            return;
        }

        // Initialize
        daysSurvived = startingDay;
    }

    private void OnEnable()
    {
        // Subscribe to TimeManager's day start event
        GameManager.OnDayStart += HandleDayStart;
        string status = (Instance == this) ? "MAIN" : "DUPLICATE";
        Debug.Log($"[DaysSurvivedTracker] 🟢 ENABLED ({status}) on '{gameObject.name}' (ID: {GetInstanceID()}). Subscribed (Day {daysSurvived})");
    }

    private void OnDisable()
    {
        GameManager.OnDayStart -= HandleDayStart;
        string status = (Instance == this) ? "MAIN" : "DUPLICATE";
        Debug.Log($"[DaysSurvivedTracker] 🔴 DISABLED ({status}) on '{gameObject.name}' (ID: {GetInstanceID()}). Unsubscribed\nStack Trace: {Environment.StackTrace}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log($"[DaysSurvivedTracker] 💀 Destroyed MAIN Instance on '{gameObject.name}' (ID: {GetInstanceID()}). Singleton cleared.");
        }
    }

    private void Start()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[DaysSurvivedTracker] ✅ Start() on '{gameObject.name}' (ID: {GetInstanceID()}). Logic active.");
        }
    }

    #endregion

    #region ═══════ DAY TRACKING ═══════

    /// <summary>
    /// Handle the start of a new day from TimeManager
    /// Called when TimeManager triggers OnDayStart (after a night cycle)
    /// </summary>
    private void HandleDayStart()
    {
        // FORCE LOG: Trace if this method is called
        Debug.Log($"[DaysSurvivedTracker] ⚡ HandleDayStart called! Instance: ID {GetInstanceID()}. Current Day: {daysSurvived}. Calling IncrementDay...");
        
        IncrementDay();
    }



    /// <summary>
    /// Increment the day counter by 1
    /// </summary>
    public void IncrementDay()
    {
        int previousDay = daysSurvived;
        daysSurvived++;
        Debug.Log($"[DaysSurvivedTracker] 📈 Day Incremented: {previousDay} -> {daysSurvived} (ID: {GetInstanceID()})");

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
            Debug.Log($"[DaysSurvivedTracker] 📂 Days set to {daysSurvived} (was {previousDay}) on ID {GetInstanceID()}");
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
