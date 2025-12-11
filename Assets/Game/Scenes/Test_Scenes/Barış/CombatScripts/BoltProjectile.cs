using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class BoltProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private int damage = 20;

    [Header("Visuals")]
    [SerializeField] private IsoSpriteRenderer isoRenderer; // Mermideki görsel script

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Setup(Vector2 direction)
    {
        // 1. Merminin görselini ayarla (Doğru açılı sprite'ı seç)
        isoRenderer.SetDirection(direction);

        // 2. Fiziksel hız ver
        rb.linearVelocity = direction * speed;

        // 3. 5 saniye sonra yok et (boşa giderse)
        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // HATA BURADA: IDamageable yerine IDamageableB yazmalısın
        // Eski hali: if (other.TryGetComponent(out IDamageable target)) 

        // YENİ HALİ:
        if (other.TryGetComponent(out IDamageableB target))
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
        // Layer kontrolü (7: Wall)
        else if (other.gameObject.layer == 7)
        {
            Destroy(gameObject);
        }
    }
}