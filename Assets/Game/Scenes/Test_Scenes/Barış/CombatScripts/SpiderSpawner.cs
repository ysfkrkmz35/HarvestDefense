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
    
    private float spawnTimer;
    private List<GameObject> activeSpiders = new List<GameObject>();
    
    void Start()
    {
        spawnTimer = 1f;
        Debug.Log("[SpiderSpawner] Started");
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
        if (spiderPrefab == null) { Debug.LogError("[SpiderSpawner] No prefab!"); return; }
        
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = new Vector3(spawnCenter.x + offset.x, spawnCenter.y + offset.y, 0);
        
        GameObject spider = Instantiate(spiderPrefab, pos, Quaternion.identity);
        
        // Ensure spider has targeting component
        if (spider.GetComponent<SpiderTargeting>() == null)
            spider.AddComponent<SpiderTargeting>();
        
        // Disable old AI scripts
        var patrol = spider.GetComponent<EnemyPatrol>();
        if (patrol) patrol.enabled = false;
        var ai = spider.GetComponent<SpiderAI>();
        if (ai) ai.enabled = false;
        
        activeSpiders.Add(spider);
        Debug.Log("[SpiderSpawner] Spawned. Count: " + activeSpiders.Count + "/" + maxSpiders);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(spawnCenter.x, spawnCenter.y, 0), spawnRadius);
    }
}