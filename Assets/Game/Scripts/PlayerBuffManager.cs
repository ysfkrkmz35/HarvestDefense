using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Player Buff Manager
/// - Centralized buff tracking and management
/// - Provides multiplier getters for other systems
/// - Auto-removes expired buffs
/// </summary>
public class PlayerBuffManager : MonoBehaviour
{
    #region ═══════ SINGLETON ═══════

    public static PlayerBuffManager Instance { get; private set; }

    #endregion

    #region ═══════ BUFF TRACKING ═══════

    /// <summary>
    /// Active buff data structure
    /// </summary>
    [System.Serializable]
    public class ActiveBuff
    {
        public string buffName;
        public float remainingDuration;
        public float damageMultiplier;
        public float speedMultiplier;
        public float defenseMultiplier;
        public bool grantsImmortality;
        public SpellData sourceSpell;

        public ActiveBuff(SpellData spell)
        {
            buffName = spell.spellName;
            remainingDuration = spell.buffDuration;
            damageMultiplier = spell.damageMultiplier;
            speedMultiplier = spell.speedMultiplier;
            defenseMultiplier = spell.defenseMultiplier;
            grantsImmortality = spell.grantImmortality;
            sourceSpell = spell;
        }
    }

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("═══ ACTIVE BUFFS (Read-Only) ═══")]
    [SerializeField] private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    #endregion

    #region ═══════ EVENTS ═══════

    /// <summary>Fired when a new buff is applied</summary>
    public static event Action<ActiveBuff> OnBuffApplied;

    /// <summary>Fired when a buff expires</summary>
    public static event Action<ActiveBuff> OnBuffExpired;

    /// <summary>Fired when buff multipliers change</summary>
    public static event Action OnMultipliersChanged;

    #endregion

    #region ═══════ PROPERTIES ═══════

    /// <summary>Current combined damage multiplier from all buffs</summary>
    public float DamageMultiplier
    {
        get
        {
            float multiplier = 1f;
            foreach (var buff in activeBuffs)
            {
                multiplier *= buff.damageMultiplier;
            }
            return multiplier;
        }
    }

    /// <summary>Current combined speed multiplier from all buffs</summary>
    public float SpeedMultiplier
    {
        get
        {
            float multiplier = 1f;
            foreach (var buff in activeBuffs)
            {
                multiplier *= buff.speedMultiplier;
            }
            return multiplier;
        }
    }

    /// <summary>Current combined defense multiplier from all buffs (lower = better)</summary>
    public float DefenseMultiplier
    {
        get
        {
            float multiplier = 1f;
            foreach (var buff in activeBuffs)
            {
                multiplier *= buff.defenseMultiplier;
            }
            return multiplier;
        }
    }

    /// <summary>Are any buffs currently active?</summary>
    public bool HasActiveBuffs => activeBuffs.Count > 0;

    /// <summary>Number of active buffs</summary>
    public int ActiveBuffCount => activeBuffs.Count;

    /// <summary>Is the player currently immortal (any buff with immortality)?</summary>
    public bool IsImmortal
    {
        get
        {
            foreach (var buff in activeBuffs)
            {
                if (buff.grantsImmortality) return true;
            }
            return false;
        }
    }

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            // Allow replacement if on player
            if (gameObject.CompareTag("Player"))
            {
                Destroy(Instance.gameObject);
                Instance = this;
            }
            else
            {
                Destroy(this);
                return;
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        UpdateBuffTimers();
    }

    #endregion

    #region ═══════ BUFF MANAGEMENT ═══════

    /// <summary>
    /// Apply a new buff from SpellData
    /// </summary>
    public void ApplyBuff(SpellData spellData)
    {
        if (spellData == null) return;

        // Check if buff already exists - refresh duration instead of stacking
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].sourceSpell == spellData)
            {
                // Refresh duration
                activeBuffs[i].remainingDuration = spellData.buffDuration;

                if (showDebugLogs)
                {
                    Debug.Log($"[PlayerBuffManager] 🔄 Refreshed buff: {spellData.spellName}");
                }
                return;
            }
        }

        // Add new buff
        ActiveBuff newBuff = new ActiveBuff(spellData);
        activeBuffs.Add(newBuff);

        OnBuffApplied?.Invoke(newBuff);
        OnMultipliersChanged?.Invoke();

        if (showDebugLogs)
        {
            Debug.Log($"[PlayerBuffManager] ⚡ Applied buff: {spellData.spellName}");
            Debug.Log($"[PlayerBuffManager] Current multipliers - Damage: x{DamageMultiplier:F2}, Speed: x{SpeedMultiplier:F2}, Defense: x{DefenseMultiplier:F2}");
        }
    }

    /// <summary>
    /// Remove a specific buff by spell name
    /// </summary>
    public void RemoveBuff(string buffName)
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].buffName == buffName)
            {
                ActiveBuff expiredBuff = activeBuffs[i];
                activeBuffs.RemoveAt(i);

                OnBuffExpired?.Invoke(expiredBuff);
                OnMultipliersChanged?.Invoke();

                if (showDebugLogs)
                {
                    Debug.Log($"[PlayerBuffManager] ❌ Removed buff: {buffName}");
                }
            }
        }
    }

    /// <summary>
    /// Clear all active buffs
    /// </summary>
    public void ClearAllBuffs()
    {
        foreach (var buff in activeBuffs)
        {
            OnBuffExpired?.Invoke(buff);
        }

        activeBuffs.Clear();
        OnMultipliersChanged?.Invoke();

        if (showDebugLogs)
        {
            Debug.Log("[PlayerBuffManager] 🧹 Cleared all buffs");
        }
    }

    /// <summary>
    /// Update buff timers and remove expired buffs
    /// </summary>
    private void UpdateBuffTimers()
    {
        bool buffExpired = false;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].remainingDuration -= Time.deltaTime;

            if (activeBuffs[i].remainingDuration <= 0)
            {
                ActiveBuff expiredBuff = activeBuffs[i];
                activeBuffs.RemoveAt(i);

                OnBuffExpired?.Invoke(expiredBuff);
                buffExpired = true;

                if (showDebugLogs)
                {
                    Debug.Log($"[PlayerBuffManager] ⏰ Buff expired: {expiredBuff.buffName}");
                }
            }
        }

        if (buffExpired)
        {
            OnMultipliersChanged?.Invoke();
        }
    }

    #endregion

    #region ═══════ PUBLIC GETTERS ═══════

    /// <summary>
    /// Get damage multiplier (for combat systems to query)
    /// </summary>
    public float GetDamageMultiplier()
    {
        return DamageMultiplier;
    }

    /// <summary>
    /// Get speed multiplier (for movement systems to query)
    /// </summary>
    public float GetSpeedMultiplier()
    {
        return SpeedMultiplier;
    }

    /// <summary>
    /// Get defense multiplier (for damage reduction calculations)
    /// </summary>
    public float GetDefenseMultiplier()
    {
        return DefenseMultiplier;
    }

    /// <summary>
    /// Check if a specific buff is active
    /// </summary>
    public bool HasBuff(string buffName)
    {
        foreach (var buff in activeBuffs)
        {
            if (buff.buffName == buffName) return true;
        }
        return false;
    }

    /// <summary>
    /// Get remaining duration of a specific buff
    /// </summary>
    public float GetBuffRemainingDuration(string buffName)
    {
        foreach (var buff in activeBuffs)
        {
            if (buff.buffName == buffName) return buff.remainingDuration;
        }
        return 0f;
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("⚡ Test: Apply 10s Damage Buff")]
    private void TestApplyDamageBuff()
    {
        // Create temporary spell data for testing
        SpellData testSpell = ScriptableObject.CreateInstance<SpellData>();
        testSpell.spellName = "Test Damage Buff";
        testSpell.buffDuration = 10f;
        testSpell.damageMultiplier = 1.5f;
        testSpell.speedMultiplier = 1.0f;
        testSpell.defenseMultiplier = 1.0f;

        ApplyBuff(testSpell);
    }

    [ContextMenu("🧹 Test: Clear All Buffs")]
    private void TestClearBuffs()
    {
        ClearAllBuffs();
    }

    [ContextMenu("📊 Debug: Print Status")]
    private void DebugPrintStatus()
    {
        Debug.Log($"[PlayerBuffManager] Active Buffs: {activeBuffs.Count}");
        Debug.Log($"[PlayerBuffManager] Damage: x{DamageMultiplier:F2}, Speed: x{SpeedMultiplier:F2}, Defense: x{DefenseMultiplier:F2}");
        foreach (var buff in activeBuffs)
        {
            Debug.Log($"  - {buff.buffName}: {buff.remainingDuration:F1}s remaining");
        }
    }

    #endregion
}
