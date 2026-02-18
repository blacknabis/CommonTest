using Kingdom.WorldMap;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.Editor
{
    /// <summary>
    /// UpgradesPopup 프리팹을 생성/갱신하는 에디터 유틸리티.
    /// </summary>
    public static class UpgradesPopupPrefabBuilder
    {
        private const string PrefabPath = "Assets/Resources/UI/WorldMap/UpgradesPopup.prefab";

        [MenuItem("Tools/Kingdom/Build UpgradesPopup Prefab")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");
            EnsureFolder("Assets/Resources/UI/WorldMap");

            GameObject root = new GameObject("UpgradesPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SkillTreeUI));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(980f, 720f);
            rootRect.anchoredPosition = Vector2.zero;

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.14f, 0.97f);
            bg.raycastTarget = true;

            GameObject title = new GameObject("txtTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.SetParent(rootRect, false);
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(420f, 64f);
            titleRect.anchoredPosition = new Vector2(0f, -20f);

            TextMeshProUGUI titleText = title.GetComponent<TextMeshProUGUI>();
            titleText.text = "업그레이드";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 42f;
            titleText.color = Color.white;

            GameObject closeButton = new GameObject("btnClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.SetParent(rootRect, false);
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(88f, 56f);
            closeRect.anchoredPosition = new Vector2(-16f, -16f);

            Image closeImage = closeButton.GetComponent<Image>();
            closeImage.color = new Color(0.6f, 0.2f, 0.2f, 0.95f);

            GameObject closeLabel = new GameObject("txtLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform closeLabelRect = closeLabel.GetComponent<RectTransform>();
            closeLabelRect.SetParent(closeRect, false);
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI closeText = closeLabel.GetComponent<TextMeshProUGUI>();
            closeText.text = "X";
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.fontSize = 30f;
            closeText.color = Color.white;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UpgradesPopupPrefabBuilder] Prefab generated: {PrefabPath}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int index = path.LastIndexOf('/');
            if (index <= 0)
            {
                return;
            }

            string parent = path.Substring(0, index);
            string name = path.Substring(index + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
