using UnityEngine;
using UnityEditor;

namespace HarvestDefense.Editor
{
    public class StoryEditorTools : UnityEditor.Editor
    {
        [MenuItem("Tools/Harvest Defense/Create Story Trigger", false, 10)]
        public static void CreateStoryTrigger()
        {
            // 1. Trigger Objesi Oluştur
            GameObject triggerObj = new GameObject("StoryTrigger");
            
            // 2. Bileşenleri Ekle
            BoxCollider2D col = triggerObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(5f, 5f); // Varsayılan boyut
            
            triggerObj.AddComponent<StoryTrigger>();

            // 3. Sahne Görünümünde Seç
            Selection.activeGameObject = triggerObj;
            SceneView.lastActiveSceneView.FrameSelected();

            // 4. Bilgi Ver
            Debug.Log("Story Trigger oluşturuldu! 'Story Data' dosyanızı sürüklemeyi unutmayın. 🎬");
        }

        [MenuItem("Tools/Harvest Defense/Create Story Manager", false, 11)]
        public static void CreateStoryManager()
        {
            StoryCutsceneManager existing = FindFirstObjectByType<StoryCutsceneManager>();
            if (existing != null)
            {
                Debug.Log("Zaten sahnede bir StoryCutsceneManager var.");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            GameObject managerObj = new GameObject("StoryCutsceneManager");
            managerObj.AddComponent<StoryCutsceneManager>();
            
            Selection.activeGameObject = managerObj;
            Debug.Log("StoryCutsceneManager oluşturuldu.");
        }
    }
}
