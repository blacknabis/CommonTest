using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Kingdom.App;
using TMPro;

namespace Kingdom.Editor
{
    /// <summary>
    /// 타이틀 에셋을 TitleView 프리팹에 강제 할당하는 일회성 도구.
    /// 배경, 로고(엠블렘+TMP텍스트), 버튼(프레임+TMP텍스트)을 설정합니다.
    /// </summary>
    public class ForceAssignTitleAssets
    {
        [MenuItem("Tools/Generators/Force Assign Title Assets")]
        public static void ForceAssign()
        {
            string folder = "Assets/Resources/UI/Title";
            string prefabPath = "Assets/Resources/UI/TitleView.prefab";

            // 1. 텍스처 임포트 설정
            EnsureSprite($"{folder}/Title_Background.png");
            EnsureSprite($"{folder}/Title_Logo.png");
            EnsureSprite($"{folder}/Title_BtnStart.png", sliced: true);
            
            AssetDatabase.Refresh();

            // 2. 프리팹 로드
            bool isSceneObject = false;
            TitleView titleView = Object.FindFirstObjectByType<TitleView>();
            GameObject rootGo = null;

            if (titleView != null)
            {
                Debug.Log("[Assign] Found TitleView in Scene.");
                rootGo = titleView.gameObject;
                isSceneObject = true;
                Undo.RecordObject(rootGo, "Assign Title Assets");
            }
            else
            {
                Debug.Log($"[Assign] Loading Prefab: {prefabPath}");
                rootGo = PrefabUtility.LoadPrefabContents(prefabPath);
                if (rootGo == null)
                {
                    Debug.LogError("[Assign] Prefab not found!");
                    return;
                }
                titleView = rootGo.GetComponent<TitleView>();
            }

            if (titleView == null)
            {
                Debug.LogError("[Assign] TitleView component missing!");
                if (!isSceneObject) PrefabUtility.UnloadPrefabContents(rootGo);
                return;
            }

            SerializedObject so = new SerializedObject(titleView);

            // ========== Background ==========
            SetupBackground(so, rootGo, folder);

            // ========== Logo (엠블렘 + 텍스트) ==========
            SetupLogo(rootGo, folder);

            // ========== Start Button (프레임 + 텍스트) ==========
            SetupButton(so, rootGo, folder);

            so.ApplyModifiedProperties();

            // 3. 저장
            if (isSceneObject)
            {
                EditorUtility.SetDirty(titleView);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(rootGo, prefabPath);
                PrefabUtility.UnloadPrefabContents(rootGo);
            }

            Debug.Log("[Assign] Force Assignment Complete!");
        }

        // ─────────────────────────────────────────────
        // Background: 전체 화면 배경
        // ─────────────────────────────────────────────
        static void SetupBackground(SerializedObject so, GameObject root, string folder)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/Title_Background.png");
            if (!sprite) return;

            Transform tr = FindChild(root.transform, "Background");
            Image img;

            if (!tr)
            {
                var go = new GameObject("Background", typeof(Image));
                go.transform.SetParent(root.transform, false);
                go.transform.SetAsFirstSibling();

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;

                img = go.GetComponent<Image>();
            }
            else
            {
                img = tr.GetComponent<Image>();
            }

            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;

            // Wire to TitleView.backgroundImage
            var prop = so.FindProperty("backgroundImage");
            if (prop != null) prop.objectReferenceValue = img;

            Debug.Log("[Assign] Background OK");
        }

        // ─────────────────────────────────────────────
        // Logo: 엠블렘 이미지 (텍스트는 별개의 txtTitle)
        // ─────────────────────────────────────────────
        static void SetupLogo(GameObject root, string folder)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/Title_Logo.png");
            if (!sprite) return;

            Transform tr = FindChild(root.transform, "Logo");
            Image img;

            if (!tr)
            {
                var go = new GameObject("Logo", typeof(Image));
                go.transform.SetParent(root.transform, false);
                img = go.GetComponent<Image>();
            }
            else
            {
                img = tr.GetComponent<Image>();
                if (!img) img = tr.gameObject.AddComponent<Image>();
            }

            img.sprite = sprite;
            img.SetNativeSize();

            // 위치: 화면 상단 중앙
            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 100);
            // 크기 제한 (너무 안 크게)
            if (rt.sizeDelta.x > 300)
            {
                float scale = 300f / rt.sizeDelta.x;
                rt.sizeDelta *= scale;
            }

            Debug.Log("[Assign] Logo OK");
        }



        // ─────────────────────────────────────────────
        // Start Button: 나무 프레임 + "TAP TO START" 텍스트
        // ─────────────────────────────────────────────
        static void SetupButton(SerializedObject so, GameObject root, string folder)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/Title_BtnStart.png");

            Transform btnTr = FindChild(root.transform, "btnStart");

            if (!btnTr)
            {
                var btnGo = new GameObject("btnStart", typeof(Image), typeof(Button));
                btnGo.transform.SetParent(root.transform, false);
                btnTr = btnGo.transform;
            }

            // Image 설정
            var btnImg = btnTr.GetComponent<Image>();
            if (!btnImg) btnImg = btnTr.gameObject.AddComponent<Image>();

            if (sprite)
            {
                btnImg.sprite = sprite;
                btnImg.type = Image.Type.Sliced;
            }

            // Button 설정
            var btn = btnTr.GetComponent<Button>();
            if (!btn) btn = btnTr.gameObject.AddComponent<Button>();

            // 크기/위치
            var rt = btnTr.GetComponent<RectTransform>();
            if (!rt) rt = btnTr.gameObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -80);
            rt.sizeDelta = new Vector2(280, 70);

            // 버튼 위에 텍스트
            Transform txtTr = FindChild(btnTr, "Text");
            TextMeshProUGUI btnTmp;

            if (!txtTr)
            {
                var txtGo = new GameObject("Text", typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(btnTr, false);
                btnTmp = txtGo.GetComponent<TextMeshProUGUI>();

                var txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;
            }
            else
            {
                btnTmp = txtTr.GetComponent<TextMeshProUGUI>();
                if (!btnTmp) btnTmp = txtTr.gameObject.AddComponent<TextMeshProUGUI>();
            }

            btnTmp.text = "TAP TO START";
            btnTmp.fontSize = 24;
            btnTmp.fontStyle = FontStyles.Bold;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;

            // Wire to TitleView.btnStart
            var prop = so.FindProperty("btnStart");
            if (prop != null) prop.objectReferenceValue = btn;

            Debug.Log("[Assign] Button OK");
        }

        // ─────────────────────────────────────────────
        // Utility
        // ─────────────────────────────────────────────
        static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var result = FindChild(child, name);
                if (result != null) return result;
            }
            return null;
        }

        static void EnsureSprite(string path, bool sliced = false)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) return;

            bool dirty = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                dirty = true;
            }

            if (sliced && importer.spriteBorder == Vector4.zero)
            {
                importer.spriteBorder = new Vector4(40, 40, 40, 40);
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
                Debug.Log($"[Assign] Configured {path} (sliced={sliced})");
            }
        }
    }
}
