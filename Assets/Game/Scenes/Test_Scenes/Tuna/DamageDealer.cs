using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    public int damageAmount = 20; // Ne kadar can yakacaðý
    public float lifeTime = 1.5f; // Ekranda kaç saniye kalacaðý

    void Start()
    {
        // Belirlenen süre sonunda kökü yok et (Hafýza dolmasýn)
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Eðer çarpan þeyin etiketi "Player" ise
        if (other.CompareTag("Player"))
        {
            // Buraya oyuncunun can azaltma fonksiyonunu çaðýracaðýz
            // Örnek: other.GetComponent<PlayerHealth>().TakeDamage(damageAmount);

            Debug.Log("Oyuncuya Dikeni Battý! Hasar: " + damageAmount);
        }
    }
}