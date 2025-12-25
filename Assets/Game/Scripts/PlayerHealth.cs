using UnityEngine;

/// <summary>
/// Player Health Sistemi
/// - Düşmanlardan gelen hasarı alır (IDamageable)
/// - ProHealthManaUI'a iletir
/// - Ölüm durumunu yönetir
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("═══ UI REFERENCE ═══")]
    [Tooltip("Sahnedeki ProHealthManaUI componenti (otomatik bulunur)")]
    [SerializeField] private ProHealthManaUI healthUI;

    [Header("═══ SETTINGS ═══")]
    [Tooltip("Hasar sonrası kısa süre yenilmezlik")]
    [SerializeField] private float invincibilityDuration = 0.5f;
    
    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    private float lastDamageTime = -999f;
    private bool isDead = false;

    private void Start()
    {
        // UI referansını otomatik bul
        if (healthUI == null)
        {
            healthUI = FindObjectOfType<ProHealthManaUI>();
            
            if (healthUI == null)
            {
                Debug.LogError("[PlayerHealth] ❌ ProHealthManaUI bulunamadı! Sahnede olduğundan emin ol.");
            }
            else
            {
                Debug.Log("[PlayerHealth] ✅ ProHealthManaUI otomatik bulundu.");
            }
        }

        // Player tag kontrolü
        if (!gameObject.CompareTag("Player"))
        {
            Debug.LogWarning("[PlayerHealth] ⚠️ Bu objenin tag'i 'Player' değil! Düşmanlar bulamayabilir.");
        }
    }

    /// <summary>
    /// Hasar al (IDamageable interface - int version)
    /// Düşmanlar bu methodu çağırır
    /// </summary>
    public void TakeDamage(int amount)
    {
        TakeDamageInternal((float)amount);
    }

    /// <summary>
    /// Hasar al (float version - direkt kullanım için)
    /// </summary>
    public void TakeDamage(float amount)
    {
        TakeDamageInternal(amount);
    }

    private void TakeDamageInternal(float amount)
    {
        // Zaten ölü mü?
        if (isDead) return;

        // Yenilmezlik süresi kontrolü
        if (Time.time < lastDamageTime + invincibilityDuration)
        {
            if (showDebugLogs)
                Debug.Log("[PlayerHealth] 🛡️ Yenilmezlik süresi - hasar bloklandı");
            return;
        }

        lastDamageTime = Time.time;

        // UI'a hasarı ilet
        if (healthUI != null)
        {
            healthUI.TakeDamage(amount);

            if (showDebugLogs)
                Debug.Log($"[PlayerHealth] 💔 Hasar alındı: -{amount} | Kalan: {healthUI.CurrentHealth}/{healthUI.MaxHealth}");

            // Ölüm kontrolü
            if (!healthUI.IsAlive)
            {
                Die();
            }
        }
        else
        {
            Debug.LogError("[PlayerHealth] ❌ HealthUI null! Hasar iletilemedi.");
        }
    }

    /// <summary>
    /// İyileştir
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        if (healthUI != null)
        {
            healthUI.Heal(amount);

            if (showDebugLogs)
                Debug.Log($"[PlayerHealth] 💚 İyileştirildi: +{amount}");
        }
    }

    /// <summary>
    /// Ölüm
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[PlayerHealth] 💀 PLAYER ÖLDÜ!");

        // Buraya eklenebilir:
        // - Ölüm animasyonu
        // - Game Over ekranı
        // - Respawn sistemi
        
        // Örnek: GameManager'a haber ver
        // GameManager.Instance?.PlayerDied();
    }

    /// <summary>
    /// Yeniden doğ (Respawn)
    /// </summary>
    public void Respawn()
    {
        isDead = false;
        lastDamageTime = -999f;

        if (healthUI != null)
        {
            healthUI.FullRestore();
        }

        Debug.Log("[PlayerHealth] ✨ Player yeniden doğdu!");
    }

    /// <summary>
    /// Mevcut can
    /// </summary>
    public float GetCurrentHealth()
    {
        return healthUI != null ? healthUI.CurrentHealth : 0;
    }

    /// <summary>
    /// Maksimum can
    /// </summary>
    public float GetMaxHealth()
    {
        return healthUI != null ? healthUI.MaxHealth : 0;
    }

    /// <summary>
    /// Hayatta mı?
    /// </summary>
    public bool IsAlive()
    {
        return !isDead && (healthUI != null && healthUI.IsAlive);
    }
}
