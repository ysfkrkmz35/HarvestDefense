using UnityEngine;
using UnityEditor;

namespace HarvestDefense.Editor
{
    public class StoryInstanceCreator
    {
        [MenuItem("Tools/Harvest Defense/FORCE CREATE STORY DATA", false, 0)]
        public static void CreateStoryDataAsset()
        {
            StoryData asset = ScriptableObject.CreateInstance<StoryData>();

            string path = "Assets/Intro_Hikayesi.asset";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            Debug.Log($"<b>[HARVEST DEFENSE]</b> Yeni Story Data oluşturuldu: {path}");
        }
    }
}
