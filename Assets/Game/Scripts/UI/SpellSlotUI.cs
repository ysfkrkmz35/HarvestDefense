using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// Spell Slot UI
/// - Displays 4 spell slots in a horizontal bar
/// - Shows spell icons, cooldown overlays, key hints
/// - Supports mouse click to select/cast spells
/// - Highlights selected slot
/// </summary>
public class SpellSlotUI : MonoBehaviour
{
    #region ═══════ STRUCTS ═══════

    [System.Serializable]
    public class SlotUI
    {
        [Tooltip("Root object for this slot")]
        public RectTransform root;

        [Tooltip("Spell icon image")]
        public Image icon;

        [Tooltip("Cooldown overlay (fills from bottom)")]
        public Image cooldownOverlay;

        [Tooltip("Cooldown text (seconds remaining)")]
        public TextMeshProUGUI cooldownText;

        [Tooltip("Key hint text (1, 2, 3, 4)")]
        public TextMeshProUGUI keyHintText;

        [Tooltip("Selection highlight border")]
        public Image selectionBorder;

        [Tooltip("Locked overlay (for unavailable spells)")]
        public Image lockedOverlay;

        [Tooltip("Button for mouse selection")]
        public Button selectButton;
    }

    #endregion

    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ SLOT REFERENCES ═══")]
    [SerializeField] private SlotUI slot1;
    [SerializeField] private SlotUI slot2;
    [SerializeField] private SlotUI slot3;
    [SerializeField] private SlotUI slot4;

    [Header("═══ VISUAL SETTINGS ═══")]
    [SerializeField] private Color iconNormalColor = Color.white;
    [SerializeField] private Color iconCooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color selectedBorderColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color unselectedBorderColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color noManaColor = new Color(0.2f, 0.2f, 0.5f, 1f);

    [Header("═══ ANIMATION ═══")]
    [SerializeField] private float pulseDuration = 0.15f;
    [SerializeField] private float pulseScale = 1.15f;

    [Header("═══ DEFAULT ICON ═══")]
    [Tooltip("Icon shown when slot is empty")]
    [SerializeField] private Sprite emptySlotIcon;

    #endregion

    #region ═══════ RUNTIME STATE ═══════

    private SlotUI[] slots;
    private int currentSelectedSlot = 0;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    private void Start()
    {
        // Initialize slots array
        slots = new SlotUI[4] { slot1, slot2, slot3, slot4 };

        // Subscribe to events
        SpellManager.OnSlotSelected += OnSlotSelected;

        // Setup button listeners
        SetupButtonListeners();

        // Initialize display
        UpdateAllSlots();
        UpdateSelection(0);
    }

    private void OnDestroy()
    {
        SpellManager.OnSlotSelected -= OnSlotSelected;
    }

    private void Update()
    {
        UpdateCooldowns();
    }

    #endregion

    #region ═══════ SETUP ═══════

    private void SetupButtonListeners()
    {
        for (int i = 0; i < 4; i++)
        {
            if (slots[i]?.selectButton != null)
            {
                int slotIndex = i; // Capture for closure
                slots[i].selectButton.onClick.AddListener(() => OnSlotClicked(slotIndex));
            }

            // Set key hints
            if (slots[i]?.keyHintText != null)
            {
                slots[i].keyHintText.text = (i + 1).ToString();
            }
        }
    }

    #endregion

    #region ═══════ EVENT HANDLERS ═══════

    private void OnSlotSelected(int slotIndex)
    {
        UpdateSelection(slotIndex);
    }

    private void OnSlotClicked(int slotIndex)
    {
        // Tell SpellManager to select and cast
        if (SpellManager.Instance != null)
        {
            if (SpellManager.Instance.SelectedSlot == slotIndex)
            {
                // Same slot - just cast
                SpellManager.Instance.CastSelectedSpell();
            }
            else
            {
                // Different slot - select it (will also cast based on SpellManager settings)
                SpellManager.Instance.SelectSlot(slotIndex);
            }
        }

        // Play click animation
        StartCoroutine(PulseSlot(slotIndex));
    }

    #endregion

    #region ═══════ UI UPDATES ═══════

