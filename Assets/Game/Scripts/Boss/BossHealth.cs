using UnityEngine;
using System;

public class BossHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 500;
    private int currentHealth;

    // UI Events
    public static event Action<float> OnHealthChanged;
    public static event Action<bool> OnBossActiveStateChanged;

    // Logic Events
    public event Action OnDeath;
    public event Action<float> OnDamageTaken; // For AI phases

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool isDead = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // Initialize UI
        OnHealthChanged?.Invoke(1f);
        // Don't show bar yet, wait for WakeUp
    }

    public void SetActive(bool active)
    {
        OnBossActiveStateChanged?.Invoke(active);
        if (active)
        {
            OnHealthChanged?.Invoke((float)currentHealth / maxHealth);
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        
        Debug.Log($"[BossHealth] Took {amount} dmg. Current: {currentHealth}");

        // Update UI
        float percent = (float)currentHealth / maxHealth;
        OnHealthChanged?.Invoke(percent);
        OnDamageTaken?.Invoke(percent);

        // Visual Feedback (Flash)
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashColor());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[BossHealth] Boss Defeated!");
        
        // Hide UI
        OnBossActiveStateChanged?.Invoke(false);

        // Notify Logic
        OnDeath?.Invoke();

        // Destroy sequence handled by Controller or Victory Handler
    }

    private System.Collections.IEnumerator FlashColor()
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = original; // Note: Controller might override this for rage mode
    }
}
