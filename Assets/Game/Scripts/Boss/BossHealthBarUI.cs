using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Boss health bar UI that appears at the top of the screen during boss fights.
/// Uses RectTransform scaling for reliable bar shrinking.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    public GameObject bossObject;
    public RectTransform healthBarFill;
    public Image healthBarFillImage;
    public Image healthBarBackground;
    public TextMeshProUGUI bossNameText;
    
    [Header("Settings")]
    public string bossName = "BOSS";
    public Color healthColor = new Color(0.8f, 0.1f, 0.1f);
    public Color lowHealthColor = new Color(1f, 0.3f, 0f);
    public float lowHealthThreshold = 0.3f;
    
    private YusufTest.EnemyHealth enemyHealth;
    private float maxWidth;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Store original width
        if (healthBarFill != null)
        {
            maxWidth = healthBarFill.sizeDelta.x;
            Debug.Log($"[BossHealthBar] Max width: {maxWidth}");
        }
        
        if (bossObject != null)
        {
            enemyHealth = bossObject.GetComponent<YusufTest.EnemyHealth>();
            if (enemyHealth != null)
            {
                Debug.Log($"[BossHealthBar] ✅ Found EnemyHealth, max HP: {enemyHealth.GetMaxHealth()}");
            }
            else
            {
                Debug.LogError("[BossHealthBar] ❌ No EnemyHealth on boss object!");
            }
        }
        else
        {
            Debug.LogError("[BossHealthBar] ❌ Boss Object not assigned!");
        }
        
        if (bossNameText != null)
        {
            bossNameText.text = bossName;
        }
        
        if (healthBarFillImage != null)
        {
            healthBarFillImage.color = healthColor;
        }
        
        canvasGroup.alpha = 1;
    }

    private void Update()
    {
        if (enemyHealth != null)
        {
            float percentage = enemyHealth.GetHealthPercentage();
            UpdateHealthBar(percentage);
        }
    }

    private void UpdateHealthBar(float percentage)
    {
        percentage = Mathf.Clamp01(percentage);
        
        // Scale the bar width based on health percentage
        if (healthBarFill != null)
        {
            Vector2 size = healthBarFill.sizeDelta;
            size.x = maxWidth * percentage;
            healthBarFill.sizeDelta = size;
        }
        
        // Change color when low health
        if (healthBarFillImage != null)
        {
            if (percentage <= lowHealthThreshold)
            {
                healthBarFillImage.color = lowHealthColor;
            }
            else
            {
                healthBarFillImage.color = healthColor;
            }
        }
        
        // Hide when dead
        if (percentage <= 0 && canvasGroup != null)
        {
            canvasGroup.alpha = 0;
        }
    }
}
