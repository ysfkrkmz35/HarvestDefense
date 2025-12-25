using UnityEngine;
using System.Collections.Generic;

public class SpiderSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject spiderPrefab;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxSpiders = 10;
    [SerializeField] private float spawnRadius = 10f;
    
    [Header("Spawn Area")]
    [SerializeField] private Vector2 spawnCenter = new Vector2(-30f, -30f);

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    
    private float spawnTimer;
    private List<GameObject> activeSpiders = new List<GameObject>();
    
    void Start()
    {
        spawnTimer = 1f;
        if(debugLogs) Debug.Log("[SpiderSpawner] Started");
    }
    
    void Update()
    {
        activeSpiders.RemoveAll(s => s == null);
        spawnTimer -= Time.deltaTime;
        
        if (spawnTimer <= 0 && activeSpiders.Count < maxSpiders)
        {
            SpawnSpider();
            spawnTimer = spawnInterval;
        }
    }
    
    void SpawnSpider()
    {
        if (spiderPrefab == null) 
        { 
            if(debugLogs) Debug.LogError("[SpiderSpawner] No prefab assigned!"); 
            return; 
        }
        
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        // Ensure strict Z=0 to prevent spawning behind map
        Vector3 pos = new Vector3(spawnCenter.x + offset.x, spawnCenter.y + offset.y, 0f);
        
        GameObject spider = Instantiate(spiderPrefab, pos, Quaternion.identity);
        
        // Strict validation: Ensure we spawned what we expected
        // Note: Instantiate appends "(Clone)" to the name.
        if (!spider.name.StartsWith(spiderPrefab.name))
        {
             if(debugLogs) Debug.LogError($"[SpiderSpawner] CRITICAL: Spawned object '{spider.name}' does not match prefab '{spiderPrefab.name}'! Destroying.");
             Destroy(spider);
             return;
        }

        // Hard Guard: Must have SpiderTargeting component to be considered a valid enemy spawn
        // If the prefab doesn't have it, we add it. If it fails to add (unlikely), we destroy.
        if (spider.GetComponent<SpiderTargeting>() == null)
        {
            spider.AddComponent<SpiderTargeting>();
        }
        
        // Final sanity check
        if (spider.GetComponent<SpiderTargeting>() == null)
        {
            if(debugLogs) Debug.LogError($"[SpiderSpawner] CRITICAL: Failed to add SpiderTargeting to '{spider.name}'. Destroying.");
            Destroy(spider);
            return;
        }

        // Ensure clear hierarchy
        spider.transform.SetParent(null);
        
        // Ensure spider has targeting component
        if (spider.GetComponent<SpiderTargeting>() == null)
            spider.AddComponent<SpiderTargeting>();
        
        // Disable old AI scripts
        var patrol = spider.GetComponent<EnemyPatrol>();
        if (patrol) patrol.enabled = false;
        var ai = spider.GetComponent<SpiderAI>();
        if (ai) ai.enabled = false;
        
        activeSpiders.Add(spider);
        if(debugLogs) Debug.Log("[SpiderSpawner] Spawned " + spider.name + ". Count: " + activeSpiders.Count + "/" + maxSpiders);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(spawnCenter.x, spawnCenter.y, 0), spawnRadius);
    }
}