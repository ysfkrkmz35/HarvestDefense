using UnityEngine;
using System;
using UnityEngine.Profiling;

/// <summary>
/// Düşman Yapay Zeka Sistemi (NavMesh Kullanmayan Versiyon)
/// Transform ile Base'e doğru hareket eder
/// Yolda Player veya Wall görürse saldırır
/// Engellerden kaçınır (Basit Raycast kontrolü)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("AI State")]
    public EnemyState currentState = EnemyState.MoveToBase;

    [Header("References")]
    private Transform baseTransform;
    private Transform currentTarget;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Attack Settings")]
    [SerializeField] private float detectionRange = 3f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 10;

    private float lastAttackTime;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleDetectionDistance = 1.5f;
    [SerializeField] private float avoidanceForce = 2f;
    [SerializeField] private LayerMask obstacleLayer; // Wall layer
    
    // Optimization: Reuse array to avoid allocations
    private readonly RaycastHit2D[] m_RaycastHits = new RaycastHit2D[1];

    [Header("Boundary Settings")]
    [SerializeField] private float maxDistanceFromCenter = 14f; // Ground yarıçapı (30/2 = 15, biraz içerde 14)
    [SerializeField] private float boundaryPushForce = 5f;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask wallLayer;

    public enum EnemyState
    {
        MoveToBase,
        AttackTarget,
        Dead
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Rigidbody2D ayarları (Top-Down 2D için)
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // SpriteRenderer bul (Top-Down 2D için flip)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        // Base objesini bul (Tag: "Base") - Cache it to avoid repeated FindGameObjectWithTag calls
        if (baseTransform == null)
        {
            TryFindBase();
        }

        if (baseTransform != null)
        {
            Debug.Log($"[EnemyAI] Base bulundu! Pozisyon: {baseTransform.position}, Mesafe: {Vector2.Distance(transform.position, baseTransform.position):F2}");
        }
        else
        {
            Debug.LogError("[EnemyAI] Base objesi bulunamadı! Scene'de 'Base' tag'ine sahip bir obje olmalı!");
        }

        // Layer maskları ayarla
        playerLayer = LayerMask.GetMask("Player");
        wallLayer = LayerMask.GetMask("Wall");
        obstacleLayer = wallLayer; // Engellerden kaçınmak için Wall layer'ı kullan

        Debug.Log($"[EnemyAI] Spawn pozisyonu: {transform.position}");

        // Başlangıçta Base'e doğru git
        ChangeState(EnemyState.MoveToBase);
    }

    private void Update()
    {
        if (currentState == EnemyState.Dead)
            return;

        switch (currentState)
        {
            case EnemyState.MoveToBase:
                HandleMoveToBase();
                break;

            case EnemyState.AttackTarget:
                HandleAttackTarget();
                break;
        }

        // Top-Down 2D için sprite yönlendirmesi
        UpdateSpriteDirection();
    }

    /// <summary>
    /// Hareket yönüne göre sprite'ı çevir (Top-Down 2D)
    /// </summary>
    private void UpdateSpriteDirection()
    {
        if (spriteRenderer == null || rb.linearVelocity.magnitude < 0.1f)
            return;

        // Sağa gidiyorsa sprite normal, sola gidiyorsa flip
        if (rb.linearVelocity.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (rb.linearVelocity.x < 0)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void HandleMoveToBase()
    {
        // Base'e doğru hareket et
        // Optimization: Only search for base if it's null, avoiding repeated FindGameObjectWithTag calls
        if (baseTransform == null)
        {
            // Base hala null ise tekrar bul
            TryFindBase();

            if (baseTransform == null)
            {
                Debug.LogWarning("[EnemyAI] Base hala bulunamadı!");
                return;
            }
        }

        // Base'e olan mesafe - Cache position to avoid multiple property accesses
        Vector3 basePosition = baseTransform.position;
        float distanceToBase = Vector2.Distance(transform.position, basePosition);

        // Base'e yeterince yakınsa saldır
        if (distanceToBase <= attackRange)
        {
            // Dur ve Base'e saldır
            rb.linearVelocity = Vector2.zero;

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                AttackBase();
                lastAttackTime = Time.time;
            }
        }
        else
        {
            // Base'e doğru git
            MoveTowards(basePosition);
        }
    }

    /// <summary>
    /// Base'i bulmaya çalış
    /// </summary>
    private void TryFindBase()
    {
        GameObject baseObject = GameObject.FindGameObjectWithTag("Base");
        if (baseObject != null)
        {
            baseTransform = baseObject.transform;
            Debug.Log($"[EnemyAI] Base bulundu! Pozisyon: {baseTransform.position}");
        }
    }

    /// <summary>
    /// Base'e saldır
    /// </summary>
    private void AttackBase()
    {
        if (baseTransform == null)
            return;

        // IDamageable interface'ini kontrol et
        IDamageable damageable = baseTransform.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(attackDamage);
            Debug.Log($"EnemyAI: Base'e {attackDamage} hasar verildi!");
        }
        else
        {
            Debug.LogWarning("EnemyAI: Base'de IDamageable component yok!");
        }
    }

    private void HandleAttackTarget()
    {
        // Bu state artık kullanılmıyor - her zaman Base'e git
        ChangeState(EnemyState.MoveToBase);
    }

    /// <summary>
    /// Belirli bir pozisyona doğru hareket et (Engellerden kaçınma ile)
    /// </summary>
    private void MoveTowards(Vector3 targetPosition)
    {
        // Hedef yönü hesapla - BU ÖNCELİKLİDİR!
        Vector2 direction = (targetPosition - transform.position).normalized;

#if UNITY_EDITOR
        Debug.DrawLine(transform.position, targetPosition, Color.green, 0.1f);
#endif

        // Engellerden kaçınma (sadece hafif düzeltme) - direction'ı parametre olarak gönder
        Vector2 avoidanceDirection = GetAvoidanceDirection(direction);
        if (avoidanceDirection != Vector2.zero)
        {
            direction = (direction + avoidanceDirection * avoidanceForce).normalized;
        }

        // Hareketi uygula
        rb.linearVelocity = direction * moveSpeed;

#if UNITY_EDITOR
        Debug.DrawRay(transform.position, direction * 2f, Color.yellow, 0.1f);
#endif
    }

    /// <summary>
    /// Ground sınırlarının dışına çıkmasını engelle
    /// </summary>
    private Vector2 GetBoundaryPush()
    {
        Vector2 pushDirection = Vector2.zero;

        // Merkeze olan uzaklık (Base'in pozisyonunu merkez olarak kullan)
        Vector2 centerPoint = baseTransform != null ? (Vector2)baseTransform.position : Vector2.zero;
        float distanceFromCenter = Vector2.Distance(transform.position, centerPoint);

        // SADECE sınırın dışına çıktıysa Base'e (merkeze) doğru GÜÇLÜ bir şekilde it
        if (distanceFromCenter > maxDistanceFromCenter)
        {
            // Base'e doğru güçlü itme
            pushDirection = (centerPoint - (Vector2)transform.position).normalized;
#if UNITY_EDITOR
            Debug.DrawLine(transform.position, centerPoint, Color.red);
#endif
        }

        return pushDirection;
    }

    /// <summary>
    /// Engel tespit et ve kaçınma yönünü hesapla
    /// </summary>
    private Vector2 GetAvoidanceDirection(Vector2 moveDirection)
    {
        Profiler.BeginSample("EnemyAI.GetAvoidanceDirection");
        Vector2 avoidance = Vector2.zero;

        // Hareket yönünde engel kontrolü (velocity değil, hedef yönü kullan!)
        Vector2[] directions = new Vector2[]
        {
            moveDirection,                                           // İleri (Base'e doğru)
            Quaternion.Euler(0, 0, 45) * moveDirection,             // Sağ çapraz
            Quaternion.Euler(0, 0, -45) * moveDirection             // Sol çapraz
        };

        foreach (Vector2 dir in directions)
        {
            // Optimization: Use non-allocating raycast version
            int hitCount = Physics2D.RaycastNonAlloc(transform.position, dir, m_RaycastHits, obstacleDetectionDistance, obstacleLayer);

            if (hitCount > 0 && m_RaycastHits[0].collider != null)
            {
                // Engelden kaçınma yönü hesapla (engelden uzaklaş)
                Vector2 awayFromObstacle = ((Vector2)transform.position - m_RaycastHits[0].point).normalized;
                avoidance += awayFromObstacle;
                
#if UNITY_EDITOR
                // Debug için ray çiz (only in editor)
                Debug.DrawRay(transform.position, dir * obstacleDetectionDistance, Color.red);
#endif
            }
#if UNITY_EDITOR
            else
            {
                // Debug için ray çiz (only in editor)
                Debug.DrawRay(transform.position, dir * obstacleDetectionDistance, Color.cyan);
            }
#endif
        }

        Profiler.EndSample();
        return avoidance.normalized;
    }


    private void ChangeState(EnemyState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case EnemyState.MoveToBase:
                currentTarget = null;
                break;

            case EnemyState.AttackTarget:
                break;

            case EnemyState.Dead:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    /// <summary>
    /// Düşman öldüğünde çağrılacak (Health script tarafından)
    /// </summary>
    public void Die()
    {
        ChangeState(EnemyState.Dead);

        // Pooling için SetActive(false) yapılacak
        // Şimdilik sadece state değiştir
        Debug.Log("EnemyAI: Düşman öldü.");
    }

    /// <summary>
    /// Düşmanı yeniden aktif hale getir (Pooling için)
    /// </summary>
    public void Respawn()
    {
        ChangeState(EnemyState.MoveToBase);
    }

    // Gizmos ile menzilleri görselleştir
    private void OnDrawGizmosSelected()
    {
        // Algılama menzili (Sarı)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Saldırı menzili (Kırmızı)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Engel algılama mesafesi (Mavi)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, obstacleDetectionDistance);

        // Ground sınırı (Cyan - Base'den)
        Gizmos.color = Color.cyan;
        Vector3 centerPoint = baseTransform != null ? baseTransform.position : Vector3.zero;
        Gizmos.DrawWireSphere(centerPoint, maxDistanceFromCenter);

        // Base'e doğru çizgi (Yeşil)
        if (baseTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, baseTransform.position);
        }
    }
}
