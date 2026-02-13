#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Kingdom.Editor
{
    public static class BuildSettingsSetter
    {
        [MenuItem("Kingdom/Setup/Register Scenes in Build Settings")]
        public static void RegisterScenes()
        {
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/InitScene.unity",
                "Assets/Scenes/TitleScene.unity",
                "Assets/Scenes/WorldMapScene.unity",
                "Assets/Scenes/GameScene.unity"
            };

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

            foreach (var path in scenePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                {
                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            UnityEngine.Debug.Log("[BuildSettingsSetter] Registered 3 scenes to Build Settings.");
        }
    }
}
#endif
