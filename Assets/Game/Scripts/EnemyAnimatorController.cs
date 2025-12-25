using UnityEngine;

/// <summary>
/// Düşman Animasyon Kontrolcüsü
/// - Hareket durumuna göre Walk/Idle oynatır
/// - Saldırı anında Attack oynatır
/// - Hasar alınca Damage oynatır
/// - Ölünce Death oynatır
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimatorController : MonoBehaviour
{
    [Header("═══ REFERENCES ═══")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;

    [Header("═══ ANIMATION NAMES ═══")]
    [Tooltip("Animator'daki animasyon isimleri")]
    [SerializeField] private string idleAnim = "idle";
    [SerializeField] private string walkAnim = "walk";
    [SerializeField] private string attackAnim = "attack";
    [SerializeField] private string damageAnim = "demage"; // Typo olabilir, prefab'a göre ayarla
    [SerializeField] private string deathAnim = "death";

    [Header("═══ SETTINGS ═══")]
    [SerializeField] private float moveThreshold = 0.1f;
    [SerializeField] private bool flipSprite = true;

    // State tracking
    private bool isAttacking = false;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private Vector2 lastVelocity;

    private void Awake()
    {
        // Referansları otomatik bul
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (rb == null)
            rb = GetComponentInParent<Rigidbody2D>();
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (isDead || isAttacking) return;

        UpdateMovementAnimation();
        UpdateSpriteDirection();
    }

    /// <summary>
    /// Hareket durumuna göre Walk/Idle animasyonu oynat
    /// </summary>
    private void UpdateMovementAnimation()
    {
        if (rb == null || animator == null) return;

        float speed = rb.linearVelocity.magnitude;

        if (speed > moveThreshold)
        {
            PlayAnimation(walkAnim);
        }
        else
        {
            PlayAnimation(idleAnim);
        }

        lastVelocity = rb.linearVelocity;
    }

    /// <summary>
    /// Hareket yönüne göre sprite'ı çevir
    /// </summary>
    private void UpdateSpriteDirection()
    {
        if (!flipSprite || spriteRenderer == null || rb == null) return;

        if (Mathf.Abs(rb.linearVelocity.x) > moveThreshold)
        {
            // Sağa gidiyorsa normal, sola gidiyorsa flip
            spriteRenderer.flipX = rb.linearVelocity.x < 0;
        }
    }

    /// <summary>
    /// Saldırı animasyonu oynat
    /// </summary>
    public void PlayAttack()
    {
        if (isDead) return;

        isAttacking = true;
        PlayAnimation(attackAnim);
        
        // Animasyon bitince saldırı durumunu kapat
        Invoke(nameof(EndAttack), GetAnimationLength(attackAnim));
    }

    private void EndAttack()
    {
        isAttacking = false;
    }

    /// <summary>
    /// Hasar alma animasyonu oynat
    /// </summary>
    public void PlayDamage()
    {
        if (isDead) return;

        PlayAnimation(damageAnim);
    }

    /// <summary>
    /// Ölüm animasyonu oynat
    /// </summary>
    public void PlayDeath()
    {
        isDead = true;
        isAttacking = false;
        PlayAnimation(deathAnim);
    }

    /// <summary>
    /// Yeniden canlandığında çağır
    /// </summary>
    public void ResetAnimator()
    {
        isDead = false;
        isAttacking = false;
        PlayAnimation(idleAnim);
    }

    /// <summary>
    /// Animasyon oynat (güvenli)
    /// </summary>
    private void PlayAnimation(string animName)
    {
        if (animator == null || string.IsNullOrEmpty(animName)) return;

        // Aynı animasyon zaten oynuyorsa tekrar başlatma
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(animName)) return;

        animator.Play(animName);
    }

    /// <summary>
    /// Animasyon süresini al
    /// </summary>
    private float GetAnimationLength(string animName)
    {
        if (animator == null) return 0.5f;

        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        foreach (var clip in clips)
        {
            if (clip.clip.name.ToLower().Contains(animName.ToLower()))
            {
                return clip.clip.length;
            }
        }

        return 0.5f; // Default
    }

    /// <summary>
    /// Debug için mevcut durumu göster
    /// </summary>
    [ContextMenu("Debug: Print State")]
    private void DebugPrintState()
    {
        Debug.Log($"[EnemyAnimator] Dead: {isDead}, Attacking: {isAttacking}, Velocity: {rb?.linearVelocity}");
    }
}
