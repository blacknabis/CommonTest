using Common.UI;
using Kingdom.WorldMap;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.Editor
{
    public static class StageInfoPopupPrefabBuilder
    {
        private const string PrefabDir = "Assets/Resources/UI";
        private const string PrefabPath = PrefabDir + "/StageInfoPopup.prefab";
        private const string PanelSpriteResourcePath = "UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Panel";
        private const string ButtonSpriteResourcePath = "UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Button";
        private const string StartButtonSpriteResourcePath = "UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Button_Start";
        private const string CloseButtonSpriteResourcePath = "UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Button_Close";
        private const string LegacyPanelSpriteResourcePath = "UI/Sprites/WorldMap/ico_stage_bg";
        private const string LegacyButtonSpriteResourcePath = "UI/Sprites/WorldMap/ico_text_bg";
        private const string PopupClickAudioResourcePath = "Audio/WorldMap/WorldMap_Click_UI";

        [MenuItem("Tools/Kingdom/Build StageInfoPopup Prefab")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(PrefabDir);

            var root = new GameObject("StageInfoPopup", typeof(RectTransform), typeof(CanvasGroup), typeof(StageInfoPopup));
            var rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);

            var panel = CreateUIObject("Panel", root.transform, typeof(Image));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 560f);
            panelRect.anchoredPosition = Vector2.zero;

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.12f, 0.12f, 0.12f, 0.98f);
            Sprite panelSprite = Resources.Load<Sprite>(PanelSpriteResourcePath);
            if (panelSprite == null)
            {
                panelSprite = Resources.Load<Sprite>(LegacyPanelSpriteResourcePath);
            }

            Sprite buttonSprite = Resources.Load<Sprite>(ButtonSpriteResourcePath);
            if (buttonSprite == null)
            {
                buttonSprite = Resources.Load<Sprite>(LegacyButtonSpriteResourcePath);
            }
            Sprite startButtonSprite = Resources.Load<Sprite>(StartButtonSpriteResourcePath);
            if (startButtonSprite == null)
            {
                startButtonSprite = buttonSprite;
            }
            Sprite closeButtonSprite = Resources.Load<Sprite>(CloseButtonSpriteResourcePath);
            if (closeButtonSprite == null)
            {
                closeButtonSprite = buttonSprite;
            }

            ApplyImageSkin(panelImage, panelSprite, Image.Type.Sliced, Color.white);
            AudioClip popupClickClip = Resources.Load<AudioClip>(PopupClickAudioResourcePath);

            var txtStageName = CreateLabel("txtStageName", panel.transform, new Vector2(0f, 218f), new Vector2(660f, 64f), 42, FontStyles.Bold, "STAGE 1 (#1)");
            var txtStarCount = CreateLabel("txtStarCount", panel.transform, new Vector2(-230f, 144f), new Vector2(280f, 44f), 30, FontStyles.Bold, "별 0/3");
            var txtRecommendedDifficulty = CreateLabel("txtRecommendedDifficulty", panel.transform, new Vector2(130f, 144f), new Vector2(390f, 44f), 26, FontStyles.Normal, "권장 난이도: Normal");
            var txtBestRecord = CreateLabel("txtBestRecord", panel.transform, new Vector2(0f, 92f), new Vector2(660f, 40f), 24, FontStyles.Normal, "최고 기록: 없음");

            var btnCasual = CreateButton("btnCasual", panel.transform, new Vector2(-230f, 18f), new Vector2(190f, 66f), "Casual", new Color(0.2f, 0.4f, 0.75f, 1f), buttonSprite);
            var btnNormal = CreateButton("btnNormal", panel.transform, new Vector2(0f, 18f), new Vector2(190f, 66f), "Normal", new Color(0.23f, 0.56f, 0.31f, 1f), buttonSprite);
            var btnVeteran = CreateButton("btnVeteran", panel.transform, new Vector2(230f, 18f), new Vector2(190f, 66f), "Veteran", new Color(0.68f, 0.36f, 0.2f, 1f), buttonSprite);

            var btnStart = CreateButton("btnStart", panel.transform, new Vector2(130f, -196f), new Vector2(230f, 74f), "Start", new Color(0.16f, 0.63f, 0.24f, 1f), startButtonSprite);
            var btnBack = CreateButton("btnBack", panel.transform, new Vector2(-130f, -196f), new Vector2(230f, 74f), "Back", new Color(0.33f, 0.33f, 0.36f, 1f), buttonSprite);
            var btnClose = CreateButton("btnClose", panel.transform, new Vector2(312f, 206f), new Vector2(62f, 62f), "X", new Color(0.74f, 0.2f, 0.2f, 1f), closeButtonSprite, 34f, new Color(1f, 0.96f, 0.86f, 1f));

            var popup = root.GetComponent<StageInfoPopup>();
            var so = new SerializedObject(popup);
            SetObjectRef(so, "txtStageName", txtStageName);
            SetObjectRef(so, "txtStarCount", txtStarCount);
            SetObjectRef(so, "txtRecommendedDifficulty", txtRecommendedDifficulty);
            SetObjectRef(so, "txtBestRecord", txtBestRecord);
            SetObjectRef(so, "panelBackground", panelImage);
            SetObjectRef(so, "btnCasual", btnCasual);
            SetObjectRef(so, "btnNormal", btnNormal);
            SetObjectRef(so, "btnVeteran", btnVeteran);
            SetObjectRef(so, "btnStart", btnStart);
            SetObjectRef(so, "btnBack", btnBack);
            SetObjectRef(so, "closeButton", btnClose);
            SetObjectRef(so, "openClip", popupClickClip);
            SetObjectRef(so, "clickClip", popupClickClip);
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[StageInfoPopupPrefabBuilder] Created/updated: {PrefabPath}");
        }

        private static GameObject CreateUIObject(string name, Transform parent, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI CreateLabel(
            string name,
            Transform parent,
            Vector2 anchoredPos,
            Vector2 size,
            float fontSize,
            FontStyles style,
            string text)
        {
            var go = CreateUIObject(name, parent, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            return label;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Vector2 anchoredPos,
            Vector2 size,
            string label,
            Color color,
            Sprite skinSprite,
            float labelFontSize = 26f,
            Color? labelColor = null)
        {
            var go = CreateUIObject(name, parent, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = color;
            if (skinSprite != null)
            {
                image.sprite = skinSprite;
                image.type = Image.Type.Sliced;
            }

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelGo = CreateUIObject("Label", go.transform, typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRt = labelGo.GetComponent<RectTransform>();
            Stretch(labelRt);

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = labelFontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = labelColor ?? Color.white;
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            return button;
        }

        private static void ApplyImageSkin(Image image, Sprite sprite, Image.Type type, Color tint)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = type;
            image.color = tint;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void SetObjectRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            string child = System.IO.Path.GetFileName(folderPath);
            if (!string.IsNullOrWhiteSpace(parent) && !string.IsNullOrWhiteSpace(child))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
