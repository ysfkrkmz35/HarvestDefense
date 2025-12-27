using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Progression UI Display
/// - Shows XP bar with smooth animation
/// - Displays current level
/// - Level-up flash effect
/// - Listens to PlayerProgression events
/// </summary>
public class ProgressionUI : MonoBehaviour
{
    [Header("═══ UI REFERENCES ═══")]
    [Tooltip("XP fill bar (Image with fill type)")]
    [SerializeField] private Image xpFill;

    [Tooltip("XP glow effect (optional)")]
    [SerializeField] private Image xpGlow;

    [Tooltip("Level number text")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Tooltip("XP amount text (e.g., '150/300')")]
    [SerializeField] private TextMeshProUGUI xpText;

    [Header("═══ ANIMATION SETTINGS ═══")]
    [Tooltip("How fast the XP bar fills")]
    [SerializeField] private float fillSpeed = 5f;

    [Tooltip("Duration of level-up flash")]
    [SerializeField] private float flashDuration = 0.3f;

    [Header("═══ COLORS ═══")]
    [SerializeField] private Color xpBarColor = new Color(0.6f, 0.2f, 0.9f, 1f); // Purple
    [SerializeField] private Color xpGlowColor = new Color(0.8f, 0.4f, 1f, 0.5f);
    [SerializeField] private Color levelUpFlashColor = Color.white;

    [Header("═══ LEVEL UP EFFECTS ═══")]
    [SerializeField] private bool enableLevelUpFlash = true;
    [SerializeField] private bool enableLevelUpScale = true;
    [SerializeField] private float levelUpScaleAmount = 1.3f;

    // Internal state
    private float displayedXP;
    private float targetXP;
    private int displayedLevel;
    private RectTransform levelTextRect;
    private Vector3 originalLevelScale;
    private float flashTimer;
    private float scaleTimer;

    private void Start()
    {
        // Subscribe to events
        PlayerProgression.OnXPChanged += OnXPChanged;
        PlayerProgression.OnLevelUp += OnLevelUp;

        // Cache references
        if (levelText != null)
        {
            levelTextRect = levelText.GetComponent<RectTransform>();
            originalLevelScale = levelTextRect.localScale;
        }

        // Set initial colors
        if (xpFill != null)
        {
            xpFill.color = xpBarColor;
        }

        if (xpGlow != null)
        {
            xpGlow.color = xpGlowColor;
        }

        // Initialize display
        InitializeDisplay();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        PlayerProgression.OnXPChanged -= OnXPChanged;
        PlayerProgression.OnLevelUp -= OnLevelUp;
    }

    private void Update()
    {
        AnimateXPBar();
        UpdateFlashEffect();
        UpdateScaleEffect();
    }

    #region ═══════ INITIALIZATION ═══════

    private void InitializeDisplay()
    {
        if (PlayerProgression.Instance != null)
        {
            displayedLevel = PlayerProgression.Instance.CurrentLevel;
            displayedXP = PlayerProgression.Instance.CurrentXP;
            targetXP = displayedXP;

            UpdateLevelText();
            UpdateXPText();
            UpdateXPBar(true); // Instant update
        }
    }

    #endregion

    #region ═══════ EVENT HANDLERS ═══════

    private void OnXPChanged(int currentXP, int xpToNextLevel)
    {
        targetXP = currentXP;
        UpdateXPText();
    }

    private void OnLevelUp(int newLevel)
    {
        displayedLevel = newLevel;
        UpdateLevelText();

        // Reset XP display for new level
        displayedXP = 0;
        targetXP = PlayerProgression.Instance?.CurrentXP ?? 0;

        // Trigger effects
        if (enableLevelUpFlash)
        {
            flashTimer = flashDuration;
        }

        if (enableLevelUpScale)
        {
            scaleTimer = flashDuration;
        }

        Debug.Log($"[ProgressionUI] 🎉 Level Up! Now level {newLevel}");
    }

    #endregion

    #region ═══════ UI UPDATES ═══════

    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = displayedLevel.ToString();
        }
    }

    private void UpdateXPText()
    {
        if (xpText != null && PlayerProgression.Instance != null)
        {
            int current = PlayerProgression.Instance.CurrentXP;
            int max = PlayerProgression.Instance.XPToNextLevel;
            xpText.text = $"{current}/{max}";
        }
    }

    private void UpdateXPBar(bool instant = false)
    {
        if (xpFill == null || PlayerProgression.Instance == null) return;

        float progress = PlayerProgression.Instance.XPProgress;

        if (instant)
        {
            displayedXP = targetXP;
            xpFill.fillAmount = progress;
        }

        if (xpGlow != null)
        {
            xpGlow.fillAmount = xpFill.fillAmount;
        }
    }

    #endregion

    #region ═══════ ANIMATIONS ═══════

    private void AnimateXPBar()
    {
        if (xpFill == null || PlayerProgression.Instance == null) return;

        // Smooth lerp to target
        float targetProgress = PlayerProgression.Instance.XPProgress;
        float currentProgress = xpFill.fillAmount;

        if (Mathf.Abs(targetProgress - currentProgress) > 0.001f)
        {
            xpFill.fillAmount = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * fillSpeed);
        }
        else
        {
            xpFill.fillAmount = targetProgress;
        }

        // Sync glow
        if (xpGlow != null)
        {
            xpGlow.fillAmount = xpFill.fillAmount;
        }
    }

    private void UpdateFlashEffect()
    {
        if (!enableLevelUpFlash || flashTimer <= 0) return;

        flashTimer -= Time.deltaTime;
        float t = flashTimer / flashDuration;

        if (xpFill != null)
        {
            xpFill.color = Color.Lerp(xpBarColor, levelUpFlashColor, t);
        }

        if (levelText != null)
        {
            levelText.color = Color.Lerp(Color.white, levelUpFlashColor, t);
        }
    }

    private void UpdateScaleEffect()
    {
        if (!enableLevelUpScale || levelTextRect == null || scaleTimer <= 0) return;

        scaleTimer -= Time.deltaTime;
        float t = scaleTimer / flashDuration;

        // Punch scale effect
        float scale = Mathf.Lerp(1f, levelUpScaleAmount, t);
        levelTextRect.localScale = originalLevelScale * scale;
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("🎉 Test: Simulate Level Up")]
    private void TestLevelUp()
    {
        OnLevelUp(displayedLevel + 1);
    }

    [ContextMenu("⭐ Test: Add XP Animation")]
    private void TestAddXP()
    {
        OnXPChanged(50, 100);
    }

    #endregion
}
