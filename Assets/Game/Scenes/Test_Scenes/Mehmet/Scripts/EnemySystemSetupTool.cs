using UnityEngine;

/// <summary>
/// Enemy System Setup Tool - Harvest Defense
/// Unity Editor'dan tek tıkla sahneyi kurar
/// - Enemy prefab oluşturur
/// - EnemySpawner kurar
/// - Test kontrolleri ekler
/// </summary>
public class EnemySystemSetupTool : MonoBehaviour
{
    [Header("=== SETUP OPTIONS ===")]
    [SerializeField] private bool createEnemyPrefab = true;
    [SerializeField] private bool createEnemySpawner = true;
    [SerializeField] private bool addTestControls = true;

    [Header("=== ENEMY SETTINGS ===")]
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private float enemySize = 0.8f;

    [Header("=== SPAWNER SETTINGS ===")]
    [SerializeField] private int poolSize = 30;
    [SerializeField] private int baseEnemiesPerWave = 6;

    [Header("=== TEST CONTROLS (F Keys) ===")]
    [Tooltip("F1: Start Night")]
    [SerializeField] private bool f1_StartNight = true;
    [Tooltip("F2: Start Day")]
    [SerializeField] private bool f2_StartDay = true;
    [Tooltip("F3: Spawn Single Enemy")]
    [SerializeField] private bool f3_SpawnEnemy = true;
    [Tooltip("F4: Clear All Enemies")]
    [SerializeField] private bool f4_ClearEnemies = true;

    private EnemySpawner spawnerReference;

    private void Update()
    {
        if (!addTestControls)
            return;

        // F1: Start Night
        if (f1_StartNight && Input.GetKeyDown(KeyCode.F1))
        {
            StartNight();
        }

        // F2: Start Day
        if (f2_StartDay && Input.GetKeyDown(KeyCode.F2))
        {
            StartDay();
        }

        // F3: Spawn Single Enemy
        if (f3_SpawnEnemy && Input.GetKeyDown(KeyCode.F3))
        {
            SpawnSingleEnemy();
        }

        // F4: Clear All Enemies
        if (f4_ClearEnemies && Input.GetKeyDown(KeyCode.F4))
        {
            ClearAllEnemies();
        }
    }

    private void StartNight()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Night);
            Debug.Log("[Setup Tool] Night started (F1)");
        }
        else
        {
            Debug.LogError("[Setup Tool] GameManager not found!");
        }
    }

    private void StartDay()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Day);
            Debug.Log("[Setup Tool] Day started (F2)");
        }
        else
        {
            Debug.LogError("[Setup Tool] GameManager not found!");
        }
    }

    private void SpawnSingleEnemy()
    {
        if (spawnerReference == null)
            spawnerReference = FindFirstObjectByType<EnemySpawner>();

        if (spawnerReference != null)
        {
            spawnerReference.TestSpawnSingleEnemy();
            Debug.Log("[Setup Tool] Single enemy spawned (F3)");
        }
        else
        {
            Debug.LogError("[Setup Tool] EnemySpawner not found!");
        }
    }

    private void ClearAllEnemies()
    {
        if (spawnerReference == null)
            spawnerReference = FindFirstObjectByType<EnemySpawner>();

        if (spawnerReference != null)
        {
            spawnerReference.TestDeactivateAll();
            Debug.Log("[Setup Tool] All enemies cleared (F4)");
        }
        else
        {
            Debug.LogError("[Setup Tool] EnemySpawner not found!");
        }
    }

    private void OnGUI()
    {
        if (!addTestControls)
            return;

        // Control panel
        GUI.Box(new Rect(10, 10, 300, 150), "ENEMY SYSTEM - TEST CONTROLS");

        GUI.Label(new Rect(20, 35, 280, 20), "F1: Start Night (Spawn Wave)");
        GUI.Label(new Rect(20, 55, 280, 20), "F2: Start Day (Clear All)");
        GUI.Label(new Rect(20, 75, 280, 20), "F3: Spawn Single Enemy");
        GUI.Label(new Rect(20, 95, 280, 20), "F4: Clear All Enemies");

        // Current state
        if (GameManager.Instance != null)
        {
            GUI.Box(new Rect(10, 170, 300, 50), "CURRENT STATE");
            GUI.Label(new Rect(20, 195, 280, 20), $"State: {GameManager.Instance.CurrentState}");
        }

        // Enemy count
        int activeEnemyCount = 0;
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.gameObject.activeInHierarchy)
                activeEnemyCount++;
        }

        GUI.Box(new Rect(10, 230, 300, 50), "STATISTICS");
        GUI.Label(new Rect(20, 255, 280, 20), $"Active Enemies: {activeEnemyCount}");
    }

