using UnityEngine;

/// <summary>
/// Ultra basit düşman AI - Sadece player'a yürü ve saldır
/// Duvar çarpışması Unity fizik motoru tarafından otomatik yapılır
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SimpleEnemyAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.2f; // Collider temas mesafesi (0.4 + 0.5 + tolerans)
    [SerializeField] private float attackCooldown = 0.5f; // Daha hızlı saldırı
    [SerializeField] private int attackDamage = 10;

    private Transform player;
    private Rigidbody2D rb;
    private float nextAttackTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("[SimpleEnemyAI] Rigidbody2D bulunamadı!");
            return;
        }

        // Fizik ayarları - Optimize edilmiş hareket için
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0; // Top-down oyun
        rb.linearDamping = 0; // Sürtünme yok, direkt kontrol
        rb.angularDamping = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Dönmesin
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Duvarlardan geçmesin
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth hareket
    }

    void LateUpdate()
    {
        // Ekstra güvenlik: Her frame rotasyonu sıfırla
        // Eğer herhangi bir şey (Animator, fizik충돌 vb.) rotasyonu değiştirirse, düzelt
        if (transform.eulerAngles != Vector3.zero)
        {
            transform.rotation = Quaternion.identity;
        }

        // Rigidbody rotasyonunu da sıfırla
        if (rb != null && rb.rotation != 0f)
        {
            rb.rotation = 0f;
        }
    }

    void Start()
    {
        FindPlayer();
    }

    void FixedUpdate()
    {
        // Player kontrolü
        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        // Pozisyonları al
        Vector2 playerPos = new Vector2(player.position.x, player.position.y);
        Vector2 myPos = new Vector2(transform.position.x, transform.position.y);

        // ═══ MEVCUT POZİSYON KONTROLÜ ═══
        // Eğer şu anda Water üzerindeyse, en yakın Ground'a geri dön
        if (!IsPositionOnGround(myPos))
        {
            Debug.LogWarning($"[SimpleEnemyAI] {gameObject.name} Water üzerinde! En yakın Ground'a dönüyor...");
            Vector2? nearestGround = FindNearestGroundPosition(myPos, 10f);
            if (nearestGround.HasValue)
            {
                transform.position = nearestGround.Value;
            }
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Mesafe hesapla
        float distance = Vector2.Distance(myPos, playerPos);

        // Saldırı menzilinde mi?
        if (distance <= attackRange)
        {
            // Dur ve saldır
            rb.linearVelocity = Vector2.zero;
            TryAttack();
        }
        else
        {
            // Player'a doğru hareket (Ground tile kontrolü ile)
            Vector2 direction = (playerPos - myPos).normalized;
            Vector2 targetVelocity = direction * moveSpeed;

            // ═══ HEDEF POZİSYON KONTROLÜ (PlayerController ile AYNI) ═══
            Vector2 targetPosition = myPos + targetVelocity * Time.fixedDeltaTime;

            // Hedef pozisyonda Ground tile var mı kontrol et
            if (HappyHarvest.GameManager.Instance?.Terrain != null)
            {
                var grid = HappyHarvest.GameManager.Instance.Terrain.Grid;
                var groundTilemap = HappyHarvest.GameManager.Instance.Terrain.GroundTilemap;

                if (grid != null && groundTilemap != null)
                {
                    // Hedef cell'de Ground tile var mı?
                    Vector3Int targetCell = grid.WorldToCell(targetPosition);
                    bool hasGroundTile = groundTilemap.HasTile(targetCell);

                    // Ground tile yoksa hareket etme (PlayerController ile aynı)
                    if (!hasGroundTile)
                    {
                        rb.linearVelocity = Vector2.zero;
                        return; // Hareketi iptal et
                    }
                }
            }

            // Ground üzerinde, hareket edebilir
            rb.linearVelocity = targetVelocity;
        }
    }

    /// <summary>
    /// Pozisyon Ground tile üzerinde mi kontrol et
    /// </summary>
    bool IsPositionOnGround(Vector2 position)
    {
        if (HappyHarvest.GameManager.Instance?.Terrain != null)
        {
            var grid = HappyHarvest.GameManager.Instance.Terrain.Grid;
            var groundTilemap = HappyHarvest.GameManager.Instance.Terrain.GroundTilemap;

            if (grid != null && groundTilemap != null)
            {
                Vector3Int cellPosition = grid.WorldToCell(new Vector3(position.x, position.y, 0f));

                // Ground tile var mı?
                return groundTilemap.HasTile(cellPosition);
            }
        }

        // Tilemap yoksa güvenli kabul et (eski davranış)
        return true;
    }

    /// <summary>
    /// En yakın Ground pozisyonunu bul (spiral search)
    /// </summary>
    Vector2? FindNearestGroundPosition(Vector2 currentPosition, float maxSearchDistance)
    {
        if (HappyHarvest.GameManager.Instance?.Terrain == null) return null;

        var grid = HappyHarvest.GameManager.Instance.Terrain.Grid;
        var groundTilemap = HappyHarvest.GameManager.Instance.Terrain.GroundTilemap;

        if (grid == null || groundTilemap == null) return null;

        Vector3Int startCell = grid.WorldToCell(new Vector3(currentPosition.x, currentPosition.y, 0f));
        int maxRadius = Mathf.CeilToInt(maxSearchDistance / grid.cellSize.x);

        // Spiral search - yakından uzağa
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Sadece kenarları kontrol et
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        continue;

                    Vector3Int checkCell = startCell + new Vector3Int(x, y, 0);

                    if (groundTilemap.HasTile(checkCell))
                    {
                        Vector3 groundWorldPos = grid.GetCellCenterWorld(checkCell);
                        return new Vector2(groundWorldPos.x, groundWorldPos.y);
                    }
                }
            }
        }

        return null;
    }

    void TryAttack()
    {
        if (Time.time >= nextAttackTime)
        {
            // Player'a hasar ver
            IDamageable damageable = player.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
                Debug.Log($"[SimpleEnemyAI] ⚔️ Player'a {attackDamage} hasar verildi!");
            }
            else
            {
                Debug.LogError($"[SimpleEnemyAI] ❌ Player'da IDamageable component yok! Player: {player.name}");
            }

            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void FindPlayer()
    {
        // Önce tag ile ara
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"[SimpleEnemyAI] ✅ Player BULUNDU: {player.name} at {player.position}");
        }
        else
        {
            // Tag ile bulamadıysa isimle ara
            playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.LogWarning($"[SimpleEnemyAI] ⚠️ Player tag ile değil isimle bulundu! Tag ekle: {player.name}");
            }
            else
            {
                Debug.LogError("[SimpleEnemyAI] ❌ PLAYER BULUNAMADI! 'Player' tag'i veya ismi yok!");
            }
        }
    }

    /// <summary>
    /// Düşman öldüğünde çağrılır (EnemyHealth tarafından)
    /// </summary>
    public void Die()
    {
        rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false); // Object pooling için
    }

    /// <summary>
    /// Düşman yeniden spawn olduğunda çağrılır (EnemySpawner tarafından)
    /// </summary>
    public void Respawn(Vector3 position)
    {
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
        nextAttackTime = 0f;
        gameObject.SetActive(true);
        FindPlayer();
    }

    // Debug için menzil gösterimi
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
