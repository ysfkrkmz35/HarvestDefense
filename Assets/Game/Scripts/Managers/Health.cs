using UnityEngine;
using System;

/// <summary>
/// Can sistemi
/// IDamageable interface'ini kullanır
/// Can 0 olunca OnDeath eventi tetiklenir
/// </summary>
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    // Events
    public static event Action<GameObject> OnDeath;
    public event Action<int> OnHealthChanged;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Hasar alma fonksiyonu (IDamageable interface)
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"Health: {gameObject.name} {amount} hasar aldı. Kalan can: {currentHealth}/{maxHealth}");

        // Can değişti eventi
        OnHealthChanged?.Invoke(currentHealth);

        // Can 0 olduysa öl
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Can ekleme fonksiyonu
    /// </summary>
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"Health: {gameObject.name} {amount} can kazandı. Kalan can: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Ölüm fonksiyonu
    /// </summary>
    private void Die()
    {
        Debug.Log($"Health: {gameObject.name} öldü!");

        // Ölüm eventi
        OnDeath?.Invoke(gameObject);

        // SimpleEnemyAI varsa die fonksiyonunu çağır
        SimpleEnemyAI enemyAI = GetComponent<SimpleEnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.Die();
        }

        // Object Pooling için SetActive(false)
        // Eğer bu düşman pooling kullanıyorsa, 2 saniye sonra pasif hale getir
        if (gameObject.CompareTag("Enemy"))
        {
            Invoke(nameof(DeactivateEnemy), 2f);
        }
        else
        {
            // Diğer objeler için Destroy
            Destroy(gameObject, 2f);
        }
    }

    /// <summary>
    /// Düşmanı pasif hale getir (Pooling için)
    /// </summary>
    private void DeactivateEnemy()
    {
        // Can'ı sıfırla
        currentHealth = maxHealth;
        gameObject.SetActive(false);
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
}
