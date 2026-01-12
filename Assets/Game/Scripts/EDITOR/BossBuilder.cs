using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class BossBuilder : EditorWindow
{
    [MenuItem("Tools/Rebuild Refined Boss")]
    public static void Build()
    {
        // 1. Cleanup
        GameObject existingBoss = GameObject.Find("Boss_Refined");
        if (existingBoss) DestroyImmediate(existingBoss);

        GameObject existingUI = GameObject.Find("BossHealthUI_Canvas");
        if (existingUI) DestroyImmediate(existingUI);

        GameObject existingVictory = GameObject.Find("BossVictoryHandler");
        if (existingVictory) DestroyImmediate(existingVictory);

        // 2. Find Location
        GameObject mond = GameObject.Find("MonD_01");
        Vector3 spawnPos = Vector3.zero;
        if (mond)
        {
            spawnPos = mond.transform.position;
            mond.SetActive(false);
        }
        else
        {
            Debug.LogWarning("MonD_01 not found, using zero.");
        }

        // 3. Create BOSS
        GameObject boss = new GameObject("Boss_Refined");
        boss.transform.position = spawnPos;
        boss.transform.localScale = Vector3.one * 1.5f; // Big boss

        // Components
        var sr = boss.AddComponent<SpriteRenderer>();
        // Load Sprite
        string[] guids = AssetDatabase.FindAssets("Spider_Walk t:Sprite");
        if (guids.Length > 0) sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        sr.color = Color.white;

        var rb = boss.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // Controlled by script
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        var col = boss.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.5f, 1f);

        var anim = boss.AddComponent<Animator>();
        // Find Controller
        string[] ctrlGuids = AssetDatabase.FindAssets("SpiderController t:AnimatorController");
        if (ctrlGuids.Length > 0) anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AssetDatabase.GUIDToAssetPath(ctrlGuids[0]));

        var health = boss.AddComponent<BossHealth>();
        var controller = boss.AddComponent<BossController>(); // Requires refs

        // 4. Create TRIGGER
        GameObject triggerObj = new GameObject("TriggerZone");
        triggerObj.transform.SetParent(boss.transform);
        triggerObj.transform.localPosition = Vector3.zero;
        var trigCol = triggerObj.AddComponent<BoxCollider2D>();
        trigCol.isTrigger = true;
        trigCol.size = new Vector2(10f, 10f); // Detection Zone

        var triggerScript = triggerObj.AddComponent<BossTrigger>();
        
        // Wire Trigger -> Controller
        var soTrigger = new SerializedObject(triggerScript);
        soTrigger.FindProperty("bossController").objectReferenceValue = controller;
        soTrigger.ApplyModifiedProperties();

        // 5. Create UI
        GameObject uiObj = new GameObject("BossHealthUI_Canvas");
        Canvas canvas = uiObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiObj.AddComponent<CanvasScaler>();
        uiObj.AddComponent<GraphicRaycaster>();

        GameObject barObj = new GameObject("BossHealthBar");
        barObj.transform.SetParent(uiObj.transform);
        var barRect = barObj.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0, -50);
        barRect.sizeDelta = new Vector2(500, 40);

        var bg = barObj.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barObj.transform);
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-4, -4); // Padding
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.color = Color.red;

        var uiScript = barObj.AddComponent<BossHealthUI>();
        // Wire UI
        var soUI = new SerializedObject(uiScript);
        soUI.FindProperty("container").objectReferenceValue = barObj;
        soUI.FindProperty("fillImage").objectReferenceValue = fillImg;
        soUI.ApplyModifiedProperties();

        // 6. Create VICTORY
        GameObject victoryObj = new GameObject("BossVictoryHandler");
        var victoryScript = victoryObj.AddComponent<BossVictoryHandler>();
        
        // Wire Victory -> BossHealth
        var soVictory = new SerializedObject(victoryScript);
        soVictory.FindProperty("bossHealth").objectReferenceValue = health;
        // Optional: Create Victory UI Text
        GameObject vicTextObj = new GameObject("VictoryText");
        vicTextObj.transform.SetParent(uiObj.transform, false);
        var vtRect = vicTextObj.AddComponent<RectTransform>();
        vtRect.anchorMin = Vector2.zero;
        vtRect.anchorMax = Vector2.one;
        var tmp = vicTextObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "VICTORY";
        tmp.fontSize = 80;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.yellow;
        tmp.alpha = 0; // Hidden initially
        
        soVictory.FindProperty("victoryText").objectReferenceValue = tmp;
        soVictory.FindProperty("victoryPanel").objectReferenceValue = vicTextObj; // Reuse object as panel
        soVictory.ApplyModifiedProperties();

        // Wire Controller -> Internal Refs (Found via GetComponent in Awake, so fine)
        // But let's wire SpriteRenderer explicitly if needed
        var soCtrl = new SerializedObject(controller);
        soCtrl.FindProperty("spriteRenderer").objectReferenceValue = sr;
        soCtrl.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(boss, "Create Boss");
        Undo.RegisterCreatedObjectUndo(uiObj, "Create Boss UI");
        Undo.RegisterCreatedObjectUndo(victoryObj, "Create Boss Victory");

        Debug.Log("Refined Boss System Built!");
        Selection.activeGameObject = boss;
    }
}
