using Kingdom.App;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.Editor
{
    /// <summary>
    /// AudioOptionsPopup 프리팹 생성 유틸리티.
    /// WorldMapView에서 Resources 경로(UI/WorldMap/AudioOptionsPopup)로 로드한다.
    /// </summary>
    public static class AudioOptionsPopupPrefabBuilder
    {
        private const string PrefabPath = "Assets/Resources/UI/WorldMap/AudioOptionsPopup.prefab";

        [MenuItem("Tools/Kingdom/Build AudioOptionsPopup Prefab")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");
            EnsureFolder("Assets/Resources/UI/WorldMap");

            // Root
            GameObject root = new GameObject("AudioOptionsPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AudioOptionsPopup));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(760f, 520f);
            rootRect.anchoredPosition = Vector2.zero;

            Image rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0.09f, 0.1f, 0.12f, 0.96f);
            rootImage.raycastTarget = true;

            // Panel
            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(rootRect, false);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700f, 450f);
            panel.GetComponent<Image>().color = new Color(0.16f, 0.17f, 0.2f, 0.98f);

            CreateText(panelRect, "Title", "오디오 설정", 36f, TextAlignmentOptions.Center, new Vector2(0f, 176f), new Vector2(400f, 64f), Color.white);

            // Content
            GameObject content = new GameObject("Content", typeof(RectTransform));
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.SetParent(panelRect, false);
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(620f, 260f);

            CreateAudioRow(contentRect, "BgmRow", "BGM", 56f, "80%", false);
            CreateAudioRow(contentRect, "SfxRow", "SFX", -56f, "100%", false);

            // Close Button
            GameObject closeButton = new GameObject("BtnClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.SetParent(panelRect, false);
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(220f, 64f);
            closeRect.anchoredPosition = new Vector2(0f, 20f);
            closeButton.GetComponent<Image>().color = new Color(0.76f, 0.76f, 0.82f, 1f);
            CreateText(closeRect, "Label", "닫기", 28f, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, Color.black);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AudioOptionsPopupPrefabBuilder] Prefab generated: {PrefabPath}");
        }

        private static void CreateAudioRow(RectTransform parent, string rowName, string label, float y, string valueText, bool toggleDefault)
        {
            GameObject row = new GameObject(rowName, typeof(RectTransform));
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.SetParent(parent, false);
            rowRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(620f, 72f);
            rowRect.anchoredPosition = new Vector2(0f, y);

            CreateText(rowRect, "Label", label, 24f, TextAlignmentOptions.Left, new Vector2(-260f, 0f), new Vector2(120f, 48f), Color.white);

            Slider slider = CreateSlider(rowRect, "Slider", new Vector2(-20f, 0f), new Vector2(280f, 28f));
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            CreateText(rowRect, "Value", valueText, 20f, TextAlignmentOptions.Center, new Vector2(190f, 0f), new Vector2(88f, 48f), Color.white);

            Toggle toggle = CreateToggle(rowRect, "MuteToggle", "Mute", new Vector2(272f, 0f), new Vector2(110f, 40f));
            toggle.isOn = toggleDefault;
        }

        private static Slider CreateSlider(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.SetParent(parent, false);
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = anchoredPos;
            sliderRect.sizeDelta = size;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.SetParent(sliderRect, false);
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f, 1f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.SetParent(sliderRect, false);
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(6f, 6f);
            fillAreaRect.offsetMax = new Vector2(-20f, -6f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.SetParent(fillAreaRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.45f, 0.7f, 0.98f, 1f);

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.SetParent(sliderRect, false);
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.SetParent(handleAreaRect, false);
            handleRect.sizeDelta = new Vector2(18f, 34f);
            handle.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f, 1f);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Toggle CreateToggle(RectTransform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
        {
            GameObject toggleObject = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
            toggleRect.SetParent(parent, false);
            toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
            toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
            toggleRect.pivot = new Vector2(0.5f, 0.5f);
            toggleRect.anchoredPosition = anchoredPos;
            toggleRect.sizeDelta = size;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.SetParent(toggleRect, false);
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(0f, 0.5f);
            bgRect.pivot = new Vector2(0f, 0.5f);
            bgRect.sizeDelta = new Vector2(26f, 26f);
            bgRect.anchoredPosition = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f, 1f);

            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.SetParent(bgRect, false);
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.sizeDelta = new Vector2(16f, 16f);
            checkmark.GetComponent<Image>().color = new Color(0.45f, 0.9f, 0.45f, 1f);

            CreateText(toggleRect, "Label", label, 18f, TextAlignmentOptions.Left, new Vector2(38f, 0f), new Vector2(72f, 30f), Color.white);

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = background.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            return toggle;
        }

        private static TextMeshProUGUI CreateText(
            RectTransform parent,
            string name,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 anchoredPos,
            Vector2 size,
            Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(parent, false);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = anchoredPos;
            textRect.sizeDelta = size;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            return label;
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

