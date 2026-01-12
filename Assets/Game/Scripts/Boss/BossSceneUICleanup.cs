using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Hides the coin counter and timer UI in boss scene.
/// Works with UI Toolkit elements.
/// </summary>
public class BossSceneUICleanup : MonoBehaviour
{
    private void Start()
    {
        // Small delay to ensure UI is loaded
        Invoke(nameof(CleanupUI), 0.1f);
    }

    private void CleanupUI()
    {
        var uiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        
        foreach (var doc in uiDocuments)
        {
            if (doc.rootVisualElement == null) continue;
            
            // Hide CoinAmount - go up to grandparent to get icon too
            var coinAmount = doc.rootVisualElement.Q<Label>("CoinAmount");
            if (coinAmount != null)
            {
                // Go up 2 levels to hide container with icon
                var container = coinAmount.parent?.parent ?? coinAmount.parent;
                if (container != null)
                {
                    container.style.display = DisplayStyle.None;
                    Debug.Log($"[BossSceneUICleanup] ✅ Hidden coin container: {container.name}");
                }
            }
            
            // Hide Timer - go up to grandparent to get icon too
            var timer = doc.rootVisualElement.Q<Label>("Timer");
            if (timer != null)
            {
                var container = timer.parent?.parent ?? timer.parent;
                if (container != null)
                {
                    container.style.display = DisplayStyle.None;
                    Debug.Log($"[BossSceneUICleanup] ✅ Hidden timer container: {container.name}");
                }
            }
        }
        
        Debug.Log("[BossSceneUICleanup] ✅ Cleanup complete");
    }
}
