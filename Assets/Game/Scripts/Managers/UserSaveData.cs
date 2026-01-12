using System;
using System.Collections.Generic;
using UnityEngine;
using HappyHarvest;

/// <summary>
/// User Save Data Model
/// Contains all persistent data for a player account
/// - Serializable for JSON storage via PlayerPrefs
/// </summary>
[System.Serializable]
public class UserSaveData
{
    #region ═══════ IDENTITY ═══════

    /// <summary>Unique username identifier</summary>
    public string username;

    #endregion

    #region ═══════ PROGRESSION ═══════

    /// <summary>Player's current level</summary>
    public int level = 1;

    /// <summary>Current XP towards next level</summary>
    public int currentXP = 0;

    /// <summary>Current gold amount</summary>
    public int gold = 0;

    #endregion

    #region ═══════ SURVIVAL ═══════

    /// <summary>Total days survived</summary>
    public int daysSurvived = 1;

    #endregion

    #region ═══════ SPELLS ═══════

    /// <summary>List of unlocked spell identifiers</summary>
    public List<string> unlockedSpellIds = new List<string>();

    /// <summary>Names of spells equipped in slots 1-4</summary>
    public string[] equippedSpellSlots = new string[4];

    #endregion

    #region ═══════ INVENTORY ═══════

    /// <summary>Player inventory items (HappyHarvest format)</summary>
    public List<InventorySaveData> inventoryItems = new List<InventorySaveData>();

    #endregion

    #region ═══════ POSITION ═══════

    /// <summary>Last known X position</summary>
    public float lastPositionX = 0f;

    /// <summary>Last known Y position</summary>
    public float lastPositionY = 0f;

    #endregion

    #region ═══════ TIMESTAMPS ═══════

    /// <summary>Account creation timestamp (ISO 8601 format)</summary>
    public string createdAt;

    /// <summary>Last played timestamp (ISO 8601 format)</summary>
    public string lastPlayedAt;

    #endregion

    #region ═══════ CONSTRUCTORS ═══════

    /// <summary>
    /// Create a new user with default starting values
    /// </summary>
    public static UserSaveData CreateNew(string username)
    {
        string now = DateTime.UtcNow.ToString("o"); // ISO 8601
        return new UserSaveData
        {
            username = username,
            level = 1,
            currentXP = 0,
            gold = 0,
            daysSurvived = 1,
            unlockedSpellIds = new List<string>(),
            equippedSpellSlots = new string[4],
            inventoryItems = new List<InventorySaveData>(),
            lastPositionX = 0f,
            lastPositionY = 0f,
            createdAt = now,
            lastPlayedAt = now
        };
    }

    /// <summary>
    /// Update the last played timestamp to current time
    /// </summary>
    public void UpdateLastPlayed()
    {
        lastPlayedAt = DateTime.UtcNow.ToString("o");
    }

    #endregion

    #region ═══════ DATA COLLECTION ═══════

    /// <summary>
    /// Collect current game state into this save data
    /// Call before saving to persist latest values
    /// </summary>
    public void CollectFromCurrentGameState()
    {
        // Collect from PlayerProgression
        if (PlayerProgression.Instance != null)
        {
            level = PlayerProgression.Instance.CurrentLevel;
            currentXP = PlayerProgression.Instance.CurrentXP;
            // Try PlayerProgression gold first
            gold = PlayerProgression.Instance.Gold;
        }

        // Also try HappyHarvest's coin system (GameManager.Instance.Player.Coins)
        // Use this if PlayerProgression gold is 0 or as the primary source
        if (HappyHarvest.GameManager.Instance != null && HappyHarvest.GameManager.Instance.Player != null)
        {
            int happyHarvestCoins = HappyHarvest.GameManager.Instance.Player.Coins;
            // Use HappyHarvest coins if it has value (this is the primary gold source)
            if (happyHarvestCoins > 0 || gold == 0)
            {
                gold = happyHarvestCoins;
            }
            Debug.Log($"[UserSaveData] 📝 Collected: Level={level}, XP={currentXP}, Gold={gold} (HH Coins={happyHarvestCoins})");
        }
        else
        {
            Debug.Log($"[UserSaveData] 📝 Collected from PlayerProgression: Level={level}, XP={currentXP}, Gold={gold}");
        }

        // Collect from SpellManager
        if (SpellManager.Instance != null)
        {
            unlockedSpellIds.Clear();
            Debug.Log($"[UserSaveData] 🔮 SpellManager found, UnlockedSpells count: {SpellManager.Instance.UnlockedSpells.Count}");
            foreach (var spell in SpellManager.Instance.UnlockedSpells)
            {
                if (spell != null && !string.IsNullOrEmpty(spell.spellName))
                {
                    unlockedSpellIds.Add(spell.spellName);
                    Debug.Log($"[UserSaveData] 🔮 Collected spell: {spell.spellName}");
                }
            }
            Debug.Log($"[UserSaveData] 🔮 Total spells collected: {unlockedSpellIds.Count}");

            // Collect equipped slots
            equippedSpellSlots = SpellManager.Instance.GetEquippedSpellNames();
            Debug.Log($"[UserSaveData] 🔮 Collected equipped slots: {string.Join(", ", equippedSpellSlots)}");
        }
        else
        {
            Debug.LogWarning("[UserSaveData] ⚠️ SpellManager.Instance is null during spell collection!");
        }

        // Collect from DaysSurvivedTracker (will be created)
        if (DaysSurvivedTracker.Instance != null)
        {
            daysSurvived = DaysSurvivedTracker.Instance.DaysSurvived;
        }

        // Collect player position
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            lastPositionX = player.transform.position.x;
            lastPositionY = player.transform.position.y;
        }

        // Collect inventory from HappyHarvest
        if (HappyHarvest.GameManager.Instance != null && HappyHarvest.GameManager.Instance.Player != null)
        {
            inventoryItems = new List<InventorySaveData>();
            HappyHarvest.GameManager.Instance.Player.Inventory.Save(ref inventoryItems);
            
            // Log each item being saved
            int itemCount = 0;
            for (int i = 0; i < inventoryItems.Count; i++)
            {
                if (inventoryItems[i] != null)
                {
                    Debug.Log($"[UserSaveData] 📦 Slot {i}: {inventoryItems[i].ItemID} x{inventoryItems[i].Amount}");
                    itemCount++;
                }
            }
            Debug.Log($"[UserSaveData] 📦 Collected {inventoryItems.Count} slots ({itemCount} with items)");
        }

        UpdateLastPlayed();
    }

    #endregion

    #region ═══════ DEBUG ═══════

    public override string ToString()
    {
        return $"User: {username}, Level: {level}, Gold: {gold}, Days: {daysSurvived}, Spells: {unlockedSpellIds.Count}";
    }

    #endregion
}
