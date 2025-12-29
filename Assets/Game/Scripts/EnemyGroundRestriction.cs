using UnityEngine;

/// <summary>
/// Tüm Enemy tag'ine sahip düşmanların sadece Ground tile üzerinde hareket etmesini sağlar.
/// Water veya boşluğa gitmeyi engeller.
/// Bu script herhangi bir Enemy GameObject'e eklenebilir (Spider, Zombie, vb.)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyGroundRestriction : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Eğer hareket ediyorsa, hedef pozisyonu kontrol et
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            Vector2 targetPosition = rb.position + rb.linearVelocity * Time.fixedDeltaTime;

            // Hedef pozisyon Ground üzerinde değilse, hareketi engelle
            if (!IsPositionOnGround(targetPosition))
            {
                rb.linearVelocity = Vector2.zero;

                if (showDebugLogs)
                {
                    Debug.Log($"[EnemyGroundRestriction] {gameObject.name} Water'a girmeye çalıştı, hareketi engellendi!");
                }
            }
        }
    }

    /// <summary>
    /// Pozisyon Ground tile üzerinde mi kontrol et
    /// </summary>
    private bool IsPositionOnGround(Vector2 position)
    {
        if (HappyHarvest.GameManager.Instance?.Terrain != null)
        {
            var grid = HappyHarvest.GameManager.Instance.Terrain.Grid;
            var groundTilemap = HappyHarvest.GameManager.Instance.Terrain.GroundTilemap;

            if (grid != null && groundTilemap != null)
            {
                Vector3Int cellPosition = grid.WorldToCell(new Vector3(position.x, position.y, 0f));

                // Ground tile var mı?
                bool hasGround = groundTilemap.HasTile(cellPosition);

                if (showDebugLogs && !hasGround)
                {
                    Debug.Log($"[EnemyGroundRestriction] Position {position} has NO Ground tile!");
                }

                return hasGround;
            }
        }

        // Tilemap yoksa güvenli kabul et (eski davranış)
        return true;
    }
}
