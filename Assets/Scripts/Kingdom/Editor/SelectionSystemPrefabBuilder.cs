using Kingdom.Game.UI;
using UnityEditor;
using UnityEngine;

namespace Kingdom.Editor
{
    /// <summary>
    /// Builds the runtime SelectionSystem prefab used by GameScene bootstrap.
    /// </summary>
    public static class SelectionSystemPrefabBuilder
    {
        private const string PrefabPath = "Assets/Resources/UI/SelectionSystem.prefab";
        private const string CircleSpritePath = "UI/Sprites/Common/SelectionCircle";

        [MenuItem("Tools/Kingdom/Build SelectionSystem Prefab")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");

            GameObject root = new GameObject("SelectionSystem", typeof(SelectionController));

            GameObject circle = new GameObject("SelectionCircleVisual", typeof(SpriteRenderer), typeof(SelectionCircleVisual));
            circle.transform.SetParent(root.transform, false);
            circle.transform.localPosition = Vector3.zero;

            SelectionController controller = root.GetComponent<SelectionController>();
            SelectionCircleVisual visual = circle.GetComponent<SelectionCircleVisual>();
            SpriteRenderer renderer = circle.GetComponent<SpriteRenderer>();

            Sprite circleSprite = Resources.Load<Sprite>(CircleSpritePath);
            if (circleSprite != null)
            {
                renderer.sprite = circleSprite;
            }

            renderer.sortingOrder = 30;

            SerializedObject controllerSo = new SerializedObject(controller);
            SerializedProperty circleVisualProp = controllerSo.FindProperty("_circleVisual");
            if (circleVisualProp != null)
            {
                circleVisualProp.objectReferenceValue = visual;
            }

            SerializedProperty clickRadiusProp = controllerSo.FindProperty("_clickRadius");
            if (clickRadiusProp != null)
            {
                clickRadiusProp.floatValue = 0.5f;
            }

            SerializedProperty selectionLayerProp = controllerSo.FindProperty("_selectionLayer");
            if (selectionLayerProp != null)
            {
                selectionLayerProp.intValue = 0;
            }

            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject visualSo = new SerializedObject(visual);
            SerializedProperty circleSpriteProp = visualSo.FindProperty("_circleSprite");
            if (circleSpriteProp != null)
            {
                circleSpriteProp.objectReferenceValue = circleSprite;
            }

            visualSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SelectionSystemPrefabBuilder] Prefab generated: {PrefabPath}");
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
