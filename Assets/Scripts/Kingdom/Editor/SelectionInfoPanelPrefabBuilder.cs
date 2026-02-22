using System.IO;
using Common.Extensions;
using Kingdom.Game.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.Editor
{
    /// <summary>
    /// Builds SelectionInfoPanel prefab and removes legacy embedded panel from GameView prefab.
    /// </summary>
    public static class SelectionInfoPanelPrefabBuilder
    {
        private const string PrefabPath = "Assets/Resources/UI/SelectionInfoPanel.prefab";
        private const string GameViewPrefabPath = "Assets/Resources/UI/GameView.prefab";
        private const string BackgroundSpritePath = "UI/Sprites/Common/HpBarBg";
        private const string FillSpritePath = "UI/Sprites/Common/HpBarFill";

        [MenuItem("Tools/Kingdom/Build SelectionInfoPanel Prefab")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");

            GameObject root = CreatePrefabRoot();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            RemoveEmbeddedSelectionInfoPanelFromGameView();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SelectionInfoPanelPrefabBuilder] Prefab generated: {PrefabPath}");
        }

        private static GameObject CreatePrefabRoot()
        {
            Sprite backgroundSprite = ResolveSprite(BackgroundSpritePath);
            Sprite fillSprite = ResolveSprite(FillSpritePath);
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

            GameObject root = new GameObject("SelectionInfoPanel", typeof(RectTransform), typeof(SelectionInfoPanel));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(24f, -208f);
            rootRect.sizeDelta = new Vector2(300f, 92f);

            GameObject panelRoot = new GameObject("PanelRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelRoot.transform.SetParent(root.transform, false);
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            Stretch(panelRect);

            Image panelImage = panelRoot.GetComponent<Image>();
            panelImage.sprite = backgroundSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0f, 0f, 0f, 0.60f);
            panelImage.raycastTarget = false;

            TextMeshProUGUI nameLabel = CreateLabel(
                panelRoot.transform,
                "txtName",
                defaultFont,
                new Vector2(10f, -8f),
                new Vector2(180f, 30f),
                24f,
                TextAlignmentOptions.Left);

            TextMeshProUGUI hpLabel = CreateLabel(
                panelRoot.transform,
                "txtHp",
                defaultFont,
                new Vector2(10f, -42f),
                new Vector2(180f, 24f),
                18f,
                TextAlignmentOptions.Left);

            Slider slider = CreateSlider(panelRoot.transform, backgroundSprite, fillSprite);

            SelectionInfoPanel infoPanel = root.GetComponent<SelectionInfoPanel>();
            SerializedObject infoSo = new SerializedObject(infoPanel);
            SerializedProperty panelRootProp = infoSo.FindProperty("_panelRoot");
            SerializedProperty txtNameProp = infoSo.FindProperty("_txtName");
            SerializedProperty txtHpProp = infoSo.FindProperty("_txtHp");
            SerializedProperty hpSliderProp = infoSo.FindProperty("_hpSlider");

            if (panelRootProp.IsNotNull())
            {
                panelRootProp.objectReferenceValue = panelRoot;
            }

            if (txtNameProp.IsNotNull())
            {
                txtNameProp.objectReferenceValue = nameLabel;
            }

            if (txtHpProp.IsNotNull())
            {
                txtHpProp.objectReferenceValue = hpLabel;
            }

            if (hpSliderProp.IsNotNull())
            {
                hpSliderProp.objectReferenceValue = slider;
            }

            infoSo.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static Slider CreateSlider(Transform parent, Sprite backgroundSprite, Sprite fillSprite)
        {
            GameObject sliderGo = new GameObject("hpSlider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(parent, false);

            RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(1f, 0.5f);
            sliderRect.anchorMax = new Vector2(1f, 0.5f);
            sliderRect.pivot = new Vector2(1f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(-10f, -28f);
            sliderRect.sizeDelta = new Vector2(90f, 16f);

            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(sliderGo.transform, false);
            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            Stretch(bgRect);

            Image bgImage = bgGo.GetComponent<Image>();
            bgImage.sprite = backgroundSprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.18f, 0.18f, 0.18f, 0.85f);
            bgImage.raycastTarget = false;

            GameObject fillAreaGo = new GameObject("FillArea", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            RectTransform fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(2f, 2f);
            fillAreaRect.offsetMax = new Vector2(-2f, -2f);
            fillAreaRect.pivot = new Vector2(0.5f, 0.5f);

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            Stretch(fillRect);

            Image fillImage = fillGo.GetComponent<Image>();
            fillImage.sprite = fillSprite;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = new Color(0.15f, 0.85f, 0.15f, 1f);
            fillImage.raycastTarget = false;

            Slider slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.targetGraphic = bgImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.wholeNumbers = false;

            return slider;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            TMP_FontAsset font,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject labelGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(parent, false);

            RectTransform rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            if (font.IsNotNull())
            {
                label.font = font;
            }

            label.text = string.Empty;
            label.alignment = alignment;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static Sprite ResolveSprite(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite.IsNotNull())
            {
                return sprite;
            }

            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static void RemoveEmbeddedSelectionInfoPanelFromGameView()
        {
            if (!File.Exists(GameViewPrefabPath))
            {
                Debug.LogWarning($"[SelectionInfoPanelPrefabBuilder] GameView prefab not found: {GameViewPrefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(GameViewPrefabPath);
            try
            {
                SelectionInfoPanel[] embeddedPanels = root.GetComponentsInChildren<SelectionInfoPanel>(true);
                int removedCount = 0;
                for (int i = 0; i < embeddedPanels.Length; i++)
                {
                    SelectionInfoPanel panel = embeddedPanels[i];
                    if (panel.IsNull())
                    {
                        continue;
                    }

                    Object.DestroyImmediate(panel.gameObject);
                    removedCount++;
                }

                if (removedCount > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, GameViewPrefabPath);
                    Debug.Log($"[SelectionInfoPanelPrefabBuilder] Removed embedded SelectionInfoPanel from GameView. count={removedCount}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
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
