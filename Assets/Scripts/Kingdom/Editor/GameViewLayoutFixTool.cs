#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.Editor
{
    /// <summary>
    /// Legacy GameView 프리팹 레이아웃을 정리하고 직렬화 필드를 재바인딩한다.
    /// </summary>
    public static class GameViewLayoutFixTool
    {
        private const string PrefabPath = "Assets/Resources/UI/GameView.prefab";

        [MenuItem("Kingdom/GameView/Fix Layout And Bindings")]
        public static void FixLayoutAndBindings()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError($"[GameViewFix] Prefab not found: {PrefabPath}");
                return;
            }

            try
            {
                var view = root.GetComponent<Kingdom.App.GameView>();
                if (view == null)
                {
                    Debug.LogError("[GameViewFix] GameView component is missing.");
                    return;
                }

                RemoveLegacyRootChildren(root.transform);

                var hudRoot = EnsureRect(root.transform, "HUDRoot");
                SetFullStretch(hudRoot);

                var btnVictory = EnsureButton(hudRoot, "btnVictory", "Victory", new Vector2(1f, 0f), new Vector2(-24f, 94f), new Vector2(150f, 56f), new Color(0.2f, 0.55f, 0.2f, 0.9f));
                var btnDefeat = EnsureButton(hudRoot, "btnDefeat", "Defeat", new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(150f, 56f), new Color(0.65f, 0.2f, 0.2f, 0.9f));
                var btnPause = EnsureButton(hudRoot, "btnPause", "Pause", new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(110f, 56f), new Color(0.18f, 0.2f, 0.25f, 0.9f));
                var btnNextWave = EnsureButton(hudRoot, "btnNextWave", "Next Wave", new Vector2(1f, 1f), new Vector2(-24f, -90f), new Vector2(180f, 56f), new Color(0.45f, 0.2f, 0.2f, 0.9f));

                var txtWaveInfo = EnsureTmp(hudRoot, "txtWaveInfo", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(500f, 48f), 40f, TextAlignmentOptions.Left);
                txtWaveInfo.text = "WAVE 1 / 3";
                var txtStateInfo = EnsureTmp(hudRoot, "txtStateInfo", new Vector2(0f, 1f), new Vector2(24f, -74f), new Vector2(500f, 40f), 30f, TextAlignmentOptions.Left);
                txtStateInfo.text = "Prepare";
                var txtLives = EnsureTmp(hudRoot, "txtLives", new Vector2(0f, 1f), new Vector2(24f, -114f), new Vector2(500f, 40f), 28f, TextAlignmentOptions.Left);
                txtLives.text = "LIVES 20";
                var txtGold = EnsureTmp(hudRoot, "txtGold", new Vector2(0f, 1f), new Vector2(24f, -152f), new Vector2(500f, 40f), 28f, TextAlignmentOptions.Left);
                txtGold.text = "GOLD 100";

                var heroPortraitRect = EnsureRect(hudRoot, "imgHeroPortrait");
                SetBottomLeftRect(heroPortraitRect, new Vector2(24f, 96f), new Vector2(84f, 84f));
                var heroPortrait = heroPortraitRect.GetComponent<Image>() ?? heroPortraitRect.gameObject.AddComponent<Image>();
                heroPortrait.color = new Color(0.9f, 0.9f, 0.9f, 0.95f);

                var btnSpellReinforce = EnsureButton(hudRoot, "btnSpellReinforce", "Reinforce", new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(160f, 56f), new Color(0.2f, 0.35f, 0.6f, 0.9f));
                var btnSpellRain = EnsureButton(hudRoot, "btnSpellRain", "Rain", new Vector2(0f, 0f), new Vector2(194f, 24f), new Vector2(140f, 56f), new Color(0.55f, 0.35f, 0.2f, 0.9f));
                var imgSpellReinforceCooldown = EnsureCooldownFill(btnSpellReinforce.transform);
                var imgSpellRainCooldown = EnsureCooldownFill(btnSpellRain.transform);

                var resultRoot = EnsureRect(hudRoot, "ResultRoot");
                SetCenterRect(resultRoot, new Vector2(760f, 360f));
                resultRoot.gameObject.SetActive(false);

                var txtResultTitle = EnsureTmp(resultRoot, "txtResultTitle", new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(600f, 80f), 42f, TextAlignmentOptions.Center);
                txtResultTitle.text = "VICTORY";
                var txtResultMessage = EnsureTmp(resultRoot, "txtResultMessage", new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(640f, 120f), 28f, TextAlignmentOptions.Center);
                txtResultMessage.text = "Result Message";

                var btnRetry = EnsureButton(resultRoot, "btnRetry", "Retry", new Vector2(0.5f, 0f), new Vector2(-120f, 36f), new Vector2(180f, 70f), new Color(0.95f, 0.95f, 0.95f, 0.95f));
                var btnExit = EnsureButton(resultRoot, "btnExit", "Exit", new Vector2(0.5f, 0f), new Vector2(120f, 36f), new Vector2(180f, 70f), new Color(0.95f, 0.95f, 0.95f, 0.95f));

                var so = new SerializedObject(view);
                SetRef(so, "btnVictory", btnVictory);
                SetRef(so, "btnDefeat", btnDefeat);
                SetRef(so, "btnPause", btnPause);
                SetRef(so, "btnNextWave", btnNextWave);
                SetRef(so, "txtWaveInfo", txtWaveInfo);
                SetRef(so, "txtStateInfo", txtStateInfo);
                SetRef(so, "txtLives", txtLives);
                SetRef(so, "txtGold", txtGold);
                SetRef(so, "imgHeroPortrait", heroPortrait);
                SetRef(so, "btnSpellReinforce", btnSpellReinforce);
                SetRef(so, "btnSpellRain", btnSpellRain);
                SetRef(so, "imgSpellReinforceCooldown", imgSpellReinforceCooldown);
                SetRef(so, "imgSpellRainCooldown", imgSpellRainCooldown);
                SetRef(so, "resultRoot", resultRoot.gameObject);
                SetRef(so, "txtResultTitle", txtResultTitle);
                SetRef(so, "txtResultMessage", txtResultMessage);
                SetRef(so, "btnRetry", btnRetry);
                SetRef(so, "btnExit", btnExit);
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[GameViewFix] Layout and bindings updated.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                var existing = child.GetComponent<RectTransform>();
                if (existing != null)
                {
                    return existing;
                }
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void RemoveLegacyRootChildren(Transform root)
        {
            string[] legacyNames =
            {
                "btnVictory",
                "btnDefeat",
                "btnPause",
                "btnNextWave",
                "txtWaveInfo",
                "txtStateInfo",
                "txtLives",
                "txtGold",
                "imgHeroPortrait",
                "btnSpellReinforce",
                "btnSpellRain"
            };

            for (int i = 0; i < legacyNames.Length; i++)
            {
                var tr = root.Find(legacyNames[i]);
                if (tr != null)
                {
                    Object.DestroyImmediate(tr.gameObject);
                }
            }
        }

        private static Button EnsureButton(RectTransform parent, string name, string label, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color bg)
        {
            var child = parent.Find(name);
            Button button;
            RectTransform rect;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                button = go.GetComponent<Button>();
                rect = go.GetComponent<RectTransform>();
            }
            else
            {
                button = child.GetComponent<Button>() ?? child.gameObject.AddComponent<Button>();
                if (child.GetComponent<Image>() == null) child.gameObject.AddComponent<Image>();
                rect = child.GetComponent<RectTransform>();
            }

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var image = button.GetComponent<Image>();
            image.color = bg;

            var textChild = rect.Find("Text");
            TextMeshProUGUI text;
            if (textChild == null)
            {
                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(rect, false);
                text = txtGo.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                text = textChild.GetComponent<TextMeshProUGUI>() ?? textChild.gameObject.AddComponent<TextMeshProUGUI>();
            }

            var txtRect = text.GetComponent<RectTransform>();
            SetFullStretch(txtRect);
            text.text = label;
            text.fontSize = 30f;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Center;

            return button;
        }

        private static TextMeshProUGUI EnsureTmp(RectTransform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var child = parent.Find(name);
            TextMeshProUGUI text;
            RectTransform rect;
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(parent, false);
                text = go.GetComponent<TextMeshProUGUI>();
                rect = go.GetComponent<RectTransform>();
            }
            else
            {
                text = child.GetComponent<TextMeshProUGUI>() ?? child.gameObject.AddComponent<TextMeshProUGUI>();
                rect = child.GetComponent<RectTransform>();
            }

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static void SetCenterRect(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void SetBottomLeftRect(RectTransform rect, Vector2 anchoredPos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Image EnsureCooldownFill(Transform parent)
        {
            var child = parent.Find("CooldownFill");
            Image image;
            RectTransform rect;
            if (child == null)
            {
                var go = new GameObject("CooldownFill", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
                rect = go.GetComponent<RectTransform>();
            }
            else
            {
                image = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
                rect = child.GetComponent<RectTransform>();
            }

            SetFullStretch(rect);
            image.color = new Color(0f, 0f, 0f, 0.45f);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Vertical;
            image.fillOrigin = (int)Image.OriginVertical.Top;
            image.fillAmount = 0f;
            image.gameObject.SetActive(false);
            return image;
        }
    }
}
#endif
