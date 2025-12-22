using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class EnemyAnimator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Animator animator;

    // Dosya isimlerindeki açıları buraya yazacağız. 
    // Senin dosya listene göre: 0, 30, 45, 60, 90, 120...
    private readonly int[] availableAngles = {
        0, 30, 45, 60, 90, 120, 135, 150,
        180, 210, 225, 240, 270, 300, 315, 330
    };

    private Rigidbody2D rb;
    private string currentAnimState = "";
    private SpriteRenderer spriteRenderer;
    private Sprite lastValidSprite;

    private void Start()
    {
        // Initialize with current sprite to prevent null issues
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            lastValidSprite = spriteRenderer.sprite;
    }

private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

private void LateUpdate()
    {
        Vector2 velocity = rb.linearVelocity;

        if (velocity.magnitude > 0.1f)
        {
            animator.enabled = true;
            PlayDirectionalAnimation(velocity);
            if (spriteRenderer != null && spriteRenderer.sprite != null)
                lastValidSprite = spriteRenderer.sprite;
        }
        else
        {
            animator.enabled = false;
            if (spriteRenderer != null && lastValidSprite != null)
                spriteRenderer.sprite = lastValidSprite;
        }
    }

    private void PlayDirectionalAnimation(Vector2 direction)
    {
        animator.speed = 1;

        // 1. Unity'nin açısını al (0 = Sağ, Artış = Saat Yönü Tersi)
        float unityAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 2. Senin Asset sistemine dönüştür (0 = Yukarı, Artış = Saat Yönü)
        // Formül: 90 - UnityAçısı.
        // Örnekler:
        // Yukarı (Unity 90) -> 90 - 90 = 0 (Senin 000)
        // Sağ (Unity 0)    -> 90 - 0 = 90 (Senin 090)
        // Aşağı (Unity -90)-> 90 - (-90) = 180 (Senin 180)
        float assetAngle = 90f - unityAngle;

        // Negatif açıları 0-360 aralığına çek (Örn: -90 olursa 270 olsun)
        if (assetAngle < 0) assetAngle += 360f;

        // 3. En yakın açıyı bul
        int closestAngle = GetClosestAngle(assetAngle);

        // 4. Dosya ismini oluştur (spider_walk_000 formatında)
        string stateName = "spider_walk_" + closestAngle.ToString("000");

        // Performans için: Eğer zaten bu animasyon oynuyorsa tekrar başlatma
        if (currentAnimState == stateName) return;

        animator.Play(stateName);
        currentAnimState = stateName;
    }

    private int GetClosestAngle(float targetAngle)
    {
        float minDiff = 360f;
        int bestAngle = 0;

        foreach (int angle in availableAngles)
        {
            // Dairesel farkı hesapla (Örn: 350 ile 10 arasındaki fark 20'dir)
            float diff = Mathf.Abs(Mathf.DeltaAngle(targetAngle, angle));
            if (diff < minDiff)
            {
                minDiff = diff;
                bestAngle = angle;
            }
        }
        return bestAngle;
    }
}