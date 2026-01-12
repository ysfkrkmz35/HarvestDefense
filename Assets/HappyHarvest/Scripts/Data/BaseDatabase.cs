using System.Collections.Generic;
using UnityEngine;

namespace HappyHarvest
{
    public interface IDatabaseEntry
    {
        string Key { get; }
    }
    
    /// <summary>
    /// This is a base class that allow to define a Database that will link a name/string id to a given object.
    /// Useful for thing like linking item to their id so we can retrieve an item by its id (e.g. when reading save).
    /// See ItemDatabase and CropDatabase for sample of how those are created.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseDatabase<T> : ScriptableObject where T: class, IDatabaseEntry
    {
        [SerializeReference]
        public List<T> Entries; 

        private Dictionary<string, T> m_LookupDictionnary;

        public void Init()
        {
            m_LookupDictionnary = new Dictionary<string, T>();

            Debug.Log($"[BaseDatabase] 🆕 Initializing {this.name} with {Entries?.Count ?? 0} entries");

            //rebuild the lookup
            if (Entries == null) return;
            
            foreach (var entry in Entries)
            {
                if (entry == null)
                {
                    continue;
                }
                
                //TryAdd as there seems to be case where entries are duplicated. My guess is when drag and dropping, it
                //will first duplicate an entry, which trigger a deserialize THEN assign the new entry, which led to
                //error.
                if (!m_LookupDictionnary.TryAdd(entry.Key, entry))
                {
                    Debug.LogWarning($"[BaseDatabase] ⚠️ Duplicate key found: {entry.Key} in {this.name}");
                }
            }
            
            // Debug: Print all keys
            string keys = string.Join(", ", m_LookupDictionnary.Keys);
            Debug.Log($"[BaseDatabase] ✅ Initialized {this.name} with {m_LookupDictionnary.Count} lookup entries. Keys: [{keys}]");
        }
        
        public T GetFromID(string uniqueID)
        {
            if (string.IsNullOrEmpty(uniqueID)) return null;

            if (m_LookupDictionnary == null)
            {
                Debug.LogError($"[BaseDatabase] ❌ Database {this.name} not initialized! Call Init() first.");
                return null;
            }

            if (m_LookupDictionnary.TryGetValue(uniqueID, out var entry))
            {
                return entry;
            }

            Debug.LogWarning($"[BaseDatabase] ⚠️ Entry not found for ID: '{uniqueID}' in {this.name}");
            // Debug dump keys
            // Debug.Log($"[BaseDatabase] Available keys: {string.Join(", ", m_LookupDictionnary.Keys)}");

            return null;
        }
    }
}