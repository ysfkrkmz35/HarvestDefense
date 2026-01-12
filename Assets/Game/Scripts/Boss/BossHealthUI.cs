using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private Image fillImage;
    [SerializeField] private float lerpSpeed = 5f;

    private float targetFill = 1f;

    private void Awake()
    {
        // Subscribe to events
        BossHealth.OnHealthChanged += UpdateHealth;
        BossHealth.OnBossActiveStateChanged += SetActive;
        
        if (container != null) container.SetActive(false);
    }

    private void OnDestroy()
    {
        BossHealth.OnHealthChanged -= UpdateHealth;
        BossHealth.OnBossActiveStateChanged -= SetActive;
    }

    private void Update()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
        }
    }

    private void UpdateHealth(float percent)
    {
        targetFill = percent;
    }

    private void SetActive(bool active)
    {
        if (container != null) container.SetActive(active);
    }
}
