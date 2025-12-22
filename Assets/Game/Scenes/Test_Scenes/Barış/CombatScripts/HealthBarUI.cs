using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar that follows a target and displays health percentage.
/// Attach this to a Canvas GameObject that is a child of the object with HealthB.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private HealthB healthComponent;
    
    [Header("Settings")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    
    [Header("Billboard")]
    [SerializeField] private bool faceCamera = true;
    
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        
        // Try to find HealthB on parent if not assigned
        if (healthComponent == null)
        {
            healthComponent = GetComponentInParent<HealthB>();
        }
    }

    private void Start()
    {
        // Subscribe to health changes (we'll poll for now since HealthB doesn't have OnHealthChanged event)
        if (healthComponent != null)
        {
            healthComponent.OnDeath += HandleDeath;
        }
    }

    private void OnDestroy()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeath -= HandleDeath;
        }
    }

    private void LateUpdate()
    {
        // Billboard effect - make healthbar face camera
        if (faceCamera && mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
        
        // Update fill amount
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (fillImage == null || healthComponent == null) return;
        
        float healthPercent = healthComponent.GetHealthPercent();
        fillImage.fillAmount = healthPercent;
        
        // Color lerp based on health
        if (healthPercent <= lowHealthThreshold)
        {
            fillImage.color = lowHealthColor;
        }
        else
        {
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, 
                (healthPercent - lowHealthThreshold) / (1f - lowHealthThreshold));
        }
    }

    private void HandleDeath()
    {
        // Hide healthbar on death
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Call this to manually set the health component reference
    /// </summary>
    public void SetHealthComponent(HealthB health)
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeath -= HandleDeath;
        }
        
        healthComponent = health;
        
        if (healthComponent != null)
        {
            healthComponent.OnDeath += HandleDeath;
        }
    }
}