using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Patlama büyüsü için basit cooldown UI göstergesi
/// </summary>
public class SpellCooldownUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private ExplosionSpell explosionSpell;
    
    [Header("UI Elementleri")]
    [SerializeField] private Image cooldownOverlay;  // Karartma overlay'i
    [SerializeField] private Text cooldownText;       // Kalan süre texti
    [SerializeField] private Image spellIcon;         // Büyü ikonu
    
    [Header("Görsel Ayarlar")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color cooldownColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

    private void Start()
    {
        if (explosionSpell == null)
        {
            explosionSpell = FindFirstObjectByType<ExplosionSpell>();
        }
        
        if (explosionSpell != null)
        {
            explosionSpell.OnCooldownChanged += UpdateCooldownDisplay;
            explosionSpell.OnSpellCast += OnSpellCast;
        }
    }

    private void Update()
    {
        if (explosionSpell == null) return;
        
        if (explosionSpell.IsReady())
        {
            SetReady();
        }
    }

    private void UpdateCooldownDisplay(float remainingTime)
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(true);
        }
        
        if (cooldownText != null)
        {
            cooldownText.text = remainingTime.ToString("F1");
        }
        
        if (spellIcon != null)
        {
            spellIcon.color = cooldownColor;
        }
    }

    private void SetReady()
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(false);
        }
        
        if (cooldownText != null)
        {
            cooldownText.text = "";
        }
        
        if (spellIcon != null)
        {
            spellIcon.color = readyColor;
        }
    }

    private void OnSpellCast()
    {
        // Büyü kullanıldığında animasyon veya efekt eklenebilir
        if (spellIcon != null)
        {
            // Basit bir scale animasyonu
            StartCoroutine(PulseAnimation());
        }
    }

    private System.Collections.IEnumerator PulseAnimation()
    {
        Vector3 originalScale = spellIcon.transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        float duration = 0.1f;
        float elapsed = 0f;
        
        // Büyüt
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            spellIcon.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            yield return null;
        }
        
        elapsed = 0f;
        
        // Küçült
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            spellIcon.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            yield return null;
        }
        
        spellIcon.transform.localScale = originalScale;
    }

    private void OnDestroy()
    {
        if (explosionSpell != null)
        {
            explosionSpell.OnCooldownChanged -= UpdateCooldownDisplay;
            explosionSpell.OnSpellCast -= OnSpellCast;
        }
    }
}