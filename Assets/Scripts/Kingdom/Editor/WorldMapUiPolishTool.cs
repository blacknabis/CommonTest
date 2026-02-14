using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Kingdom.Editor
{
    public static class WorldMapUiPolishTool
    {
        private const string SourceFontPath = @"C:\Users\black\Downloads\MaplestoryFont_TTF\Maplestory Bold.ttf";
        private const string ProjectFontPath = "Assets/Resources/Fonts/Maplestory Bold.ttf";
        private const string TmpFontPath = "Assets/Resources/Fonts/TMP/Maplestory Bold SDF.asset";

        private const string WorldMapPrefabPath = "Assets/Resources/UI/WorldMapView.prefab";
        private const string TitlePrefabPath = "Assets/Resources/UI/TitleView.prefab";

        [MenuItem("Tools/WorldMap/Apply UI Polish (2-4)")]
        public static void ApplyPolish()
        {
            var tmpFont = PrepareMaplestoryTmpFont();
            if (tmpFont == null)
            {
                Debug.LogError("[WorldMapPolish] Failed to prepare TMP font.");
                return;
            }

            ApplyWorldMapPrefab(tmpFont);
            ApplyTitlePrefab(tmpFont);
            SetTmpDefaultFont(tmpFont);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WorldMapPolish] Completed.");
        }

        [MenuItem("Tools/WorldMap/Apply UI Polish (5-8)")]
        public static void ApplyPolishFiveToEight()
        {
            var tmpFont = PrepareMaplestoryTmpFont();
            if (tmpFont == null)
            {
                Debug.LogError("[WorldMapPolish] Failed to prepare TMP font for 5-8.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(WorldMapPrefabPath);
            if (root == null)
            {
                Debug.LogError($"[WorldMapPolish] WorldMap prefab missing: {WorldMapPrefabPath}");
                return;
            }

            ApplyWorldMapPolishFiveToEight(root.transform, tmpFont);

            PrefabUtility.SaveAsPrefabAsset(root, WorldMapPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WorldMapPolish] 5-8 completed.");
        }

        private static TMP_FontAsset PrepareMaplestoryTmpFont()
        {
            EnsureDirectory("Assets/Resources");
            EnsureDirectory("Assets/Resources/Fonts");
            EnsureDirectory("Assets/Resources/Fonts/TMP");

            if (!File.Exists(SourceFontPath))
            {
                Debug.LogError($"[WorldMapPolish] Source font not found: {SourceFontPath}");
                return null;
            }

            File.Copy(SourceFontPath, ProjectFontPath, true);
            AssetDatabase.ImportAsset(ProjectFontPath, ImportAssetOptions.ForceUpdate);

            var font = AssetDatabase.LoadAssetAtPath<Font>(ProjectFontPath);
            if (font == null)
            {
                Debug.LogError("[WorldMapPolish] Failed to load copied TTF font asset.");
                return null;
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            if (existing != null)
            {
                EnsureFontAssetSubAssets(existing);
                SeedCommonCharacters(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            var tmp = TMP_FontAsset.CreateFontAsset(
                font,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            if (tmp == null)
            {
                Debug.LogError("[WorldMapPolish] TMP font asset creation failed.");
                return null;
            }

            AssetDatabase.CreateAsset(tmp, TmpFontPath);
            EnsureFontAssetSubAssets(tmp);
            SeedCommonCharacters(tmp);
            AssetDatabase.ImportAsset(TmpFontPath, ImportAssetOptions.ForceUpdate);

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
        }

        private static void EnsureFontAssetSubAssets(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return;
            }

            if (fontAsset.atlasTextures == null || fontAsset.atlasTextures.Length == 0)
            {
                fontAsset.atlasTextures = new Texture2D[1];
            }

            if (fontAsset.atlasTextures[0] == null)
            {
                var atlas = new Texture2D(1, 1, TextureFormat.Alpha8, false);
                atlas.name = fontAsset.name + " Atlas";
                fontAsset.atlasTextures[0] = atlas;
            }

            var atlasTex = fontAsset.atlasTextures[0];
            if (atlasTex != null && !AssetDatabase.Contains(atlasTex))
            {
                AssetDatabase.AddObjectToAsset(atlasTex, fontAsset);
            }

            if (fontAsset.material == null)
            {
                var shader = Shader.Find("TextMeshPro/Distance Field");
                var mat = new Material(shader);
                mat.name = atlasTex != null ? atlasTex.name + " Material" : fontAsset.name + " Material";
                fontAsset.material = mat;
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.SetTexture(ShaderUtilities.ID_MainTex, atlasTex);
                fontAsset.material.SetFloat(ShaderUtilities.ID_TextureWidth, fontAsset.atlasWidth);
                fontAsset.material.SetFloat(ShaderUtilities.ID_TextureHeight, fontAsset.atlasHeight);
                fontAsset.material.SetFloat(ShaderUtilities.ID_GradientScale, fontAsset.atlasPadding + 1f);

                if (!AssetDatabase.Contains(fontAsset.material))
                {
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }
                EditorUtility.SetDirty(fontAsset.material);
            }

            EditorUtility.SetDirty(fontAsset);
            if (atlasTex != null)
            {
                EditorUtility.SetDirty(atlasTex);
            }
        }

        private static void SeedCommonCharacters(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
            {
                return;
            }

            const string seedChars = " WORLDMAPHEROUPGRADEBACKSTAGE0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            _ = fontAsset.TryAddCharacters(seedChars);

            EditorUtility.SetDirty(fontAsset);
            if (fontAsset.material != null)
            {
                EditorUtility.SetDirty(fontAsset.material);
            }

            var atlases = fontAsset.atlasTextures;
            if (atlases == null)
            {
                return;
            }

            foreach (var atlas in atlases)
            {
                if (atlas != null)
                {
                    EditorUtility.SetDirty(atlas);
                }
            }
        }

        private static void ApplyWorldMapPolishFiveToEight(Transform root, TMP_FontAsset font)
        {
            var bottomBar = FindByPath(root, "BottomBar");
            var back = FindByPath(root, "btnBack");
            var stage = FindByPath(root, "btnStage1");
            var hero = FindByPath(root, "BottomBar/btnHeroRoom") ?? FindByPath(root, "btnHeroRoom");
            var upgrade = FindByPath(root, "BottomBar/btnUpgrades") ?? FindByPath(root, "btnUpgrades");
            var title = FindByPath(root, "txtTitle")?.GetComponent<TextMeshProUGUI>();
            var heroLabel = FindByPath(root, "lblHero")?.GetComponent<TextMeshProUGUI>();
            var upgradeLabel = FindByPath(root, "lblUpgrade")?.GetComponent<TextMeshProUGUI>();

            if (bottomBar != null)
            {
                var barRt = bottomBar.GetComponent<RectTransform>();
                barRt.anchorMin = new Vector2(0f, 0f);
                barRt.anchorMax = new Vector2(1f, 0f);
                barRt.anchoredPosition = Vector2.zero;
                barRt.sizeDelta = new Vector2(0f, 220f);

                var barImage = bottomBar.GetComponent<Image>();
                if (barImage != null)
                {
                    barImage.color = new Color(0f, 0f, 0f, 0.28f);
                }
            }

            if (hero != null && bottomBar != null)
            {
                hero.SetParent(bottomBar, false);
                var rt = hero.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.38f, 1f);
                rt.anchorMax = new Vector2(0.38f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(180f, 180f);
                EnsureSquare(hero.gameObject);
                ApplyButtonTransition(hero.GetComponent<Button>());
            }

            if (upgrade != null && bottomBar != null)
            {
                upgrade.SetParent(bottomBar, false);
                var rt = upgrade.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.62f, 1f);
                rt.anchorMax = new Vector2(0.62f, 1f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(180f, 180f);
                EnsureSquare(upgrade.gameObject);
                ApplyButtonTransition(upgrade.GetComponent<Button>());
            }

            if (stage != null)
            {
                var rt = stage.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.46f);
                rt.anchorMax = new Vector2(0.5f, 0.46f);
                rt.anchoredPosition = new Vector2(-40f, 10f);
                rt.sizeDelta = new Vector2(230f, 230f);
                EnsureSquare(stage.gameObject);
                ApplyButtonTransition(stage.GetComponent<Button>());
            }

            if (back != null)
            {
                var rt = back.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.08f, 0.92f);
                rt.anchorMax = new Vector2(0.08f, 0.92f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(170f, 170f);
                EnsureSquare(back.gameObject);
                ApplyButtonTransition(back.GetComponent<Button>());
            }

            if (heroLabel != null)
            {
                var rt = heroLabel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.38f, 0.07f);
                rt.anchorMax = new Vector2(0.38f, 0.07f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(240f, 44f);
                heroLabel.font = font;
                heroLabel.fontSharedMaterial = font.material;
                heroLabel.fontSize = 26f;
                heroLabel.color = new Color(1f, 0.98f, 0.86f, 0.97f);
                heroLabel.alignment = TextAlignmentOptions.Center;
            }

            if (upgradeLabel != null)
            {
                var rt = upgradeLabel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.62f, 0.07f);
                rt.anchorMax = new Vector2(0.62f, 0.07f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(240f, 44f);
                upgradeLabel.font = font;
                upgradeLabel.fontSharedMaterial = font.material;
                upgradeLabel.fontSize = 26f;
                upgradeLabel.color = new Color(1f, 0.98f, 0.86f, 0.97f);
                upgradeLabel.alignment = TextAlignmentOptions.Center;
            }

            if (title != null)
            {
                title.font = font;
                title.fontSharedMaterial = font.material;
                title.fontSize = 50f;
                title.color = new Color(1f, 0.97f, 0.82f, 1f);
                title.alignment = TextAlignmentOptions.Center;
            }

            EnsureStageHint(root, font);
        }

        private static void EnsureSquare(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            var fitter = go.GetComponent<AspectRatioFitter>() ?? go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = 1f;
        }

        private static void ApplyButtonTransition(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.84f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.selectedColor = new Color(1f, 0.96f, 0.84f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        private static void EnsureStageHint(Transform root, TMP_FontAsset font)
        {
            var hintTr = FindByPath(root, "lblStageHint");
            GameObject go;
            if (hintTr == null)
            {
                go = new GameObject("lblStageHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.transform.SetParent(root, false);
                go.transform.SetAsLastSibling();
            }
            else
            {
                go = hintTr.gameObject;
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.355f);
            rt.anchorMax = new Vector2(0.5f, 0.355f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(320f, 42f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = "TAP STAGE";
            tmp.font = font;
            tmp.fontSharedMaterial = font.material;
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.98f, 0.88f, 0.96f);
            tmp.raycastTarget = false;
        }

        private static void ApplyWorldMapPrefab(TMP_FontAsset font)
        {
            var root = PrefabUtility.LoadPrefabContents(WorldMapPrefabPath);
            if (root == null)
            {
                Debug.LogError($"[WorldMapPolish] WorldMap prefab missing: {WorldMapPrefabPath}");
                return;
            }

            var btnStage = FindByPath(root.transform, "btnStage1")?.GetComponent<Button>();
            if (btnStage != null)
            {
                var img = btnStage.GetComponent<Image>();
                if (img != null)
                {
                    var outline = btnStage.GetComponent<Outline>() ?? btnStage.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.86f, 0.22f, 0.9f);
                    outline.effectDistance = new Vector2(5f, -5f);

                    var shadow = btnStage.GetComponent<Shadow>() ?? btnStage.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
                    shadow.effectDistance = new Vector2(2f, -2f);
                }

                var pulse = btnStage.GetComponent<Kingdom.App.UIButtonPulse>() ?? btnStage.gameObject.AddComponent<Kingdom.App.UIButtonPulse>();
                _ = pulse;
            }

            var titleText = FindByPath(root.transform, "txtTitle")?.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.font = font;
                titleText.fontSharedMaterial = font.material;
                titleText.fontSize = 52;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = new Color(1f, 0.96f, 0.78f, 1f);

                var banner = FindByPath(root.transform, "TitleBanner")?.gameObject;
                if (banner == null)
                {
                    banner = new GameObject("TitleBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    banner.transform.SetParent(root.transform, false);
                    banner.transform.SetSiblingIndex(Mathf.Max(0, titleText.transform.GetSiblingIndex() - 1));
                }

                var bannerRt = banner.GetComponent<RectTransform>();
                bannerRt.anchorMin = new Vector2(0.5f, 0.94f);
                bannerRt.anchorMax = new Vector2(0.5f, 0.94f);
                bannerRt.anchoredPosition = Vector2.zero;
                bannerRt.sizeDelta = new Vector2(560f, 88f);

                var bannerImg = banner.GetComponent<Image>();
                bannerImg.color = new Color(0f, 0f, 0f, 0.32f);
                bannerImg.raycastTarget = false;
            }

            EnsureBottomBar(root.transform);
            EnsureBottomLabel(root.transform, "lblHero", new Vector2(0.3f, 0.03f), "HERO", font);
            EnsureBottomLabel(root.transform, "lblUpgrade", new Vector2(0.5f, 0.03f), "UPGRADE", font);

            ApplyFontToAllTmp(root, font);

            PrefabUtility.SaveAsPrefabAsset(root, WorldMapPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void ApplyTitlePrefab(TMP_FontAsset font)
        {
            var root = PrefabUtility.LoadPrefabContents(TitlePrefabPath);
            if (root == null)
            {
                Debug.LogWarning($"[WorldMapPolish] Title prefab missing: {TitlePrefabPath}");
                return;
            }

            ApplyFontToAllTmp(root, font);
            PrefabUtility.SaveAsPrefabAsset(root, TitlePrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void SetTmpDefaultFont(TMP_FontAsset font)
        {
            if (TMP_Settings.instance == null)
            {
                Debug.LogWarning("[WorldMapPolish] TMP_Settings.instance is null.");
                return;
            }

            TMP_Settings.defaultFontAsset = font;
            EditorUtility.SetDirty(TMP_Settings.instance);
        }

        private static void ApplyFontToAllTmp(GameObject root, TMP_FontAsset font)
        {
            var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                t.font = font;
                t.fontSharedMaterial = font.material;
            }
        }

        private static void EnsureBottomBar(Transform root)
        {
            var bar = FindByPath(root, "BottomBar")?.gameObject;
            if (bar == null)
            {
                bar = new GameObject("BottomBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bar.transform.SetParent(root, false);
                bar.transform.SetSiblingIndex(1);
            }

            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(0f, 230f);

            var img = bar.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.22f);
            img.raycastTarget = false;
        }

        private static void EnsureBottomLabel(Transform root, string name, Vector2 anchor, string text, TMP_FontAsset font)
        {
            var tr = FindByPath(root, name);
            GameObject go;
            if (tr == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.transform.SetParent(root, false);
                go.transform.SetAsLastSibling();
            }
            else
            {
                go = tr.gameObject;
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(220f, 42f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = font;
            tmp.fontSharedMaterial = font.material;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.97f, 0.84f, 0.95f);
            tmp.raycastTarget = false;
        }

        private static Transform FindByPath(Transform root, string path)
        {
            return root.Find(path);
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureDirectory(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
