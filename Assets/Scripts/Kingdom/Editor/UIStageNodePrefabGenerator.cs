using Kingdom.WorldMap;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.Editor
{
    /// <summary>
    /// UIStageNode 기본 프리팹 생성 도구입니다.
    /// </summary>
    public static class UIStageNodePrefabGenerator
    {
        private const string FolderPath = "Assets/Resources/UI/Components/WorldMap";
        private const string PrefabPath = FolderPath + "/UIStageNode.prefab";

        [MenuItem("Kingdom/WorldMap/Create UIStageNode Prefab")]
        public static void CreatePrefab()
        {
            EnsureFolder(FolderPath);

            GameObject root = new GameObject("UIStageNode", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIStageNode));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(128f, 128f);
            rootRect.anchoredPosition = Vector2.zero;

            Image rootImage = root.GetComponent<Image>();
            Sprite defaultStageSprite = Resources.Load<Sprite>("UI/Sprites/WorldMap/Icon_Stage");
            rootImage.sprite = defaultStageSprite;
            rootImage.type = Image.Type.Simple;
            rootImage.preserveAspect = true;
            rootImage.color = Color.white;

            CreateStageLabel(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WorldMap] UIStageNode prefab created: {PrefabPath}");
        }

        private static void CreateStageLabel(Transform parent)
        {
            GameObject labelGo = new GameObject("lblStage", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(parent, false);

            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, 24f);
            labelRect.sizeDelta = new Vector2(180f, 36f);

            TextMeshProUGUI text = labelGo.GetComponent<TextMeshProUGUI>();
            text.text = "STAGE 1";
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = false;
            text.color = Color.white;
        }

        private static void EnsureFolder(string fullPath)
        {
            string[] parts = fullPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
