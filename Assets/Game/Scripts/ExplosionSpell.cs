using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D Patlama Büyüsü - Karakter tarafından kullanılabilir
/// Fare pozisyonuna veya karakterin baktığı yöne patlama oluşturur
/// </summary>
public class ExplosionSpell : MonoBehaviour
{
    [Header("Büyü Ayarları")]
    [Tooltip("Patlamanın verdiği hasar")]
    [SerializeField] private float damage = 25f;
    
    [Tooltip("Patlama yarıçapı")]
    [SerializeField] private float explosionRadius = 3f;
    
    [Tooltip("Büyü bekleme süresi (saniye)")]
    [SerializeField] private float cooldown = 2f;
    
    [Tooltip("Mana maliyeti (opsiyonel)")]
    [SerializeField] private float manaCost = 10f;
    
    [Header("Menzil Ayarları")]
    [Tooltip("Maksimum büyü menzili")]
    [SerializeField] private float maxCastRange = 10f;
    
    [Tooltip("Fare pozisyonuna mı yoksa karakterin önüne mi?")]
    [SerializeField] private bool useMousePosition = true;
    
    [Header("Hasar Ayarları")]
    [Tooltip("Hangi layer'lara hasar verilecek")]
    [SerializeField] private LayerMask damageableLayers;
    
    [Tooltip("Merkeze yakın düşmanlara daha fazla hasar ver")]
    [SerializeField] private bool damageDropoff = true;
    
    [Header("Görsel Efektler")]
    [Tooltip("Patlama efekti prefab'ı")]
    [SerializeField] private GameObject explosionEffectPrefab;
    
    [Tooltip("Efekt yok edilme süresi (Cartoon FX için 2-3 saniye önerilir)")]
    [SerializeField] private float effectDuration = 3f;
    
    [Tooltip("Patlama rengi")]
    [SerializeField] private Color explosionColor = new Color(1f, 0.5f, 0f, 1f); // Turuncu
    
