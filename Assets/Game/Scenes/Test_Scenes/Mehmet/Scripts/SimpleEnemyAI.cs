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
            // Player'a doğru hareket
            Vector2 direction = (playerPos - myPos).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
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
