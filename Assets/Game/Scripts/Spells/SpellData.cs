using UnityEngine;

/// <summary>
/// Spell Type Enum
/// Defines the behavior category of the spell
/// </summary>
public enum SpellType
{
    Area,       // AoE damage at location
    Projectile, // Fires projectile toward target
    Melee       // Single-target close range
}

/// <summary>
/// Spell Data ScriptableObject
/// - Defines all properties of a spell
/// - Used by SpellBase and UI systems
/// - Create via Assets > Create > HarvestDefense > Spell Data
/// </summary>
[CreateAssetMenu(fileName = "NewSpell", menuName = "HarvestDefense/Spell Data", order = 1)]
public class SpellData : ScriptableObject
{
    [Header("═══ BASIC INFO ═══")]
    [Tooltip("Display name of the spell")]
    public string spellName = "New Spell";

    [Tooltip("Description shown in UI")]
    [TextArea(2, 4)]
    public string description = "A powerful spell.";

    [Tooltip("Icon displayed in spell slots")]
    public Sprite icon;

    [Header("═══ SPELL TYPE ═══")]
    [Tooltip("How the spell behaves")]
    public SpellType spellType = SpellType.Area;

    [Header("═══ COMBAT STATS ═══")]
    [Tooltip("Base damage dealt")]
    [Min(0)]
    public float damage = 25f;

    [Tooltip("Cooldown in seconds")]
    [Min(0.1f)]
    public float cooldown = 2f;

    [Tooltip("Mana cost to cast")]
    [Min(0)]
    public float manaCost = 10f;

    [Header("═══ RANGE & AREA ═══")]
    [Tooltip("Maximum cast range from player")]
    [Min(1f)]
    public float maxRange = 10f;

    [Tooltip("Area of effect radius (for Area spells)")]
    [Min(0.5f)]
    public float areaRadius = 3f;

    [Tooltip("Projectile speed (for Projectile spells)")]
    [Min(1f)]
    public float projectileSpeed = 15f;

    [Header("═══ UNLOCK REQUIREMENTS ═══")]
    [Tooltip("Minimum level required to unlock")]
    [Min(1)]
    public int requiredLevel = 1;

    [Tooltip("Gold cost to unlock")]
    [Min(0)]
    public int goldCost = 0;

    [Tooltip("Is this spell unlocked by default?")]
    public bool unlockedByDefault = false;

    [Header("═══ VISUAL EFFECTS ═══")]
    [Tooltip("Prefab spawned at cast/impact location")]
    public GameObject effectPrefab;

    [Tooltip("Projectile prefab (for Projectile spells)")]
    public GameObject projectilePrefab;

    [Tooltip("Duration before effect is destroyed")]
    [Min(0.1f)]
    public float effectDuration = 2f;

    [Tooltip("Spell color for procedural effects")]
    public Color spellColor = new Color(1f, 0.5f, 0f, 1f);

    [Header("═══ AUDIO ═══")]
    [Tooltip("Sound when spell is cast")]
    public AudioClip castSound;

    [Tooltip("Sound on impact")]
    public AudioClip impactSound;

    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    [Header("═══ SCREEN EFFECTS ═══")]
    [Tooltip("Enable screen shake on cast")]
    public bool enableScreenShake = true;

    [Tooltip("Screen shake intensity")]
    [Range(0f, 1f)]
    public float shakeIntensity = 0.3f;

    [Tooltip("Screen shake duration")]
    [Min(0.05f)]
    public float shakeDuration = 0.2f;

    #region ═══════ HELPER METHODS ═══════

    /// <summary>
    /// Check if player meets level requirement
    /// </summary>
    public bool MeetsLevelRequirement(int playerLevel)
    {
        return playerLevel >= requiredLevel;
    }

    /// <summary>
    /// Check if player can afford the gold cost
    /// </summary>
    public bool CanAfford(int playerGold)
    {
        return playerGold >= goldCost;
    }

    /// <summary>
    /// Get formatted unlock requirements string
    /// </summary>
    public string GetRequirementsText()
    {
        if (unlockedByDefault) return "Unlocked";

        string text = "";
        if (requiredLevel > 1) text += $"Level {requiredLevel}";
        if (goldCost > 0)
        {
            if (!string.IsNullOrEmpty(text)) text += " + ";
            text += $"{goldCost} Gold";
        }
        return text;
    }

    #endregion
}
