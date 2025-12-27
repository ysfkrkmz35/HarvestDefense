using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Spell Base Abstract Class
/// - Base class for all spell implementations
/// - Handles cooldown, mana consumption, events
/// - Subclasses implement specific Cast behavior
/// </summary>
public abstract class SpellBase : MonoBehaviour
{
    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ SPELL DATA ═══")]
    [Tooltip("ScriptableObject defining spell properties")]
    [SerializeField] protected SpellData spellData;

    [Header("═══ REFERENCES ═══")]
    [Tooltip("Reference to player's ProHealthManaUI for mana consumption")]
    [SerializeField] protected ProHealthManaUI manaUI;

    [Tooltip("Reference to player animator (auto-found if not set)")]
    [SerializeField] protected Animator playerAnimator;

    [Header("═══ DEBUG ═══")]
    [SerializeField] protected bool showDebugLogs = true;

    #endregion

    #region ═══════ RUNTIME STATE ═══════

    protected float lastCastTime = -999f;
    protected Camera mainCamera;
    protected AudioSource audioSource;
    protected Transform playerTransform; // The actual player for position reference

    #endregion

    #region ═══════ PROPERTIES ═══════

    /// <summary>Get the spell data</summary>
    public SpellData Data => spellData;

    /// <summary>Is spell currently on cooldown?</summary>
    public bool IsOnCooldown => spellData != null && Time.time < lastCastTime + spellData.cooldown;

    /// <summary>Is spell ready to cast?</summary>
    public bool IsReady => !IsOnCooldown && HasEnoughMana;

    /// <summary>Does player have enough mana?</summary>
    public bool HasEnoughMana => manaUI == null || manaUI.HasEnoughMana(spellData?.manaCost ?? 0);

    /// <summary>Remaining cooldown time</summary>
    public float RemainingCooldown
    {
        get
        {
            if (spellData == null || !IsOnCooldown) return 0f;
            return (lastCastTime + spellData.cooldown) - Time.time;
        }
    }

    /// <summary>Cooldown progress (0-1, 1 = ready)</summary>
    public float CooldownProgress
    {
        get
        {
            if (spellData == null || spellData.cooldown <= 0) return 1f;
            if (!IsOnCooldown) return 1f;
            return 1f - (RemainingCooldown / spellData.cooldown);
        }
    }

    #endregion

    #region ═══════ EVENTS ═══════

    /// <summary>Fired when spell is cast</summary>
    public event Action OnSpellCast;

    /// <summary>Fired when cooldown changes. Parameter: remaining time</summary>
    public event Action<float> OnCooldownChanged;

