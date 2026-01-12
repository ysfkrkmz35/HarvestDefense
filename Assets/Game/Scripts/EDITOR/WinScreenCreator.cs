using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool to create a Win Screen scene.
/// Run via: Tools > Create Win Screen Scene
/// </summary>
public class WinScreenCreator : EditorWindow
{
    [MenuItem("Tools/Create Win Screen Scene")]
    public static void CreateWinScene()
    {
        Debug.Log("=== CREATING WIN SCREEN SCENE ===");

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasObj.transform);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.2f, 1f); // Dark blue
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Victory Title
        GameObject titleObj = new GameObject("VictoryTitle");
        titleObj.transform.SetParent(canvasObj.transform);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "VICTORY!";
        title.fontSize = 72;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(1f, 0.84f, 0f); // Gold
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(600, 100);

        // Subtitle
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(canvasObj.transform);
        TextMeshProUGUI sub = subObj.AddComponent<TextMeshProUGUI>();
        sub.text = "The Monster Has Been Defeated!";
        sub.fontSize = 28;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color = Color.white;
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 0.55f);
        subRect.anchorMax = new Vector2(0.5f, 0.55f);
        subRect.sizeDelta = new Vector2(600, 50);

        // Restart Button
        GameObject restartBtn = CreateButton(canvasObj.transform, "RestartButton", "Play Again", new Vector2(0, -50));
        
        // Quit Button
        GameObject quitBtn = CreateButton(canvasObj.transform, "QuitButton", "Quit", new Vector2(0, -120));

        // Add Controller
        GameObject controller = new GameObject("WinScreenController");
        var ctrl = controller.AddComponent<WinScreenController>();
        
        // Wire buttons via SerializedObject
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("restartButton").objectReferenceValue = restartBtn.GetComponent<Button>();
        so.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        // EventSystem
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject evtSys = new GameObject("EventSystem");
            evtSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evtSys.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Save Scene
        string scenePath = "Assets/Game/Scenes/WinScreen.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[WinScreenCreator] ✅ Scene saved: {scenePath}");

        // Add to Build Settings
        AddSceneToBuildSettings(scenePath);

        Debug.Log("=== WIN SCREEN SCENE CREATED ===");
        EditorApplication.Beep();
    }

    private static GameObject CreateButton(Transform parent, string name, string text, Vector2 offset)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent);
        
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 0.2f); // Green
        
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.8f, 0.3f);
        colors.pressedColor = new Color(0.1f, 0.4f, 0.1f);
        btn.colors = colors;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.4f);
        rect.anchorMax = new Vector2(0.5f, 0.4f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(200, 50);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform);
        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = text;
        btnText.fontSize = 24;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return btnObj;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        
        // Check if already exists
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[WinScreenCreator] ✅ Added to Build Settings: {scenePath}");
    }
}
