using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData item; // Bu nesne hangi eşyayı temsil ediyor?

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Çarpan obje "Player" etiketine sahip mi?
        if (other.CompareTag("Player"))
        {
            // Envantere eklemeyi dene
            bool wasPickedUp = InventoryManager.Instance.AddItem(item);

            // Eğer başarıyla eklendiyse yerdeki objeyi yok et
            if (wasPickedUp)
            {
                Destroy(gameObject);
            }
        }
    }
}