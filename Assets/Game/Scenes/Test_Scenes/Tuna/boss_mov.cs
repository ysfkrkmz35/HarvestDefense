using UnityEngine;
using System.Collections;

public class CorruptedBossAI : MonoBehaviour
{
    [Header("--- Hýz ve Menzil Ayarlarý ---")]
    public float patrolSpeed = 1f;        // Boþta gezinme hýzý (Yavaþ)
    public float chaseSpeed = 2.5f;       // Kovalama hýzý (Daha hýzlý)
    public float detectionRange = 8f;     // Oyuncuyu fark etme mesafesi
    public float attackRange = 3.5f;      // Saldýrý mesafesi

    [Header("--- Devriye Ayarlarý ---")]
    public float patrolRadius = 5f;       // Baþlangýç noktasýndan ne kadar uzaða gidebilir?
    public float patrolWaitTime = 2f;     // Bir noktaya varýnca kaç saniye beklesin?

    [Header("--- Saldýrý Ayarlarý ---")]
    public float attackCooldown = 3f;
    public GameObject rootSpikePrefab;
    public GameObject rockProjectilePrefab;
    public Transform firePoint;

    [Header("--- Referanslar ---")]
    public Transform player;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    // Durum Deðiþkenleri
    private Vector2 startPosition;        // Boss'un doðduðu yer (Burasý merkez olacak)
    private Vector2 patrolTarget;         // Rastgele gitmek istediði nokta
    private bool isPatrolling = false;    // Þu an devriye mi geziyor?
    private bool isWaiting = false;       // Hedefe vardý, bekliyor mu?
    private bool isAttacking = false;     // Saldýrý yapýyor mu?
    private float nextAttackTime = 0;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        // Baþlangýç noktasýný kaydet (Buradan çok uzaklaþmasýn)
        startPosition = transform.position;

        // Ýlk devriye noktasýný belirle
        SetNewPatrolTarget();

        // Fizik ayarlarý (Düþmemesi için)
        if (rb != null)
        {
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (player == null && GameObject.FindGameObjectWithTag("Player") != null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null) return;
        if (isAttacking) return; // Saldýrý yapýyorsa baþka hiçbir þey yapma

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // --- KARAR MEKANÝZMASI ---

        if (distanceToPlayer < detectionRange)
        {
            // 1. DURUM: OYUNCU MENZÝLDE (KOVALA VEYA SALDIR)
            HandleCombat(distanceToPlayer);
        }
        else
        {
            // 2. DURUM: OYUNCU UZAKTA (DEVRÝYE GEZ)
            HandlePatrol();
        }
    }

    // --- DEVRÝYE (PATROL) MANTIÐI ---
    void HandlePatrol()
    {
        if (isWaiting) return; // Bekleme süresindeyse hareket etme

        animator.SetBool("walk", true); // Yürüme animasyonunu aç

        // Hedefe doðru git
        transform.position = Vector2.MoveTowards(transform.position, patrolTarget, patrolSpeed * Time.deltaTime);

        // Yönünü hedefe çevir
        FaceTarget(patrolTarget);

        // Hedefe vardý mý?
        if (Vector2.Distance(transform.position, patrolTarget) < 0.2f)
        {
            StartCoroutine(WaitAtPatrolPoint());
        }
    }

    IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        animator.SetBool("walk", false); // Durma animasyonu

        yield return new WaitForSeconds(patrolWaitTime); // 2 saniye bekle

        SetNewPatrolTarget(); // Yeni rastgele nokta seç
        isWaiting = false;
    }

    void SetNewPatrolTarget()
    {
        // Baþlangýç noktasýnýn etrafýnda rastgele bir nokta seç
        Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
        patrolTarget = startPosition + randomPoint;
    }

    // --- SAVAÞ (COMBAT) MANTIÐI ---
    void HandleCombat(float dist)
    {
        // Oyuncuya dön
        FaceTarget(player.position);

        if (dist <= attackRange)
        {
            // Saldýrý menzilinde
            animator.SetBool("walk", false); // Dur

            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(PickRandomAttack());
            }
        }
        else
        {
            // Oyuncuyu kovala
            animator.SetBool("walk", true);
            transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);
        }
    }

    // Yön Çevirme (Hem oyuncuya hem devriye noktasýna bakabilmesi için genel yaptým)
    void FaceTarget(Vector2 targetPos)
    {
        if (targetPos.x > transform.position.x)
            spriteRenderer.flipX = false; // Saða bak
        else
            spriteRenderer.flipX = true;  // Sola bak
    }

    IEnumerator PickRandomAttack()
    {
        isAttacking = true;
        animator.SetBool("walk", false);

        int rand = Random.Range(0, 2);
        if (rand == 0) animator.SetTrigger("Attack1");
        else animator.SetTrigger("Attack2");

        yield return new WaitForSeconds(1.5f); // Animasyon süresi kadar bekle

        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    // --- ANIMATION EVENTS ---
    public void AE_SpawnRootSpikes()
    {
        // Ama iþi kendisi yapmayacak, iþçiyi (Coroutine) baþlatacak
        StartCoroutine(SpawnSpikeWave());
    }

    // 2. Asýl iþi yapan, zaman ayarlý fonksiyon
    IEnumerator SpawnSpikeWave()
    {
        // AYARLAR (Ýstersen buradaki sayýlarý deðiþtirebilirsin)
        int spikeCount = 4;           // Toplam kaç diken çýksýn? (3 blok dediðin için 4 yaptým, daha uzun gider)
        float distanceBetween = 1.2f; // Dikenler arasý mesafe ne kadar olsun?
        float timeBetween = 0.15f;    // Her diken arasý kaç saniye beklesin? (Düþükse hýzlý gider)

        // Boss'un ne tarafa baktýðýný bul (Sað = 1, Sol = -1)
        float direction = spriteRenderer.flipX ? -1f : 1f;

        for (int i = 0; i < spikeCount; i++)
        {
            if (rootSpikePrefab != null)
            {
                // MESAFE HESABI:
                // (i + 1) demek: 1. diken, 2. diken, 3. diken...
                // Bunu yön ve aralýkla çarpýyoruz.
                float currentDist = (i + 1) * distanceBetween;

                // Boss'un merkezinden hesaplanan pozisyon
                Vector3 spawnPos = transform.position + new Vector3(direction * currentDist, -0.5f, 0);

                // Dikeni oluþtur
                Instantiate(rootSpikePrefab, spawnPos, Quaternion.identity);
            }

            // Bir sonraki dikeni çýkarmadan önce bekle (Bu dalga efektini yaratýr)
            yield return new WaitForSeconds(timeBetween);
        }
    }

    public void AE_ThrowRock()
    {
        if (rockProjectilePrefab != null)
        {
            // Boss'un baktýðý yönü bul (Sað mý sol mu?)
            // Kayanýn boss'un tam içinden deðil, biraz önünden çýkmasý için:
            float direction = spriteRenderer.flipX ? -1f : 1f;
            Vector3 spawnPos = transform.position + new Vector3(direction * 1.5f, 0.5f, 0);
            // (Y eksenini 0.5f yaptým ki el hizasýndan çýksýn)

            Instantiate(rockProjectilePrefab, spawnPos, Quaternion.identity);
        }
    }

    // Editörde menzilleri görmek için (DEBUG)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange); // Görüþ alaný

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange); // Saldýrý alaný

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, patrolRadius); // Devriye alaný
    }
}