using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Profesyonel Health & Mana Bar UI Sistemi
/// 
/// Özellikler:
/// - Delayed Damage Bar (Sekiro/Elden Ring tarzı)
/// - Hasar alınca shake efekti
/// - Smooth animasyonlu geçişler
/// - Dinamik renk değişimi (Yeşil → Sarı → Kırmızı)
/// - Düşük canda pulse ve glow efektleri
/// - İkon parlamaları
/// </summary>
public class ProHealthManaUI : MonoBehaviour
{
    [Header("══════ HEALTH BAR ══════")]
    public Image healthFill;
    public Image healthGlow;
    public Image healthDamageBar;
    public Image healthIconGlow;
    public TextMeshProUGUI healthText;

    [Header("══════ MANA BAR ══════")]
    public Image manaFill;
    public Image manaGlow;
    public Image manaIconGlow;
    public TextMeshProUGUI manaText;

    [Header("══════ STATS ══════")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float currentMana = 100f;

    [Header("══════ ANIMATION SETTINGS ══════")]
    [SerializeField] private float fillSpeed = 8f;
    [SerializeField] private float damageBarDelay = 0.5f;
    [SerializeField] private float damageBarSpeed = 3f;
    [SerializeField] private float lowHealthThreshold = 0.3f;

    [Header("══════ SHAKE SETTINGS ══════")]
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeMagnitude = 3f;

    [Header("══════ COLORS ══════")]
    [SerializeField] private Color healthFull = new Color(0.3f, 0.95f, 0.4f, 1f);
    [SerializeField] private Color healthMid = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color healthLow = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color manaColor = new Color(0.3f, 0.6f, 1f, 1f);

    // Internal
    private float displayedHealth;
    private float displayedMana;
    private float damageBarValue;
    private float damageBarTimer;
    private float shakeTimer;
    private Vector3 originalPosition;
    private RectTransform containerRect;
    
    private float pulseTimer;
    private float glowTimer;
    private bool wasLowHealth;

    // Properties
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0;
    public float CurrentMana => currentMana;
    public float MaxMana => maxMana;
    public float ManaPercent => maxMana > 0 ? currentMana / maxMana : 0;
    public bool IsAlive => currentHealth > 0;

    private void Start()
    {
        containerRect = GetComponent<RectTransform>();
        originalPosition = containerRect.anchoredPosition;
        
        displayedHealth = currentHealth;
        displayedMana = currentMana;
        damageBarValue = currentHealth;

        UpdateVisuals();
    }

    private void Update()
    {
        AnimateHealthBar();
        AnimateManaBar();
        AnimateDamageBar();
        UpdateShake();
        UpdateEffects();
        UpdateTexts();
    }

    #region ═══════ PUBLIC METHODS ═══════

    /// <summary>
    /// Hasar ver
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (damage <= 0 || currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // Damage bar gecikmesini başlat
        damageBarTimer = damageBarDelay;
        
        // Shake efekti
        TriggerShake();
        
        // Flash efekti
        StartCoroutine(DamageFlash());

        if (currentHealth <= 0)
        {
            OnDeath();
        }
    }

    /// <summary>
    /// İyileştir
    /// </summary>
    public void Heal(float amount)
    {
        if (amount <= 0) return;
        
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        // İyileşirken damage bar'ı da güncelle
        if (currentHealth > damageBarValue)
        {
            damageBarValue = currentHealth;
        }

        if (currentHealth > oldHealth)
        {
            StartCoroutine(HealFlash());
        }
    }

    /// <summary>
    /// Mana harca
    /// </summary>
    public void UseMana(float amount)
    {
        currentMana = Mathf.Max(0, currentMana - amount);
    }

    /// <summary>
    /// Mana yenile
    /// </summary>
    public void RestoreMana(float amount)
    {
        currentMana = Mathf.Min(maxMana, currentMana + amount);
    }

    /// <summary>
    /// Tam iyileştir
    /// </summary>
    public void FullRestore()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
        damageBarValue = maxHealth;
    }

