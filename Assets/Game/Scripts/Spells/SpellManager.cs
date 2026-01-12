using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Spell Manager
/// - Manages multiple spell slots (keys 1, 2, 3, 4)
/// - Handles input for casting
/// - Tracks unlocked spells
/// - Supports mouse-click spell selection
/// </summary>
public class SpellManager : MonoBehaviour
{
    #region ═══════ SINGLETON ═══════

    public static SpellManager Instance { get; private set; }

    #endregion

    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ SPELL SLOTS ═══")]
    [Tooltip("Spell in slot 1 (key: 1)")]
    [SerializeField] private SpellBase spellSlot1;

    [Tooltip("Spell in slot 2 (key: 2)")]
    [SerializeField] private SpellBase spellSlot2;

    [Tooltip("Spell in slot 3 (key: 3)")]
    [SerializeField] private SpellBase spellSlot3;

    [Tooltip("Spell in slot 4 (key: 4)")]
    [SerializeField] private SpellBase spellSlot4;

    [Header("═══ AVAILABLE SPELLS ═══")]
    [Tooltip("All spell data available in the game")]
    [SerializeField] private List<SpellData> allSpells = new List<SpellData>();

    [Header("═══ INPUT SETTINGS ═══")]
    [Tooltip("Allow keyboard input for casting (1-4 keys)")]
    [SerializeField] private bool enableKeyboardInput = true;

    [Tooltip("Allow mouse click to cast selected spell")]
    [SerializeField] private bool enableMouseCast = true;

    [Tooltip("Mouse button for casting (0=Left, 1=Right, 2=Middle)")]
    [SerializeField] private int castMouseButton = 1;

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region ═══════ RUNTIME STATE ═══════

    private int selectedSlot = 0; // 0-3 for slots 1-4
    private List<SpellData> unlockedSpells = new List<SpellData>();
    private SpellBase[] spellSlots;

    #endregion

    #region ═══════ PROPERTIES ═══════

    /// <summary>Currently selected slot index (0-3)</summary>
    public int SelectedSlot => selectedSlot;

    /// <summary>Get the currently selected spell</summary>
    public SpellBase SelectedSpell => spellSlots?[selectedSlot];

    /// <summary>Get spell data for selected spell</summary>
    public SpellData SelectedSpellData => SelectedSpell?.Data;

    /// <summary>List of all unlocked spells</summary>
    public IReadOnlyList<SpellData> UnlockedSpells => unlockedSpells;

    #endregion

    #region ═══════ EVENTS ═══════

    /// <summary>Fired when selected slot changes. Parameter: new slot index (0-3)</summary>
    public static event Action<int> OnSlotSelected;

    /// <summary>Fired when a spell is unlocked. Parameter: spell data</summary>
    public static event Action<SpellData> OnSpellUnlocked;

    /// <summary>Fired when a spell is equipped to a slot. Parameters: slot index, spell data</summary>
    public static event Action<int, SpellData> OnSpellEquipped;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // AUTO-LOAD FIX: If inspector list is empty, try to find all SpellData assets
        if (allSpells == null || allSpells.Count == 0)
        {
            Debug.LogWarning("[SpellManager] ⚠️ 'All Spells' list is empty! Attempting to auto-load...");
            
            // Try 1: Resources folder
            allSpells = new List<SpellData>(Resources.LoadAll<SpellData>(""));
            
            // Try 2: Resources/Spells subfolder
            if (allSpells.Count == 0)
            {
                allSpells = new List<SpellData>(Resources.LoadAll<SpellData>("Spells"));
            }
            
            // Try 3: FindObjectsOfTypeAll (works in editor, finds all loaded assets)
            #if UNITY_EDITOR
            if (allSpells.Count == 0)
            {
                var foundAssets = UnityEditor.AssetDatabase.FindAssets("t:SpellData");
                foreach (var guid in foundAssets)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var spellData = UnityEditor.AssetDatabase.LoadAssetAtPath<SpellData>(path);
                    if (spellData != null && !allSpells.Contains(spellData))
                    {
                        allSpells.Add(spellData);
                    }
                }
                if (allSpells.Count > 0)
                {
                    Debug.Log($"[SpellManager] ✅ Found {allSpells.Count} spells via AssetDatabase (Editor only).");
                }
            }
            #endif
            
