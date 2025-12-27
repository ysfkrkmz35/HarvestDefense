using UnityEditor;
using UnityEngine;

public class DebugSwordVisual
{
    [MenuItem("Tools/Debug Sword Visual")]
    public static void SpawnSword()
    {
        var prefabPath = "Assets/Game/Scenes/Test_Scenes/Yusuf/Assets/Sword4_FBX.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"❌ Could not load prefab at {prefabPath}");
            return;
        }

        var player = GameObject.Find("Character"); // We know the name is Character now
        Vector3 spawnPos = player ? player.transform.position + Vector3.right : Vector3.zero;

        var inst = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        inst.name = "DEBUG_SWORD_VISUAL";
        
        Debug.Log($"✅ Spawned Debug Sword at {spawnPos}. Scale: {inst.transform.localScale}");
        
        // Also try to find the Hand Bone and parent it there to test attachment
        if (player)
        {
            // Try to find the bone by name "hand_r_slot_bone"
            Transform[] children = player.GetComponentsInChildren<Transform>();
            foreach(var t in children)
            {
                if (t.name == "hand_r_slot_bone")
                {
                    var attached = Object.Instantiate(prefab, t);
                    attached.name = "DEBUG_SWORD_ATTACHED";
                    attached.transform.localPosition = Vector3.zero;
                    Debug.Log("✅ Attached Debug Sword to hand_r_slot_bone!");
                    break;
                }
            }
        }
    }
}
