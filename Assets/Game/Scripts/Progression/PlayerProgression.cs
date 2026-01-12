using UnityEngine;
using System;

/// <summary>
/// Player Progression System
/// - Tracks XP, Level, and Gold
/// - Exponential XP curve: baseXP * (1.5^level)
/// - Broadcasts events for UI and other systems
/// </summary>
public class PlayerProgression : MonoBehaviour
{
    #region ═══════ SINGLETON ═══════

    public static PlayerProgression Instance { get; private set; }

    #endregion

    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ LEVEL SETTINGS ═══")]
    [Tooltip("Base XP needed for level 2")]
    [SerializeField] private int baseXP = 100;

    [Tooltip("XP multiplier per level (exponential curve)")]
    [SerializeField] private float xpMultiplier = 1.5f;

    [Tooltip("Starting level")]
    [SerializeField] private int startingLevel = 1;

    [Tooltip("Maximum level cap")]
    [SerializeField] private int maxLevel = 50;

    [Header("═══ STARTING VALUES ═══")]
    [SerializeField] private int startingGold = 0;

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region ═══════ PROPERTIES ═══════

    /// <summary>Current player level</summary>
    public int CurrentLevel { get; private set; }

    /// <summary>Current XP towards next level</summary>
    public int CurrentXP { get; private set; }

    /// <summary>XP required to reach next level</summary>
    public int XPToNextLevel { get; private set; }

    /// <summary>Current gold amount</summary>
    public int Gold { get; private set; }

    /// <summary>XP progress as percentage (0-1)</summary>
    public float XPProgress => XPToNextLevel > 0 ? (float)CurrentXP / XPToNextLevel : 0f;

    /// <summary>Is player at max level?</summary>
    public bool IsMaxLevel => CurrentLevel >= maxLevel;

    #endregion

    #region ═══════ EVENTS ═══════

    /// <summary>Fired when player levels up. Parameter: new level</summary>
    public static event Action<int> OnLevelUp;

    /// <summary>Fired when XP changes. Parameters: currentXP, xpToNextLevel</summary>
    public static event Action<int, int> OnXPChanged;

    /// <summary>Fired when gold changes. Parameters: newAmount, delta</summary>
    public static event Action<int, int> OnGoldChanged;

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

        Initialize();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region ═══════ INITIALIZATION ═══════

    private void Initialize()
    {
        CurrentLevel = startingLevel;
        CurrentXP = 0;
        Gold = startingGold;
        XPToNextLevel = CalculateXPForLevel(CurrentLevel);

        if (showDebugLogs)
        {
            Debug.Log($"[PlayerProgression] ✅ Initialized - Level: {CurrentLevel}, XP: {CurrentXP}/{XPToNextLevel}, Gold: {Gold}");
        }
    }

    #endregion

    #region ═══════ XP SYSTEM ═══════

    /// <summary>
    /// Add XP to the player. Handles level-up automatically.
    /// </summary>
    /// <param name="amount">Amount of XP to add</param>
    public void AddXP(int amount)
    {
        if (amount <= 0 || IsMaxLevel) return;

        CurrentXP += amount;

        if (showDebugLogs)
        {
            Debug.Log($"[PlayerProgression] ⭐ +{amount} XP | Total: {CurrentXP}/{XPToNextLevel}");
        }

        // Check for level up
        while (CurrentXP >= XPToNextLevel && !IsMaxLevel)
        {
            LevelUp();
        }

        OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
    }

    private void LevelUp()
    {
        CurrentXP -= XPToNextLevel;
        CurrentLevel++;
        XPToNextLevel = CalculateXPForLevel(CurrentLevel);

        if (showDebugLogs)
        {
            Debug.Log($"[PlayerProgression] 🎉 LEVEL UP! Now Level {CurrentLevel} | Next level needs {XPToNextLevel} XP");
        }

        OnLevelUp?.Invoke(CurrentLevel);

        // Clamp overflow XP at max level
        if (IsMaxLevel)
        {
            CurrentXP = 0;
            XPToNextLevel = 0;
        }
    }

