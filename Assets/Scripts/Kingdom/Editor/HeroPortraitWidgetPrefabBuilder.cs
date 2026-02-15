#if UNITY_EDITOR
using System.IO;
using Kingdom.App;
using UnityEditor;
using UnityEngine;

namespace Kingdom.Editor
{
    public static class HeroPortraitWidgetPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Resources/UI/Widgets";
        private const string PrefabPath = PrefabFolder + "/HeroPortraitWidget.prefab";

        [MenuItem("Tools/Kingdom/Build HeroPortraitWidget Prefab")]
        public static void Build()
        {
            if (!Directory.Exists(PrefabFolder))
            {
                Directory.CreateDirectory(PrefabFolder);
            }

            var root = new GameObject("HeroPortraitWidget", typeof(RectTransform), typeof(HeroPortraitWidget));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(84f, 84f);

            var widget = root.GetComponent<HeroPortraitWidget>();
            widget.EnsureRuntimeDefaults();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log("[HeroPortraitWidgetPrefabBuilder] Prefab built: " + PrefabPath);
        }
    }
}
#endif
