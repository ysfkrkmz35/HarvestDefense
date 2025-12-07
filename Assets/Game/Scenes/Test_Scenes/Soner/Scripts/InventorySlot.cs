using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Bu kütüphane mouse olayları için şart

// IPointerEnter ve Exit, mouse'un üzerine gelip gittiğini algılar
public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconImage;
    public ItemData currentItem;

    private bool isHovered = false; // Mouse üzerinde mi?

    private void Update()
    {
        // Eğer mouse üzerindeyse VE içinde eşya varsa VE G tuşuna basıldıysa
        if (isHovered && currentItem != null && Input.GetKeyDown(KeyCode.G))
        {
            DropItem();
        }
    }

    public void DropItem()
{
    GameObject player = GameObject.FindGameObjectWithTag("Player");

    if (player != null && currentItem.prefab != null)
    {
        // --- DEĞİŞİKLİK BURADA ---
        
        // 1. Oyuncunun merkezini al
        Vector2 playerPos = player.transform.position;

        // 2. Rastgele bir yön belirle (Sağ, sol, yukarı, aşağı vs.)
        // .normalized diyerek bu yönün uzunluğunu 1 birime sabitliyoruz
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        // 3. Fırlatma Mesafesi (Bunu artırırsan daha uzağa düşer)
        float throwDistance = 1.5f; 

        // 4. Yeni pozisyonu hesapla: Oyuncu Yeri + (Yön * Mesafe)
        Vector2 dropPosition = playerPos + (randomDirection * throwDistance);

        // -------------------------

        // Eşyayı o noktada oluştur
        Instantiate(currentItem.prefab, dropPosition, Quaternion.identity);

        // Slotu temizle
        ClearSlot();
    }
}

    public void AddItem(ItemData newItem)
    {
        currentItem = newItem;
        iconImage.sprite = newItem.icon;
        iconImage.enabled = true;
    }

    public void ClearSlot()
    {
        currentItem = null;
        iconImage.sprite = null;
        iconImage.enabled = false;
    }

    // Mouse üzerine gelince çalışır
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    // Mouse üzerinden gidince çalışır
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}