    /// <summary>
    /// Update all slot displays
    /// </summary>
    public void UpdateAllSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            UpdateSlotDisplay(i);
        }
    }

    /// <summary>
    /// Update single slot display
    /// </summary>
    private void UpdateSlotDisplay(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4 || slots[slotIndex] == null) return;

        SlotUI slot = slots[slotIndex];
        SpellData spellData = SpellManager.Instance?.GetSpellDataAtSlot(slotIndex);

        // Icon
        if (slot.icon != null)
        {
            if (spellData != null && spellData.icon != null)
            {
                slot.icon.sprite = spellData.icon;
                slot.icon.color = iconNormalColor;
            }
            else
            {
                slot.icon.sprite = emptySlotIcon;
                slot.icon.color = new Color(1, 1, 1, 0.3f);
            }
        }

        // Locked overlay
        if (slot.lockedOverlay != null)
        {
            bool isLocked = spellData == null ||
                           (SpellManager.Instance != null && !SpellManager.Instance.IsSpellUnlocked(spellData));
            slot.lockedOverlay.gameObject.SetActive(isLocked && spellData != null);
        }
    }

    private void UpdateSelection(int selectedIndex)
    {
        currentSelectedSlot = selectedIndex;

        for (int i = 0; i < 4; i++)
        {
            if (slots[i]?.selectionBorder != null)
            {
                bool isSelected = i == selectedIndex;
                slots[i].selectionBorder.color = isSelected ? selectedBorderColor : unselectedBorderColor;

                // Scale selected slot slightly
                if (slots[i].root != null)
                {
                    slots[i].root.localScale = isSelected ? Vector3.one * 1.05f : Vector3.one;
                }
            }
        }
    }

    private void UpdateCooldowns()
    {
        if (SpellManager.Instance == null) return;

        for (int i = 0; i < 4; i++)
        {
            UpdateSlotCooldown(i);
        }
    }

    private void UpdateSlotCooldown(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4 || slots[slotIndex] == null) return;

        SlotUI slot = slots[slotIndex];
        SpellBase spell = SpellManager.Instance?.GetSpellAtSlot(slotIndex);

        if (spell == null)
        {
            // Empty slot
            if (slot.cooldownOverlay != null)
                slot.cooldownOverlay.fillAmount = 0;
            if (slot.cooldownText != null)
                slot.cooldownText.text = "";
            return;
        }

        // Cooldown overlay
        if (slot.cooldownOverlay != null)
        {
            float progress = spell.CooldownProgress;
            slot.cooldownOverlay.fillAmount = 1f - progress;
        }

        // Cooldown text
        if (slot.cooldownText != null)
        {
            if (spell.IsOnCooldown)
            {
                slot.cooldownText.text = spell.RemainingCooldown.ToString("F1");
            }
            else
            {
                slot.cooldownText.text = "";
            }
        }

        // Icon color based on state
        if (slot.icon != null)
        {
            if (spell.IsOnCooldown)
            {
                slot.icon.color = iconCooldownColor;
            }
            else if (!spell.HasEnoughMana)
            {
                slot.icon.color = noManaColor;
            }
            else
            {
                slot.icon.color = iconNormalColor;
            }
        }
    }

    #endregion

    #region ═══════ ANIMATIONS ═══════

    private IEnumerator PulseSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4 || slots[slotIndex]?.root == null) yield break;

        RectTransform root = slots[slotIndex].root;
        Vector3 originalScale = root.localScale;
        Vector3 targetScale = originalScale * pulseScale;

        float elapsed = 0f;
        float halfDuration = pulseDuration / 2f;

        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            root.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / halfDuration);
            yield return null;
        }

        elapsed = 0f;

        // Scale down
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            root.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / halfDuration);
            yield return null;
        }

        root.localScale = originalScale;
    }

    /// <summary>
    /// Flash animation when spell is cast
    /// </summary>
    public void PlayCastAnimation(int slotIndex)
    {
        StartCoroutine(PulseSlot(slotIndex));
    }

    #endregion

    #region ═══════ PUBLIC API ═══════

    /// <summary>
    /// Force refresh all slot displays
    /// </summary>
    public void RefreshDisplay()
    {
        UpdateAllSlots();
        UpdateSelection(currentSelectedSlot);
    }

    #endregion
}
