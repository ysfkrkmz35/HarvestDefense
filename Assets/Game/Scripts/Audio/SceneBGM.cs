using UnityEngine;

/// <summary>
/// Sahne boyunca döngülü müzik çalar
/// - AudioSource yoksa otomatik ekler
/// - DontDestroyOnLoad ile sahne geçişlerinde de devam edebilir (opsiyonel)
/// </summary>
public class SceneBGM : MonoBehaviour
{
    [Header("Music Settings")]
    [Tooltip("Çalınacak müzik dosyası")]
    [SerializeField] private AudioClip musicClip;
    
    [Tooltip("Ses seviyesi (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;
    
    [Tooltip("Sahne geçişlerinde devam etsin mi?")]
    [SerializeField] private bool persistAcrossScenes = false;

    private AudioSource audioSource;
    private static SceneBGM instance;

    private void Awake()
    {
        // Singleton pattern (persistAcrossScenes için)
        if (persistAcrossScenes)
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // AudioSource ekle veya bul
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Ayarları uygula
        audioSource.clip = musicClip;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (musicClip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log($"[SceneBGM] 🎵 Müzik başladı: {musicClip.name}");
        }
        else if (musicClip == null)
        {
            Debug.LogWarning("[SceneBGM] ⚠️ Müzik dosyası atanmadı!");
        }
    }

    /// <summary>
    /// Müziği durdur
    /// </summary>
    public void StopMusic()
    {
        if (audioSource != null) audioSource.Stop();
    }

    /// <summary>
    /// Ses seviyesini değiştir
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null) audioSource.volume = volume;
    }
}
