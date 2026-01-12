using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple Win Screen controller.
/// Displays victory message and allows restart or quit.
/// </summary>
public class WinScreenController : MonoBehaviour
{
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        Time.timeScale = 1f; // Ensure game runs

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    public void RestartGame()
    {
        // Load main game scene (adjust name as needed)
        SceneManager.LoadScene("MainGame");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
