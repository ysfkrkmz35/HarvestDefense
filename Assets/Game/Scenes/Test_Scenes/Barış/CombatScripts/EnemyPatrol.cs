using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float moveDistance = 5f; // Ne kadar uzağa gidip dönsün?

    private Rigidbody2D rb;
    private Vector2 startPos;
    private bool movingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }

    private void FixedUpdate()
    {
        // Basit Ping-Pong mantığı
        // Başlangıç noktasından X ekseninde +moveDistance kadar git, sonra geri dön.

        float distCovered = transform.position.x - startPos.x;

        if (movingRight)
        {
            if (distCovered >= moveDistance)
            {
                movingRight = false; // Yön değiştir
            }
            rb.linearVelocity = new Vector2(speed, 0); // Sağa git (Açı: 0)
        }
        else
        {
            if (distCovered <= -moveDistance) // İsteğe bağlı: sol tarafa da gitsin
            {
                movingRight = true; // Yön değiştir
            }
            // Başlangıç noktasına geri döndüyse veya solu geçtiyse
            if (distCovered <= 0) movingRight = true;

            rb.linearVelocity = new Vector2(-speed, 0); // Sola git (Açı: 180)
        }
    }
}