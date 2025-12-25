using UnityEngine;
using System;

public class LifecycleTracer : MonoBehaviour
{
    [SerializeField] private bool debugLogs = true;

    private void OnEnable()
    {
        if (debugLogs) Debug.Log($"[LifecycleTracer] {name} OnEnable at {Time.time}");
    }

    private void OnDisable()
    {
        if (debugLogs)
        {
            Debug.Log($"[LifecycleTracer] {name} OnDisable at {Time.time}\nStackTrace: {Environment.StackTrace}");
        }
    }

    private void OnDestroy()
    {
        if (debugLogs)
        {
            Debug.Log($"[LifecycleTracer] {name} OnDestroy at {Time.time}\nStackTrace: {Environment.StackTrace}");
        }
    }
}
