using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kingdom.Editor
{
    public static class GameViewScreenshotTool
    {
        [MenuItem("Tools/WorldMap/Capture GameView Screenshot")]
        public static void CaptureGameViewScreenshot()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var outputDir = Path.Combine(projectRoot, "Assets", "Screenshots");
            Directory.CreateDirectory(outputDir);

            var fileName = $"worldmap_gameview_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            var fullPath = Path.Combine(outputDir, fileName).Replace('\\', '/');
            ScreenCapture.CaptureScreenshot(fullPath, 1);

            Debug.Log($"[WorldMapScreenshot] Saved: {fullPath}");
            AssetDatabase.Refresh();
        }
    }
}
