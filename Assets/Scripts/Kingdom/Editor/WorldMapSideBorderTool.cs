using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.Editor
{
    public static class WorldMapSideBorderTool
    {
        private const string WorldMapPrefabPath = "Assets/Resources/UI/WorldMapView.prefab";
        private const string LeftBorderResourcePath = "UI/Sprites/WorldMap/WorldMap_SideBorder_Left";
        private const string RightBorderResourcePath = "UI/Sprites/WorldMap/WorldMap_SideBorder_Right";

        private const float DefaultSideBorderWidth = 280f;
        private const float MinSideBorderWidth = 220f;
        private const float MaxSideBorderWidth = 340f;

        [MenuItem("Kingdom/WorldMap/Setup Side Borders (Structure)")]
        public static void SetupSideBorderStructure()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(WorldMapPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[WorldMap] Prefab not found: {WorldMapPrefabPath}");
                return;
            }

            try
            {
                SetupStructure(prefabRoot.transform);
                SetupViewportAndBindings(prefabRoot.transform);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, WorldMapPrefabPath);
                Debug.Log("[WorldMap] Side border structure setup completed.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void SetupStructure(Transform root)
        {
            Transform background = root.Find("imgBackground");
            Transform sideBorders = root.Find("SideBorders");
            if (sideBorders == null)
            {
                var sideBorderRoot = new GameObject("SideBorders", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                sideBorders = sideBorderRoot.transform;
                sideBorders.SetParent(root, false);
                sideBorders.SetSiblingIndex(1);
            }

            var sideBordersRect = sideBorders.GetComponent<RectTransform>();
            sideBordersRect.anchorMin = Vector2.zero;
            sideBordersRect.anchorMax = Vector2.one;
            sideBordersRect.offsetMin = Vector2.zero;
            sideBordersRect.offsetMax = Vector2.zero;

            var sideImage = sideBorders.GetComponent<Image>();
            sideImage.raycastTarget = false;
            sideImage.color = new Color(0f, 0f, 0f, 0f);

            SetupSideBorder(sideBordersRect, "LeftBorder", true);
            SetupSideBorder(sideBordersRect, "RightBorder", false);

            Transform safeAreaContent = root.Find("SafeAreaContent");
            if (safeAreaContent == null)
            {
                var safeAreaContentGo = new GameObject("SafeAreaContent", typeof(RectTransform));
                safeAreaContent = safeAreaContentGo.transform;
                safeAreaContent.SetParent(root, false);
                safeAreaContent.SetAsLastSibling();
            }

            var safeAreaRect = safeAreaContent.GetComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child == null || child == safeAreaContent)
                {
                    continue;
                }

                if (child == sideBorders || child == background)
                {
                    continue;
                }

                child.SetParent(safeAreaContent, false);
            }
        }

        private static void SetupSideBorder(RectTransform sideRoot, string childName, bool isLeft)
        {
            Transform border = sideRoot.Find(childName);
            if (border == null)
            {
                var borderGo = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                border = borderGo.transform;
                border.SetParent(sideRoot, false);
            }

            var borderRect = border.GetComponent<RectTransform>();
            var borderImage = border.GetComponent<Image>();
            borderImage.raycastTarget = false;

            borderRect.anchorMin = isLeft ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            borderRect.anchorMax = isLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            borderRect.pivot = isLeft ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(Mathf.Clamp(DefaultSideBorderWidth, MinSideBorderWidth, MaxSideBorderWidth), 0f);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(isLeft
                ? ToAssetPath(LeftBorderResourcePath)
                : ToAssetPath(RightBorderResourcePath));

            if (sprite != null)
            {
                borderImage.sprite = sprite;
                borderImage.type = Image.Type.Sliced;
                borderImage.preserveAspect = true;
                borderImage.color = Color.white;
            }
            else
            {
                borderImage.sprite = null;
                borderImage.color = Color.clear;
            }
        }

        private static void SetupViewportAndBindings(Transform root)
        {
            var view = root.GetComponent<Kingdom.App.WorldMapView>();
            if (view == null)
            {
                return;
            }

            Transform sideBorders = root.Find("SideBorders");
            Transform safeAreaContent = root.Find("SafeAreaContent");
            if (sideBorders == null || safeAreaContent == null)
            {
                return;
            }

            SerializedObject serializedView = new SerializedObject(view);
            var sideBordersProp = serializedView.FindProperty("sideBordersRoot");
            var safeAreaProp = serializedView.FindProperty("safeAreaContent");
            var viewportLayoutProp = serializedView.FindProperty("viewportLayout");
            if (sideBordersProp == null || safeAreaProp == null)
            {
                Debug.LogWarning("[WorldMap] SerializedProperty not found. Skipped assigning runtime refs.");
                return;
            }

            sideBordersProp.objectReferenceValue = sideBorders.GetComponent<RectTransform>();
            safeAreaProp.objectReferenceValue = safeAreaContent.GetComponent<RectTransform>();

            Kingdom.App.ViewportSafeAreaLayout viewport = root.GetComponent<Kingdom.App.ViewportSafeAreaLayout>();
            if (viewportLayoutProp != null)
            {
                if (viewport == null)
                {
                    viewport = root.GetComponent<Kingdom.App.ViewportSafeAreaLayout>();
                }

                if (viewport == null)
                {
                    viewport = root.gameObject.AddComponent<Kingdom.App.ViewportSafeAreaLayout>();
                }

                viewportLayoutProp.objectReferenceValue = viewport;

                // SafeArea 적용 타겟을 루트가 아닌 SafeAreaContent로 고정한다.
                SerializedObject serializedViewport = new SerializedObject(viewport);
                var viewportSafeAreaTargetProp = serializedViewport.FindProperty("safeAreaTarget");
                if (viewportSafeAreaTargetProp != null)
                {
                    viewportSafeAreaTargetProp.objectReferenceValue = safeAreaContent.GetComponent<RectTransform>();
                }

                var bottomLiftTargetsProp = serializedViewport.FindProperty("bottomLiftTargets");
                if (bottomLiftTargetsProp != null)
                {
                    var bottomBar = root.Find("SafeAreaContent/BottomBar") as RectTransform;
                    if (bottomBar == null)
                    {
                        bottomBar = root.Find("BottomBar") as RectTransform;
                    }

                    if (bottomBar != null)
                    {
                        bottomLiftTargetsProp.arraySize = 1;
                        bottomLiftTargetsProp.GetArrayElementAtIndex(0).objectReferenceValue = bottomBar;
                    }
                    else
                    {
                        bottomLiftTargetsProp.arraySize = 0;
                    }
                }

                serializedViewport.ApplyModifiedProperties();
                EditorUtility.SetDirty(viewport);
            }

            serializedView.ApplyModifiedProperties();

            EditorUtility.SetDirty(view);
        }

        private static string ToAssetPath(string resourcesPath)
        {
            return $"Assets/Resources/{resourcesPath}.png";
        }
    }
}
