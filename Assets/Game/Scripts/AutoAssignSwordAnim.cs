using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AutoAssignSwordAnim : MonoBehaviour
{
    void Start()
    {
#if UNITY_EDITOR
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/SwordOverride.overrideController");
        if (controller != null)
        {
            // Find finding by type
            var playerController = FindObjectOfType<HappyHarvest.PlayerController>();
            GameObject player = playerController != null ? playerController.gameObject : GameObject.Find("Player");

            if (player != null)
            {
                var anim = player.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.runtimeAnimatorController = controller;
                    Debug.Log("⚔️ [AutoAssignSwordAnim] Assigned SwordOverride controller to Player!");
                }
                else Debug.LogError("Player has no Animator!");
            }
            else Debug.LogError("Player not found!");
        }
        else
        {
            Debug.LogError("Could not load Assets/Game/SwordOverride.overrideController");
        }
#endif
        Destroy(this.gameObject); // Cleanup self
    }
}
