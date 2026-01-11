using UnityEngine;
using UnityEditor;
using YusufTest;
using System.Collections.Generic;
using HappyHarvest;

/// <summary>
/// Yusuf Test 4 sahnesi için tek tıkla düşman sistemi kurulum aracı
/// Menu: Tools > Yusuf Test 4 > Setup Enemy System
/// </summary>
public class EnemySystemSetup : EditorWindow
{
    private List<GameObject> enemyPrefabs = new List<GameObject>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Yusuf Test 4/Setup Enemy System")]
    public static void ShowWindow()
    {
        GetWindow<EnemySystemSetup>("Enemy System Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Yusuf Test 4 - Düşman Sistemi Kurulumu", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Bu araç sahneye otomatik olarak şunları ekler:\n" +
            "1. GameManager (eğer yoksa)\n" +
            "2. Enemy Spawner (sağladığınız prefab'larla)\n\n" +
            "UYARI: \n" +
            "- Player objesi 'Player' tag'ine sahip olmalı!\n" +
            "- Enemy prefab'lar SimpleEnemyAI ve EnemyHealth componentlerine sahip olmalı!",
            MessageType.Info);

        GUILayout.Space(10);

        // Enemy Prefab Listesi
        GUILayout.Label("Enemy Prefab'ları (Birden fazla ekleyebilirsiniz)", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Birden fazla prefab eklerseniz, spawn sırasında RASTGELE seçilir!\n" +
            "En az 1 prefab eklemelisiniz.",
            MessageType.Warning);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        // Mevcut prefab listesi
        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            enemyPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                $"Enemy Prefab {i + 1}",
                enemyPrefabs[i],
                typeof(GameObject),
                false);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                enemyPrefabs.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // Yeni prefab ekleme butonu
        if (GUILayout.Button("+ Yeni Enemy Prefab Ekle"))
        {
            enemyPrefabs.Add(null);
        }

        GUILayout.Space(20);

        // Setup butonu - prefab yoksa disabled
        GUI.enabled = enemyPrefabs.Count > 0 && enemyPrefabs.Exists(p => p != null);

        if (GUILayout.Button("🚀 SETUP - Sistemi Kur", GUILayout.Height(40)))
        {
            SetupEnemySystem();
        }

        GUI.enabled = true;

        GUILayout.Space(10);