#if UNITY_EDITOR
    [ContextMenu("1. Setup Complete System")]
    public void SetupCompleteSystem()
    {
        Debug.Log("=== STARTING COMPLETE SYSTEM SETUP ===");

        if (createEnemyPrefab)
        {
            CreateEnemyPrefab();
        }

        if (createEnemySpawner)
        {
            CreateEnemySpawner();
        }

        CreateAStarPathfinding();

        Debug.Log("=== SETUP COMPLETE ===");
        Debug.Log("Press F1-F4 in Play Mode to test!");
    }

    [ContextMenu("2. Create Enemy Prefab Only")]
    public void CreateEnemyPrefab()
    {
        Debug.Log("[Setup Tool] Creating Enemy Prefab...");

        // Check if prefab already exists
        string prefabPath = "Assets/Game/Scenes/Test_Scenes/Mehmet/Prefabs/Enemy.prefab";
        GameObject existingPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (existingPrefab != null)
        {
            Debug.LogWarning("[Setup Tool] Enemy prefab already exists! Delete it first or use a different name.");
            return;
        }

        // Create Enemy GameObject
        GameObject enemy = new GameObject("Enemy");
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.tag = "Enemy";

        // Add Rigidbody2D
        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Add CircleCollider2D
        CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
        collider.radius = 0.4f;

        // Add EnemyAI
        enemy.AddComponent<EnemyAI>();

        // Add EnemyHealth
        enemy.AddComponent<EnemyHealth>();

        // Create Sprite child
        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(enemy.transform);
        spriteObj.transform.localPosition = Vector3.zero;

        SpriteRenderer spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();

        // Create a simple sprite (circle)
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dx = x - 32;
                float dy = y - 32;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance < 28)
                {
                    pixels[y * 64 + x] = enemyColor;
                }
                else
                {
                    pixels[y * 64 + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 5;

        // Scale
        enemy.transform.localScale = Vector3.one * enemySize;

        // Save as prefab
        string directory = "Assets/Game/Scenes/Test_Scenes/Mehmet/Prefabs";
        if (!UnityEditor.AssetDatabase.IsValidFolder(directory))
        {
            Debug.LogError($"[Setup Tool] Directory not found: {directory}");
            DestroyImmediate(enemy);
            return;
        }

        GameObject prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);
        DestroyImmediate(enemy);

        Debug.Log($"[Setup Tool] Enemy prefab created: {prefabPath}");
        UnityEditor.Selection.activeObject = prefab;
    }

    [ContextMenu("3. Create EnemySpawner Only")]
    public void CreateEnemySpawner()
    {
        Debug.Log("[Setup Tool] Creating EnemySpawner...");

        // Check if spawner already exists
        EnemySpawner existingSpawner = FindFirstObjectByType<EnemySpawner>();
        if (existingSpawner != null)
        {
            Debug.LogWarning("[Setup Tool] EnemySpawner already exists in scene!");
            UnityEditor.Selection.activeGameObject = existingSpawner.gameObject;
            return;
        }

        // Create spawner GameObject
        GameObject spawnerObj = new GameObject("EnemySpawner");
        EnemySpawner spawner = spawnerObj.AddComponent<EnemySpawner>();

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // Load enemy prefab
        string prefabPath = "Assets/Game/Scenes/Test_Scenes/Mehmet/Prefabs/Enemy.prefab";
        GameObject enemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (enemyPrefab != null)
        {
            // Use reflection to set private fields (for setup only)
            var spawnerType = typeof(EnemySpawner);

            var enemyPrefabField = spawnerType.GetField("enemyPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (enemyPrefabField != null)
                enemyPrefabField.SetValue(spawner, enemyPrefab);

            var playerTransformField = spawnerType.GetField("playerTransform",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (playerTransformField != null && player != null)
                playerTransformField.SetValue(spawner, player.transform);

            var poolSizeField = spawnerType.GetField("initialPoolSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolSizeField != null)
                poolSizeField.SetValue(spawner, poolSize);

            var baseEnemiesField = spawnerType.GetField("baseEnemiesPerWave",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (baseEnemiesField != null)
                baseEnemiesField.SetValue(spawner, baseEnemiesPerWave);

            Debug.Log("[Setup Tool] EnemySpawner created and configured!");
        }
        else
        {
            Debug.LogWarning("[Setup Tool] Enemy prefab not found! Create it first with option 2.");
        }

        spawnerReference = spawner;
        UnityEditor.Selection.activeGameObject = spawnerObj;
    }

    [ContextMenu("4. Create A* Pathfinding")]
    public void CreateAStarPathfinding()
    {
        Debug.Log("[Setup Tool] Creating A* Pathfinding System...");

        // Check if already exists
        AStarPathfinding existingAStar = FindFirstObjectByType<AStarPathfinding>();
        if (existingAStar != null)
        {
            Debug.LogWarning("[Setup Tool] A* Pathfinding already exists in scene!");
            UnityEditor.Selection.activeGameObject = existingAStar.gameObject;
            return;
        }

        // Create A* GameObject
        GameObject astarObj = new GameObject("A_Star_Pathfinding");
        AStarPathfinding astar = astarObj.AddComponent<AStarPathfinding>();

        Debug.Log("[Setup Tool] A* Pathfinding created! Configure grid settings in Inspector.");
        UnityEditor.Selection.activeGameObject = astarObj;
    }

    [ContextMenu("5. Validate Setup")]
    public void ValidateSetup()
    {
        Debug.Log("=== VALIDATING SETUP ===");

        bool allGood = true;

        // Check GameManager
        if (GameManager.Instance == null)
        {
            Debug.LogError("✗ GameManager not found in scene!");
            allGood = false;
        }
        else
        {
            Debug.Log("✓ GameManager found");
        }

        // Check Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("✗ Player not found! Tag an object as 'Player'");
            allGood = false;
        }
        else
        {
            Debug.Log($"✓ Player found: {player.name}");
        }

        // Check Enemy Prefab
        string prefabPath = "Assets/Game/Scenes/Test_Scenes/Mehmet/Prefabs/Enemy.prefab";
        GameObject enemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (enemyPrefab == null)
        {
            Debug.LogError("✗ Enemy prefab not found!");
            allGood = false;
        }
        else
        {
            Debug.Log($"✓ Enemy prefab found: {prefabPath}");
        }

        // Check EnemySpawner
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            Debug.LogError("✗ EnemySpawner not found in scene!");
            allGood = false;
        }
        else
        {
            Debug.Log("✓ EnemySpawner found");
        }

        // Check Layers
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer == -1)
        {
            Debug.LogError("✗ 'Enemy' layer not found! Create it in Project Settings → Tags and Layers");
            allGood = false;
        }
        else
        {
            Debug.Log($"✓ Enemy layer found (Layer {enemyLayer})");
        }

        if (allGood)
        {
            Debug.Log("=== ✓ SETUP VALIDATION PASSED ===");
            Debug.Log("You can now press Play and use F1-F4 to test!");
        }
        else
        {
            Debug.LogWarning("=== ✗ SETUP VALIDATION FAILED ===");
            Debug.LogWarning("Fix the errors above and try again.");
        }
    }
#endif
}
