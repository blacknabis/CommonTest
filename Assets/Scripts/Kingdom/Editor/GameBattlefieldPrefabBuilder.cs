#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Kingdom.Game;

namespace Kingdom.EditorTools
{
    public static class GameBattlefieldPrefabBuilder
    {
        private const string FolderPath = "Assets/Resources/Prefabs/Game";
        private const string PrefabPath = FolderPath + "/GameBattlefield.prefab";

        [MenuItem("Kingdom/Game/Build GameBattlefield Prefab")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Prefabs");
            EnsureFolder(FolderPath);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(PrefabPath);
            }

            var root = new GameObject("GameBattlefield");
            try
            {
                var battlefield = root.AddComponent<GameBattlefield>();
                battlefield.EnsureRuntimeDefaults();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[GameBattlefield] Prefab generated: {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AssetDatabase.Refresh();
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
