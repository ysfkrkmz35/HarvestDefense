using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime scene initializer - creates healthbars dynamically at runtime.
/// Attach to GameManager or any always-active object.
/// </summary>
public class SceneInitializer : MonoBehaviour
{
    [Header("Healthbar Visual Settings")]
    [SerializeField] private float barWidth = 1.5f;
    [SerializeField] private float barHeight = 0.2f;
    [SerializeField] private float yOffset = 1.5f;
    [SerializeField] private float canvasScaleFactor = 0.01f; // Very small for world space
    
    [Header("Colors")]
    [SerializeField] private Color bgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    [SerializeField] private Color fullColor = new Color(0.1f, 0.9f, 0.1f, 1f);
    [SerializeField] private Color lowColor = new Color(0.9f, 0.1f, 0.1f, 1f);
    
    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    
    void Start()
    {
        Invoke("InitializeScene", 0.1f); // Small delay to ensure all objects are ready
    }
    
    void InitializeScene()
    {
        if (debugLog) Debug.Log("[SceneInit] Starting scene initialization...");
        
        // Create healthbars for all HealthB objects
        HealthB[] allHealth = FindObjectsByType<HealthB>(FindObjectsSortMode.None);
        if (debugLog) Debug.Log($"[SceneInit] Found {allHealth.Length} objects with HealthB");
        
        foreach (HealthB h in allHealth)
        {
            CreateHealthBar(h);
        }
        
        // Setup spider targeting
        SetupSpiderTargeting();
        
        if (debugLog) Debug.Log("[SceneInit] Initialization complete!");
    }
    
    void CreateHealthBar(HealthB health)
    {
        // Skip if already has our healthbar
        if (health.GetComponentInChildren<RuntimeHealthBar>() != null) return;
        
        GameObject owner = health.gameObject;
        float ownerScale = owner.transform.localScale.x;
        
        // Create canvas
        GameObject canvasObj = new GameObject("HP_Bar");
        canvasObj.transform.SetParent(owner.transform, false);
        canvasObj.transform.localPosition = new Vector3(0, yOffset / ownerScale, 0);
        canvasObj.transform.localScale = Vector3.one * 0.002f;
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(barWidth * 100f, barHeight * 100f);
        
        // Background
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = bgColor;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Fill bar
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = fullColor;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0;
        fillImg.fillAmount = 1f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.02f, 0.1f);
        fillRect.anchorMax = new Vector2(0.98f, 0.9f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        // Add controller
        RuntimeHealthBar bar = canvasObj.AddComponent<RuntimeHealthBar>();
        bar.Setup(health, fillImg, fullColor, lowColor);
        
        if (debugLog) Debug.Log($"[SceneInit] Created healthbar for {owner.name}");
    }
    
    void SetupSpiderTargeting()
    {
        // Find objects that should target towers (spiders)
        // Look for any object with HealthB on layer 9 (enemy layer)
        HealthB[] allHealth = FindObjectsByType<HealthB>(FindObjectsSortMode.None);
        
        foreach (HealthB h in allHealth)
        {
            if (h.gameObject.layer == 9) // Enemy layer
            {
                SpiderTargeting targeting = h.GetComponent<SpiderTargeting>();
                if (targeting == null)
                {
                    targeting = h.gameObject.AddComponent<SpiderTargeting>();
                    if (debugLog) Debug.Log($"[SceneInit] Added SpiderTargeting to {h.gameObject.name}");
                }
            }
        }
    }
}