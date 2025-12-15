using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Enemy AI Test Sahnesi Otomatik Kurulum Aracı
/// Unity üst menüde "Tools/Enemy AI/Setup Test Scene" butonuyla çalışır
/// </summary>
public class EnemyAITestSetup : EditorWindow
{
    private GameObject enemyPrefab;
    private bool useQuickSettings = true;
    private float dayDuration = 5f;
    private float nightDuration = 10f;

    [MenuItem("Tools/Enemy AI/Setup Test Scene")]
    public static void ShowWindow()
    {
        GetWindow<EnemyAITestSetup>("Enemy AI Test Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("ENEMY AI TEST SAHNE KURULUMU", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Bu araç otomatik olarak test sahnesi kurar:\n" +
            "✅ Managers (_Managers + GameManager + TimeManager)\n" +
            "✅ Ground (Zemin)\n" +
            "✅ Player (Mavi circle)\n" +
            "✅ EnemySpawner\n" +
            "✅ Camera ayarları",
            MessageType.Info
        );

        GUILayout.Space(10);

        // Enemy Prefab seçimi
        GUILayout.Label("Enemy Prefab:", EditorStyles.boldLabel);
        enemyPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Spider Enemy Prefab",
            enemyPrefab,
            typeof(GameObject),
            false
        );

        GUILayout.Space(10);

        // Hızlı ayarlar
        useQuickSettings = EditorGUILayout.Toggle("Hızlı Test Ayarları", useQuickSettings);

        if (!useQuickSettings)
        {
            dayDuration = EditorGUILayout.FloatField("Day Duration (s)", dayDuration);
            nightDuration = EditorGUILayout.FloatField("Night Duration (s)", nightDuration);
        }
        else
        {
            EditorGUILayout.HelpBox("Hızlı Test: Day 3s, Night 15s (debug için)", MessageType.None);
        }

        GUILayout.Space(20);

        // Setup butonu (Mevcut sahneye ekle)
        if (GUILayout.Button("➕ MEVCUT SAHNEYE EKLE", GUILayout.Height(40)))
        {
            AddToCurrentScene();
        }

        GUILayout.Space(10);

        // Temiz sahne kur butonu
        if (GUILayout.Button("🚀 YENİ TEST SAHNESİ KUR (Sahneyi Temizler)", GUILayout.Height(40)))
        {
            SetupTestScene();
        }

        GUILayout.Space(10);

        // Enemy Prefab oluşturma butonu
        if (GUILayout.Button("🕷️ ENEMY PREFAB OLUŞTUR", GUILayout.Height(30)))
        {
            CreateEnemyPrefab();
        }