    /// <summary>Fired when enemies are hit. Parameter: hit count</summary>
    public event Action<int> OnEnemiesHit;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    protected virtual void Awake()
    {
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null && spellData != null &&
            (spellData.castSound != null || spellData.impactSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    protected virtual void Start()
    {
        // Auto-find mana UI if not assigned
        if (manaUI == null)
        {
            manaUI = FindFirstObjectByType<ProHealthManaUI>();
        }

        // Find the player for position reference
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;

            // Auto-find player animator if not assigned
            if (playerAnimator == null)
            {
                playerAnimator = player.GetComponent<Animator>();
                if (playerAnimator == null)
                {
                    playerAnimator = player.GetComponentInChildren<Animator>();
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"[SpellBase] ✅ Found player: {player.name}");
            }
        }
        else
        {
            Debug.LogWarning("[SpellBase] ⚠️ No GameObject with 'Player' tag found! Spell position may be wrong.");
        }
    }

    protected virtual void Update()
    {
        // Broadcast cooldown changes for UI
        if (IsOnCooldown)
        {
            OnCooldownChanged?.Invoke(RemainingCooldown);
        }
    }

    #endregion

    #region ═══════ PUBLIC API ═══════

    /// <summary>
    /// Attempt to cast the spell at the given target position.
    /// Checks cooldown and mana before casting.
    /// </summary>
    /// <param name="targetPosition">World position to cast at</param>
    /// <returns>True if spell was cast, false otherwise</returns>
    public bool TryCast(Vector2 targetPosition)
    {
        if (spellData == null)
        {
            if (showDebugLogs) Debug.LogWarning("[SpellBase] No SpellData assigned!");
            return false;
        }

        // Check cooldown
        if (IsOnCooldown)
        {
            if (showDebugLogs) Debug.Log($"[{spellData.spellName}] ⏳ On cooldown: {RemainingCooldown:F1}s remaining");
            return false;
        }

        // Check mana
        if (!HasEnoughMana)
        {
            if (showDebugLogs) Debug.Log($"[{spellData.spellName}] ❌ Not enough mana! Need {spellData.manaCost}");
            return false;
        }

        // Execute cast
        ExecuteCast(targetPosition);
        return true;
    }

    /// <summary>
    /// Attempt to cast at mouse position
    /// </summary>
    public bool TryCastAtMouse()
    {
        Vector2 targetPos = GetMouseWorldPosition();
        return TryCast(targetPos);
    }

    /// <summary>
    /// Force reset cooldown (for testing or power-ups)
    /// </summary>
    public void ResetCooldown()
    {
        lastCastTime = -999f;
    }

    /// <summary>
    /// Set spell data at runtime
    /// </summary>
    public void SetSpellData(SpellData data)
    {
        spellData = data;
    }

    #endregion

    #region ═══════ CASTING LOGIC ═══════

    protected virtual void ExecuteCast(Vector2 targetPosition)
    {
        // Get player position (use player transform, fallback to this transform)
        Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : (Vector2)transform.position;
        
        // Clamp to max range from player
        float distance = Vector2.Distance(playerPos, targetPosition);
        if (distance > spellData.maxRange)
        {
            Vector2 direction = (targetPosition - playerPos).normalized;
            targetPosition = playerPos + direction * spellData.maxRange;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[{spellData.spellName}] 🎯 Player at {playerPos}, Target at {targetPosition}");
        }

        // Start cooldown
        lastCastTime = Time.time;

        // Consume mana
        if (manaUI != null && spellData.manaCost > 0)
        {
            manaUI.UseMana(spellData.manaCost);
        }

        // Play cast sound
        PlaySound(spellData.castSound);

        // Trigger player cast animation
        TriggerCastAnimation();

        // Trigger event
        OnSpellCast?.Invoke();

        if (showDebugLogs)
        {
            Debug.Log($"[{spellData.spellName}] 🔥 Casting at {targetPosition}");
        }

        // If there's an animation delay, wait before casting
        if (spellData.castAnimationDelay > 0)
        {
            StartCoroutine(DelayedCast(targetPosition));
        }
        else
        {
            // Call abstract implementation immediately
            Cast(targetPosition);
        }

        // Screen shake
        if (spellData.enableScreenShake)
        {
            StartCoroutine(ScreenShake());
        }
    }

    /// <summary>
    /// Trigger the player's cast animation for this spell
    /// </summary>
    protected virtual void TriggerCastAnimation()
    {
        if (playerAnimator == null) return;
        if (spellData == null || string.IsNullOrEmpty(spellData.castAnimationTrigger)) return;

        playerAnimator.SetTrigger(spellData.castAnimationTrigger);

        if (showDebugLogs)
        {
            Debug.Log($"[{spellData.spellName}] 🎬 Triggered animation: {spellData.castAnimationTrigger}");
        }
    }

    /// <summary>
    /// Delayed cast for animation sync
    /// </summary>
    protected IEnumerator DelayedCast(Vector2 targetPosition)
    {
        yield return new WaitForSeconds(spellData.castAnimationDelay);
        Cast(targetPosition);
    }

    /// <summary>
    /// Abstract method - implement spell-specific behavior
    /// </summary>
    protected abstract void Cast(Vector2 targetPosition);

    #endregion

    #region ═══════ DAMAGE HELPERS ═══════

    [Header("═══ ENEMY TARGETING ═══")]
    [Tooltip("Tag used to identify enemies")]
    [SerializeField] protected string enemyTag = "Enemy";

    /// <summary>
    /// Deal damage to all enemies in radius (tag-based detection like SwordCombat)
    /// </summary>
    protected int DealDamageInRadius(Vector2 center, float radius, float damage, LayerMask layers, bool damageDropoff = true)
    {
        // Get ALL colliders in radius (ignore layer mask for detection, use tag instead)
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, radius);
        int hitCount = 0;

        if (showDebugLogs)
        {
            Debug.Log($"[SpellBase] 🔍 Found {hitColliders.Length} colliders in radius {radius}");
        }

        foreach (Collider2D col in hitColliders)
        {
            // Skip player
            if (playerTransform != null && (col.transform == playerTransform || col.transform.IsChildOf(playerTransform)))
                continue;

            // Check for Enemy tag in hierarchy (like SwordCombat does)
            Transform targetTransform = col.transform;
            bool hasEnemyTag = CheckEnemyTag(col.transform, ref targetTransform);

            if (!hasEnemyTag)
            {
                continue;
            }

            // Find IDamageable in hierarchy
            IDamageable damageable = FindIDamageable(col.transform);
            if (damageable == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"[SpellBase] ⚠️ {targetTransform.name} has Enemy tag but no IDamageable!");
                continue;
            }

            // Calculate damage
            float finalDamage = damage;
            if (damageDropoff)
            {
                float dist = Vector2.Distance(center, targetTransform.position);
                float multiplier = 1f - (dist / radius);
                finalDamage = damage * Mathf.Clamp01(multiplier);
            }

            // Deal damage
            damageable.TakeDamage(Mathf.RoundToInt(finalDamage));
            hitCount++;

            if (showDebugLogs)
            {
                Debug.Log($"[SpellBase] ⚔️ HIT {targetTransform.name} for {Mathf.RoundToInt(finalDamage)} damage!");
            }
        }

