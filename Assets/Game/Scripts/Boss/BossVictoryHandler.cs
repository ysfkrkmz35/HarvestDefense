using UnityEngine;
using TMPro;
using System.Collections;

public class BossVictoryHandler : MonoBehaviour
{
    [Header("Boss Reference")]
    [Tooltip("Assign the boss GameObject (MonD_01). Will auto-detect health component.")]
    public GameObject bossObject;
    
    [Header("UI Settings")]
    public BossHealth bossHealth;
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryText;
    
    [Header("Portal Settings")]
    [Tooltip("Portal object in scene (will be activated on boss death)")]
    public GameObject portal;
    [Tooltip("If true, loads win screen. If false, player uses portal to continue.")]
    public bool autoLoadWinScreen = false;

    private YusufTest.EnemyHealth enemyHealth;

    private void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (portal != null) portal.SetActive(false);
        
        // Try to get BossHealth first
        if (bossHealth != null)
        {
            bossHealth.OnDeath += HandleVictory;
            Debug.Log("[BossVictoryHandler] ✅ Subscribed to BossHealth.OnDeath");
        }
        else if (bossObject != null)
        {
            // Try BossHealth on boss object
            bossHealth = bossObject.GetComponent<BossHealth>();
            if (bossHealth != null)
            {
                bossHealth.OnDeath += HandleVictory;
                Debug.Log("[BossVictoryHandler] ✅ Found BossHealth on bossObject");
            }
            else
            {
                // Fallback: Try EnemyHealth
                enemyHealth = bossObject.GetComponent<YusufTest.EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.OnDeath += HandleVictory;
                    Debug.Log("[BossVictoryHandler] ✅ Found EnemyHealth on bossObject, subscribed to OnDeath");
                }
            }
        }
        else
        {
            Debug.LogError("[BossVictoryHandler] ❌ No Boss assigned! Drag MonD_01 to Boss Object field.");
        }
    }

    private void HandleVictory()
    {
        Debug.Log("🎉 VICTORY! Boss Defeated. 🎉");
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        yield return new WaitForSeconds(2f);

        // Spawn Portal at boss death location
        if (portal != null && bossObject != null)
        {
            portal.transform.position = bossObject.transform.position;
            portal.SetActive(true);
            Debug.Log($"[BossVictoryHandler] 🌀 Portal spawned at boss location: {portal.transform.position}");
        }
        else if (portal != null)
        {
            portal.SetActive(true);
            Debug.Log("[BossVictoryHandler] 🌀 Portal spawned (no boss reference for position)");
        }

        // Show Victory UI
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            if (victoryText != null)
            {
                victoryText.text = "VICTORY!";
                victoryText.alpha = 0;
                for (float t = 0; t < 1; t += Time.deltaTime)
                {
                    victoryText.alpha = t;
                    yield return null;
                }
                victoryText.alpha = 1;
            }
        }

        if (autoLoadWinScreen)
        {
            yield return new WaitForSeconds(3f);
            UnityEngine.SceneManagement.SceneManager.LoadScene("WinScreen");
        }
    }

    private void OnDestroy()
    {
        if (bossHealth != null) bossHealth.OnDeath -= HandleVictory;
        if (enemyHealth != null) enemyHealth.OnDeath -= HandleVictory;
    }
}
