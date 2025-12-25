using UnityEngine;
using System;

public class HealthB : MonoBehaviour, IDamageableB
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private bool debugLogs = true;
    private int currentHealth;
    private bool isDead;

    // Observer Pattern: Ölüm gerçekleştiğinde dinleyenlere haber verir.
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

public void TakeDamage(int amount)
    {
        if (isDead) return;
        
        currentHealth -= amount;
        if(debugLogs) Debug.Log($"[HealthB] {gameObject.name} took {amount} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

        /// <summary>
    /// Returns current health as a percentage (0-1)
    /// </summary>
    public float GetHealthPercent()
    {
        return (float)currentHealth / maxHealth;
    }

    /// <summary>
    /// Returns current health value
    /// </summary>
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Returns max health value
    /// </summary>
    public int GetMaxHealth()
    {
        return maxHealth;
    }

private void Die()
    {
        if (isDead) return;
        isDead = true;
        
        if(debugLogs) Debug.Log($"[HealthB] {gameObject.name} DIE called! Destroying...");
        OnDeath?.Invoke();
        
        // Immediately deactivate to stop all behavior
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}