#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EDITOR ONLY: Game Over UI'ı otomatik oluşturur
/// Unity menüden: GameObject -> UI -> Create Game Over UI
/// </summary>
public class GameOverUISetup
{
    [UnityEditor.MenuItem("GameObject/UI/Create Game Over UI", false, 0)]
    public static void CreateGameOverUI()
    {
        // Canvas bul veya oluştur
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // Canvas yoksa oluştur
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("[GameOverUISetup] ✅ Canvas oluşturuldu.");
        }

        // Game Over Panel oluştur
        GameObject panelObj = new GameObject("GameOverPanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f); // Yarı saydam siyah

        // Game Over Text oluştur
        GameObject textObj = new GameObject("GameOverText");
        textObj.transform.SetParent(panelObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, 100);
        textRect.sizeDelta = new Vector2(600, 100);

        TextMeshProUGUI gameOverText = textObj.AddComponent<TextMeshProUGUI>();
        gameOverText.text = "GAME OVER";
        gameOverText.fontSize = 80;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.color = Color.red;
        gameOverText.fontStyle = FontStyles.Bold;

        // Restart Button oluştur
        GameObject buttonObj = new GameObject("RestartButton");
        buttonObj.transform.SetParent(panelObj.transform, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(0, -50);
        buttonRect.sizeDelta = new Vector2(300, 80);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0, 0.7f, 0, 1); // Yeşil

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0, 0.7f, 0, 1);
        colors.highlightedColor = new Color(0, 0.9f, 0, 1);
        colors.pressedColor = new Color(0, 0.5f, 0, 1);
        button.colors = colors;

        // Button Text
        GameObject buttonTextObj = new GameObject("Text (TMP)");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);

        RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "RESTART";
        buttonText.fontSize = 36;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        buttonText.fontStyle = FontStyles.Bold;

        // GameOverUI Component ekle
        GameOverUI gameOverUI = canvas.gameObject.AddComponent<GameOverUI>();

        // Reflection ile private field'ları set et
        var type = typeof(GameOverUI);

        type.GetField("gameOverPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(gameOverUI, panelObj);

        type.GetField("restartButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(gameOverUI, button);

        type.GetField("gameOverText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(gameOverUI, gameOverText);

        // Panel'i başlangıçta gizle
        panelObj.SetActive(false);

        // Seçimi yeni oluşturulan Canvas'a ayarla
        UnityEditor.Selection.activeGameObject = canvas.gameObject;

        Debug.Log("[GameOverUISetup] ✅ Game Over UI başarıyla oluşturuldu!");
        Debug.Log("[GameOverUISetup] Canvas seçildi - Inspector'da GameOverUI component'ini kontrol et.");
    }
}
#endif
