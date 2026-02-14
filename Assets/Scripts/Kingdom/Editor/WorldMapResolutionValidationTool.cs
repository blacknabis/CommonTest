using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Kingdom.Editor
{
    public static class WorldMapResolutionValidationTool
    {
        private readonly struct ResolutionPreset
        {
            public readonly string Name;
            public readonly int Width;
            public readonly int Height;

            public ResolutionPreset(string name, int width, int height)
            {
                Name = name;
                Width = width;
                Height = height;
            }
        }

        private static readonly ResolutionPreset[] Presets =
        {
            new ResolutionPreset("Landscape_16x9", 1920, 1080),
            new ResolutionPreset("Landscape_195x9", 2340, 1080),
            new ResolutionPreset("Landscape_4x3", 1440, 1080),
        };

        private const string CapturePrefix = "worldmap_validation";
        private static string _outputDir;
        private static bool _running;
        private static int _index;
        private static int _waitFrames;
        private static bool _capturePending;

        [MenuItem("Tools/WorldMap/Capture Validation Set")]
        public static void CaptureValidationSet()
        {
            if (_running)
            {
                Debug.LogWarning("[WorldMapResolutionValidationTool] Capture is already running.");
                return;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            _outputDir = Path.Combine(projectRoot, "Assets", "Screenshots");
            Directory.CreateDirectory(_outputDir);

            _running = true;
            _index = -1;
            _waitFrames = 0;
            _capturePending = false;
            EditorApplication.update += OnEditorUpdate;

            Debug.Log("[WorldMapResolutionValidationTool] Started validation capture set.");
        }

        private static void OnEditorUpdate()
        {
            if (!_running)
            {
                EditorApplication.update -= OnEditorUpdate;
                return;
            }

            if (_waitFrames > 0)
            {
                _waitFrames--;
                return;
            }

            if (_capturePending)
            {
                CaptureCurrentPreset();
                _capturePending = false;
                _waitFrames = 12;
                return;
            }

            _index++;
            if (_index >= Presets.Length)
            {
                _running = false;
                EditorApplication.update -= OnEditorUpdate;
                AssetDatabase.Refresh();
                Debug.Log("[WorldMapResolutionValidationTool] Completed validation capture set.");
                return;
            }

            ResolutionPreset preset = Presets[_index];
            bool changed = TrySetGameViewSize(preset.Width, preset.Height, preset.Name);
            if (!changed)
            {
                Debug.LogWarning($"[WorldMapResolutionValidationTool] Failed to set GameView size: {preset.Width}x{preset.Height}");
            }

            _capturePending = true;
            _waitFrames = 12;
        }

        private static void CaptureCurrentPreset()
        {
            ResolutionPreset preset = Presets[_index];
            string fileName = $"{CapturePrefix}_{preset.Width}x{preset.Height}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = Path.Combine(_outputDir, fileName).Replace('\\', '/');
            ScreenCapture.CaptureScreenshot(fullPath, 1);
            Debug.Log($"[WorldMapResolutionValidationTool] Saved: {fullPath}");
        }

        private static bool TrySetGameViewSize(int width, int height, string label)
        {
            try
            {
                Assembly editorAssembly = typeof(EditorWindow).Assembly;
                Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type singletonTemplate = editorAssembly.GetType("UnityEditor.ScriptableSingleton`1");
                if (gameViewType == null || sizesType == null || sizeType == null || singletonTemplate == null)
                {
                    return TryResizeGameViewWindow(gameViewType, width, height);
                }

                Type singletonType = singletonTemplate.MakeGenericType(sizesType);
                object sizesInstance = singletonType.GetProperty("instance").GetValue(null, null);
                if (sizesInstance == null)
                {
                    return TryResizeGameViewWindow(gameViewType, width, height);
                }

                object currentGroupType = null;
                PropertyInfo currentGroupTypeProperty = sizesType.GetProperty("currentGroupType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (currentGroupTypeProperty != null)
                {
                    currentGroupType = currentGroupTypeProperty.GetValue(sizesInstance, null);
                }

                if (currentGroupType == null)
                {
                    MethodInfo getCurrentGroupTypeMethod = sizesType.GetMethod("GetCurrentGroupType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (getCurrentGroupTypeMethod != null)
                    {
                        currentGroupType = getCurrentGroupTypeMethod.Invoke(sizesInstance, null);
                    }
                }

                if (currentGroupType == null)
                {
                    return TryResizeGameViewWindow(gameViewType, width, height);
                }

                MethodInfo getGroup = sizesType.GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (getGroup == null)
                {
                    return TryResizeGameViewWindow(gameViewType, width, height);
                }

                object group = getGroup.Invoke(sizesInstance, new[] { currentGroupType });
                if (group == null)
                {
                    return TryResizeGameViewWindow(gameViewType, width, height);
                }

                MethodInfo getBuiltinCount = group.GetType().GetMethod("GetBuiltinCount");
                MethodInfo getCustomCount = group.GetType().GetMethod("GetCustomCount");
                MethodInfo getGameViewSize = group.GetType().GetMethod("GetGameViewSize");
                MethodInfo addCustomSize = group.GetType().GetMethod("AddCustomSize");
                if (getBuiltinCount == null || getCustomCount == null || getGameViewSize == null || addCustomSize == null)
                {
                    return TryResizeGameViewWindow(gameViewType, width, height);
                }

                int builtinCount = (int)getBuiltinCount.Invoke(group, null);
                int customCount = (int)getCustomCount.Invoke(group, null);

                string targetText = $"{label} ({width}x{height})";
                int foundIndex = -1;
                for (int i = 0; i < builtinCount + customCount; i++)
                {
                    object currentSize = getGameViewSize.Invoke(group, new object[] { i });
                    string currentText = currentSize.ToString();
                    if (string.Equals(currentText, targetText, StringComparison.Ordinal))
                    {
                        foundIndex = i;
                        break;
                    }
                }

                if (foundIndex < 0)
                {
                    ConstructorInfo ctor = sizeType.GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(string) });
                    if (ctor == null)
                    {
                        return TryResizeGameViewWindow(gameViewType, width, height);
                    }

                    object newSize = ctor.Invoke(new object[] { 1, width, height, label });
                    addCustomSize.Invoke(group, new[] { newSize });
                    customCount = (int)getCustomCount.Invoke(group, null);
                    foundIndex = builtinCount + customCount - 1;
                }

                EditorWindow gameViewWindow = EditorWindow.GetWindow(gameViewType);
                if (gameViewWindow == null)
                {
                    return TryResizeGameViewWindow(gameViewType, width, height);
                }

                PropertyInfo selectedSizeIndex = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (selectedSizeIndex == null)
                {
                    return TryResizeGameViewWindow(gameViewType, width, height);
                }

                selectedSizeIndex.SetValue(gameViewWindow, foundIndex, null);
                gameViewWindow.Repaint();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WorldMapResolutionValidationTool] Reflection failed: {ex.Message}");
                Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                return TryResizeGameViewWindow(gameViewType, width, height);
            }
        }

        private static bool TryResizeGameViewWindow(Type gameViewType, int width, int height)
        {
            if (gameViewType == null)
            {
                return false;
            }

            EditorWindow gameViewWindow = EditorWindow.GetWindow(gameViewType);
            if (gameViewWindow == null)
            {
                return false;
            }

            Rect position = gameViewWindow.position;
            position.width = Mathf.Max(320, width);
            position.height = Mathf.Max(180, height);
            gameViewWindow.position = position;
            gameViewWindow.Repaint();

            return true;
        }
    }
}