    [Header("Ses Efektleri")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioClip castSound;
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.7f;
    
    [Header("Ekran Sarsıntısı")]
    [SerializeField] private bool enableScreenShake = true;
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private float shakeDuration = 0.2f;
    
    // Private değişkenler
    private float lastCastTime = -999f;
    private Camera mainCamera;
    private AudioSource audioSource;
    private bool isOnCooldown => Time.time < lastCastTime + cooldown;
    
    // Eventler (UI veya diğer sistemler için)
    public System.Action<float> OnCooldownChanged;
    public System.Action OnSpellCast;
    public System.Action<int> OnEnemiesHit;

    private void Awake()
    {
        Debug.Log("[ExplosionSpell] ✅ Script başlatıldı! " + gameObject.name + " üzerinde");
        
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null && (explosionSound != null || castSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        Debug.Log($"[ExplosionSpell] Ayarlar - Damage: {damage}, Radius: {explosionRadius}, Cooldown: {cooldown}, Layers: {damageableLayers.value}");
    }

    private void Update()
    {
        // Büyü tuşu kontrolü (varsayılan: Q veya sağ tık)
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
        {
            Debug.Log("[ExplosionSpell] 🔥 Input algılandı! Q veya Sağ Tık basıldı");
            TryCastSpell();
        }
        
        // Cooldown bilgisini güncelle
        if (isOnCooldown)
        {
            float remainingCooldown = (lastCastTime + cooldown) - Time.time;
            OnCooldownChanged?.Invoke(remainingCooldown);
        }
    }

    /// <summary>
    /// Büyüyü kullanmayı dener
    /// </summary>
    public void TryCastSpell()
    {
        Debug.Log("[ExplosionSpell] TryCastSpell() çağrıldı");
        
        if (isOnCooldown)
        {
            Debug.Log($"[ExplosionSpell] ⏳ Büyü bekleme süresinde! Kalan: {GetRemainingCooldown():F1}s");
            return;
        }
        
        Vector2 targetPosition = GetTargetPosition();
        Debug.Log($"[ExplosionSpell] 🎯 Hedef pozisyon: {targetPosition}");
        CastExplosion(targetPosition);
    }

    /// <summary>
    /// Hedef pozisyonunu hesaplar
    /// </summary>
    private Vector2 GetTargetPosition()
    {
        if (useMousePosition && mainCamera != null)
        {
            Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mouseWorldPos - (Vector2)transform.position).normalized;
            float distance = Vector2.Distance(transform.position, mouseWorldPos);
            
            // Maksimum menzili aşma
            if (distance > maxCastRange)
            {
                return (Vector2)transform.position + direction * maxCastRange;
            }
            return mouseWorldPos;
        }
        else
        {
            // Karakterin baktığı yöne (sprite'ın scale'ine göre)
            float direction = transform.localScale.x > 0 ? 1f : -1f;
            return (Vector2)transform.position + Vector2.right * direction * (maxCastRange * 0.5f);
        }
    }

    /// <summary>
    /// Patlamayı oluşturur
    /// </summary>
    public void CastExplosion(Vector2 position)
    {
        Debug.Log($"[ExplosionSpell] 💥 CastExplosion() çağrıldı - Pozisyon: {position}");
        
        lastCastTime = Time.time;
        OnSpellCast?.Invoke();
        
        // Ses efekti
        PlaySound(castSound);
        
        // Görsel efekt
        SpawnExplosionEffect(position);
        Debug.Log("[ExplosionSpell] ✨ Görsel efekt spawn edildi");
        
        // Hasar ver
        int hitCount = DealDamageInRadius(position);
        OnEnemiesHit?.Invoke(hitCount);
        
        // Ekran sarsıntısı
        if (enableScreenShake)
        {
            StartCoroutine(ScreenShake());
        }
        
        Debug.Log($"[ExplosionSpell] 🎯 Patlama tamamlandı! Vurulan düşman sayısı: {hitCount}");
    }

    /// <summary>
    /// Belirli yarıçaptaki düşmanlara hasar verir
    /// </summary>
    private int DealDamageInRadius(Vector2 center)
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, explosionRadius, damageableLayers);
        Debug.Log($"[ExplosionSpell] 🔍 Yarıçap taraması: {hitColliders.Length} collider bulundu (Radius: {explosionRadius}, Layer: {damageableLayers.value})");
        
        int hitCount = 0;
        
