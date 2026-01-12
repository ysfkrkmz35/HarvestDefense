using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to clean up the Boss Scene by removing spawners and non-boss enemies.
/// Run via: Tools > Clean Boss Scene
/// </summary>
public class BossSceneCleanup : EditorWindow
{
    // Objects to DELETE (by name, partial match)
    private static readonly string[] ObjectsToDelete = new string[]
    {
        "EnemySpawner",
        "SimpleEnemySpawner",
        "SpiderSpawner",
        "Prefab_Market",
        "boatentity",
        "Boss_Spider",
        "Boss_Refined"
    };

    // Objects to KEEP (protected, won't be deleted)
    private static readonly string[] ObjectsToKeep = new string[]
    {
        "MonD_01",          // Main Boss
        "TriggerZone",      // Boss activation
        "Player",
        "GameManager",
        "Canvas",
        "EventSystem",
        "Managers",
        "BossVictoryHandler",
        "GAMEOVERUI"
    };

    [MenuItem("Tools/Clean Boss Scene")]
    public static void CleanScene()
    {
        Debug.Log("=== BOSS SCENE CLEANUP STARTED ===");

        int deletedCount = 0;
        List<GameObject> toDelete = new List<GameObject>();

        // Find all root objects
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;

            string objName = obj.name;

            // Check if should delete
            bool shouldDelete = false;
            foreach (string deletePattern in ObjectsToDelete)
            {
                if (objName.Contains(deletePattern))
                {
                    shouldDelete = true;
                    break;
                }
            }

            // Check if protected
            bool isProtected = false;
            foreach (string keepPattern in ObjectsToKeep)
            {
                if (objName.Contains(keepPattern))
                {
                    isProtected = true;
                    break;
                }
            }

            if (shouldDelete && !isProtected)
            {
                toDelete.Add(obj);
            }
        }

        // Delete collected objects
        foreach (GameObject obj in toDelete)
        {
            Debug.Log($"[BossSceneCleanup] 🗑️ Deleting: {obj.name}");
            DestroyImmediate(obj);
            deletedCount++;
        }

        Debug.Log($"=== BOSS SCENE CLEANUP COMPLETE: {deletedCount} objects deleted ===");
        EditorApplication.Beep();
    }
}
