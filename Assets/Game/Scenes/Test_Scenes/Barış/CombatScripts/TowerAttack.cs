using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float range = 6f;
    [SerializeField] private float fireRate = 1.5f;

    [Header("References")]
    [SerializeField] private IsoSpriteRenderer isoRenderer; // Yukarıdaki script
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint; // Okun çıkacağı nokta (Kulenin ortası olabilir)
    [SerializeField] private LayerMask enemyLayer;

    private float nextFireTime;
    private Transform currentTarget;

    private void Update()
    {
        FindTarget();

        if (currentTarget != null)
        {
            // Düşmana doğru olan vektör
            Vector2 direction = (currentTarget.position - transform.position).normalized;

            // 1. Kule görselini düşmana çevir (Sprite değiştirerek)
            isoRenderer.SetDirection(direction);

            // 2. Ateş et
            if (Time.time >= nextFireTime)
            {
                Shoot(direction);
                nextFireTime = Time.time + 1f / fireRate;
            }
        }
    }

    private void Shoot(Vector2 dir)
    {
        // Mermiyi oluştur (Rotasyon identity çünkü mermi de sprite değiştirerek dönecek)
        GameObject bolt = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        // Merminin scriptine yönü ver
        BoltProjectile boltScript = bolt.GetComponent<BoltProjectile>();
        boltScript.Setup(dir, gameObject);
    }

    private void FindTarget()
    {
        // Basit en yakın hedef bulma
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyLayer);
        float minDist = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (var hit in hits)
        {
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                bestTarget = hit.transform;
            }
        }
        currentTarget = bestTarget;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}