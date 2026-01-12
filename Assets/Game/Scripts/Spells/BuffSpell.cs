using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Buff Spell Implementation
/// - Applies temporary stat buffs to the player
/// - Uses PlayerBuffManager for buff tracking
/// - Spawns visual buff effect on player
/// </summary>
public class BuffSpell : SpellBase
{
    [Header("═══ BUFF SPELL SETTINGS ═══")]
    [Tooltip("Visual effect that stays on player while buff is active")]
    [SerializeField] private GameObject buffEffectPrefab;

    /// <summary>
    /// Cast the buff spell (target position is ignored - always buffs self)
    /// </summary>
    protected override void Cast(Vector2 targetPosition)
    {
        if (spellData == null)
        {
            Debug.LogWarning("[BuffSpell] No SpellData assigned!");
            return;
        }

        // Apply buff via PlayerBuffManager
        bool buffApplied = ApplyBuff();

        if (buffApplied)
        {
            // Spawn buff VFX on player
            SpawnBuffEffect();

            // Play impact sound
            PlaySound(spellData.impactSound);

            if (showDebugLogs)
            {
                Debug.Log($"[BuffSpell] ⚡ Applied buff: {spellData.spellName} for {spellData.buffDuration}s");
                Debug.Log($"[BuffSpell] Damage: x{spellData.damageMultiplier}, Speed: x{spellData.speedMultiplier}, Defense: x{spellData.defenseMultiplier}");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[BuffSpell] ⚠️ Could not apply buff - PlayerBuffManager not found!");
            }
        }
    }

    /// <summary>
    /// Apply the buff to the player
    /// </summary>
    private bool ApplyBuff()
    {
        // Find or create PlayerBuffManager
        PlayerBuffManager buffManager = PlayerBuffManager.Instance;

        if (buffManager == null && playerTransform != null)
        {
            // Try to find on player
            buffManager = playerTransform.GetComponent<PlayerBuffManager>();
        }

        if (buffManager == null)
        {
            // Create one on the player if it doesn't exist
            if (playerTransform != null)
            {
                buffManager = playerTransform.gameObject.AddComponent<PlayerBuffManager>();
                if (showDebugLogs)
                {
                    Debug.Log("[BuffSpell] 🔧 Created PlayerBuffManager on player");
                }
            }
            else
            {
                return false;
            }
        }

        // Apply the buff
        buffManager.ApplyBuff(spellData);
        return true;
    }

    /// <summary>
    /// Spawn buff visual effect on player
    /// </summary>
    private void SpawnBuffEffect()
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

        if (targetTransform == null) return;

        Vector2 effectPosition = targetTransform.position;
        bool shouldFollow = spellData != null && spellData.followsCharacter;

        // Use custom buff effect prefab if assigned
        if (buffEffectPrefab != null)
        {
            GameObject effect;
            
            if (shouldFollow && targetTransform != null)
            {
                // FIX: Use the 4-parameter Instantiate to explicitly preserve prefab's rotation
                // Then parent it to the target transform
                effect = Instantiate(
                    buffEffectPrefab, 
                    targetTransform.position, 
                    buffEffectPrefab.transform.rotation,  // Use world rotation from prefab
                    targetTransform  // Set parent
                );
                
                // Only reset position to center on player
                // DO NOT touch rotation - Instantiate already set it correctly!
                effect.transform.localPosition = Vector3.zero;
                
                // Scale can be set if needed
                // effect.transform.localScale = Vector3.one;
            }
            else
            {
                // Not following - spawn at world position with prefab rotation
                Vector3 spawnPos = new Vector3(effectPosition.x, effectPosition.y, 0f);
                effect = Instantiate(buffEffectPrefab, spawnPos, buffEffectPrefab.transform.rotation);
            }

            // Start particle systems if present
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();

            ParticleSystem[] childPS = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (var child in childPS)
            {
                child.Play();
            }

            // Destroy after buff duration
            Destroy(effect, spellData?.buffDuration ?? 10f);
        }
        // Fallback to spellData.effectPrefab
        else if (spellData?.effectPrefab != null)
        {
            GameObject effect;
            
            if (shouldFollow && targetTransform != null)
            {
                // FIX: Same approach - use 4-parameter Instantiate
                effect = Instantiate(
                    spellData.effectPrefab, 
                    targetTransform.position, 
                    spellData.effectPrefab.transform.rotation,
                    targetTransform
                );
                effect.transform.localPosition = Vector3.zero;
            }
            else
            {
                effect = Instantiate(spellData.effectPrefab, effectPosition, spellData.effectPrefab.transform.rotation);
            }
            
            Destroy(effect, spellData.buffDuration);
        }
        // Create procedural buff effect if no prefab
        else
        {
            StartCoroutine(CreateProceduralBuffEffect(targetTransform, shouldFollow));
        }
    }

    /// <summary>
    /// Create a simple buff aura effect without prefab
    /// </summary>
    private IEnumerator CreateProceduralBuffEffect(Transform targetTransform, bool shouldFollow)
    {
        if (targetTransform == null) yield break;

        // Create visual object
        GameObject visual = new GameObject("BuffAuraEffect");
        
        if (shouldFollow)
        {
            visual.transform.SetParent(targetTransform);
            visual.transform.localPosition = Vector3.zero;
        }
        else
        {
            visual.transform.position = targetTransform.position;
        }

        // Add sprite renderer with circle
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = spellData?.spellColor ?? new Color(1f, 0.8f, 0.2f, 0.5f); // Golden buff color
        sr.sortingOrder = -1; // Behind player

        float duration = spellData?.buffDuration ?? 10f;
        float elapsed = 0f;

        // Pulsing aura effect
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Pulse scale
            float pulse = 1f + Mathf.Sin(elapsed * 3f) * 0.2f;
            visual.transform.localScale = Vector3.one * pulse * 2f;

            // Pulse alpha
            Color c = sr.color;
            c.a = 0.3f + Mathf.Sin(elapsed * 3f) * 0.1f;
            sr.color = c;

            // Flash when buff is about to expire (last 3 seconds)
            if (duration - elapsed < 3f)
            {
                float flash = Mathf.PingPong(elapsed * 5f, 1f);
                c.a = 0.1f + flash * 0.4f;
                sr.color = c;
            }

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
                    // Ring effect - more visible at edges
                    float normalized = distance / radius;
                    float alpha = normalized > 0.7f ? (normalized - 0.7f) / 0.3f : 0f;
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