using UnityEngine;
using System.Collections;

/// <summary>
/// Area Spell Implementation
/// - Deals AoE damage at target location
/// - Distance-based damage falloff
/// - Visual explosion effect
/// - Refactored from ExplosionSpell
/// </summary>
public class AreaSpell : SpellBase
{
    [Header("═══ AREA SPELL SETTINGS ═══")]
    [Tooltip("Layer mask for damageable objects")]
    [SerializeField] private LayerMask damageableLayers;

    [Tooltip("Apply distance-based damage falloff")]
    [SerializeField] private bool useDamageDropoff = true;

    /// <summary>
    /// Cast the area spell at target position
    /// </summary>
    protected override void Cast(Vector2 targetPosition)
    {
        // Spawn visual effect
        SpawnExplosionEffect(targetPosition);

        // Deal damage
        int hitCount = DealDamageInRadius(
            targetPosition,
            spellData.areaRadius,
            spellData.damage,
            damageableLayers,
            useDamageDropoff
        );

        // Play impact sound
        PlaySoundAtPosition(spellData.impactSound, targetPosition);

        if (showDebugLogs)
        {
            Debug.Log($"[AreaSpell] 💥 {spellData.spellName} hit {hitCount} enemies at {targetPosition}");
        }
    }

    /// <summary>
    /// Spawn explosion visual effect
    /// </summary>
    private void SpawnExplosionEffect(Vector2 position)
    {
        if (spellData.effectPrefab != null)
        {
            Vector3 spawnPos = new Vector3(position.x, position.y, 0f);
            GameObject effect = Instantiate(spellData.effectPrefab, spawnPos, Quaternion.identity);

            // Start particle systems if present
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();

            ParticleSystem[] childPS = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (var child in childPS)
            {
                child.Play();
            }

            Destroy(effect, spellData.effectDuration);
        }
        else
        {
            // Create simple procedural effect if no prefab
            StartCoroutine(CreateSimpleExplosionEffect(position));
        }
    }

    /// <summary>
    /// Create a simple visual effect without prefab
    /// </summary>
    private IEnumerator CreateSimpleExplosionEffect(Vector2 position)
    {
        // Create visual object
        GameObject visual = new GameObject("ExplosionEffect");
        visual.transform.position = position;

        // Add sprite renderer with circle
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = spellData?.spellColor ?? new Color(1f, 0.5f, 0f, 1f);
        sr.sortingOrder = 100;

        // Animate scale and fade
        float elapsed = 0f;
        float duration = 0.3f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * spellData.areaRadius * 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Scale up
            visual.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            // Fade out
            Color c = sr.color;
            c.a = 1f - t;
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
                    colors[y * resolution + x] = new Color(1, 1, 1, alpha);
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

    #region ═══════ EDITOR ═══════

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Show area radius at mouse in play mode
        if (Application.isPlaying && spellData != null && mainCamera != null)
        {
            Vector2 mousePos = GetMouseWorldPosition();
            Gizmos.color = spellData.spellColor;
            Gizmos.DrawWireSphere(mousePos, spellData.areaRadius);
            Gizmos.DrawSphere(mousePos, 0.2f);
        }
    }

    #endregion
}
