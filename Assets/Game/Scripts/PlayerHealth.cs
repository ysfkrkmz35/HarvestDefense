using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("═══ DEATH & RESPAWN ═══")]
    [Tooltip("Game Over UI kullan (butona basarak restart)")]
    [SerializeField] private bool useGameOverUI = true;

    [Tooltip("Game Over UI referansı (otomatik bulunur)")]
    [SerializeField] private GameOverUI gameOverUI;

    [Tooltip("Game Over UI kullanılmıyorsa, otomatik restart yapsın mı?")]
    [SerializeField] private bool autoRestartIfNoUI = true;

    [Tooltip("Otomatik restart için bekleme süresi (saniye)")]
    [SerializeField] private float autoRestartDelay = 2f;

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    private float lastDamageTime = -999f;
    private bool isDead = false;

    private void Start()
    {
        // UI referanslarını otomatik bul
        if (healthUI == null)
        {
            healthUI = FindFirstObjectByType<ProHealthManaUI>();

            if (healthUI == null)
            {
                Debug.LogError("[PlayerHealth] ❌ ProHealthManaUI bulunamadı! Sahnede olduğundan emin ol.");
            }
            else
            {
                Debug.Log("[PlayerHealth] ✅ ProHealthManaUI otomatik bulundu.");
            }
        }

        // Game Over UI'ı otomatik bul
        if (useGameOverUI && gameOverUI == null)
        {
            gameOverUI = FindFirstObjectByType<GameOverUI>();

            if (gameOverUI == null)
            {
                Debug.LogWarning("[PlayerHealth] ⚠️ GameOverUI bulunamadı! Sahnede GameOverUI ekleyin!");
                Debug.LogWarning("[PlayerHealth] ⚠️ Geçici olarak otomatik restart kullanılacak.");
                useGameOverUI = false;
            }
            else
            {
                Debug.Log("[PlayerHealth] ✅ GameOverUI otomatik bulundu.");
            }
        }

        // Player tag kontrolü
        if (!gameObject.CompareTag("Player"))
        {
            Debug.LogWarning("[PlayerHealth] ⚠️ Bu objenin tag'i 'Player' değil! Düşmanlar bulamayabilir.");
        }

        // Ölüm durumunu sıfırla
        isDead = false;
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

        // Player kontrolünü devre dışı bırak
        DisablePlayerControls();

        // Game Over UI göster
        if (useGameOverUI && gameOverUI != null)
        {
            // UI ile restart (buton ile)
            gameOverUI.Show();

            if (showDebugLogs)
                Debug.Log("[PlayerHealth] 🎮 Game Over ekranı gösteriliyor. Restart için butona basın.");
        }
        else if (autoRestartIfNoUI)
        {
            // Otomatik restart (UI yoksa)
            StartCoroutine(RestartSceneAfterDelay());

            if (showDebugLogs)
                Debug.Log($"[PlayerHealth] ⏱️ {autoRestartDelay} saniye sonra otomatik restart...");
        }
        else
        {
            Debug.Log("[PlayerHealth] ⚠️ Game Over! Restart sistemi kapalı.");
        }
    }

    /// <summary>
    /// Player kontrollerini devre dışı bırak
    /// </summary>
    private void DisablePlayerControls()
    {
        // PlayerController'ı devre dışı bırak
        var playerController = GetComponent<HappyHarvest.PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
            if (showDebugLogs)
                Debug.Log("[PlayerHealth] 🚫 PlayerController devre dışı bırakıldı.");
        }

        // Rigidbody'yi durdur
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Belirli süre bekleyip sahneyi yenile (otomatik restart için)
    /// </summary>
    private System.Collections.IEnumerator RestartSceneAfterDelay()
    {
        yield return new WaitForSeconds(autoRestartDelay);

        // Aktif sahneyi yeniden yükle
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (showDebugLogs)
            Debug.Log($"[PlayerHealth] 🔄 Sahne yenileniyor: {currentSceneName}");

        SceneManager.LoadScene(currentSceneName);
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