        GUILayout.Space(5);
        EditorGUILayout.HelpBox("Önce Enemy Prefab oluştur, sonra sahneye ekle!", MessageType.Warning);
    }

    /// <summary>
    /// Mevcut sahneye sadece eksik olanları ekler (sahneyi temizlemez)
    /// </summary>
    void AddToCurrentScene()
    {
        if (enemyPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Hata",
                "Lütfen önce Enemy Prefab'ı seç veya oluştur!",
                "Tamam"
            );
            return;
        }

        int addedCount = 0;
        string addedItems = "";

        // Managers kontrol et
        GameManager existingGameManager = FindObjectOfType<GameManager>();
        TimeManager existingTimeManager = FindObjectOfType<TimeManager>();

        if (existingGameManager == null || existingTimeManager == null)
        {
            CreateManagers();
            addedItems += "✅ Managers (_Managers + GameManager + TimeManager)\n";
            addedCount++;
        }
        else
        {
            Debug.Log("⏭️ Managers zaten var, atlanıyor");
        }

        // Player kontrol et
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer == null)
        {
            // Tag ile bulamadıysa isimle ara
            existingPlayer = GameObject.Find("Player");
        }

        if (existingPlayer == null)
        {
            CreatePlayer();
            addedItems += "✅ Player (Kinematic, Health, 'Player' tag)\n";
            addedCount++;
        }
        else
        {
            Debug.Log("⏭️ Player zaten var, atlanıyor");
        }

        // EnemySpawner kontrol et
        SimpleEnemySpawner existingSpawner = FindObjectOfType<SimpleEnemySpawner>();
        if (existingSpawner == null)
        {
            CreateEnemySpawner();
            addedItems += "✅ EnemySpawner (Enemy prefab atanmış)\n";
            addedCount++;
        }
        else
        {
            Debug.Log("⏭️ EnemySpawner zaten var, atlanıyor");
        }

        // Camera'ya dokunma (mevcut sahne ayarları korunsun)
        Debug.Log("⏭️ Camera ayarlarına dokunulmadı (mevcut ayarlar korundu)");

        // Sonuç mesajı
        if (addedCount > 0)
        {
            EditorUtility.DisplayDialog(
                "Başarılı! ✅",
                $"Mevcut sahneye {addedCount} öğe eklendi:\n\n{addedItems}\n" +
                "▶️ Play'e basarak test edebilirsin.\n\n" +
                "📋 Console'da debug loglarına bak!",
                "Harika!"
            );
            Debug.Log($"✅ [EnemyAITestSetup] Mevcut sahneye {addedCount} öğe eklendi");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Bilgi ℹ️",
                "Sahne zaten hazır!\n\n" +
                "Managers, Player ve EnemySpawner mevcut.\n\n" +
                "▶️ Play'e basarak test edebilirsin.",
                "Tamam"
            );
            Debug.Log("ℹ️ [EnemyAITestSetup] Sahneye eklenecek bir şey yok, her şey mevcut");
        }
    }

    /// <summary>
    /// Sahneyi temizler ve sıfırdan test sahnesi kurar
    /// </summary>
    void SetupTestScene()
    {
        if (enemyPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Hata",
                "Lütfen önce Enemy Prefab'ı seç veya oluştur!",
                "Tamam"
            );
            return;
        }

        // Onay
        if (!EditorUtility.DisplayDialog(
            "Test Sahnesi Kur",
            "⚠️ UYARI: Mevcut sahne tamamen temizlenecek!\n\n" +
            "Sadece eksik olanları eklemek için 'MEVCUT SAHNEYE EKLE' butonunu kullan.\n\n" +
            "Devam edilsin mi?",
            "Evet, Sahneyi Temizle ve Kur",
            "İptal"))
        {
            return;
        }

        // Sahneyi temizle
        ClearScene();

        // Objeleri oluştur
        CreateManagers();
        CreateGround();
        CreatePlayer();
        CreateEnemySpawner();
        SetupCamera();

        // Başarı mesajı
        EditorUtility.DisplayDialog(
            "Başarılı! ✅",
            "Temiz test sahnesi kuruldu!\n\n" +
            "▶️ Play'e basarak test edebilirsin.\n\n" +
            "Beklenenler:\n" +
            "- 3s gündüz (düşman yok)\n" +
            "- 15s gece (2-5 düşman spawn)\n" +
            "- Düşmanlar player'a saldırıyor\n" +
            "- 3s gündüz (düşmanlar kayboluyor)\n" +
            "- Döngü devam ediyor...\n\n" +
            "📋 Console'da [SimpleEnemyAI] debug loglarına bak!\n" +
            "- Player bulundu mu?\n" +
            "- Enemy ve Player pozisyonları doğru mu?\n" +
            "- Direction ve Velocity değerleri ne?",
            "Harika!"
        );

        Debug.Log("✅ [EnemyAITestSetup] Test sahnesi başarıyla kuruldu!");
    }

    void ClearScene()
    {
        // Sahneyi temizle (Camera hariç)
        var allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.GetComponent<Camera>() == null)
            {
                DestroyImmediate(obj);
            }
        }
    }

    void CreateManagers()
    {
        GameObject managers = new GameObject("_Managers");
        managers.transform.position = Vector3.zero;

        // GameManager ekle
        managers.AddComponent<GameManager>();

        // TimeManager ekle
        TimeManager timeManager = managers.AddComponent<TimeManager>();

        // Reflection ile private field'ları set et
        var dayField = typeof(TimeManager).GetField("dayDuration",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nightField = typeof(TimeManager).GetField("nightDuration",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Hızlı test için süreleri kısalt
        float dayTime = useQuickSettings ? 3f : dayDuration;
        float nightTime = useQuickSettings ? 15f : nightDuration; // Daha uzun gece süresi test için

        if (dayField != null)
            dayField.SetValue(timeManager, dayTime);
        if (nightField != null)
            nightField.SetValue(timeManager, nightTime);

        Debug.Log($"✅ Managers oluşturuldu (Day: {dayTime}s, Night: {nightTime}s)");
    }

    void CreateGround()
    {
        GameObject ground = new GameObject("Ground");
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(30, 30, 1);
        ground.layer = LayerMask.NameToLayer("Ground");

        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(0.3f, 0.7f, 0.3f); // Yeşil

        Debug.Log("✅ Ground oluşturuldu");
    }

    void CreatePlayer()
    {
        GameObject player = new GameObject("Player");
        player.transform.position = Vector3.zero;
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Player");

        // Sprite - Daha büyük ve görünür
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = Color.cyan; // Daha parlak renk
        sr.sortingOrder = 10; // En üstte görünsün
        player.transform.localScale = new Vector3(1.2f, 1.2f, 1f); // Biraz büyüt

        // Rigidbody2D - Kinematic yapıyoruz ki düşmanlar itemesin
        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // Kinematic = başka objeler itemiyor
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Collider - Enemy ile çarpışmasın (sadece trigger)
        CircleCollider2D col = player.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = false; // Fiziksel çarpışma olsun ama enemy layer ile değil

        // Health
        Health health = player.AddComponent<Health>();
        // Reflection ile maxHealth'i 1000 yap (test için)
        var maxHealthField = typeof(Health).GetField("maxHealth",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (maxHealthField != null)
            maxHealthField.SetValue(health, 1000);

        // Movement (eğer varsa)
        var movementType = System.Type.GetType("TopDownMovement");
        if (movementType == null)
            movementType = System.Type.GetType("TopDownPlayerController");

        if (movementType != null)
        {
            player.AddComponent(movementType);
            Debug.Log("✅ Player oluşturuldu (WASD ile hareket edebilirsin)");
        }
        else
        {
            Debug.Log("✅ Player oluşturuldu (Movement script bulunamadı, manuel hareket yok)");
        }
    }

    void CreateEnemySpawner()
    {
        GameObject spawner = new GameObject("EnemySpawner");
        spawner.transform.position = Vector3.zero;

        SimpleEnemySpawner spawnerScript = spawner.AddComponent<SimpleEnemySpawner>();

        // Reflection ile private field'ları set et
        var prefabField = typeof(SimpleEnemySpawner).GetField("enemyPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var poolField = typeof(SimpleEnemySpawner).GetField("poolSize",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var minEnemiesField = typeof(SimpleEnemySpawner).GetField("minEnemiesPerNight",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var maxEnemiesField = typeof(SimpleEnemySpawner).GetField("maxEnemiesPerNight",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var minIntervalField = typeof(SimpleEnemySpawner).GetField("minSpawnInterval",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var maxIntervalField = typeof(SimpleEnemySpawner).GetField("maxSpawnInterval",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var intervalIncreaseField = typeof(SimpleEnemySpawner).GetField("intervalIncrease",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var minDistField = typeof(SimpleEnemySpawner).GetField("minDistanceFromPlayer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var maxDistField = typeof(SimpleEnemySpawner).GetField("maxDistanceFromPlayer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (prefabField != null) prefabField.SetValue(spawnerScript, enemyPrefab);
        if (poolField != null) poolField.SetValue(spawnerScript, 20); // Biraz daha fazla
        if (minEnemiesField != null) minEnemiesField.SetValue(spawnerScript, 2); // Test için az başla
        if (maxEnemiesField != null) maxEnemiesField.SetValue(spawnerScript, 5); // Test için az başla
        if (minIntervalField != null) minIntervalField.SetValue(spawnerScript, 0.5f); // Biraz yavaş başla
        if (maxIntervalField != null) maxIntervalField.SetValue(spawnerScript, 1.5f);
        if (intervalIncreaseField != null) intervalIncreaseField.SetValue(spawnerScript, 0.1f);
        if (minDistField != null) minDistField.SetValue(spawnerScript, 8f); // Biraz daha yakın
        if (maxDistField != null) maxDistField.SetValue(spawnerScript, 15f);

        Debug.Log("✅ EnemySpawner oluşturuldu");
    }

    void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        mainCam.transform.position = new Vector3(0, 0, -10);
        mainCam.orthographic = true;
        mainCam.orthographicSize = 15; // Biraz daha geniş görsün
        mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f); // Koyu mavi-gri

        Debug.Log("✅ Camera ayarlandı (Size: 15, daha geniş görüş)");
    }

    void CreateEnemyPrefab()
    {
        // Enemy objesi oluştur
        GameObject enemy = new GameObject("Spider_Enemy");
        enemy.transform.position = new Vector3(999, 999, 0); // Kenarda
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");

        // Sprite
        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = new Color(1f, 0.3f, 0f); // Turuncu-kırmızı
        sr.sortingOrder = 5; // Player'ın altında ama ground'un üstünde
        enemy.transform.localScale = new Vector3(0.9f, 0.9f, 1);

        // Rigidbody2D - Optimize edilmiş hareket
        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.linearDamping = 0; // Sürtünme sıfır
        rb.angularDamping = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Collider - Trigger yapıyoruz ki Player'ın üzerine binebilsin
        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true; // Trigger = fiziksel çarpışma yok, sadece mesafe kontrolü

        // SimpleEnemyAI
        SimpleEnemyAI ai = enemy.AddComponent<SimpleEnemyAI>();

        // Reflection ile parametreleri ayarla
        var moveSpeedField = typeof(SimpleEnemyAI).GetField("moveSpeed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var attackRangeField = typeof(SimpleEnemyAI).GetField("attackRange",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var attackDamageField = typeof(SimpleEnemyAI).GetField("attackDamage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (moveSpeedField != null)
            moveSpeedField.SetValue(ai, 4f);
        if (attackRangeField != null)
            attackRangeField.SetValue(ai, 1.2f); // Collider temas mesafesi
        if (attackDamageField != null)
            attackDamageField.SetValue(ai, 10);

        // Attack cooldown da ayarla
        var attackCooldownField = typeof(SimpleEnemyAI).GetField("attackCooldown",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (attackCooldownField != null)
            attackCooldownField.SetValue(ai, 0.5f); // Daha hızlı saldırı

        // EnemyHealth
        enemy.AddComponent<EnemyHealth>();

        // Prefab klasörü oluştur
        string folderPath = "Assets/Game/Scenes/Test_Scenes/Mehmet/Prefabs";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Game/Scenes/Test_Scenes/Mehmet", "Prefabs");
        }

        // Prefab yap
        string prefabPath = folderPath + "/Spider_Enemy.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);

        // Hierarchy'den sil
        DestroyImmediate(enemy);

        // Otomatik seç
        enemyPrefab = prefab;
        Selection.activeObject = prefab;

        EditorUtility.DisplayDialog(
            "Enemy Prefab Oluşturuldu! ✅",
            "Spider_Enemy prefab'ı oluşturuldu!\n\n" +
            "📂 Konum: " + prefabPath + "\n\n" +
            "Şimdi 'TEST SAHNESİNİ KUR' butonuna basabilirsin.",
            "Tamam"
        );

        Debug.Log($"✅ Enemy Prefab oluşturuldu: {prefabPath}");
    }

    // Basit sprite oluşturucular
    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateCircleSprite()
    {
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        Vector2 center = new Vector2(16, 16);

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * 32 + x] = dist < 15 ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
}
