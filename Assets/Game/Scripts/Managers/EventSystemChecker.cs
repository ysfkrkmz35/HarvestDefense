using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class EventSystemChecker : MonoBehaviour
{
    // Auto-run this check whenever any scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeGlobalCheck()
    {
        // Start a coroutine on a temporary object if valid
        var checker = new GameObject("GlobalEventSystemChecker");
        DontDestroyOnLoad(checker);
        checker.AddComponent<EventSystemChecker>();
    }

    void Awake()
    {
        CheckAndFixEventSystems();
    }

    void OnEnable()
    {
        StartCoroutine(PeriodicCheck());
    }

    public static void CheckAndFixEventSystems()
    {
        EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (systems.Length > 1)
        {
            Debug.LogWarning($"[EventSystemChecker] Found {systems.Length} EventSystems. Cleaning up duplicates...");
            
            // Priority 1: Smart Check - keep the one tagged "GameController" or "Main"
            // Priority 2: Keep the "current" one
            // Priority 3: Keep the oldest (first in list)

            EventSystem keeper = EventSystem.current;
            
            // If current is null, pick the first one
            if (keeper == null) keeper = systems[0];
            
            foreach (var es in systems)
            {
                if (es != keeper)
                {
                    // Double check equality just in case
                    if (es.gameObject == keeper.gameObject) continue;

                    Debug.LogWarning($"[EventSystemChecker] Destroying duplicate EventSystem on: {es.gameObject.name}");
                    
                    // If the specific EventSystem component is on a GameObject with OTHER important stuff, 
                    // we should only destroy the COMPONENT.
                    // But usually EventSystem is on its own object.
                    
                    if (es.GetComponents<Component>().Length > 3) // Transform + EventSystem + InputModule + maybe one more?
                    {
                         // Safer to destroy just the component if the object looks busy
                         Destroy(es);
                         var inputModule = es.GetComponent<BaseInputModule>();
                         if(inputModule) Destroy(inputModule);
                    }
                    else
                    {
                        // Clean destroy of the object
                        Destroy(es.gameObject);
                    }
                }
            }
        }
    }

    IEnumerator PeriodicCheck()
    {
        WaitForSeconds wait = new WaitForSeconds(1f);
        while (true)
        {
            yield return wait;
            // Lightweight check
            if (EventSystem.current == null || FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length > 1)
            {
                CheckAndFixEventSystems();
            }
        }
    }
}
