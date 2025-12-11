using UnityEngine;

public class IsoSpriteRenderer : MonoBehaviour
{
    [Header("Ayarlar")]
    // Sprite Renderer'ı buraya sürükle
    [SerializeField] private SpriteRenderer spriteRenderer;

    // 16 adet sprite'ı buraya sırayla sürükle (Kuzey'den başlayarak saat yönünde)
    [SerializeField] private Sprite[] sprites;

    [Tooltip("Eğer resimler yanlış yöne bakıyorsa bunu 90, -90 veya 180 yap")]
    [SerializeField] private float angleOffset = 0f;

    // 360 / 16 = 22.5 derece (Her bir dilimin açısı)
    private const float stepAngle = 22.5f;

    public void SetDirection(Vector2 direction)
    {
        if (sprites == null || sprites.Length == 0) return;

        // Yön vektörü (0,0) ise işlem yapma
        if (direction == Vector2.zero) return;

        // Vektörden açıyı bul (Unity'de 0 derece Sağ'dır)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Unity'nin matematiksel açısını (Counter-Clockwise), 
        // Sprite listemizin sırasına (Clockwise from Top) uydurmak için dönüşüm:
        // Bu formül: 90 dereceden çıkararak Kuzey referanslı saat yönüne çevirir.
        float finalAngle = 90f - angle + angleOffset;

        // Negatif açıları pozitife çevir (Örn: -90 -> 270)
        if (finalAngle < 0) finalAngle += 360f;

        // Açıyı dilimlere böl ve indexi bul
        int index = Mathf.RoundToInt(finalAngle / stepAngle);

        // Index 16 çıkarsa (360 derece), 0'a (başa) sar
        index = index % 16;

        // Güvenlik kontrolü ve atama
        if (index >= 0 && index < sprites.Length)
        {
            spriteRenderer.sprite = sprites[index];
        }
    }
}