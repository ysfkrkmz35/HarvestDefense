using UnityEngine;

/// <summary>
/// Hasar alabilen tüm objeler için ortak interface
/// Barış ve Mehmet tarafından kullanılacak
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Hasar alma fonksiyonu
    /// </summary>
    /// <param name="amount">Hasar miktarı</param>
    void TakeDamage(int amount);
}
