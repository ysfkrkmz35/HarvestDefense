using UnityEngine;
using UnityEditor;

/// <summary>
/// Mehmet'in AI Test Sahnesini Otomatik Kuran Editor Script (NavMesh Yok)
/// Unity Menu: Tools → Mehmet → Setup Test Scene
/// </summary>
public class MehmetSceneSetup : EditorWindow
{
    [MenuItem("Tools/Mehmet/Setup Test Scene (No NavMesh)")]
    public static void SetupTestScene()
    {
        if (EditorUtility.DisplayDialog("Mehmet Test Sahne Kurulumu",
            "Bu araç otomatik olarak:\n\n" +
            "1. Ground (Zemin)\n" +
            "2. Base\n" +
            "3. Enemy Prefab\n" +
            "4. EnemySpawner\n" +
            "5. GameManager\n" +
            "6. TestHelper\n\n" +
            "oluşturacak (NavMesh YOK - Transform bazlı hareket).\n\n" +
            "Devam etmek istiyor musun?",
            "Evet, Kur!",
            "İptal"))
        {
            CreateTestScene();
        }
    }

    private static void CreateTestScene()
    {
        Debug.Log("=== MEHMET TEST SAHNE KURULUMU BAŞLADI (NavMesh YOK) ===");

        // 1. GROUND OLUŞTUR
        GameObject ground = CreateGround();
        Debug.Log("✓ Ground oluşturuldu");

        // 2. BASE OLUŞTUR
        GameObject baseObj = CreateBase();
        Debug.Log("✓ Base oluşturuldu");

        // 3. ENEMY PREFAB OLUŞTUR
        GameObject enemyPrefab = CreateEnemyPrefab();
        Debug.Log("✓ Enemy Prefab oluşturuldu");

        // 4. ENEMY SPAWNER OLUŞTUR
        GameObject spawner = CreateEnemySpawner(enemyPrefab);
        Debug.Log("✓ Enemy Spawner oluşturuldu");

        // 5. GAMEMANAGER OLUŞTUR
        GameObject gameManager = CreateGameManager();
        Debug.Log("✓ GameManager oluşturuldu");

        // 6. TEST HELPER OLUŞTUR
        GameObject testHelper = CreateTestHelper(spawner);
        Debug.Log("✓ TestHelper oluşturuldu");

        Debug.Log("=== KURULUM TAMAMLANDI! ===");
        Debug.Log("Play butonuna basıp F1 tuşuna bas veya GameManager eventlerini kullan!");

        EditorUtility.DisplayDialog("Kurulum Tamamlandı!",
            "Test sahnesi hazır!\n\n" +
            "PLAY basıp test et:\n" +
            "• F1: Gece Başlat (Manual)\n" +
            "• F2: Gündüz Başlat (Manual)\n" +
            "• F3: Tek Düşman Spawn\n" +
            "• F4: Game Over\n\n" +
            "Düşmanlar Transform ile Base'e gidecek!",
            "Tamam");
    }

    // ============================================
    // 1. GROUND OLUŞTUR
    // ============================================
    private static GameObject CreateGround()
    {
        GameObject ground = new GameObject("Ground");
        ground.layer = LayerMask.NameToLayer("Ground");

        // Sprite Renderer
        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(0.4f, 0.7f, 0.4f); // Yeşil zemin
        ground.transform.localScale = new Vector3(30, 30, 1);
        ground.transform.position = Vector3.zero;

        // Box Collider 2D
        BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;

        return ground;
    }

    // ============================================
    // 2. BASE OLUŞTUR
    // ============================================
    private static GameObject CreateBase()
    {
        GameObject baseObj = new GameObject("Base");
        baseObj.tag = "Base";
        baseObj.layer = LayerMask.NameToLayer("Wall"); // Wall layer (düşman hedef alır)
        baseObj.transform.position = Vector3.zero;

        // Sprite Renderer
        SpriteRenderer sr = baseObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = Color.blue;
        sr.sortingOrder = 1;
        baseObj.transform.localScale = new Vector3(2, 2, 1);

        // Box Collider 2D
        BoxCollider2D collider = baseObj.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;

        // Health
        Health health = baseObj.AddComponent<Health>();
        SerializedObject so = new SerializedObject(health);
        so.FindProperty("maxHealth").intValue = 500;
        so.ApplyModifiedProperties();

        return baseObj;
    }

