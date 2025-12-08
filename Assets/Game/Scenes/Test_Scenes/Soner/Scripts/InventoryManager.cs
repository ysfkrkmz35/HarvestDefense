using UnityEngine;
using UnityEngine.UI; // Text kullanmak için gerekli

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("UI Referansları")]
    public InventorySlot[] slots;
    public Text woodText; // Odun sayısını yazan UI Text
    public Text rockText; // Taş sayısını yazan UI Text

    [Header("Kaynak Sayıları")]
    public int woodCount = 0;
    public int rockCount = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Oyun başladığında sayaçları ekrana yazdır (0 olarak)
        UpdateResourceUI();
    }

    // Kaynak harcama fonksiyonu. Eğer yeterli kaynak varsa true döner ve azaltır.
    public bool SpendResource(ItemType type, int amount)
    {
        if (type == ItemType.Wood)
        {
            if (woodCount >= amount)
            {
                woodCount -= amount;
                UpdateResourceUI(); // UI'ı güncelle
                return true; // Harcama başarılı
            }
        }
        else if (type == ItemType.Rock)
        {
            if (rockCount >= amount)
            {
                rockCount -= amount;
                UpdateResourceUI();
                return true;
            }
        }
        return false; // Yetersiz bakiye
    }

    public bool AddItem(ItemData item)
    {
        // --- 1. DURUM: Eşya bir KAYNAK ise (Odun veya Taş) ---
        if (item.itemType == ItemType.Wood)
        {
            woodCount++; // Sayıyı artır
            UpdateResourceUI(); // Ekrana yaz
            return true; // İşlem başarılı, yerdeki objeyi yok et
        }
        else if (item.itemType == ItemType.Rock)
        {
            rockCount++;
            UpdateResourceUI();
            return true;
        }

        // --- 2. DURUM: Eşya NORMAL bir eşya ise (Slota girecek) ---
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].currentItem == null)
            {
                slots[i].AddItem(item);
                return true;
            }
        }
        
        Debug.Log("Envanter Dolu!");
        return false;
    }

    // UI Metinlerini güncelleyen yardımcı fonksiyon
    void UpdateResourceUI()
    {
        if(woodText != null) woodText.text = woodCount.ToString();
        if(rockText != null) rockText.text = rockCount.ToString();
    }
}