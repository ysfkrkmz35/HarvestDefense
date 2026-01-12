using UnityEngine;

/// <summary>
/// Place this in the Boss Scene.
/// Fixes persistent objects from previous scene and resets them for boss fight.
/// </summary>
public class CleanupPersistentObjects : MonoBehaviour
{
    [SerializeField] private Transform playerSpawnPoint;

    private void Start()
    {
        // Small delay to let scene fully load
        Invoke(nameof(FixPersistentObjects), 0.1f);
    }

    private void FixPersistentObjects()
    {
        // Find the persistent Character
        var player = FindFirstObjectByType<HappyHarvest.PlayerController>();
        if (player != null)
        {
            Debug.Log($"[Cleanup] Found player: {player.name}");
            
            // Move to spawn point if set
            if (playerSpawnPoint != null)
            {
                player.transform.position = playerSpawnPoint.position;
                Debug.Log($"[Cleanup] ✅ Player moved to spawn: {playerSpawnPoint.position}");
            }
            
            // Ensure player is active and visible
            player.gameObject.SetActive(true);
            var sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.enabled = true;
            
            // Re-enable controls
            player.enabled = true;
        }
        else
        {
            Debug.LogError("[Cleanup] ❌ No player found! Boss scene needs a Character.");
        }

        var gameManagers = FindObjectsByType<HappyHarvest.GameManager>(FindObjectsSortMode.None);
        if (gameManagers.Length > 1)
        {
            for (int i = 1; i < gameManagers.Length; i++)
            {
                Debug.Log($"[Cleanup] 🗑️ Destroying duplicate GameManager");
                Destroy(gameManagers[i].gameObject);
            }
        }

        Debug.Log("[Cleanup] ✅ Scene setup complete!");
    }
}
