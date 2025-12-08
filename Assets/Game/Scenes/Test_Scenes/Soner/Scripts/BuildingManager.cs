using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingManager : MonoBehaviour
{
    [Header("Ayarlar")]
    public float gridSize = 1.0f;
    public GameObject buildPanel;

    [Header("Prefablar")]
    public GameObject woodBuildPrefab;
    public GameObject rockBuildPrefab;

    private GameObject objectToPlace; // Seçili orijinal prefab
    private ItemType costType;
    
    // YENİ: Hayalet objeyi tutacak değişken
    private GameObject ghostObject; 

    void Update()
    {
        if (objectToPlace != null && ghostObject != null)
        {
            UpdateGhostPosition();
        }

        // --- CASUS KOD BAŞLANGICI ---
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("Tıklama engellendi! Mouse şu an bir UI elemanının (Buton/Panel/Text) üzerinde.");
            }
            else
            {
                Debug.Log("UI engeli yok, yerleştirme deneniyor...");
                if (objectToPlace != null) PlaceObject();
            }
        }
        // --- CASUS KOD BİTİŞİ ---

        // (Eski if bloğunu silebilir veya yorum satırına alabilirsin çünkü yukarıda yenisini yazdık)
        
        if (Input.GetMouseButtonDown(1))
        {
            CancelBuilding();
        }
    }

    // YENİ: Hayaletin pozisyonunu güncelleyen fonksiyon
    void UpdateGhostPosition()
    {
        // Mouse pozisyonunu grid'e yuvarla (PlaceObject'teki aynı mantık)
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        float gridX = Mathf.Round(mousePos.x / gridSize) * gridSize;
        float gridY = Mathf.Round(mousePos.y / gridSize) * gridSize;

        // Hayaleti o konuma taşı
        ghostObject.transform.position = new Vector3(gridX, gridY, 0);
    }

    void PlaceObject()
    {
        // Debug 1: Fonksiyon hiç çalışıyor mu?
        Debug.Log("1. Tıklama algılandı, yerleştirme deneniyor...");

        Vector3 spawnPos = ghostObject.transform.position;

        // Debug 2: Kaynak harcama denemesi
        Debug.Log($"2. Şu anki maliyet türü: {costType}. Harcama deneniyor...");

        if (InventoryManager.Instance.SpendResource(costType, 1)) 
        {
            Debug.Log("3. Kaynak başarıyla harcandı! Blok oluşturuluyor.");
            Instantiate(objectToPlace, spawnPos, Quaternion.identity);
        }
        else
        {
            // Eğer burası çalışıyorsa kaynağın yok demektir
            Debug.LogError("HATA: Yetersiz Kaynak! Envanterinde bu malzemeden yok.");
        }
    }

    // YENİ: İnşaat modunu iptal eden yardımcı fonksiyon
    void CancelBuilding()
    {
        objectToPlace = null;
        buildPanel.SetActive(false);
        
        // Varsa hayalet objeyi yok et
        if (ghostObject != null)
        {
            Destroy(ghostObject);
            ghostObject = null;
        }
    }

    // YENİ: Hayalet oluşturma fonksiyonu
    void CreateGhost(GameObject prefab)
    {
        if (ghostObject != null) Destroy(ghostObject);
        ghostObject = Instantiate(prefab);

        // Sadece ana objede değil, tüm çocuk objelerdeki colliderları da kapat
        Collider2D[] allColliders = ghostObject.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in allColliders)
        {
            col.enabled = false;
        }

        // Rengi ayarla
        SpriteRenderer sr = ghostObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color ghostColor = sr.color;
            ghostColor.a = 0.5f;
            sr.color = ghostColor;
        }
    }

    // UI Buton Fonksiyonları (Güncellendi)
    public void SelectWoodBuilding()
    {
        objectToPlace = woodBuildPrefab;
        costType = ItemType.Wood;
        CreateGhost(woodBuildPrefab); // YENİ: Hayaleti oluştur
    }

    public void SelectRockBuilding()
    {
        objectToPlace = rockBuildPrefab;
        costType = ItemType.Rock;
        CreateGhost(rockBuildPrefab); // YENİ: Hayaleti oluştur
    }

    public void TogglePanel()
    {
        buildPanel.SetActive(!buildPanel.activeSelf);
        // Eğer panel kapanıyorsa inşaat modunu da iptal et
        if (!buildPanel.activeSelf)
        {
            CancelBuilding();
        }
    }
}