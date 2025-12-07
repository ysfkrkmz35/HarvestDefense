using UnityEngine;

public enum ItemType { Normal, Wood, Rock }

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemType itemType;
    
    [Header("Yere Atılacak Obje")]
    public GameObject prefab; // YENİ: Yere atıldığında oluşacak nesne
}