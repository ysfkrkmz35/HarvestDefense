using System;
using System.Collections.Generic;

/// <summary>
/// Leaderboard Entry
/// Data model for a single leaderboard entry
/// Implements IComparable for sorting by days survived
/// </summary>
[System.Serializable]
public class LeaderboardEntry : IComparable<LeaderboardEntry>
{
    /// <summary>Player's username</summary>
    public string username;

    /// <summary>Player's current level</summary>
    public int level;

    /// <summary>Player's total gold</summary>
    public int gold;

    /// <summary>Number of spells unlocked</summary>
    public int spellCount;

    /// <summary>Total days survived</summary>
    public int daysSurvived;

    /// <summary>Account creation timestamp</summary>
    public string createdAt;

    /// <summary>Last played timestamp</summary>
    public string lastPlayedAt;

    #region ═══════ CONSTRUCTORS ═══════

    public LeaderboardEntry() { }

    /// <summary>
    /// Create from UserSaveData
    /// </summary>
    public static LeaderboardEntry FromUserSaveData(UserSaveData data)
    {
        if (data == null) return null;

        return new LeaderboardEntry
        {
            username = data.username,
            level = data.level,
            gold = data.gold,
            spellCount = data.unlockedSpellIds?.Count ?? 0,
            daysSurvived = data.daysSurvived,
            createdAt = data.createdAt,
            lastPlayedAt = data.lastPlayedAt
        };
    }

    #endregion

    #region ═══════ COMPARISON ═══════

    /// <summary>
    /// Compare for sorting - sorts by days survived DESCENDING (highest first)
    /// Secondary sort by level, then by gold
    /// </summary>
    public int CompareTo(LeaderboardEntry other)
    {
        if (other == null) return -1;

        // Primary sort: days survived (descending)
        int daysCompare = other.daysSurvived.CompareTo(daysSurvived);
        if (daysCompare != 0) return daysCompare;

        // Secondary sort: level (descending)
        int levelCompare = other.level.CompareTo(level);
        if (levelCompare != 0) return levelCompare;

        // Tertiary sort: gold (descending)
        return other.gold.CompareTo(gold);
    }

    #endregion

    #region ═══════ DEBUG ═══════

    public override string ToString()
    {
        return $"{username}: Day {daysSurvived}, Lvl {level}, Gold {gold}, Spells {spellCount}";
    }

    #endregion
}
