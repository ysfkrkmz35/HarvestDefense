using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class StoryTrigger : MonoBehaviour
{
    [Header("Cutscene Data")]
    [Tooltip("Oynatılacak hikaye verisi")]
    public StoryData storyData;

    [Header("Settings")]
    [Tooltip("Sadece bir kere mi oynasın?")]
    public bool playOnce = true;
    
    [Tooltip("Trigger'a girdiği an otomatik başlasın mı?")]
    public bool autoTrigger = true;

    [Tooltip("Sahne başladığında otomatik oynasın mı?")]
    public bool playOnStart = false;

    private bool hasPlayed = false;

    private void Start()
    {
        if (playOnStart)
        {
            Invoke(nameof(PlayCutscene), 0.1f); // Küçük bir gecikme ile başlat
        }
    }

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playOnStart) return; // Zaten startta oynadıysa trigger tetiklemesin

        if (hasPlayed && playOnce) return;

        if (other.CompareTag("Player") && autoTrigger)
        {
            PlayCutscene();
        }
    }

    public void PlayCutscene()
    {
        if (StoryCutsceneManager.Instance != null && storyData != null)
        {
            StoryCutsceneManager.Instance.PlayStory(storyData, OnCutsceneFinished);
        }
        else
        {
            Debug.LogError("[StoryTrigger] Manager veya Data eksik!");
        }
    }

    private void OnCutsceneFinished()
    {
        Debug.Log("Cutscene bitti.");
        hasPlayed = true;
        
        // İsterseniz burada trigger'ı yok edebilirsiniz
        if (playOnce)
        {
            // Destroy(gameObject); // Opsiyonel
        }
    }
}