        if (hitCount > 0)
        {
            OnEnemiesHit?.Invoke(hitCount);
        }

        return hitCount;
    }

    /// <summary>
    /// Check if collider or any parent has Enemy tag
    /// </summary>
    private bool CheckEnemyTag(Transform startTransform, ref Transform targetTransform)
    {
        // Check self
        if (startTransform.CompareTag(enemyTag))
        {
            targetTransform = startTransform;
            return true;
        }

        // Check parents
        Transform parent = startTransform.parent;
        while (parent != null)
        {
            if (parent.CompareTag(enemyTag))
            {
                targetTransform = parent;
                return true;
            }
            parent = parent.parent;
        }

        // Check root
        Transform root = startTransform.root;
        if (root != startTransform && root.CompareTag(enemyTag))
        {
            targetTransform = root;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Find IDamageable in entire hierarchy
    /// </summary>
    private IDamageable FindIDamageable(Transform startTransform)
    {
        // Self
        IDamageable damageable = startTransform.GetComponent<IDamageable>();
        if (damageable != null) return damageable;

        // Parents
        damageable = startTransform.GetComponentInParent<IDamageable>();
        if (damageable != null) return damageable;

        // Children
        damageable = startTransform.GetComponentInChildren<IDamageable>();
        if (damageable != null) return damageable;

        // Root and its children
        Transform root = startTransform.root;
        if (root != startTransform)
        {
            damageable = root.GetComponent<IDamageable>();
            if (damageable != null) return damageable;

            damageable = root.GetComponentInChildren<IDamageable>();
            if (damageable != null) return damageable;
        }

        return null;
    }

    /// <summary>
    /// Deal damage to single target
    /// </summary>
    protected bool DealDamageToTarget(GameObject target, float damage)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(Mathf.RoundToInt(damage));
            OnEnemiesHit?.Invoke(1);
            return true;
        }

        Health health = target.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(Mathf.RoundToInt(damage));
            OnEnemiesHit?.Invoke(1);
            return true;
        }

        return false;
    }

    #endregion

    #region ═══════ UTILITY METHODS ═══════

    protected Vector2 GetMouseWorldPosition()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return transform.position;

        return mainCamera.ScreenToWorldPoint(Input.mousePosition);
    }

    protected void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, spellData?.soundVolume ?? 0.7f);
        }
    }

    protected void PlaySoundAtPosition(AudioClip clip, Vector2 position)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, spellData?.soundVolume ?? 0.7f);
        }
    }

    protected void SpawnEffect(Vector2 position)
    {
        if (spellData?.effectPrefab != null)
        {
            GameObject effect = Instantiate(spellData.effectPrefab, position, Quaternion.identity);
            Destroy(effect, spellData.effectDuration);
        }
    }

    protected IEnumerator ScreenShake()
    {
        if (mainCamera == null) yield break;

        Vector3 originalPos = mainCamera.transform.position;
        float elapsed = 0f;
        float duration = spellData?.shakeDuration ?? 0.2f;
        float intensity = spellData?.shakeIntensity ?? 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = UnityEngine.Random.Range(-1f, 1f) * intensity;
            float y = UnityEngine.Random.Range(-1f, 1f) * intensity;
            mainCamera.transform.position = originalPos + new Vector3(x, y, 0);
            yield return null;
        }

        mainCamera.transform.position = originalPos;
    }

    #endregion

    #region ═══════ GIZMOS ═══════

    protected virtual void OnDrawGizmosSelected()
    {
        if (spellData == null) return;

        // Draw max range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spellData.maxRange);

        // Draw area radius if applicable
        if (spellData.spellType == SpellType.Area)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            // Show at mouse position in play mode
            if (Application.isPlaying && mainCamera != null)
            {
                Vector2 mousePos = GetMouseWorldPosition();
                Gizmos.DrawWireSphere(mousePos, spellData.areaRadius);
            }
        }
    }

    #endregion
}
