using UnityEditor;
using UnityEngine;

public class FixSwordAnim
{
    [MenuItem("Tools/Fix Sword Animation")]
    public static void ApplyFix()
    {
        // Find player by PlayerController component (more robust than name)
        var playerController = Object.FindObjectOfType<HappyHarvest.PlayerController>();
        GameObject player = playerController != null ? playerController.gameObject : GameObject.Find("Player");
        
        if (player == null) 
        { 
            Debug.LogError("❌ Player not found in scene!"); 
            return; 
        }
        
        var animator = player.GetComponent<Animator>();
        if (animator == null) 
        { 
            Debug.LogError("❌ Animator not found on Player!"); 
            return; 
        }
        
        var overrideController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/SwordOverride.overrideController");
        if (overrideController == null) 
        { 
            Debug.LogError("❌ OverrideController not found at 'Assets/Game/SwordOverride.overrideController'!"); 
            return; 
        }
        
        // Assign the controller
        animator.runtimeAnimatorController = overrideController;
        Debug.Log($"✅ Success! Assigned SwordOverride to Player '{player.name}'.");
    }
}
