using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to configure MonD_01 as the boss.
/// Adds BossController and BossHealth, disables old enemy scripts.
/// Run via: Tools > Setup MonD_01 as Boss
/// </summary>
public class BossSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup MonD_01 as Boss")]
    public static void SetupBoss()
    {
        Debug.Log("=== BOSS SETUP STARTED ===");

        // Find MonD_01 in scene
        GameObject boss = GameObject.Find("MonD_01");
        if (boss == null)
        {
            Debug.LogError("[BossSetupTool] ❌ MonD_01 not found in scene!");
            return;
        }

        Debug.Log($"[BossSetupTool] Found: {boss.name}");

        // Disable old enemy scripts (keep for fallback)
        var simpleAI = boss.GetComponent<YusufTest.SimpleEnemyAI>();
        if (simpleAI != null)
        {
            simpleAI.enabled = false;
            Debug.Log("[BossSetupTool] 🔇 Disabled SimpleEnemyAI");
        }

        var enemyHealth = boss.GetComponent<YusufTest.EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.enabled = false;
            Debug.Log("[BossSetupTool] 🔇 Disabled EnemyHealth (using BossHealth instead)");
        }

        // Disable coin/XP drops - boss has special victory handling
        var dropHandler = boss.GetComponent<YusufTest.EnemyDropHandler>();
        if (dropHandler != null)
        {
            dropHandler.enabled = false;
            Debug.Log("[BossSetupTool] 🔇 Disabled EnemyDropHandler (no coins for boss)");
        }

        // Add Rigidbody2D if missing
        var rb = boss.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = boss.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.bodyType = RigidbodyType2D.Kinematic;
            Debug.Log("[BossSetupTool] ✅ Added Rigidbody2D");
        }

        // Add Animator if missing
        var animator = boss.GetComponent<Animator>();
        if (animator == null)
        {
            animator = boss.AddComponent<Animator>();
            Debug.Log("[BossSetupTool] ✅ Added Animator (assign controller manually)");
        }

        // Add BossHealth
        var bossHealth = boss.GetComponent<BossHealth>();
        if (bossHealth == null)
        {
            bossHealth = boss.AddComponent<BossHealth>();
            Debug.Log("[BossSetupTool] ✅ Added BossHealth");
        }

        // Add BossController
        var bossController = boss.GetComponent<BossController>();
        if (bossController == null)
        {
            bossController = boss.AddComponent<BossController>();
            Debug.Log("[BossSetupTool] ✅ Added BossController");
        }

        // Wire TriggerZone if exists
        var triggerZone = GameObject.Find("TriggerZone");
        if (triggerZone != null)
        {
            var bossTrigger = triggerZone.GetComponent<BossTrigger>();
            if (bossTrigger != null)
            {
                // Use serialized property to set bossController field
                SerializedObject so = new SerializedObject(bossTrigger);
                so.FindProperty("bossController").objectReferenceValue = bossController;
                so.ApplyModifiedProperties();
                Debug.Log("[BossSetupTool] ✅ Wired TriggerZone -> BossController");
            }
        }

        // Wire BossVictoryHandler if exists
        var victoryHandler = GameObject.Find("BossVictoryHandler");
        if (victoryHandler != null)
        {
            var handler = victoryHandler.GetComponent<BossVictoryHandler>();
            if (handler != null)
            {
                SerializedObject so = new SerializedObject(handler);
                var prop = so.FindProperty("bossHealth");
                if (prop != null)
                {
                    prop.objectReferenceValue = bossHealth;
                    so.ApplyModifiedProperties();
                    Debug.Log("[BossSetupTool] ✅ Wired BossVictoryHandler -> BossHealth");
                }
            }
        }

        Debug.Log("=== BOSS SETUP COMPLETE ===");
        EditorUtility.SetDirty(boss);
        EditorApplication.Beep();
    }
}
