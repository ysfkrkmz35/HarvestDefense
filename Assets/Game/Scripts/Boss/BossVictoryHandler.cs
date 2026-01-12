using UnityEngine;
using TMPro;
using System.Collections;

public class BossVictoryHandler : MonoBehaviour
{
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI victoryText;

    private void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        
        if (bossHealth != null)
        {
            bossHealth.OnDeath += HandleVictory;
        }
        else
        {
            // Try to find if not assigned
            bossHealth = FindObjectOfType<BossHealth>();
            if (bossHealth != null) bossHealth.OnDeath += HandleVictory;
        }
    }

    private void HandleVictory()
    {
        Debug.Log("🎉 VICTORY! Boss Defeated. 🎉");
        
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        // Wait for death animation
        yield return new WaitForSeconds(2f);

        // Show UI
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            if (victoryText != null)
            {
                victoryText.text = "VICTORY!";
                victoryText.alpha = 0;
                // Simple fade in
                for (float t = 0; t < 1; t += Time.deltaTime)
                {
                    victoryText.alpha = t;
                    yield return null;
                }
                victoryText.alpha = 1;
            }
        }

        // Wait before loading win scene
        yield return new WaitForSeconds(3f);

        // Load Win Screen
        Debug.Log("[BossVictoryHandler] 🏆 Loading Win Screen!");
        UnityEngine.SceneManagement.SceneManager.LoadScene("WinScreen");
    }

    private void OnDestroy()
    {
        if (bossHealth != null) bossHealth.OnDeath -= HandleVictory;
    }
}