            // Try 4: Runtime fallback - search loaded ScriptableObjects
            if (allSpells.Count == 0)
            {
                var allLoadedSpells = Resources.FindObjectsOfTypeAll<SpellData>();
                allSpells = new List<SpellData>(allLoadedSpells);
            }
            
            if (allSpells.Count > 0)
            {
                Debug.Log($"[SpellManager] ✅ Auto-loaded {allSpells.Count} spells: {string.Join(", ", allSpells.ConvertAll(s => s.spellName))}");
            }
            else
            {
                Debug.LogError("[SpellManager] ❌ Could not find any SpellData! Please assign spells in the inspector or move assets to Resources/Spells/");
            }
        }

        // Initialize spell slots array
        spellSlots = new SpellBase[4];
        spellSlots[0] = spellSlot1;
        spellSlots[1] = spellSlot2;
        spellSlots[2] = spellSlot3;
        spellSlots[3] = spellSlot4;

        // Initialize unlocked spells (default unlocked ones)
        foreach (var spell in allSpells)
        {
            if (spell != null && spell.unlockedByDefault)
            {
                unlockedSpells.Add(spell);
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
        HandleInput();
    }

    #endregion

    #region ═══════ INPUT HANDLING ═══════

    private void HandleInput()
    {
        if (enableKeyboardInput)
        {
            // Number keys for slot SELECTION only (not casting)
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SelectSlot(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SelectSlot(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SelectSlot(2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SelectSlot(3);
            }
        }

        // Right-click casts selected spell
        if (enableMouseCast && Input.GetMouseButtonDown(castMouseButton))
        {
            CastSelectedSpell();
        }
    }

    // SelectSlotAndCast kept for backwards compatibility with UI clicks
    private void SelectSlotAndCast(int slotIndex)
    {
        SelectSlot(slotIndex);
        CastSelectedSpell();
    }

    #endregion

    #region ═══════ SLOT MANAGEMENT ═══════

    /// <summary>
    /// Select a spell slot (0-3)
    /// </summary>
    public void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;

        selectedSlot = slotIndex;
        OnSlotSelected?.Invoke(selectedSlot);

        if (showDebugLogs)
        {
            string spellName = spellSlots[selectedSlot]?.Data?.spellName ?? "Empty";
            Debug.Log($"[SpellManager] 🎯 Selected slot {slotIndex + 1}: {spellName}");
        }
    }

    /// <summary>
    /// Get spell at specific slot
    /// </summary>
    public SpellBase GetSpellAtSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return null;
        return spellSlots[slotIndex];
    }

    /// <summary>
    /// Get spell data at specific slot
    /// </summary>
    public SpellData GetSpellDataAtSlot(int slotIndex)
    {
        return GetSpellAtSlot(slotIndex)?.Data;
    }

    /// <summary>
    /// Assign a spell component to a slot
    /// </summary>
    public void AssignSpellToSlot(int slotIndex, SpellBase spell)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;

        spellSlots[slotIndex] = spell;

        // Update serialized fields
        switch (slotIndex)
        {
            case 0: spellSlot1 = spell; break;
            case 1: spellSlot2 = spell; break;
            case 2: spellSlot3 = spell; break;
            case 3: spellSlot4 = spell; break;
        }

        OnSpellEquipped?.Invoke(slotIndex, spell?.Data);

        if (showDebugLogs)
        {
            Debug.Log($"[SpellManager] ✨ Equipped {spell?.Data?.spellName ?? "None"} to slot {slotIndex + 1}");
        }
    }

    #endregion

    #region ═══════ CASTING ═══════

    /// <summary>
    /// Cast the currently selected spell
    /// </summary>
    public void CastSelectedSpell()
    {
        CastSpellInSlot(selectedSlot);
    }

    /// <summary>
    /// Cast spell in specific slot
    /// </summary>
    public void CastSpellInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;

        SpellBase spell = spellSlots[slotIndex];
        if (spell == null)
        {
            if (showDebugLogs) Debug.Log($"[SpellManager] ⚠️ Slot {slotIndex + 1} is empty!");
            return;
        }

        spell.TryCastAtMouse();
    }

    #endregion

    #region ═══════ UNLOCK SYSTEM ═══════

    /// <summary>
    /// Check if a spell is unlocked
    /// </summary>
    public bool IsSpellUnlocked(SpellData spell)
    {
        return spell != null && unlockedSpells.Contains(spell);
    }

    /// <summary>
    /// Try to unlock a spell (checks requirements and deducts gold)
    /// </summary>
    public bool TryUnlockSpell(SpellData spell)
    {
        if (spell == null) return false;
        if (IsSpellUnlocked(spell)) return true; // Already unlocked

        // Check level requirement
        int playerLevel = PlayerProgression.Instance?.CurrentLevel ?? 1;
        if (!spell.MeetsLevelRequirement(playerLevel))
        {
            if (showDebugLogs)
                Debug.Log($"[SpellManager] ❌ Level too low! Need {spell.requiredLevel}, have {playerLevel}");
            return false;
        }

        // Check and spend gold
        if (spell.goldCost > 0)
        {
            if (PlayerProgression.Instance == null ||
                !PlayerProgression.Instance.SpendGold(spell.goldCost))
            {
                if (showDebugLogs)
                    Debug.Log($"[SpellManager] ❌ Not enough gold! Need {spell.goldCost}");
                return false;
            }
        }

        // Unlock the spell
        unlockedSpells.Add(spell);
        OnSpellUnlocked?.Invoke(spell);

        if (showDebugLogs)
        {
            Debug.Log($"[SpellManager] 🔓 Unlocked spell: {spell.spellName}");
        }

        return true;
    }

    /// <summary>
    /// Force unlock a spell (no requirements check)
    /// </summary>
    public void ForceUnlock(SpellData spell)
    {
        if (spell == null || IsSpellUnlocked(spell)) return;

        unlockedSpells.Add(spell);
        OnSpellUnlocked?.Invoke(spell);
    }

    /// <summary>
    /// Set unlocked spells from a list of spell names (for loading saves)
    /// </summary>
    public void SetUnlockedSpellsByName(List<string> spellNames)
    {
        if (spellNames == null) return;

        // Clear current unlocked spells (except defaults)
        unlockedSpells.Clear();

        // Debug: List all available spells
        if (showDebugLogs)
        {
             Debug.Log($"[SpellManager] 🔍 Available spells in database ({allSpells.Count}): " + 
                       string.Join(", ", allSpells.Select(s => s != null ? $"'{s.spellName}'" : "null")));
             Debug.Log($"[SpellManager] 🔍 Trying to restore names: " + string.Join(", ", spellNames));
        }

        // Re-add default unlocked spells
        foreach (var spell in allSpells)
        {
            if (spell != null && spell.unlockedByDefault)
            {
                unlockedSpells.Add(spell);
            }
        }

        // Add saved unlocked spells
        foreach (var spellName in spellNames)
        {
            if (string.IsNullOrEmpty(spellName)) continue;

            // Trim name just in case
            var cleanName = spellName.Trim();
            
            var spell = allSpells.Find(s => s != null && s.spellName == cleanName);
            if (spell != null && !unlockedSpells.Contains(spell))
            {
                unlockedSpells.Add(spell);
                if (showDebugLogs)
                {
                    Debug.Log($"[SpellManager] 📂 Restored unlocked spell: '{cleanName}'");
                }
            }
            else
            {
                if (showDebugLogs) Debug.LogWarning($"[SpellManager] ⚠️ Could not find spell with name: '{cleanName}'");
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[SpellManager] 📂 Restored {unlockedSpells.Count} unlocked spells");
        }
    }

    /// <summary>
    /// Get the names of spells equipped in each slot (for saving)
    /// Returns array of 4 strings (empty string if slot is empty)
    /// </summary>
    public string[] GetEquippedSpellNames()
    {
        string[] names = new string[4];
        for (int i = 0; i < 4; i++)
        {
            names[i] = spellSlots[i]?.Data?.spellName ?? "";
        }
        return names;
    }

    /// <summary>
    /// Set equipped spells by name (for restoring saves)
    /// Creates SpellBase components if they don't exist
    /// </summary>
    public void SetEquippedSpellsByName(string[] slotNames)
    {
        if (slotNames == null || slotNames.Length != 4) return;

        if (showDebugLogs) Debug.Log($"[SpellManager] 📂 Restoring equipped spells: [{string.Join(", ", slotNames)}]");

        for (int i = 0; i < 4; i++)
        {
            if (string.IsNullOrEmpty(slotNames[i]))
            {
                // Clear the slot
                spellSlots[i] = null;
                continue;
            }

            // Find the spell data by name
            var spellData = allSpells.Find(s => s != null && s.spellName == slotNames[i]);
            if (spellData == null)
            {
                if (showDebugLogs) Debug.LogWarning($"[SpellManager] ⚠️ Could not find spell data for: '{slotNames[i]}'");
                continue;
            }

            // Try to find existing SpellBase component
            var existingSpell = GetComponentsInChildren<SpellBase>(true)
                .FirstOrDefault(s => s.Data == spellData);
            
            if (existingSpell != null)
            {
                spellSlots[i] = existingSpell;
                if (showDebugLogs) Debug.Log($"[SpellManager] 📂 Restored slot {i + 1}: {slotNames[i]} (existing)");
            }
            else
            {
                // CREATE spell component dynamically (like SpellItem does)
                SpellBase newSpell = CreateSpellComponent(spellData);
                if (newSpell != null)
                {
                    spellSlots[i] = newSpell;
                    if (showDebugLogs) Debug.Log($"[SpellManager] ✨ Created and restored slot {i + 1}: {slotNames[i]}");
                }
                else
                {
                    if (showDebugLogs) Debug.LogError($"[SpellManager] ❌ Failed to create spell: {slotNames[i]}");
                }
            }
        }

        // Update serialized fields
        spellSlot1 = spellSlots[0];
        spellSlot2 = spellSlots[1];
        spellSlot3 = spellSlots[2];
        spellSlot4 = spellSlots[3];
        
        if (showDebugLogs)
        {
            Debug.Log($"[SpellManager] 📂 Equipped slots after restore: " +
                $"[{spellSlots[0]?.Data?.spellName ?? "Empty"}, " +
                $"{spellSlots[1]?.Data?.spellName ?? "Empty"}, " +
                $"{spellSlots[2]?.Data?.spellName ?? "Empty"}, " +
                $"{spellSlots[3]?.Data?.spellName ?? "Empty"}]");
        }
    }

    /// <summary>
    /// Create a SpellBase component for the given SpellData
    /// Used for restoring spells from saves
    /// </summary>
    private SpellBase CreateSpellComponent(SpellData spellData)
    {
        if (spellData == null) return null;

        // Create spell object as child of SpellManager
        GameObject spellObj = new GameObject(spellData.spellName);
        spellObj.transform.SetParent(transform);

        SpellBase spellBase = null;
        try
        {
            // Add appropriate component based on spell type
            switch (spellData.spellType)
            {
                case SpellType.Buff:
                    spellBase = spellObj.AddComponent<BuffSpell>();
                    break;
                case SpellType.SelfHeal:
                    spellBase = spellObj.AddComponent<HealSpell>();
                    break;
                case SpellType.Area:
                case SpellType.Projectile:
                case SpellType.Melee:
                default:
                    spellBase = spellObj.AddComponent<AreaSpell>();
                    break;
            }

            if (spellBase != null)
            {
                spellBase.SetSpellData(spellData);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SpellManager] Exception creating spell component: {ex.Message}");
            if (spellObj != null) Destroy(spellObj);
            return null;
        }

        return spellBase;
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("🔥 Test: Cast Slot 1")]
    private void TestCastSlot1() { CastSpellInSlot(0); }

    [ContextMenu("🔥 Test: Cast Slot 2")]
    private void TestCastSlot2() { CastSpellInSlot(1); }

    [ContextMenu("📊 Debug: Print Status")]
    private void DebugPrintStatus()
    {
        Debug.Log($"[SpellManager] Selected: {selectedSlot + 1}, Unlocked: {unlockedSpells.Count}");
        for (int i = 0; i < 4; i++)
        {
            string name = spellSlots[i]?.Data?.spellName ?? "Empty";
            Debug.Log($"  Slot {i + 1}: {name}");
        }
    }

    #endregion
}
