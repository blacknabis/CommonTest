using UnityEngine;
using UnityEditor;
using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using Kingdom.App;
using Debug = UnityEngine.Debug;

namespace Kingdom.Editor
{
    public class TitleAssetGenerator : EditorWindow
    {
        private const string PythonScriptPath = "Assets/Scripts/Kingdom/Editor/comfy_bridge.py";
        private const string OutputPathRel = "Assets/Resources/UI/Title";

        [MenuItem("Tools/Generators/Title Assets (Final)")]
        public static void ShowWindow()
        {
            GetWindow<TitleAssetGenerator>("Title Assets");
        }

        private void OnGUI()
        {
            GUILayout.Label("Title Scene Asset Generator (Python Bridge)", EditorStyles.boldLabel);
            GUILayout.Label("Uses external python script to bypass Unity networking issues.");

            if (GUILayout.Button("1. Generate All Assets (Run Python)"))
            {
                this.StartCoroutine(RunPythonBridge());
            }

            if (GUILayout.Button("2. Delete Generator Script"))
            {
                DeleteScript();
            }
        }

        private IEnumerator RunPythonBridge()
        {
            string scriptFullPath = Path.GetFullPath(PythonScriptPath);
            if (!File.Exists(scriptFullPath))
            {
                Debug.LogError($"Python script not found at {scriptFullPath}");
                yield break;
            }

            Debug.Log($"[Generator] Launching Python script: {scriptFullPath}...");

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "python"; // Assumes python is in PATH
            start.Arguments = $"\"{scriptFullPath}\"";
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.CreateNoWindow = true;
            start.WorkingDirectory = Path.GetFullPath("."); // Project Root

            using (Process process = Process.Start(start))
            {
                // Async read logs
                process.OutputDataReceived += (sender, e) => { if (e.Data != null) Debug.Log($"[Python] {e.Data}"); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) Debug.LogError($"[Python Error] {e.Data}"); };
                
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                while (!process.HasExited)
                {
                    yield return null;
                }
                
                Debug.Log($"[Generator] Python process exited with code {process.ExitCode}");
            }

            Debug.Log("[Generator] Refreshing AssetDatabase...");
            AssetDatabase.Refresh();
            yield return null;
            
            AssignAssets();
        }

        private void AssignAssets()
        {
            var titleView = FindFirstObjectByType<TitleView>();
            if (titleView == null)
            {
                Debug.LogWarning("[Generator] TitleView not in scene. Trying Prefab...");
                string prefabPath = "Assets/Resources/UI/TitleView.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null) titleView = prefab.GetComponent<TitleView>();
            }

            if (titleView == null)
            {
                Debug.LogError("[Generator] TitleView Prefab not found!");
                return;
            }

            string bgPath = $"{OutputPathRel}/Title_Background.png";
            SetTextureImporter(bgPath);
            SetTextureImporter($"{OutputPathRel}/Title_Logo.png");
            SetTextureImporter($"{OutputPathRel}/Title_BtnStart.png");

            AssetDatabase.Refresh();

            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
            if (bgSprite != null)
            {
                titleView.SetBackgroundImage(bgSprite);
                EditorUtility.SetDirty(titleView);
                Debug.Log("[Generator] Background Assigned!");
            }
            
            if (PrefabUtility.IsPartOfPrefabAsset(titleView))
            {
                PrefabUtility.SavePrefabAsset(titleView.gameObject);
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log("[Generator] ALL TASKS COMPLETED.");
        }

        private void SetTextureImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
        }

        private static void DeleteScript()
        {
            AssetDatabase.DeleteAsset("Assets/Scripts/Kingdom/Editor/TitleAssetGenerator.cs");
            AssetDatabase.DeleteAsset("Assets/Scripts/Kingdom/Editor/comfy_bridge.py");
            AssetDatabase.Refresh();
            Debug.Log("Generator & Bridge Deleted.");
        }
        
        private class EditorCoroutine
        {
            public static void Start(IEnumerator routine)
            {
                UnityEditor.EditorApplication.CallbackFunction callback = null;
                callback = () =>
                {
                    try { if (!routine.MoveNext()) UnityEditor.EditorApplication.update -= callback; }
                    catch (Exception ex) { Debug.LogError(ex); UnityEditor.EditorApplication.update -= callback; }
                };
                UnityEditor.EditorApplication.update += callback;
            }
        }
        public void StartCoroutine(IEnumerator routine) => EditorCoroutine.Start(routine);
    }
}