        foreach (Collider2D hitCollider in hitColliders)
        {
            Debug.Log($"[ExplosionSpell] 👾 Collider bulundu: {hitCollider.gameObject.name} (Layer: {hitCollider.gameObject.layer})");
            
            // Hasar hesaplama
            float finalDamage = damage;
            
            if (damageDropoff)
            {
                // Merkeze uzaklığa göre hasar azalması
                float distance = Vector2.Distance(center, hitCollider.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                finalDamage = damage * Mathf.Clamp01(damageMultiplier);
            }
            
            // Hasar değerini int'e çevir (projenizde int kullanılıyor)
            int intDamage = Mathf.RoundToInt(finalDamage);
            
            // Projenizde mevcut olan IDamageable interface'ini kullan
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(intDamage);
                hitCount++;
                continue;
            }
            
            // Alternatif: Health component'i ara (projenizde mevcut)
            Health health = hitCollider.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(intDamage);
                hitCount++;
            }
        }
        
        return hitCount;
    }

    /// <summary>
    /// Patlama görsel efektini oluşturur
    /// </summary>
    private void SpawnExplosionEffect(Vector2 position)
    {
        if (explosionEffectPrefab != null)
        {
            // 2D oyunda Z pozisyonunu 0 yap (veya kameranın görebileceği bir değer)
            Vector3 spawnPos = new Vector3(position.x, position.y, 0f);
            GameObject effect = Instantiate(explosionEffectPrefab, spawnPos, Quaternion.identity);
            
            Debug.Log($"[ExplosionSpell] 🎆 Prefab spawn edildi: {effect.name} - Pozisyon: {spawnPos}");
            
            // Particle System varsa başlat (Play On Awake kapalı olabilir)
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Debug.Log("[ExplosionSpell] ▶️ Ana ParticleSystem başlatıldı");
            }
            
            // Çocuk objelerdeki Particle System'leri de başlat
            ParticleSystem[] childPS = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (var childParticle in childPS)
            {
                childParticle.Play();
            }
            Debug.Log($"[ExplosionSpell] ▶️ Toplam {childPS.Length} ParticleSystem başlatıldı");
            
            // Efekt süresini ParticleSystem'den al veya varsayılanı kullan
            float destroyTime = effectDuration;
            if (ps != null && ps.main.duration > effectDuration)
            {
                destroyTime = ps.main.duration + 1f;
            }
            
            Destroy(effect, destroyTime);
        }
        else
        {
            // Prefab yoksa basit bir görsel efekt oluştur
            StartCoroutine(CreateSimpleExplosionEffect(position));
        }
        
        // Patlama sesi
        PlaySoundAtPosition(explosionSound, position);
    }

    /// <summary>
    /// Prefab olmadan basit bir patlama efekti oluşturur
    /// </summary>
    private IEnumerator CreateSimpleExplosionEffect(Vector2 position)
    {
        // Geçici bir GameObject oluştur
        GameObject explosionVisual = new GameObject("ExplosionEffect");
        explosionVisual.transform.position = position;
        
        // SpriteRenderer ekle
        SpriteRenderer sr = explosionVisual.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = explosionColor;
        sr.sortingOrder = 100;
        
        // Animasyon
        float elapsed = 0f;
        float duration = 0.3f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * explosionRadius * 2f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Büyüme animasyonu
            explosionVisual.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            // Solma animasyonu
            Color c = sr.color;
            c.a = 1f - t;
            sr.color = c;
            
            yield return null;
        }
        
        Destroy(explosionVisual);
    }

    /// <summary>
    /// Basit bir daire sprite'ı oluşturur
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
                    // Merkeze yakın daha parlak
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

    /// <summary>
    /// Ekran sarsıntısı efekti
    /// </summary>
    private IEnumerator ScreenShake()
    {
        if (mainCamera == null) yield break;
        
        Vector3 originalPos = mainCamera.transform.position;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            
            mainCamera.transform.position = originalPos + new Vector3(x, y, 0);
            
            yield return null;
        }
        
        mainCamera.transform.position = originalPos;
    }

    /// <summary>
    /// Ses çalma yardımcı metodu
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    /// <summary>
    /// Belirli pozisyonda ses çalma
    /// </summary>
    private void PlaySoundAtPosition(AudioClip clip, Vector2 position)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, soundVolume);
        }
    }

    /// <summary>
    /// Kalan bekleme süresini döndürür
    /// </summary>
    public float GetRemainingCooldown()
    {
        if (!isOnCooldown) return 0f;
        return (lastCastTime + cooldown) - Time.time;
    }

    /// <summary>
    /// Büyünün hazır olup olmadığını kontrol eder
    /// </summary>
    public bool IsReady()
    {
        return !isOnCooldown;
    }

    /// <summary>
    /// Cooldown'u sıfırlar (test veya power-up için)
    /// </summary>
    public void ResetCooldown()
    {
        lastCastTime = -999f;
    }

    // Gizmos - Editor'de patlama yarıçapını görselleştir
    private void OnDrawGizmosSelected()
    {
        // Menzil
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxCastRange);
        
        // Fare pozisyonunda patlama önizlemesi (oyun modunda)
        if (Application.isPlaying && mainCamera != null)
        {
            Vector2 targetPos = GetTargetPosition();
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(targetPos, explosionRadius);
            Gizmos.DrawSphere(targetPos, 0.2f);
        }
    }
}