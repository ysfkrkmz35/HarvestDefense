using UnityEngine;
using System; // Action için gerekli

public class GameManager : MonoBehaviour
{
    // 1. SINGLETON YAPISI (Her yerden ulaşılabilmesi için)
    public static GameManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Sahne değişse de yok olmasın
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 2. STATE MACHINE (Oyunun Durumları)
    public enum GameState { Day, Night, GameOver, Pause }
    public GameState CurrentState;

    // 3. EVENT SYSTEM (Observer Pattern)
    // Diğer scriptlerin abone olacağı olaylar
    public static event Action OnDayStart;   // Gündüz olunca tetiklenecek
    public static event Action OnNightStart; // Gece olunca tetiklenecek
    public static event Action OnGameOver;   // Oyun bitince tetiklenecek

    private void Start()
    {
        // Oyun gündüz başlasın
        ChangeState(GameState.Day);
    }

    // Durumu değiştiren fonksiyon
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Day:
                Debug.Log("Gündüz Oldu! İnşaat Vakti.");
                OnDayStart?.Invoke(); // Abone olan herkese haber ver
                break;

            case GameState.Night:
                Debug.Log("Gece Oldu! Savaş Vakti.");
                OnNightStart?.Invoke(); // Abone olan herkese haber ver
                break;

            case GameState.GameOver:
                Debug.Log("Oyun Bitti!");
                OnGameOver?.Invoke();
                Time.timeScale = 0; // Oyunu dondur
                break;
        }
    }
}