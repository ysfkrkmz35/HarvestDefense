using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public float dayDuration = 5f; // Test için 5 saniye
    private float currentTimer;
    private bool isTimerRunning = false;

    private void Start()
    {
        // Kimseden haber bekleme, direkt sayacı başlat
        Debug.Log("TimeManager: Sayaç zorla başlatıldı.");
        currentTimer = dayDuration;
        isTimerRunning = true;
    }

    void Update()
    {
        if (isTimerRunning)
        {
            currentTimer -= Time.deltaTime;

            if (currentTimer <= 0)
            {
                currentTimer = 0;
                isTimerRunning = false;
                
                Debug.Log("TimeManager: Süre bitti! Gece çağrılıyor...");
                
                // GameManager varsa geceyi başlat
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ChangeState(GameManager.GameState.Night);
                }
                else
                {
                    Debug.LogError("HATA: GameManager sahnede bulunamadı!");
                }
            }
        }
    }
}