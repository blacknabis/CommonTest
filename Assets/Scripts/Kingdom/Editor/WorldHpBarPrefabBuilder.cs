using Kingdom.Game.UI;
using UnityEditor;
using UnityEngine;

namespace Kingdom.Editor
{
    /// <summary>
    /// Builds the runtime world-space HP bar prefab used by WorldHpBarManager.
    /// </summary>
    public static class WorldHpBarPrefabBuilder
    {
        private const string PrefabPath = "Assets/Resources/UI/WorldHpBar.prefab";
        private const string BackgroundSpritePath = "UI/Sprites/Common/HpBarBg";
        private const string FillSpritePath = "UI/Sprites/Common/HpBarFill";

        [MenuItem("Tools/Kingdom/Build WorldHpBar Prefab")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");

            GameObject root = new GameObject("WorldHpBar", typeof(WorldHpBar));

            GameObject background = new GameObject("Background", typeof(SpriteRenderer));
            background.transform.SetParent(root.transform, false);

            GameObject fill = new GameObject("Fill", typeof(SpriteRenderer));
            fill.transform.SetParent(root.transform, false);

            SpriteRenderer backgroundRenderer = background.GetComponent<SpriteRenderer>();
            SpriteRenderer fillRenderer = fill.GetComponent<SpriteRenderer>();

            Sprite backgroundSprite = Resources.Load<Sprite>(BackgroundSpritePath);
            if (backgroundSprite != null)
            {
                backgroundRenderer.sprite = backgroundSprite;
            }

            Sprite fillSprite = Resources.Load<Sprite>(FillSpritePath);
            if (fillSprite != null)
            {
                fillRenderer.sprite = fillSprite;
            }

            backgroundRenderer.sortingOrder = 100;
            fillRenderer.sortingOrder = 101;

            WorldHpBar hpBar = root.GetComponent<WorldHpBar>();
            SerializedObject hpBarSo = new SerializedObject(hpBar);

            SerializedProperty fillRendererProp = hpBarSo.FindProperty("_fillRenderer");
            if (fillRendererProp != null)
            {
                fillRendererProp.objectReferenceValue = fillRenderer;
            }

            SerializedProperty offsetProp = hpBarSo.FindProperty("_offset");
            if (offsetProp != null)
            {
                offsetProp.vector3Value = new Vector3(0f, 0.08f, 0f);
            }

            hpBarSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WorldHpBarPrefabBuilder] Prefab generated: {PrefabPath}");
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
