using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime healthbar controller - updates fill based on HealthB.
/// </summary>
public class RuntimeHealthBar : MonoBehaviour
{
    private HealthB health;
    private Image fillImg;
    private Color colorFull;
    private Color colorLow;
    private Camera cam;
    
    public void Setup(HealthB healthComponent, Image fill, Color full, Color low)
    {
        health = healthComponent;
        fillImg = fill;
        colorFull = full;
        colorLow = low;
        cam = Camera.main;
        
        if (health != null)
        {
            health.OnDeath += () => gameObject.SetActive(false);
        }
    }
    
void LateUpdate()
    {
        if (cam != null)
            transform.rotation = cam.transform.rotation;
        
        if (health != null && fillImg != null)
        {
            float pct = health.GetHealthPercent();
            fillImg.fillAmount = Mathf.Clamp01(pct);
            fillImg.color = Color.Lerp(colorLow, colorFull, pct);
        }
    }
}