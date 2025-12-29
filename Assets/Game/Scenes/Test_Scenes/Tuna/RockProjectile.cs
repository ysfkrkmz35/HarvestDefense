using UnityEngine;

public class RockProjectile : MonoBehaviour
{
    public float speed = 7f;      // Kayanýn uçuþ hýzý
    public int damage = 15;       // Vereceði hasar
    public float lifeTime = 3f;   // Kaç saniye sonra yok olsun

    private Vector3 targetDirection;

    void Start()
    {
        // 1. Oyuncuyu bul
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // 2. Oyuncunun o anki konumuna doðru yön belirle
            targetDirection = (player.transform.position - transform.position).normalized;
        }
        else
        {
            // Oyuncu yoksa düz git (Sola veya saða)
            targetDirection = Vector3.right;
        }

        // 3. Hafýza dolmasýn diye 3 saniye sonra kendi kendini yok et
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Belirlenen yöne doðru uç
        transform.position += targetDirection * speed * Time.deltaTime;

        // Kendi etrafýnda dönsün (Görsel efekt)
        transform.Rotate(0, 0, 360 * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Oyuncuya çarparsa
        if (hitInfo.CompareTag("Player"))
        {
            // Hasar ver (PlayerHealth scriptin varsa burayý aç)
            // hitInfo.GetComponent<PlayerHealth>().TakeDamage(damage);

            Debug.Log("Kaya kafana geldi! Hasar: " + damage);
            Destroy(gameObject); // Kayayý yok et
        }
        // Yere veya duvara çarparsa (Ground katmaný)
        else if (hitInfo.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}