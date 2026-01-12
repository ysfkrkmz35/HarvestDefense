using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Days Survived UI
/// - Displays current day count on HUD
/// - Animates on day change
/// - Follows existing UI patterns (GoldUI style)
/// </summary>
public class DaysSurvivedUI : MonoBehaviour
{
    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ UI REFERENCES ═══")]
    [Tooltip("Day count text (e.g., 'Day 5')")]
    [SerializeField] private TextMeshProUGUI dayText;

    [Tooltip("Day icon (optional, sun/calendar)")]
    [SerializeField] private Image dayIcon;

    [Tooltip("Background panel (optional)")]
    [SerializeField] private Image backgroundPanel;

    [Header("═══ FORMAT ═══")]
    [Tooltip("Format string for day display (use {0} for day number)")]
    [SerializeField] private string formatString = "Day {0}";

    [Header("═══ ANIMATION SETTINGS ═══")]
    [Tooltip("Animation duration for day change")]
    [SerializeField] private float animationDuration = 0.3f;

    [Tooltip("Scale punch amount on day change")]
    [SerializeField] private float punchScale = 1.3f;

    [Header("═══ COLORS ═══")]
    [SerializeField] private Color normalColor = new Color(1f, 0.9f, 0.6f, 1f); // Warm yellow
    [SerializeField] private Color newDayFlashColor = new Color(1f, 1f, 1f, 1f); // White flash

    #endregion

    #region ═══════ RUNTIME STATE ═══════

    private int displayedDay;
    private RectTransform textRect;
    private Vector3 originalScale;
    private float animationTimer;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    private void Start()
    {
        // Auto-discover components if not assigned
        AutoDiscoverComponents();

        // Subscribe to events
        DaysSurvivedTracker.OnDaysChanged += OnDaysChanged;
        DaysSurvivedTracker.OnNewDayStarted += OnNewDayStarted;

        // Cache references
        if (dayText != null)
        {
            textRect = dayText.GetComponent<RectTransform>();
            originalScale = textRect != null ? textRect.localScale : Vector3.one;
            dayText.color = normalColor;
        }

        // Initialize display
        InitializeDisplay();
    }

    private void AutoDiscoverComponents()
    {
        // Auto-find dayText if not assigned
        if (dayText == null)
        {
            var textObj = transform.Find("DayText");
            if (textObj != null) dayText = textObj.GetComponent<TextMeshProUGUI>();
            // Fallback: find first TMP text
            if (dayText == null) dayText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Auto-find dayIcon if not assigned
        if (dayIcon == null)
        {
            var iconObj = transform.Find("SunIcon");
            if (iconObj != null) dayIcon = iconObj.GetComponent<Image>();
        }
    }

    private void OnDestroy()
    {
        DaysSurvivedTracker.OnDaysChanged -= OnDaysChanged;
        DaysSurvivedTracker.OnNewDayStarted -= OnNewDayStarted;
    }

    private void Update()
    {
        UpdateAnimation();
    }

    #endregion

    #region ═══════ INITIALIZATION ═══════

    private void InitializeDisplay()
    {
        if (DaysSurvivedTracker.Instance != null)
        {
            displayedDay = DaysSurvivedTracker.Instance.DaysSurvived;
            UpdateDayText();
        }
        else
        {
            // Default to day 1 if tracker not available yet
            displayedDay = 1;
            UpdateDayText();
        }
    }

    #endregion

    #region ═══════ EVENT HANDLERS ═══════

    private void OnDaysChanged(int newDay, int delta)
    {
        displayedDay = newDay;
        UpdateDayText();
    }

    private void OnNewDayStarted(int newDay)
    {
        // Trigger animation for new day
        animationTimer = animationDuration;

        // Flash color
        if (dayText != null)
        {
            dayText.color = newDayFlashColor;
        }

        Debug.Log($"[DaysSurvivedUI] 🌅 New day animation: Day {newDay}");
    }

    #endregion

    #region ═══════ UI UPDATES ═══════

    private void UpdateDayText()
    {
        if (dayText != null)
        {
            dayText.text = string.Format(formatString, displayedDay);
        }
    }

    #endregion

    #region ═══════ ANIMATION ═══════

    private void UpdateAnimation()
    {
        if (animationTimer <= 0) return;

        animationTimer -= Time.deltaTime;
        float t = animationTimer / animationDuration;

        // Scale punch effect
        if (textRect != null)
        {
            float scale = Mathf.Lerp(1f, punchScale, t);
            textRect.localScale = originalScale * scale;
        }

        // Color fade from flash to normal
        if (dayText != null)
        {
            dayText.color = Color.Lerp(normalColor, newDayFlashColor, t);
        }

        // Animation finished
        if (animationTimer <= 0)
        {
            if (textRect != null)
            {
                textRect.localScale = originalScale;
            }
            if (dayText != null)
            {
                dayText.color = normalColor;
            }
        }
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("🌅 Test: New Day Animation")]
    private void TestNewDayAnimation()
    {
        displayedDay++;
        UpdateDayText();
        OnNewDayStarted(displayedDay);
    }

    #endregion
}