    // ============================================
    // 3. ENEMY PREFAB OLUŞTUR
    // ============================================
    private static GameObject CreateEnemyPrefab()
    {
        GameObject enemy = new GameObject("Enemy");
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");

        // Visual (Child Sprite)
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(enemy.transform);
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = Color.red;
        sr.sortingOrder = 1;
        visual.transform.localScale = Vector3.one;

        // Circle Collider 2D
        CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = false;

        // Rigidbody 2D
        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // EnemyAI
        EnemyAI ai = enemy.AddComponent<EnemyAI>();
        SerializedObject aiSO = new SerializedObject(ai);
        aiSO.FindProperty("moveSpeed").floatValue = 2.5f;
        aiSO.FindProperty("detectionRange").floatValue = 3f;
        aiSO.FindProperty("attackRange").floatValue = 1.5f;
        aiSO.FindProperty("attackCooldown").floatValue = 1f;
        aiSO.FindProperty("attackDamage").intValue = 10;
        aiSO.FindProperty("obstacleDetectionDistance").floatValue = 1.5f;
        aiSO.FindProperty("avoidanceForce").floatValue = 2f;
        aiSO.FindProperty("maxDistanceFromCenter").floatValue = 14f; // Ground 30x30, yarısı 15, biraz içeride
        aiSO.FindProperty("boundaryPushForce").floatValue = 5f;

        // Layer masks ayarla
        int playerLayerMask = 1 << LayerMask.NameToLayer("Player");
        int wallLayerMask = 1 << LayerMask.NameToLayer("Wall");
        aiSO.FindProperty("playerLayer.m_Bits").intValue = playerLayerMask;
        aiSO.FindProperty("wallLayer.m_Bits").intValue = wallLayerMask;
        aiSO.FindProperty("obstacleLayer.m_Bits").intValue = wallLayerMask;
        aiSO.ApplyModifiedProperties();

        // Health
        Health health = enemy.AddComponent<Health>();
        SerializedObject healthSO = new SerializedObject(health);
        healthSO.FindProperty("maxHealth").intValue = 100;
        healthSO.ApplyModifiedProperties();

        // Prefab olarak kaydet
        string prefabPath = "Assets/Game/Scenes/Test_Scenes/Mehmet/Prefabs/Enemy.prefab";

        // Klasör yoksa oluştur
        if (!AssetDatabase.IsValidFolder("Assets/Game/Scenes/Test_Scenes/Mehmet/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/Game/Scenes/Test_Scenes/Mehmet", "Prefabs");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);
        Object.DestroyImmediate(enemy); // Scene'den sil, prefab yeterli

        return prefab;
    }

    // ============================================
    // 4. ENEMY SPAWNER OLUŞTUR
    // ============================================
    private static GameObject CreateEnemySpawner(GameObject enemyPrefab)
    {
        GameObject spawner = new GameObject("EnemySpawner");
        spawner.transform.position = Vector3.zero;

        EnemySpawner spawnerScript = spawner.AddComponent<EnemySpawner>();

        // Serialized Object ile private field'ları ayarla
        SerializedObject so = new SerializedObject(spawnerScript);
        so.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
        so.FindProperty("poolSize").intValue = 20;
        so.FindProperty("spawnRadius").floatValue = 10f;
        so.FindProperty("spawnCenter").vector2Value = Vector2.zero;
        so.FindProperty("initialEnemiesPerWave").intValue = 5;
        so.FindProperty("enemyIncreasePerWave").floatValue = 2f;
        so.FindProperty("spawnInterval").floatValue = 2f;
        so.FindProperty("minDistanceFromBase").floatValue = 5f;

        // Obstacle layer maskı ayarla
        int wallLayerMask = 1 << LayerMask.NameToLayer("Wall");
        so.FindProperty("obstacleLayer.m_Bits").intValue = wallLayerMask;

        so.ApplyModifiedProperties();

        return spawner;
    }

    // ============================================
    // 5. GAMEMANAGER OLUŞTUR
    // ============================================
    private static GameObject CreateGameManager()
    {
        // Eğer sahnede zaten GameManager varsa onu kullan
        GameManager existingManager = Object.FindObjectOfType<GameManager>();
        if (existingManager != null)
        {
            Debug.Log("✓ GameManager zaten var, mevcut kullanılıyor.");
            return existingManager.gameObject;
        }

        // Manuel oluştur
        GameObject managers = new GameObject("GameManager");
        managers.AddComponent<GameManager>();

        // TimeManager varsa ekle
        if (System.Type.GetType("TimeManager") != null)
        {
            managers.AddComponent(System.Type.GetType("TimeManager"));
        }

        return managers;
    }

    // ============================================
    // 6. TEST HELPER OLUŞTUR
    // ============================================
    private static GameObject CreateTestHelper(GameObject spawner)
    {
        GameObject helper = new GameObject("TestHelper");
        TestHelper testHelper = helper.AddComponent<TestHelper>();

        // Spawner referansını ata
        SerializedObject so = new SerializedObject(testHelper);
        so.FindProperty("enemySpawner").objectReferenceValue = spawner.GetComponent<EnemySpawner>();
        so.ApplyModifiedProperties();

        return helper;
    }

    // ============================================
    // HELPER: SQUARE SPRITE OLUŞTUR
    // ============================================
    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
