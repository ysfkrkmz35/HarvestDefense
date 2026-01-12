using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool to create a Boss Health Bar UI.
/// Run via: Tools > Create Boss Health Bar
/// </summary>
public class BossHealthBarCreator : EditorWindow
{
    [MenuItem("Tools/Create Boss Health Bar")]
    public static void CreateHealthBar()
    {
        Debug.Log("=== CREATING BOSS HEALTH BAR ===");

        // Find or create Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create Health Bar Container
        GameObject healthBarObj = new GameObject("BossHealthBar");
        healthBarObj.transform.SetParent(canvas.transform);
        
        RectTransform rect = healthBarObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(600, 40);

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(healthBarObj.transform);
        Image bg = bgObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Health Fill - use left-aligned rect that shrinks
        GameObject fillObj = new GameObject("HealthFill");
        fillObj.transform.SetParent(healthBarObj.transform);
        Image fill = fillObj.AddComponent<Image>();
        fill.color = new Color(0.8f, 0.1f, 0.1f);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        // Anchor to left side, fixed size that will be modified by script
        fillRect.anchorMin = new Vector2(0, 0.5f);
        fillRect.anchorMax = new Vector2(0, 0.5f);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchoredPosition = new Vector2(5, 0);
        fillRect.sizeDelta = new Vector2(590, 30); // Full width minus padding

        // Boss Name Text
        GameObject textObj = new GameObject("BossName");
        textObj.transform.SetParent(healthBarObj.transform);
        TextMeshProUGUI nameText = textObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "BOSS";
        nameText.fontSize = 24;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        nameText.fontStyle = FontStyles.Bold;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Add BossHealthBarUI component
        BossHealthBarUI healthBarUI = healthBarObj.AddComponent<BossHealthBarUI>();
        
        // Wire references using public fields
        healthBarUI.healthBarFill = fillRect;
        healthBarUI.healthBarFillImage = fill;
        healthBarUI.healthBarBackground = bg;
        healthBarUI.bossNameText = nameText;

        // Select it
        Selection.activeGameObject = healthBarObj;
        EditorUtility.SetDirty(healthBarObj);
        
        Debug.Log("=== BOSS HEALTH BAR CREATED ===");
        Debug.Log("Drag MonD_01 to the 'Boss Object' field on BossHealthBarUI!");
        EditorApplication.Beep();
    }
}
