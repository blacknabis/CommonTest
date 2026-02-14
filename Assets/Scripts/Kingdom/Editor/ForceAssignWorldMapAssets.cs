using Kingdom.App;
using UnityEditor;

using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.Editor
{
    /// <summary>
    /// Assigns generated world-map assets to WorldMapView prefab.
    /// </summary>
    public static class ForceAssignWorldMapAssets
    {
        private const string PrefabPath = "Assets/Resources/UI/WorldMapView.prefab";
        private const string BackgroundPath = "Assets/Resources/UI/Sprites/WorldMap/WorldMap_Background.png";
        private const string BgmPath = "Assets/Resources/Audio/WorldMap/WorldMap_BGM.wav";
        private const string ClickPath = "Assets/Resources/Audio/WorldMap/WorldMap_Click_UI.mp3";

        [MenuItem("Tools/Generators/Force Assign WorldMap Assets")]
        public static void ForceAssign()
        {
            EnsureSpriteImporter(BackgroundPath);
            EnsureAudioImporter(BgmPath);
            EnsureAudioImporter(ClickPath);
            AssetDatabase.Refresh();

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError($"[WorldMapAssign] Prefab not found: {PrefabPath}");
                return;
            }

            var worldMapView = root.GetComponent<WorldMapView>();
            if (worldMapView == null)
            {
                Debug.LogError("[WorldMapAssign] WorldMapView component missing.");
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            var backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            var bgImage = EnsureBackgroundImage(root.transform, backgroundSprite);
            var bgm = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmPath);
            var click = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickPath);

            var so = new SerializedObject(worldMapView);
            so.FindProperty("backgroundImage").objectReferenceValue = bgImage;
            so.FindProperty("bgmClip").objectReferenceValue = bgm;
            so.FindProperty("clickClip").objectReferenceValue = click;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();

            Debug.Log("[WorldMapAssign] WorldMapView prefab asset assignment completed.");
        }

        private static Image EnsureBackgroundImage(Transform root, Sprite sprite)
        {
            Transform tr = root.Find("imgBackground");
            Image img;

            if (tr == null)
            {
                var go = new GameObject("imgBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(root, false);
                go.transform.SetAsFirstSibling();

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                img = go.GetComponent<Image>();
            }
            else
            {
                img = tr.GetComponent<Image>();
                if (img == null)
                {
                    img = tr.gameObject.AddComponent<Image>();
                }
            }

            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

            return img;
        }

        private static void EnsureSpriteImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureAudioImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                return;
            }

            var sampleSettings = importer.defaultSampleSettings;
            sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
            sampleSettings.compressionFormat = AudioCompressionFormat.Vorbis;
            sampleSettings.quality = 0.75f;
            sampleSettings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;

            sampleSettings.preloadAudioData = true; // Moved from importer.preloadAudioData

            importer.defaultSampleSettings = sampleSettings;
            // importer.preloadAudioData = true; // Obsolete
            importer.forceToMono = false;
            importer.SaveAndReimport();
        }
    }
}
