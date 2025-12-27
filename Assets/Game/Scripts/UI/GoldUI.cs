using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Gold/Currency UI Display
/// - Shows current gold amount
/// - Animates on gold gain/loss
/// - Coin icon with optional pulse
/// </summary>
public class GoldUI : MonoBehaviour
{
    [Header("═══ UI REFERENCES ═══")]
    [Tooltip("Gold amount text")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Tooltip("Gold/Coin icon (optional)")]
    [SerializeField] private Image goldIcon;

    [Tooltip("Background panel (optional)")]
    [SerializeField] private Image backgroundPanel;

    [Header("═══ ANIMATION SETTINGS ═══")]
    [Tooltip("How fast the number counts up")]
    [SerializeField] private float countSpeed = 50f;

    [Tooltip("Pulse duration when gaining gold")]
    [SerializeField] private float pulseDuration = 0.2f;

    [Tooltip("Pulse scale amount")]
    [SerializeField] private float pulseScale = 1.2f;

    [Header("═══ COLORS ═══")]
    [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0f, 1f); // Gold yellow
    [SerializeField] private Color gainColor = new Color(0.5f, 1f, 0.5f, 1f); // Green
    [SerializeField] private Color lossColor = new Color(1f, 0.3f, 0.3f, 1f); // Red

    [Header("═══ FORMAT ═══")]
    [Tooltip("Format string for gold display")]
    [SerializeField] private string formatString = "{0}";

    [Tooltip("Use K/M abbreviations for large numbers")]
    [SerializeField] private bool useAbbreviation = true;

    // Internal state
    private int displayedGold;
    private int targetGold;
    private RectTransform iconRect;
    private Vector3 originalIconScale;
    private Coroutine pulseCoroutine;

    private void Start()
    {
        // Subscribe to events
        PlayerProgression.OnGoldChanged += OnGoldChanged;

        // Cache references
        if (goldIcon != null)
        {
            iconRect = goldIcon.GetComponent<RectTransform>();
            originalIconScale = iconRect.localScale;
        }

        // Set initial color
        if (goldText != null)
        {
            goldText.color = normalColor;
        }

        // Initialize display
        InitializeDisplay();
    }

    private void OnDestroy()
    {
        PlayerProgression.OnGoldChanged -= OnGoldChanged;
    }

    private void Update()
    {
        AnimateGoldCount();
    }

    #region ═══════ INITIALIZATION ═══════

    private void InitializeDisplay()
    {
        if (PlayerProgression.Instance != null)
        {
            displayedGold = PlayerProgression.Instance.Gold;
            targetGold = displayedGold;
            UpdateGoldText();
        }
    }

    #endregion

    #region ═══════ EVENT HANDLERS ═══════

    private void OnGoldChanged(int newAmount, int delta)
    {
        targetGold = newAmount;

        // Start pulse animation
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        pulseCoroutine = StartCoroutine(PulseAnimation(delta > 0));

        // Flash color
        StartCoroutine(FlashColor(delta > 0 ? gainColor : lossColor));

        Debug.Log($"[GoldUI] 💰 Gold changed: {delta:+#;-#;0} → Total: {newAmount}");
    }

    #endregion

    #region ═══════ UI UPDATES ═══════

    private void UpdateGoldText()
    {
        if (goldText == null) return;

        string displayValue = useAbbreviation ? FormatWithAbbreviation(displayedGold) : displayedGold.ToString();
        goldText.text = string.Format(formatString, displayValue);
    }

    private string FormatWithAbbreviation(int value)
    {
        if (value >= 1000000)
        {
            return (value / 1000000f).ToString("0.#") + "M";
        }
        else if (value >= 1000)
        {
            return (value / 1000f).ToString("0.#") + "K";
        }
        return value.ToString();
    }

    #endregion

    #region ═══════ ANIMATIONS ═══════

    private void AnimateGoldCount()
    {
        if (displayedGold == targetGold) return;

        // Count up/down smoothly
        float diff = targetGold - displayedGold;
        int step = Mathf.CeilToInt(Mathf.Abs(diff) * Time.deltaTime * countSpeed / 10f);
        step = Mathf.Max(1, step);

        if (diff > 0)
        {
            displayedGold = Mathf.Min(displayedGold + step, targetGold);
        }
        else
        {
            displayedGold = Mathf.Max(displayedGold - step, targetGold);
        }

        UpdateGoldText();
    }

    private IEnumerator PulseAnimation(bool isGain)
    {
        if (iconRect == null) yield break;

        float elapsed = 0f;
        float halfDuration = pulseDuration / 2f;

        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            iconRect.localScale = Vector3.Lerp(originalIconScale, originalIconScale * pulseScale, t);
            yield return null;
        }

        elapsed = 0f;

        // Scale down
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            iconRect.localScale = Vector3.Lerp(originalIconScale * pulseScale, originalIconScale, t);
            yield return null;
        }

        iconRect.localScale = originalIconScale;
    }

    private IEnumerator FlashColor(Color flashColor)
    {
        if (goldText == null) yield break;

        goldText.color = flashColor;
        yield return new WaitForSeconds(pulseDuration);
        goldText.color = normalColor;
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("💰 Test: Add 100 Gold")]
    private void TestAddGold()
    {
        OnGoldChanged(displayedGold + 100, 100);
    }

    [ContextMenu("💸 Test: Lose 50 Gold")]
    private void TestLoseGold()
    {
        OnGoldChanged(Mathf.Max(0, displayedGold - 50), -50);
    }

    #endregion
}
