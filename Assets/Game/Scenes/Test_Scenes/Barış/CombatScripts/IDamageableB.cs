using UnityEngine;

public interface IDamageableB
{
    // Hasar alma fonksiyonu
    void TakeDamage(int amount);

    // Opsiyonel: Eğer mermi çarpınca efekt çıkacaksa pozisyon gerekebilir
    Transform transform { get; }
}