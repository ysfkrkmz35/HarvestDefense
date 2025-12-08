using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class TopDownPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator anim;

    private Vector2 input;
    private Vector2 lastMoveDir = Vector2.down; // oyuna ba�larken a�a�� baks�n

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Top-down i�in yer�ekimi istemiyoruz
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // Klavye input
        input.x = Input.GetAxisRaw("Horizontal");  // A/D veya ? ?
        input.y = Input.GetAxisRaw("Vertical");    // W/S veya ? ?

        // K��egen h�z�n� sabitle
        if (input.sqrMagnitude > 1f)
            input = input.normalized;

        // Hareket ediyorsa en son bakt��� y�n� kaydet
        if (input.sqrMagnitude > 0.01f)
            lastMoveDir = input;

        // Animator parametreleri
        // Animator�da �u 3 parametreyi olu�turacaks�n:
        // float Horizontal, float Vertical, float Speed
        anim.SetFloat("Horizontal", lastMoveDir.x);
        anim.SetFloat("Vertical", lastMoveDir.y);
        anim.SetFloat("Speed", input.sqrMagnitude);
    }

    private void FixedUpdate()
    {
        // Fizik hareketi
        rb.linearVelocity = input * moveSpeed;
    }
}