        if (GUILayout.Button("🗑️ Sistemi Kaldır (Temizle)", GUILayout.Height(30)))
        {
            CleanupEnemySystem();
        }
    }

    void SetupEnemySystem()
    {
        // Null prefab'ları temizle
        enemyPrefabs.RemoveAll(p => p == null);

        if (enemyPrefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("Hata", "En az 1 enemy prefab eklemelisiniz!", "Tamam");
            return;
        }

        // Prefab'ları kontrol et
        if (!ValidateEnemyPrefabs())
        {
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Düşman Sistemi Kurulumu",
            $"Sahneye {enemyPrefabs.Count} farklı enemy tipi ile düşman sistemi kurulacak.\n\n" +
            "Devam edilsin mi?",
            "Evet, Kur",
            "İptal"))
        {
            return;
        }

        try
        {
            // 1. GameManager kontrolü ve ekleme
            SetupGameManager();

            // 2. Enemy Spawner oluşturma (artık prefab oluşturmuyor)
            SetupEnemySpawner();

            // 3. Player kontrolü
            CheckPlayer();

            EditorUtility.DisplayDialog(
                "Başarılı!",
                $"Düşman sistemi başarıyla kuruldu!\n\n" +
                $"✅ {enemyPrefabs.Count} enemy prefab eklendi\n" +
                $"✅ Spawner yapılandırıldı\n\n" +
                "Gece olduğunda düşmanlar otomatik spawn olacak!",
                "Tamam");

            Debug.Log($"<color=green>[EnemySystemSetup] ✅ Düşman sistemi başarıyla kuruldu! ({enemyPrefabs.Count} enemy tipi)</color>");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Hata", "Kurulum sırasında hata oluştu:\n" + e.Message, "Tamam");
            Debug.LogError($"[EnemySystemSetup] Hata: {e.Message}");
        }
    }

    bool ValidateEnemyPrefabs()
    {
        List<string> errors = new List<string>();

        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            GameObject prefab = enemyPrefabs[i];

            // SimpleEnemyAI kontrolü
            if (prefab.GetComponent<SimpleEnemyAI>() == null)
            {
                errors.Add($"Prefab {i + 1}: SimpleEnemyAI component'i eksik!");
            }

            // EnemyHealth kontrolü
            if (prefab.GetComponent<EnemyHealth>() == null)
            {
                errors.Add($"Prefab {i + 1}: EnemyHealth component'i eksik!");
            }

            // Rigidbody2D kontrolü
            if (prefab.GetComponent<Rigidbody2D>() == null)
            {
                errors.Add($"Prefab {i + 1}: Rigidbody2D component'i eksik!");
            }

            // Collider2D kontrolü
            if (prefab.GetComponent<Collider2D>() == null)
            {
                errors.Add($"Prefab {i + 1}: Collider2D component'i eksik!");
            }
        }

        if (errors.Count > 0)
        {
            string errorMessage = "Enemy prefab'larda eksikler var:\n\n" + string.Join("\n", errors);
            EditorUtility.DisplayDialog("Prefab Hataları", errorMessage, "Tamam");
            return false;
        }

        return true;
    }

    void SetupGameManager()
    {
        // Sahnede GameManager var mı kontrol et
        GameManager existingGM = FindFirstObjectByType<GameManager>();

        if (existingGM != null)
        {
            Debug.Log("[EnemySystemSetup] GameManager zaten mevcut, atlaniyor.");
            return;
        }

        // GameManager oluştur
        GameObject gmObject = new GameObject("GameManager");
        gmObject.AddComponent<GameManager>();

        Debug.Log("[EnemySystemSetup] ✅ GameManager oluşturuldu");
        Debug.Log("[EnemySystemSetup] 💡 Gece/Gündüz geçişi oyun süresine göre otomatik olacak!");
    }


    void SetupEnemySpawner()
    {
        // Sahnede spawner var mı kontrol et
        SimpleEnemySpawner existingSpawner = FindFirstObjectByType<SimpleEnemySpawner>();

        if (existingSpawner != null)
        {
            Debug.LogWarning("[EnemySystemSetup] EnemySpawner zaten mevcut, yenisiyle değiştiriliyor...");
            DestroyImmediate(existingSpawner.gameObject);
        }

        // Spawner oluştur
        GameObject spawnerObject = new GameObject("EnemySpawner");
        SimpleEnemySpawner spawner = spawnerObject.AddComponent<SimpleEnemySpawner>();

        // SerializedObject ile güvenli atama
        SerializedObject so = new SerializedObject(spawner);

        try
        {
            // Enemy prefab listesini temizle ve ekle
            SerializedProperty enemyPrefabsProp = so.FindProperty("enemyPrefabs");

            if (enemyPrefabsProp == null)
            {
                Debug.LogError("[EnemySystemSetup] ❌ 'enemyPrefabs' property bulunamadı! SimpleEnemySpawner script'inde değişiklik yapıldı mı?");
                return;
            }

            // Önce array'i temizle
            enemyPrefabsProp.arraySize = 0;

            // Yeni elemanları ekle
            for (int i = 0; i < enemyPrefabs.Count; i++)
            {
                enemyPrefabsProp.arraySize++;
                SerializedProperty element = enemyPrefabsProp.GetArrayElementAtIndex(i);
                element.objectReferenceValue = enemyPrefabs[i];
            }

            // Diğer ayarlar - her birini kontrol ederek ata
            SerializedProperty poolSizeProp = so.FindProperty("poolSizePerPrefab");
            if (poolSizeProp != null) poolSizeProp.intValue = 20;

            SerializedProperty minEnemiesProp = so.FindProperty("minEnemiesPerNight");
            if (minEnemiesProp != null) minEnemiesProp.intValue = 5;

            SerializedProperty maxEnemiesProp = so.FindProperty("maxEnemiesPerNight");
            if (maxEnemiesProp != null) maxEnemiesProp.intValue = 15;

            SerializedProperty minIntervalProp = so.FindProperty("minSpawnInterval");
            if (minIntervalProp != null) minIntervalProp.floatValue = 0.2f;

            SerializedProperty maxIntervalProp = so.FindProperty("maxSpawnInterval");
            if (maxIntervalProp != null) maxIntervalProp.floatValue = 0.8f;

            SerializedProperty intervalIncreaseProp = so.FindProperty("intervalIncrease");
            if (intervalIncreaseProp != null) intervalIncreaseProp.floatValue = 0.05f;

            SerializedProperty minDistanceProp = so.FindProperty("minDistanceFromPlayer");
            if (minDistanceProp != null) minDistanceProp.floatValue = 10f;

            SerializedProperty maxDistanceProp = so.FindProperty("maxDistanceFromPlayer");
            if (maxDistanceProp != null) maxDistanceProp.floatValue = 20f;

            SerializedProperty safeRadiusProp = so.FindProperty("spawnSafeRadius");
            if (safeRadiusProp != null) safeRadiusProp.floatValue = 1f;

            // Değişiklikleri uygula
            so.ApplyModifiedProperties();

            Debug.Log($"[EnemySystemSetup] ✅ EnemySpawner oluşturuldu ({enemyPrefabs.Count} enemy tipi eklendi)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemySystemSetup] ❌ Spawner setup hatası: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }

    void CheckPlayer()
    {
        try
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                Debug.LogWarning("[EnemySystemSetup] ⚠️ UYARI: Sahnede 'Player' tag'li obje bulunamadı!");
                Debug.LogWarning("[EnemySystemSetup] Player objenizin tag'ini 'Player' olarak ayarlayın!");
            }
            else
            {
                Debug.Log($"[EnemySystemSetup] ✅ Player bulundu: {player.name}");

                // IDamageable kontrolü
                IDamageable damageable = player.GetComponent<IDamageable>();
                if (damageable == null)
                {
                    Debug.LogWarning("[EnemySystemSetup] ⚠️ UYARI: Player'da IDamageable component yok!");

                    // Otomatik olarak SimplePlayerHealth ekle
                    if (EditorUtility.DisplayDialog(
                        "IDamageable Eksik",
                        "Player'da IDamageable component bulunamadı.\n\n" +
                        "Test için SimplePlayerHealth eklemek ister misiniz?\n" +
                        "(Düşmanlar hasar verebilmek için gerekli)",
                        "Evet, Ekle",
                        "Hayır"))
                    {
                        player.AddComponent<YusufTest.SimplePlayerHealth>();
                        Debug.Log("[EnemySystemSetup] ✅ SimplePlayerHealth eklendi!");
                    }
                    else
                    {
                        Debug.LogWarning("[EnemySystemSetup] Düşmanlar hasar veremeyecek!");
                    }
                }
                else
                {
                    Debug.Log("[EnemySystemSetup] ✅ Player'da IDamageable var!");
                }
            }
        }
        catch (UnityException)
        {
            // Tag yoksa exception atabilir
            Debug.LogWarning("[EnemySystemSetup] ⚠️ UYARI: 'Player' tag'i bulunamadı. Tags & Layers ayarlarından ekleyin!");
        }
    }

    void CleanupEnemySystem()
    {
        if (!EditorUtility.DisplayDialog(
            "Sistemi Kaldır",
            "Sahnedeki düşman sistemi kaldırılacak (prefab silinmeyecek). Devam edilsin mi?",
            "Evet, Kaldır",
            "İptal"))
        {
            return;
        }

        int removedCount = 0;

        // Spawner'ı kaldır
        SimpleEnemySpawner spawner = FindFirstObjectByType<SimpleEnemySpawner>();
        if (spawner != null)
        {
            DestroyImmediate(spawner.gameObject);
            removedCount++;
            Debug.Log("[EnemySystemSetup] EnemySpawner kaldırıldı");
        }

        // GameManager'ı KALDIRMA (başka sistemler kullanıyor olabilir)
        // Sadece uyarı ver
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            Debug.LogWarning("[EnemySystemSetup] GameManager kaldırılmadı (başka sistemler kullanıyor olabilir)");
        }

        EditorUtility.DisplayDialog(
            "Tamamlandı",
            $"{removedCount} obje kaldırıldı.\n\nPrefab dosyası korundu.",
            "Tamam");
    }
}
