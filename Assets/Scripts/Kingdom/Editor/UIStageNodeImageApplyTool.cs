using System.Collections.Generic;
using Kingdom.WorldMap;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.Editor
{
    /// <summary>
    /// ComfyUI 생성 이미지를 UIStageNode 프리팹에 연결하는 도구입니다.
    /// </summary>
    public static class UIStageNodeImageApplyTool
    {
        private const string PrefabPath = "Assets/Resources/UI/Components/WorldMap/UIStageNode.prefab";
        private const string SpriteFolder = "Assets/Resources/UI/Sprites/WorldMap";
        private const string HighlightPath = SpriteFolder + "/UIStageNode_SelectedHighlight.png";
        private const string LockPath = SpriteFolder + "/UIStageNode_LockIcon.png";
        private const string DotPath = SpriteFolder + "/UIStageNode_NotificationDot.png";
        private const string StarPath = SpriteFolder + "/UIStageNode_Star.png";

        [MenuItem("Kingdom/WorldMap/Apply UIStageNode Generated Images")]
        public static void Apply()
        {
            EnsureSpriteImportSettings(HighlightPath);
            EnsureSpriteImportSettings(LockPath);
            EnsureSpriteImportSettings(DotPath);
            EnsureSpriteImportSettings(StarPath);
            AssetDatabase.Refresh();

            Sprite highlightSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HighlightPath);
            Sprite lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LockPath);
            Sprite dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DotPath);
            Sprite starSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StarPath);

            if (highlightSprite == null || lockSprite == null || dotSprite == null || starSprite == null)
            {
                Debug.LogError("[UIStageNode] 생성 이미지가 누락되어 프리팹 연결을 중단합니다.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError($"[UIStageNode] 프리팹 로드 실패: {PrefabPath}");
                return;
            }

            try
            {
                UIStageNode node = root.GetComponent<UIStageNode>();
                if (node == null)
                {
                    Debug.LogError("[UIStageNode] UIStageNode 컴포넌트가 없습니다.");
                    return;
                }

                Button button = root.GetComponent<Button>();
                Image iconImage = root.GetComponent<Image>();
                TextMeshProUGUI stageLabel = root.GetComponentInChildren<TextMeshProUGUI>(true);

                GameObject selectedHighlight = EnsureImageObject(root.transform, "SelectedHighlight", highlightSprite, new Vector2(156f, 156f));
                SetRect(selectedHighlight.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(156f, 156f));
                selectedHighlight.SetActive(false);

                GameObject lockIcon = EnsureImageObject(root.transform, "LockIcon", lockSprite, new Vector2(70f, 70f));
                SetRect(lockIcon.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
                lockIcon.SetActive(false);

                GameObject notificationDot = EnsureImageObject(root.transform, "NotificationDot", dotSprite, new Vector2(30f, 30f));
                SetRect(notificationDot.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(30f, 30f));
                notificationDot.SetActive(false);

                GameObject starContainer = EnsureRectObject(root.transform, "StarContainer");
                SetRect(starContainer.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(96f, 24f));

                var stars = new List<Image>();
                for (int i = 0; i < 3; i++)
                {
                    string starName = $"Star{i + 1}";
                    GameObject starGo = EnsureImageObject(starContainer.transform, starName, starSprite, new Vector2(24f, 24f));
                    SetRect(starGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 30f, 0f), new Vector2(24f, 24f));
                    stars.Add(starGo.GetComponent<Image>());
                }

                // 렌더 순서를 맞추기 위해 하이라이트를 가장 먼저, 라벨 루트는 마지막 순서 유지.
                selectedHighlight.transform.SetSiblingIndex(0);

                SerializedObject so = new SerializedObject(node);
                so.FindProperty("button").objectReferenceValue = button;
                so.FindProperty("iconImage").objectReferenceValue = iconImage;
                so.FindProperty("stageNumberText").objectReferenceValue = stageLabel;
                so.FindProperty("lockIcon").objectReferenceValue = lockIcon;
                so.FindProperty("selectedHighlight").objectReferenceValue = selectedHighlight;
                so.FindProperty("notificationDot").objectReferenceValue = notificationDot;

                SerializedProperty starArray = so.FindProperty("starImages");
                starArray.arraySize = stars.Count;
                for (int i = 0; i < stars.Count; i++)
                {
                    starArray.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[UIStageNode] 생성 이미지 연결 완료.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureSpriteImportSettings(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (importer.alphaIsTransparency != true)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private static GameObject EnsureRectObject(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject EnsureImageObject(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            GameObject go = EnsureRectObject(parent, name);
            Image image = go.GetComponent<Image>();
            if (image == null)
            {
                image = go.AddComponent<Image>();
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return go;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }
    }
}
