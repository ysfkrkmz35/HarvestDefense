using UnityEngine;

/// <summary>
/// Mehmet'in test sahnesi için yardımcı script
/// Gece/Gündüz döngüsünü manuel test etmek için
/// </summary>
public class TestHelper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;

    private void Update()
    {
        // F1 tuşu: Gece başlat
        if (Input.GetKeyDown(KeyCode.F1))
        {
            StartNight();
        }

        // F2 tuşu: Gündüz başlat
        if (Input.GetKeyDown(KeyCode.F2))
        {
            StartDay();
        }

        // F3 tuşu: Tek düşman spawn et (test için)
        if (Input.GetKeyDown(KeyCode.F3))
        {
            TestSpawnSingleEnemy();
        }

        // F4 tuşu: Game Over
        if (Input.GetKeyDown(KeyCode.F4))
        {
            GameOver();
        }
    }

    /// <summary>
    /// Gece başlat (Manuel test için)
    /// </summary>
    public void StartNight()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Night);
            Debug.Log("TestHelper: Gece başlatıldı (Manuel)");
        }
        else
        {
            Debug.LogError("TestHelper: GameManager bulunamadı!");
        }
    }

    /// <summary>
    /// Gündüz başlat (Manuel test için)
    /// </summary>
    public void StartDay()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Day);
            Debug.Log("TestHelper: Gündüz başlatıldı (Manuel)");
        }
        else
        {
            Debug.LogError("TestHelper: GameManager bulunamadı!");
        }
    }

    /// <summary>
    /// Game Over
    /// </summary>
    public void GameOver()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.GameOver);
            Debug.Log("TestHelper: Game Over (Manuel)");
        }
        else
        {
            Debug.LogError("TestHelper: GameManager bulunamadı!");
        }
    }

    /// <summary>
    /// Tek düşman spawn et (Test için)
    /// </summary>
    public void TestSpawnSingleEnemy()
    {
        if (enemySpawner != null)
        {
            enemySpawner.TestSpawn();
            Debug.Log("TestHelper: Tek düşman spawn edildi (Manuel)");
        }
        else
        {
            Debug.LogError("TestHelper: EnemySpawner referansı eksik!");
        }
    }

    private void OnGUI()
    {
        // Ekranın sol üstünde klavye kontrollerini göster
        GUI.Box(new Rect(10, 10, 300, 120), "TEST KONTROLLERI");
        GUI.Label(new Rect(20, 35, 280, 20), "F1: Gece Başlat (Night)");
        GUI.Label(new Rect(20, 55, 280, 20), "F2: Gündüz Başlat (Day)");
        GUI.Label(new Rect(20, 75, 280, 20), "F3: Tek Düşman Spawn Et");
        GUI.Label(new Rect(20, 95, 280, 20), "F4: Game Over");

        // Mevcut oyun durumunu göster
        if (GameManager.Instance != null)
        {
            GUI.Box(new Rect(10, 140, 300, 50), "MEVCUT DURUM");
            GUI.Label(new Rect(20, 165, 280, 20), $"State: {GameManager.Instance.CurrentState}");
        }
    }
}
