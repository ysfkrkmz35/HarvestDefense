using UnityEngine;

namespace YusufTest
{
    /// <summary>
    /// Düşman Can Sistemi (Mehmet)
    /// - IDamageable interface'ini implement eder
    /// - Can 0 olunca SimpleEnemyAI'nin Die() fonksiyonunu çağırır
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("== HEALTH ==")]
        [SerializeField] private int maxHealth = 100;
        private int currentHealth;

        [Header("== REFERENCES ==")]
        private SimpleEnemyAI enemyAI;
        private EnemyDropHandler dropHandler;

    [Header("== DEBUG ==")]
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        enemyAI = GetComponent<SimpleEnemyAI>();
        if (enemyAI == null)
        {
            Debug.LogWarning("[EnemyHealth] SimpleEnemyAI bulunamadı!");
        }

        // Get drop handler if present
        dropHandler = GetComponent<EnemyDropHandler>();
    }

    private void OnEnable()
    {
        // Aktif olduğunda canı resetle
        ResetHealth();
    }

    /// <summary>
    /// Canı tam doldur
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;

        if (showDebugLogs)
            Debug.Log($"[EnemyHealth] Can resetlendi: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Hasar al (IDamageable interface'inden)
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0)
            return; // Zaten ölü

        currentHealth -= amount;

        if (showDebugLogs)
            Debug.Log($"[EnemyHealth] Hasar alındı: -{amount} | Kalan: {currentHealth}/{maxHealth}");

        // Can 0 veya altına düştüyse öl
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// Ölüm
    /// </summary>
    private void Die()
    {
        if (showDebugLogs)
            Debug.Log("[EnemyHealth] Düşman öldü!");

        // Drop rewards (XP/Gold) if handler exists
        if (dropHandler != null)
        {
            dropHandler.OnEnemyDeath();
        }

        // EnemyAI'ye haber ver
        if (enemyAI != null)
        {
            enemyAI.Die();
        }
        else
        {
            // AI yoksa direkt devre dışı bırak
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Can yüzdesi (UI için kullanılabilir)
    /// </summary>
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    /// <summary>
    /// Mevcut canı al
    /// </summary>
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Maksimum canı al
    /// </summary>
    public int GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Gizmos ile can barını görselleştir (Düşman seçiliyken)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Düşmanın üstünde can barı çiz
        Vector3 healthBarPosition = transform.position + Vector3.up * 1.5f;

        // Arka plan (Siyah)
        Gizmos.color = Color.black;
        Gizmos.DrawCube(healthBarPosition, new Vector3(1f, 0.1f, 0.1f));

        // Can barı (Yeşil/Kırmızı)
        float healthPercentage = Application.isPlaying ? GetHealthPercentage() : 1f;
        Gizmos.color = Color.Lerp(Color.red, Color.green, healthPercentage);
        Gizmos.DrawCube(healthBarPosition, new Vector3(healthPercentage * 1f, 0.08f, 0.08f));
    }
    }
}