    /// <summary>
    /// Mana yeterli mi?
    /// </summary>
    public bool HasEnoughMana(float required) => currentMana >= required;

    /// <summary>
    /// Can ayarla
    /// </summary>
    public void SetMaxHealth(float value)
    {
        maxHealth = value;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        damageBarValue = Mathf.Min(damageBarValue, maxHealth);
    }

    /// <summary>
    /// Mana ayarla
    /// </summary>
    public void SetMaxMana(float value)
    {
        maxMana = value;
        currentMana = Mathf.Min(currentMana, maxMana);
    }

    #endregion

    #region ═══════ ANIMATIONS ═══════

    private void AnimateHealthBar()
    {
        if (healthFill == null) return;

        // Smooth lerp
        displayedHealth = Mathf.Lerp(displayedHealth, currentHealth, Time.deltaTime * fillSpeed);
        float percent = maxHealth > 0 ? displayedHealth / maxHealth : 0;

        // Scale bazlı fill
        healthFill.transform.localScale = new Vector3(percent, 1, 1);
        
        // Glow da aynı boyutta
        if (healthGlow != null)
        {
            healthGlow.transform.localScale = new Vector3(percent, 1, 1);
        }

        // Renk geçişi
        UpdateHealthColor(percent);
    }

    private void AnimateManaBar()
    {
        if (manaFill == null) return;

        displayedMana = Mathf.Lerp(displayedMana, currentMana, Time.deltaTime * fillSpeed);
        float percent = maxMana > 0 ? displayedMana / maxMana : 0;

        manaFill.transform.localScale = new Vector3(percent, 1, 1);
        
        if (manaGlow != null)
        {
            manaGlow.transform.localScale = new Vector3(percent, 1, 1);
        }
    }

    private void AnimateDamageBar()
    {
        if (healthDamageBar == null) return;

        // Gecikme süresi
        if (damageBarTimer > 0)
        {
            damageBarTimer -= Time.deltaTime;
            return;
        }

        // Yavaşça mevcut cana düş
        if (damageBarValue > currentHealth)
        {
            damageBarValue = Mathf.Lerp(damageBarValue, currentHealth, Time.deltaTime * damageBarSpeed);
        }
        else
        {
            damageBarValue = currentHealth;
        }

        float percent = maxHealth > 0 ? damageBarValue / maxHealth : 0;
        healthDamageBar.transform.localScale = new Vector3(percent, 1, 1);
    }