    /// <summary>
    /// Calculate XP required for a specific level using exponential curve.
    /// Formula: baseXP * (xpMultiplier ^ (level - 1))
    /// </summary>
    private int CalculateXPForLevel(int level)
    {
        if (level >= maxLevel) return 0;
        return Mathf.RoundToInt(baseXP * Mathf.Pow(xpMultiplier, level - 1));
    }

    #endregion

    #region ═══════ GOLD SYSTEM ═══════

    /// <summary>
    /// Add gold to the player.
    /// </summary>
    /// <param name="amount">Amount of gold to add</param>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        Gold += amount;

        if (showDebugLogs)
        {
            Debug.Log($"[PlayerProgression] 💰 +{amount} Gold | Total: {Gold}");
        }

        OnGoldChanged?.Invoke(Gold, amount);
    }

    /// <summary>
    /// Try to spend gold. Returns true if successful.
    /// </summary>
    /// <param name="amount">Amount to spend</param>
    /// <returns>True if purchase successful, false if not enough gold</returns>
    public bool SpendGold(int amount)
    {
        if (amount <= 0) return true;

        if (Gold >= amount)
        {
            Gold -= amount;

            if (showDebugLogs)
            {
                Debug.Log($"[PlayerProgression] 💸 -{amount} Gold | Remaining: {Gold}");
            }

            OnGoldChanged?.Invoke(Gold, -amount);
            return true;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[PlayerProgression] ❌ Not enough gold! Need {amount}, have {Gold}");
        }

        return false;
    }

    /// <summary>
    /// Check if player has enough gold.
    /// </summary>
    public bool HasGold(int amount)
    {
        return Gold >= amount;
    }

    #endregion

    #region ═══════ SAVE/LOAD SUPPORT ═══════

    /// <summary>
    /// Set all progression values directly (for loading saved data)
    /// </summary>
    /// <param name="level">Level to set</param>
    /// <param name="xp">Current XP to set</param>
    /// <param name="gold">Gold to set</param>
    public void SetValues(int level, int xp, int gold)
    {
        CurrentLevel = Mathf.Clamp(level, 1, maxLevel);
        CurrentXP = Mathf.Max(0, xp);
        Gold = Mathf.Max(0, gold);
        XPToNextLevel = CalculateXPForLevel(CurrentLevel);

        if (showDebugLogs)
        {
            Debug.Log($"[PlayerProgression] 📂 Loaded values - Level: {CurrentLevel}, XP: {CurrentXP}/{XPToNextLevel}, Gold: {Gold}");
        }

        // Fire events to update UIs
        OnXPChanged?.Invoke(CurrentXP, XPToNextLevel);
        OnGoldChanged?.Invoke(Gold, 0);
        OnLevelUp?.Invoke(CurrentLevel);
    }

    /// <summary>
    /// Set gold directly (for loading saved data)
    /// </summary>
    public void SetGold(int amount)
    {
        Gold = Mathf.Max(0, amount);
        OnGoldChanged?.Invoke(Gold, 0);
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("⭐ Test: Add 50 XP")]
    private void TestAddXP50() { AddXP(50); }

    [ContextMenu("⭐ Test: Add 200 XP")]
    private void TestAddXP200() { AddXP(200); }

    [ContextMenu("💰 Test: Add 100 Gold")]
    private void TestAddGold100() { AddGold(100); }

    [ContextMenu("💸 Test: Spend 50 Gold")]
    private void TestSpendGold50() { SpendGold(50); }

    [ContextMenu("📊 Debug: Print Status")]
    private void DebugPrintStatus()
    {
        Debug.Log($"[PlayerProgression] Level: {CurrentLevel}, XP: {CurrentXP}/{XPToNextLevel} ({XPProgress:P0}), Gold: {Gold}");
    }

    #endregion
}
