using UnityEngine;
using System;

public class HealthB : MonoBehaviour, IDamageableB
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 50;
    private int currentHealth;

    // Observer Pattern: Ölüm gerçekleştiğinde dinleyenlere haber verir.
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        // Debug.Log($"{gameObject.name} hasar aldı. Kalan Can: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Event'i dinleyen (örn: Puan sistemi, Spawner) varsa tetikle
        OnDeath?.Invoke();

        // Şimdilik test için objeyi yok ediyoruz. İleride ObjectPool kullanacağız.
        Destroy(gameObject);
    }
}