    private void UpdateHealthColor(float percent)
    {
        Color targetColor;

        if (percent > 0.5f)
        {
            // Yeşil → Sarı
            float t = (percent - 0.5f) * 2f;
            targetColor = Color.Lerp(healthMid, healthFull, t);
        }
        else
        {
            // Sarı → Kırmızı
            float t = percent * 2f;
            targetColor = Color.Lerp(healthLow, healthMid, t);
        }

        healthFill.color = targetColor;

        // Glow rengi
        if (healthGlow != null)
        {
            Color glowColor = targetColor;
            glowColor.a = 0.5f;
            healthGlow.color = glowColor;
        }
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            
            float offsetX = Random.Range(-shakeMagnitude, shakeMagnitude);
            float offsetY = Random.Range(-shakeMagnitude, shakeMagnitude);
            
            containerRect.anchoredPosition = originalPosition + new Vector3(offsetX, offsetY, 0);
        }
        else
        {
            containerRect.anchoredPosition = originalPosition;
        }
    }

    private void TriggerShake()
    {
        shakeTimer = shakeDuration;
    }

    #endregion

    #region ═══════ EFFECTS ═══════

    private void UpdateEffects()
    {
        float healthPercent = HealthPercent;
        bool isLowHealth = healthPercent <= lowHealthThreshold && healthPercent > 0;

        // Düşük can efektleri
        if (isLowHealth)
        {
            pulseTimer += Time.deltaTime * 4f;
            float pulse = (Mathf.Sin(pulseTimer) + 1f) / 2f;

            // Health bar alpha pulse
            if (healthFill != null)
            {
                Color c = healthFill.color;
                c.a = 0.6f + (pulse * 0.4f);
                healthFill.color = c;
            }

            // İkon glow pulse
            if (healthIconGlow != null)
            {
                Color c = healthIconGlow.color;
                c.a = 0.3f + (pulse * 0.5f);
                healthIconGlow.color = c;
            }

            // İlk düşük can anında feedback
            if (!wasLowHealth)
            {
                TriggerShake();
            }
        }
        else
        {
            pulseTimer = 0;
        }

        wasLowHealth = isLowHealth;

        // Genel glow animasyonu
        glowTimer += Time.deltaTime * 2f;
        float glowPulse = (Mathf.Sin(glowTimer) + 1f) / 2f;

        if (healthGlow != null && !isLowHealth)
        {
            Color c = healthGlow.color;
            c.a = 0.3f + (glowPulse * 0.2f);
            healthGlow.color = c;
        }

        if (manaGlow != null)
        {
            Color c = manaColor;
            c.a = 0.3f + (glowPulse * 0.2f);
            manaGlow.color = c;
        }

        if (manaIconGlow != null)
        {
            Color c = manaIconGlow.color;
            c.a = 0.2f + (glowPulse * 0.2f);
            manaIconGlow.color = c;
        }
    }

    private System.Collections.IEnumerator DamageFlash()
    {
        if (healthFill == null) yield break;

        Color originalColor = healthFill.color;
        
        // Beyaz flash
        healthFill.color = Color.white;
        yield return new WaitForSeconds(0.05f);
        
        // Geri dön
        healthFill.color = originalColor;
    }

    private System.Collections.IEnumerator HealFlash()
    {
        if (healthFill == null) yield break;

        Color originalColor = healthFill.color;
        
        // Yeşil parlama
        healthFill.color = new Color(0.5f, 1f, 0.5f, 1f);
        
        if (healthGlow != null)
        {
            healthGlow.color = new Color(0.5f, 1f, 0.5f, 0.8f);
        }
        
        yield return new WaitForSeconds(0.1f);
        
        healthFill.color = originalColor;
    }

    private void UpdateTexts()
    {
        if (healthText != null)
        {
            healthText.text = Mathf.CeilToInt(displayedHealth).ToString();
        }

        if (manaText != null)
        {
            manaText.text = Mathf.CeilToInt(displayedMana).ToString();
        }
    }

    private void UpdateVisuals()
    {
        // İlk frame'de barları doğru konumda göster
        float hp = HealthPercent;
        float mp = ManaPercent;

        if (healthFill) healthFill.transform.localScale = new Vector3(hp, 1, 1);
        if (healthGlow) healthGlow.transform.localScale = new Vector3(hp, 1, 1);
        if (healthDamageBar) healthDamageBar.transform.localScale = new Vector3(hp, 1, 1);
        if (manaFill) manaFill.transform.localScale = new Vector3(mp, 1, 1);
        if (manaGlow) manaGlow.transform.localScale = new Vector3(mp, 1, 1);

        UpdateHealthColor(hp);
        UpdateTexts();
    }

    #endregion

    #region ═══════ EVENTS ═══════

    private void OnDeath()
    {
        Debug.Log("<color=red>☠ OYUNCU ÖLDÜ!</color>");
        // GameManager.Instance?.PlayerDied();
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("⚔ Test: 25 Hasar")]
    private void TestDamage25() { TakeDamage(25); }

    [ContextMenu("⚔ Test: 50 Hasar")]
    private void TestDamage50() { TakeDamage(50); }

    [ContextMenu("💚 Test: 30 İyileştir")]
    private void TestHeal30() { Heal(30); }

    [ContextMenu("💙 Test: 20 Mana Harca")]
    private void TestUseMana() { UseMana(20); }

    [ContextMenu("✨ Test: Tam İyileştir")]
    private void TestFullRestore() { FullRestore(); }

    [ContextMenu("💀 Test: Öldür")]
    private void TestKill() { TakeDamage(currentHealth); }

    #endregion
}