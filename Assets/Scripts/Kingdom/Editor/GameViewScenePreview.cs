#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Kingdom.Editor
{
    /// <summary>
    /// Edit-mode preview utility for GameView prefab.
    /// Creates "Canvas (Environment)" as parent so the prefab is visible standalone.
    /// </summary>
    public static class GameViewScenePreview
    {
        private const string PrefabPath = "Assets/Resources/UI/GameView.prefab";
        private const string PreviewCanvasName = "Canvas (Environment)";
        private const string PreviewRootName = "GameView";

        [MenuItem("Tools/Preview/GameView/Spawn In Scene")]
        public static void SpawnInScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[GameViewPreview] Active scene is not loaded.");
                return;
            }

            var existing = GameObject.Find(PreviewRootName);
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                Debug.Log("[GameViewPreview] Preview already exists.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[GameViewPreview] Prefab not found: {PrefabPath}");
                return;
            }

            var previewCanvas = GetOrCreatePreviewCanvas(scene);
            if (previewCanvas == null)
            {
                Debug.LogError("[GameViewPreview] Failed to create preview canvas.");
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[GameViewPreview] Failed to instantiate prefab.");
                return;
            }

            instance.name = PreviewRootName;
            instance.transform.SetParent(previewCanvas.transform, false);
            Undo.RegisterCreatedObjectUndo(instance, "Spawn GameView Preview");
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log("[GameViewPreview] Spawned under preview canvas.");
        }

        [MenuItem("Tools/Preview/GameView/Remove From Scene")]
        public static void RemoveFromScene()
        {
            var existing = GameObject.Find(PreviewRootName);
            if (existing != null)
            {
                var scene = existing.scene;
                Undo.DestroyObjectImmediate(existing);
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }

            var previewCanvas = GameObject.Find(PreviewCanvasName);
            if (previewCanvas != null && previewCanvas.transform.Find(PreviewRootName) != null)
            {
                var scene = previewCanvas.scene;
                Undo.DestroyObjectImmediate(previewCanvas);
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }

            Debug.Log("[GameViewPreview] Removed.");
        }

        [MenuItem("Tools/Preview/GameView/Open Prefab In Prefab Mode")]
        public static void OpenPrefabMode()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[GameViewPreview] Prefab not found: {PrefabPath}");
                return;
            }

            AssetDatabase.OpenAsset(prefab);
        }

        [MenuItem("Tools/Preview/GameView/Spawn In Scene", true)]
        [MenuItem("Tools/Preview/GameView/Remove From Scene", true)]
        [MenuItem("Tools/Preview/GameView/Open Prefab In Prefab Mode", true)]
        private static bool ValidateOnlyInEditMode()
        {
            return !EditorApplication.isPlaying;
        }

        private static Canvas GetOrCreatePreviewCanvas(Scene scene)
        {
            var existing = GameObject.Find(PreviewCanvasName);
            if (existing != null)
            {
                var canvas = existing.GetComponent<Canvas>();
                if (canvas != null)
                {
                    return canvas;
                }
            }

            var canvasGo = new GameObject(
                PreviewCanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasGo, scene);

            var canvasComp = canvasGo.GetComponent<Canvas>();
            canvasComp.renderMode = RenderMode.ScreenSpaceCamera;
            canvasComp.worldCamera = Camera.main;
            canvasComp.planeDistance = 100f;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Undo.RegisterCreatedObjectUndo(canvasGo, "Create GameView Preview Canvas");
            return canvasComp;
        }
    }
}
#endif
