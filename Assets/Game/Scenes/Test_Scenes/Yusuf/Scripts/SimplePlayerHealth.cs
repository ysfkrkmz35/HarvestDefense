using UnityEngine;

namespace YusufTest
{
    /// <summary>
    /// Basit Player Health sistemi - Test için
    /// IDamageable interface'ini implement eder
    /// </summary>
    public class SimplePlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        private int currentHealth;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        void Start()
        {
            currentHealth = maxHealth;
            Debug.Log($"[SimplePlayerHealth] Player başladı. Can: {currentHealth}/{maxHealth}");
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;

            if (showDebugLogs)
            {
                Debug.Log($"[SimplePlayerHealth] 💔 Hasar alındı: -{damage} | Kalan Can: {currentHealth}/{maxHealth}");
            }

            // Can sıfırın altına düşerse
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        }

        void Die()
        {
            Debug.Log("[SimplePlayerHealth] 💀 PLAYER ÖLDÜ!");
            // Burada ölüm animasyonu, game over ekranı vs. ekleyebilirsiniz
        }

        // UI için kullanılabilir
        public float GetHealthPercentage()
        {
            return (float)currentHealth / maxHealth;
        }

        public int GetCurrentHealth()
        {
            return currentHealth;
        }

        public int GetMaxHealth()
        {
            return maxHealth;
        }

        // Healing için
        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

            if (showDebugLogs)
            {
                Debug.Log($"[SimplePlayerHealth] 💚 İyileştirildi: +{amount} | Can: {currentHealth}/{maxHealth}");
            }
        }
    }
}
