using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger zone that loads the boss scene when player enters.
/// Uses POSITION checking, NOT colliders - safe for players without colliders.
/// </summary>
public class BossZoneTrigger : MonoBehaviour
{
    [Header("Zone Settings")]
    [Tooltip("Size of the trigger zone")]
    [SerializeField] private Vector2 zoneSize = new Vector2(50f, 50f);
    
    [Header("Scene Settings")]
    [Tooltip("Name of the boss scene to load")]
    [SerializeField] private string bossSceneName = "BossScene";
    
    [Header("Visual Settings")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private Color gizmoBorderColor = new Color(1f, 0f, 0f, 1f);

    private Transform playerTransform;
    private bool triggered = false;

    private void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Fallback: find by name
            player = GameObject.Find("Player");
        }
        if (player == null)
        {
            // Fallback: find PlayerController
            var pc = FindObjectOfType<HappyHarvest.PlayerController>();
            if (pc != null) player = pc.gameObject;
        }

        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log($"[BossZoneTrigger] ✅ Found player: {player.name}");
        }
        else
        {
            Debug.LogError("[BossZoneTrigger] ❌ Player not found! Tag 'Player' or name 'Player' required.");
        }

        Debug.Log($"[BossZoneTrigger] Zone: {zoneSize.x}x{zoneSize.y}, Target: {bossSceneName}");
    }

    private void Update()
    {
        if (triggered || playerTransform == null) return;

        // Check if player is inside zone bounds
        if (IsPlayerInZone())
        {
            triggered = true;
            Debug.Log($"[BossZoneTrigger] 🚪 Player entered Boss Zone! Loading: {bossSceneName}");
            
            if (Application.CanStreamedLevelBeLoaded(bossSceneName))
            {
                SceneManager.LoadScene(bossSceneName);
            }
            else
            {
                Debug.LogError($"[BossZoneTrigger] ❌ Scene '{bossSceneName}' NOT in Build Settings!");
            }
        }
    }

    private bool IsPlayerInZone()
    {
        Vector2 playerPos = playerTransform.position;
        Vector2 zoneCenter = transform.position;
        Vector2 halfSize = zoneSize / 2f;

        // Simple AABB check
        return playerPos.x >= zoneCenter.x - halfSize.x &&
               playerPos.x <= zoneCenter.x + halfSize.x &&
               playerPos.y >= zoneCenter.y - halfSize.y &&
               playerPos.y <= zoneCenter.y + halfSize.y;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, new Vector3(zoneSize.x, zoneSize.y, 0.1f));
        Gizmos.color = gizmoBorderColor;
        Gizmos.DrawWireCube(transform.position, new Vector3(zoneSize.x, zoneSize.y, 0.1f));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawCube(transform.position, new Vector3(zoneSize.x, zoneSize.y, 0.1f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(zoneSize.x, zoneSize.y, 0.1f));

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * (zoneSize.y / 2 + 2f), 
            $"BOSS ZONE\n{zoneSize.x}x{zoneSize.y}\nLoads: {bossSceneName}");
        #endif
    }
}
