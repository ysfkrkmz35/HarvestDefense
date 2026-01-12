using UnityEngine;
using System.Collections;

/// <summary>
/// Self-Heal Spell Implementation
/// - Heals the player who casts it
/// - Ignores target position (always heals self)
/// - Spawns visual healing effect at player position
/// </summary>
public class HealSpell : SpellBase
{
    [Header("═══ HEAL SPELL SETTINGS ═══")]
    [Tooltip("Visual effect spawned on the player during heal")]
    [SerializeField] private GameObject healEffectPrefab;

    [Tooltip("Duration of the heal visual effect")]
    [SerializeField] private float healEffectDuration = 2f;

    // Cached references
    private PlayerHealth playerHealth;
    private ProHealthManaUI healthUI;

    protected override void Start()
    {
        base.Start();

        // Find player health components
        if (playerTransform != null)
        {
            playerHealth = playerTransform.GetComponent<PlayerHealth>();
        }

        // Also find ProHealthManaUI for direct healing
        healthUI = FindFirstObjectByType<ProHealthManaUI>();

        if (showDebugLogs)
        {
            Debug.Log($"[HealSpell] PlayerHealth: {(playerHealth != null ? "✅" : "❌")}, HealthUI: {(healthUI != null ? "✅" : "❌")}");
        }
    }

    /// <summary>
    /// Cast the heal spell (target position is ignored - always heals self)
    /// </summary>
    protected override void Cast(Vector2 targetPosition)
    {
        // Heal the player
        float healAmount = spellData != null ? spellData.healAmount : 25f;
        bool healed = HealPlayer(healAmount);

        if (healed)
        {
            // Spawn heal VFX at player position
            SpawnHealEffect();

            // Play impact/heal sound
            PlaySound(spellData?.impactSound);

            if (showDebugLogs)
            {
                Debug.Log($"[HealSpell] 💚 Healed player for {healAmount} HP!");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[HealSpell] ⚠️ Could not heal - no PlayerHealth or ProHealthManaUI found!");
            }
        }
    }

    /// <summary>
    /// Heal the player using available health systems
    /// </summary>
    private bool HealPlayer(float amount)
    {
        // Try PlayerHealth first (preferred)
        if (playerHealth != null)
        {
            playerHealth.Heal(amount);
            return true;
        }

        // Fallback to ProHealthManaUI
        if (healthUI != null)
        {
            healthUI.Heal(amount);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Spawn healing visual effect at player position
    /// </summary>
    private void SpawnHealEffect()
    {
        // Find player by tag if followsCharacter is enabled
        Transform targetTransform = null;
        if (spellData != null && spellData.followsCharacter)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetTransform = player.transform;
            }
        }

        // Fallback to playerTransform from SpellBase
        if (targetTransform == null)
        {
            targetTransform = playerTransform;
        }

        Vector2 effectPosition = targetTransform != null 
            ? (Vector2)targetTransform.position 
            : (Vector2)transform.position;

        bool shouldFollow = spellData != null && spellData.followsCharacter;

        // Use custom heal effect prefab if assigned
        if (healEffectPrefab != null)
        {
            Vector3 spawnPos = new Vector3(effectPosition.x, effectPosition.y, 0f);
            GameObject effect = Instantiate(healEffectPrefab, spawnPos, Quaternion.identity);

            // Parent to player so effect follows (if followsCharacter is enabled)
            if (shouldFollow && targetTransform != null)
            {
                effect.transform.SetParent(targetTransform);
                effect.transform.localPosition = Vector3.zero;
            }

            // Start particle systems if present
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();

            ParticleSystem[] childPS = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (var child in childPS)
            {
                child.Play();
            }

            Destroy(effect, healEffectDuration);
        }
        // Fallback to spellData.effectPrefab
        else if (spellData?.effectPrefab != null)
        {
            GameObject effect;
            
            if (shouldFollow && targetTransform != null)
            {
                // FIX: Use 4-parameter Instantiate to properly parent and follow character
                effect = Instantiate(
                    spellData.effectPrefab, 
                    targetTransform.position, 
                    spellData.effectPrefab.transform.rotation,
                    targetTransform  // Set parent so effect follows player
                );
                effect.transform.localPosition = Vector3.zero;
            }
            else
            {
                // Not following - spawn at world position with prefab rotation
                Vector3 spawnPos = new Vector3(effectPosition.x, effectPosition.y, 0f);
                effect = Instantiate(spellData.effectPrefab, spawnPos, spellData.effectPrefab.transform.rotation);
            }

            // Start particle systems if present
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();

            ParticleSystem[] childPS = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (var child in childPS)
            {
                child.Play();
            }

            Destroy(effect, healEffectDuration);
        }
        // Create procedural heal effect if no prefab
        else
        {
            StartCoroutine(CreateProceduralHealEffect(effectPosition, targetTransform, shouldFollow));
        }
    }

    /// <summary>
    /// Create a simple green healing effect without prefab
    /// </summary>
    private IEnumerator CreateProceduralHealEffect(Vector2 position, Transform targetTransform, bool shouldFollow)
    {
        // Create visual object
        GameObject visual = new GameObject("HealEffect");
        visual.transform.position = position;

        // Parent to player if followsCharacter is enabled
        if (shouldFollow && targetTransform != null)
        {
            visual.transform.SetParent(targetTransform);
            visual.transform.localPosition = Vector3.zero;
        }

        // Add sprite renderer with circle
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = new Color(0.3f, 1f, 0.5f, 0.8f); // Green healing color
        sr.sortingOrder = 100;

        // Animate - pulse effect
        float elapsed = 0f;
        float duration = 0.5f;
        float maxScale = 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Scale up
            float scale = Mathf.Sin(t * Mathf.PI) * maxScale;
            visual.transform.localScale = Vector3.one * scale;

            // Fade out
            Color c = sr.color;
            c.a = 0.8f * (1f - t);
            sr.color = c;

            yield return null;
        }

        Destroy(visual);
    }

    /// <summary>
    /// Create a simple circle sprite procedurally
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        int resolution = 64;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] colors = new Color[resolution * resolution];

        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance < radius)
                {
                    float alpha = 1f - (distance / radius);
                    colors[y * resolution + x] = new Color(1, 1, 1, alpha * 0.5f);
                }
                else
                {
                    colors[y * resolution + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }
}
