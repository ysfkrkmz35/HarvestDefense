using UnityEngine;

/// <summary>
/// Day/Night döngüsünü yöneten timer sistemi
/// Gündüz → Gece → Gündüz sonsuz döngü
/// </summary>
public class TimeManager : MonoBehaviour
{
    [Header("Duration Settings")]
    [SerializeField] private float dayDuration = 60f;   // Gündüz süresi (saniye)
    [SerializeField] private float nightDuration = 45f; // Gece süresi (saniye)

    private float currentTimer;
    private bool isTimerRunning = false;

    private void Start()
    {
        // Gündüz ile başla
        StartDay();
    }

    void Update()
    {
        if (!isTimerRunning) return;

        currentTimer -= Time.deltaTime;

        if (currentTimer <= 0)
        {
            // Süre bitti, state'e göre geçiş yap
            if (GameManager.Instance == null)
            {
                Debug.LogError("[TimeManager] GameManager bulunamadı!");
                return;
            }

            // Gündüz bitti → Gece başlat
            if (GameManager.Instance.CurrentState == GameManager.GameState.Day)
            {
                StartNight();
            }
            // Gece bitti → Gündüz başlat
            else if (GameManager.Instance.CurrentState == GameManager.GameState.Night)
            {
                StartDay();
            }
        }
    }

    /// <summary>
    /// Gündüzü başlat
    /// </summary>
    void StartDay()
    {
        Debug.Log($"[TimeManager] ☀️ GÜNDÜZ BAŞLADI ({dayDuration}s)");

        currentTimer = dayDuration;
        isTimerRunning = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Day);
        }
    }

    /// <summary>
    /// Geceyi başlat
    /// </summary>
    void StartNight()
    {
        Debug.Log($"[TimeManager] 🌙 GECE BAŞLADI ({nightDuration}s)");

        currentTimer = nightDuration;
        isTimerRunning = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Night);
        }
    }

    /// <summary>
    /// Kalan süreyi al (UI için kullanılabilir)
    /// </summary>
    public float GetRemainingTime()
    {
        return Mathf.Max(0, currentTimer);
    }

    /// <summary>
    /// Süre yüzdesi (UI için kullanılabilir)
    /// </summary>
    public float GetTimePercentage()
    {
        if (GameManager.Instance == null) return 0;

        float totalDuration = GameManager.Instance.CurrentState == GameManager.GameState.Day
            ? dayDuration
            : nightDuration;

        return currentTimer / totalDuration;
    }
}