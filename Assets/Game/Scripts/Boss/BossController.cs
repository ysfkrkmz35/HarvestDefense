using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BossHealth), typeof(Rigidbody2D), typeof(Animator))]
public class BossController : MonoBehaviour
{
    public enum BossState { Dormant, Chasing, Attacking, Stunned, Dead }
    public enum BossPhase { Normal, Enraged }

    [Header("Configuration")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float enragedSpeedMultiplier = 1.5f;
    [SerializeField] private float attackRange = 2f;
    
    [Header("Combat")]
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackWindup = 0.5f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private BossState currentState = BossState.Dormant;
    private BossPhase currentPhase = BossPhase.Normal;
    
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private BossHealth health;

    private float nextAttackTime;
    private bool isFacingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<BossHealth>();
        
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        health.OnDamageTaken += CheckPhaseTransition;
        health.OnDeath += HandleDeath;
    }

    private void Start()
    {
        // Initial State
        currentState = BossState.Dormant;
        rb.bodyType = RigidbodyType2D.Kinematic; // Don't move
    }

    private void Update()
    {
        if (currentState == BossState.Dormant || currentState == BossState.Dead) return;

        if (player == null)
        {
            FindPlayer();
            return;
        }

        switch (currentState)
        {
            case BossState.Chasing:
                HandleChase();
                break;
            case BossState.Attacking:
                // Handled by Coroutine
                break;
        }
    }

    public void WakeUp()
    {
        if (currentState != BossState.Dormant) return;

        Debug.Log("[BossController] 🔥 BOSS AWAKENING!");
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        // Spawn Effects
        if (spriteRenderer != null)
        {
            // Flash white briefly
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }

        // TODO: Camera shake (if CameraShake component exists)
        // var cameraShake = Camera.main.GetComponent<CameraShake>();
        // if (cameraShake != null) cameraShake.Shake(0.5f, 0.3f);

        // Activate
        currentState = BossState.Chasing;
        rb.bodyType = RigidbodyType2D.Dynamic;
        
        // Notify Health to show UI
        health.SetActive(true);
        FindPlayer();

        // Small delay before starting chase
        yield return new WaitForSeconds(0.3f);
        Debug.Log("[BossController] Boss is now chasing player!");
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void HandleChase()
    {
        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= attackRange)
        {
            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
            }
            else
            {
                rb.linearVelocity = Vector2.zero; // Wait for cooldown
            }
        }
        else
        {
            float speed = currentPhase == BossPhase.Normal ? moveSpeed : moveSpeed * enragedSpeedMultiplier;
            Vector2 dir = (player.position - transform.position).normalized;
            rb.linearVelocity = dir * speed;
            
            // Flip Sprite
            if (dir.x > 0 && !isFacingRight) Flip();
            else if (dir.x < 0 && isFacingRight) Flip();

            // Anim
            animator.SetBool("isMoving", true);
        }
    }

    private IEnumerator AttackRoutine()
    {
        currentState = BossState.Attacking;
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isMoving", false);

        // Windup
        float windup = currentPhase == BossPhase.Normal ? attackWindup : attackWindup * 0.7f;
        animator.SetTrigger("attack"); // Assumes parameter exists
        yield return new WaitForSeconds(windup);

        // Damage Check
        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange * 1.2f)
        {
            // Deal Damage
            var damageable = player.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
            }
        }

        // Cooldown
        float cooldown = currentPhase == BossPhase.Normal ? attackCooldown : attackCooldown * 0.7f;
        nextAttackTime = Time.time + cooldown;

        currentState = BossState.Chasing;
    }

    private void CheckPhaseTransition(float healthPercent)
    {
        if (currentPhase == BossPhase.Normal && healthPercent <= 0.5f)
        {
            EnterEnragedPhase();
        }
    }

    private void EnterEnragedPhase()
    {
        currentPhase = BossPhase.Enraged;
        Debug.Log("BOSS ENRAGED!");
        
        // Visuals
        spriteRenderer.color = Color.red;
        
        // Effects
        // StartCoroutine(Roar()); // Optional
    }

    private void HandleDeath()
    {
        currentState = BossState.Dead;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("die"); // Assumes parameter exists
        
        // Destroy or Disable logic handled by specific events
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
