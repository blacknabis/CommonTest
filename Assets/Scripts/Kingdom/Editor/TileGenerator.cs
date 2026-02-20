using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using CatSudoku.Editor; // Using ComfyUIManager namespace

namespace Kingdom.Editor
{
    /// <summary>
    /// Editor tool to generate seamless tile textures via ComfyUI.
    /// Uses Assets/Editor/ComfyUI/workflow_tile_generation.json
    /// </summary>
    public static class TileGenerator
    {
        private const string WorkflowPath = "Assets/Editor/ComfyUI/workflow_tile_generation.json";
        private const string OutputFolder = "Assets/Resources/Generated/Tiles";

        // Common Style Prompts
        private const string StylePrefix = "2d game asset, hand-painted flat texture, perfectly flat top-down angle, seamless pattern, stylized fantasy art, no perspective, zenith view, "; 
        private const string NegativePrompt = "landscape, scenery, shore, beach, wave, perspective, horizon, sky, character, human, 3d, realistic, shadow, isometric, object, gradient, watermark, text";

        [MenuItem("Tools/ComfyUI/Generate Tile/Grass")]
        public static async void GenerateGrassAsync()
        {
            await GenerateTile(
                "green grass covering, lawn surface texture, solid foliage pattern",
                "Tile_Grass.png"
            );
        }

        [MenuItem("Tools/ComfyUI/Generate Tile/Dirt")]
        public static async void GenerateDirtAsync()
        {
            await GenerateTile(
                "brown dirt texture, dry earth surface, muddy terrain pattern",
                "Tile_Dirt.png"
            );
        }

        [MenuItem("Tools/ComfyUI/Generate Tile/Water")]
        public static async void GenerateWaterAsync()
        {
            await GenerateTile(
                "blue water texture, liquid surface pattern, calm pool",
                "Tile_Water.png"
            );
        }

        private static async Task GenerateTile(string corePrompt, string outputFilename)
        {
            EditorUtility.DisplayProgressBar("ComfyUI Tile Generator", $"Generating {outputFilename}...", 0.1f);

            try
            {
                // 1. Load Workflow Template
                if (!File.Exists(WorkflowPath))
                {
                    Debug.LogError($"[TileGenerator] Workflow not found: {WorkflowPath}");
                    return;
                }

                string workflowJson = File.ReadAllText(WorkflowPath);

                // 2. Prepare Prompt
                string fullPrompt = $"{StylePrefix}{corePrompt}";
                workflowJson = workflowJson.Replace("%PROMPT%", fullPrompt);
                workflowJson = workflowJson.Replace("%NEGATIVE_PROMPT%", NegativePrompt);
                
                // Use a standard model suitable for environments/textures (User needs to download this!)
                workflowJson = workflowJson.Replace("%MODEL_NAME%", "sd_xl_base_1.0.safetensors");

                // 3. Randomize Seed
                var random = new System.Random();
                long newSeed = (long)(random.NextDouble() * 999999999999999);
                
                // Simple string replacement for placeholders
                workflowJson = workflowJson.Replace("\"%SEED%\"", newSeed.ToString());

                // 4. Queue Prompt
                var workflow = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowJson);
                string promptId = await ComfyUIManager.QueuePrompt(workflow);

                if (string.IsNullOrEmpty(promptId))
                {
                    Debug.LogError("[TileGenerator] Failed to queue prompt. Is ComfyUI running?");
                    return;
                }

                Debug.Log($"[TileGenerator] Prompt queued: {promptId}");
                EditorUtility.DisplayProgressBar("ComfyUI Tile Generator", "Waiting for generation...", 0.3f);

                // 5. Poll for Completion
                HistoryData history = null;
                for (int i = 0; i < 120; i++) // Max 2 minutes
                {
                    await Task.Delay(1000);
                    history = await ComfyUIManager.GetHistory(promptId);
                    if (history?.outputs != null && history.outputs.Count > 0)
                        break;
                    EditorUtility.DisplayProgressBar("ComfyUI Tile Generator", $"Generating... ({i}s)", 0.3f + (i / 120f) * 0.5f);
                }

                if (history?.outputs == null)
                {
                    Debug.LogError("[TileGenerator] Generation timed out.");
                    return;
                }

                // 6. Save Image
                foreach (var nodeOutput in history.outputs.Values)
                {
                    if (nodeOutput.images != null && nodeOutput.images.Length > 0)
                    {
                        var img = nodeOutput.images[0];
                        byte[] imageData = await ComfyUIManager.GetImage(img.filename, img.subfolder, img.type);

                        if (imageData != null)
                        {
                            if (!Directory.Exists(OutputFolder))
                                Directory.CreateDirectory(OutputFolder);

                            string path = $"{OutputFolder}/{outputFilename}";
                            File.WriteAllBytes(path, imageData);
                            AssetDatabase.Refresh();

                            // 7. Set Import Settings (Seamless Check)
                            SetTileImportSettings(path);

                            Debug.Log($"[TileGenerator] Tile saved to: {path}");
                            // Select the asset to show it to the user
                            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                            EditorGUIUtility.PingObject(obj);
                            Selection.activeObject = obj;
                        }
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TileGenerator] Error: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void SetTileImportSettings(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default; // Default is better for tiles than Sprite sometimes, but for 2D game Sprite is okay. 
                // Let's use Sprite but ensure Wrap Mode is Repeat
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.wrapMode = TextureWrapMode.Repeat; // Critical for seamless tiling check
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }
    }
}
