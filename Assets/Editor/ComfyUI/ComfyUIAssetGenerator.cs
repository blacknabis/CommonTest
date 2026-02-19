using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

namespace CatSudoku.Editor
{
    /// <summary>
    /// Editor tool to generate UI assets via ComfyUI with transparent backgrounds for 9-slice.
    /// Uses the project's workflow_api.json template.
    /// </summary>
    public static class ComfyUIAssetGenerator
    {
        private const string WorkflowPath = "Assets/Editor/ComfyUI/workflow_api.json";
        private const string OutputFolder = "Assets/Resources/Generated/Images";

        [MenuItem("Tools/ComfyUI/Generate UI Panel (9-slice)")]
        public static async void GenerateUIPanelAsync()
        {
            await GenerateAsset(
                "A game UI panel frame, rounded rectangle corners, dark semi-transparent background, clean minimalist design, no text, centered composition, transparent background outside panel, icon style",
                "UI_Panel_9slice.png"
            );
        }

        [MenuItem("Tools/ComfyUI/Generate UI Button (9-slice)")]
        public static async void GenerateUIButtonAsync()
        {
            await GenerateAsset(
                "A small game UI button, rounded rectangle, subtle gradient, clean minimalist design, no text, centered composition, transparent background, icon style",
                "UI_Button_9slice.png"
            );
        }

        [MenuItem("Tools/ComfyUI/Generate Close Icon")]
        public static async void GenerateCloseIconAsync()
        {
            await GenerateAsset(
                "A simple X close button icon, white color, minimalist design, transparent background, UI icon style",
                "UI_Icon_Close.png"
            );
        }

        [MenuItem("Tools/ComfyUI/Generate Toggle On Icon")]
        public static async void GenerateToggleOnAsync()
        {
            await GenerateAsset(
                "A toggle switch in ON position, green color, rounded pill shape, minimalist UI design, transparent background, icon style",
                "UI_Toggle_On.png"
            );
        }

        [MenuItem("Tools/ComfyUI/Generate Toggle Off Icon")]
        public static async void GenerateToggleOffAsync()
        {
            await GenerateAsset(
                "A toggle switch in OFF position, gray color, rounded pill shape, minimalist UI design, transparent background, icon style",
                "UI_Toggle_Off.png"
            );
        }

        [MenuItem("Tools/ComfyUI/Generate All Settings Assets")]
        public static async void GenerateAllSettingsAssetsAsync()
        {
            Debug.Log("[ComfyUI] Generating all Settings UI assets...");
            
            await GenerateAsset(
                "A game UI panel frame, rounded rectangle corners, dark semi-transparent background, clean minimalist design, no text, centered composition, transparent background outside panel, pixel art style",
                "UI_Panel_9slice.png"
            );
            
            await GenerateAsset(
                "A simple X close button icon, white color, minimalist design, transparent background, pixel art icon style",
                "UI_Icon_Close.png"
            );
            
            await GenerateAsset(
                "A toggle switch in ON position, green color, rounded pill shape, minimalist UI design, transparent background, pixel art icon style",
                "UI_Toggle_On.png"
            );
            
            await GenerateAsset(
                "A toggle switch in OFF position, gray color, rounded pill shape, minimalist UI design, transparent background, pixel art icon style",
                "UI_Toggle_Off.png"
            );
            
            Debug.Log("[ComfyUI] All Settings UI assets generated!");
        }

        [MenuItem("Tools/ComfyUI/Generate All Game Assets")]
        public static async void GenerateAllGameAssetsAsync()
        {
            Debug.Log("[ComfyUI] Starting batch generation for Game Assets...");

            // 1. Numbers 1-9 (Updated for cleaner look)
            for (int i = 1; i <= 9; i++)
            {
                await GenerateAsset(
                    $"Pixel art number {i}, solid black digit, thick distinct lines, high contrast, minimalist, transparent background, UI icon style",
                    $"Num_{i:D2}.png"
                );
            }

            // 2. Game Icons
            await GenerateAsset("Back arrow button icon, left pointing, pixel art, transparent background", "Btn_Back.png");
            await GenerateAsset("Undo arrow icon, curved counter-clockwise, pixel art, transparent background", "Icon_Undo.png");
            await GenerateAsset("Eraser tool icon, rubber, pixel art, transparent background", "Icon_Erase.png");
            await GenerateAsset("Pencil tool icon, memo note, pixel art, transparent background", "Icon_Note.png");
            await GenerateAsset("Lightbulb icon, hint idea, yellow glow, pixel art, transparent background", "Icon_Hint.png");

            // 3. Info Icons
            await GenerateAsset("Red cross mark icon, error mistake indicator, pixel art, transparent background", "Icon_Mistake.png");
            await GenerateAsset("Golden star icon, score achievement, pixel art, transparent background", "Icon_Score.png");
            await GenerateAsset("Clock hourglass icon, time timer, pixel art, transparent background", "Icon_Time.png");

            // 4. Board & Cats & Cells
            await GenerateAsset("Sudoku 9x9 grid board background frame, square, clean lines, minimalist, transparent background", "UI_Panel_Board.png");
            
            // New: Cell Background (Cleaner, less cluttered)
            await GenerateAsset("Sudoku cell background square, light cream paper texture, subtle noise, minimalist, clean, no border", "Cell_Background.png");
            
            await GenerateAsset("Cute pixel art cat face, neutral expression, detailed, transparent background", "Cat_Normal.png");
            await GenerateAsset("Cute pixel art cat face, happy smiling expression, detailed, transparent background", "Cat_Happy.png");
            await GenerateAsset("Cute pixel art cat face, sad crying expression, detailed, transparent background", "Cat_Sad.png");
            await GenerateAsset("Cute pixel art cat face, angry expression, detailed, transparent background", "Cat_Angry.png");

            Debug.Log("[ComfyUI] Batch generation complete!");
        }

        [MenuItem("Tools/ComfyUI/Generate Sound On Icon")]
        public static async void GenerateSoundOnAsync()
        {
            await GenerateAsset(
                "A speaker icon with sound waves, audio on symbol, white color, minimalist design, transparent background, pixel art icon style",
                "UI_Sound_On.png"
            );
        }

        [MenuItem("Tools/ComfyUI/Generate Sound Off Icon")]
        public static async void GenerateSoundOffAsync()
        {
            await GenerateAsset(
                "A speaker icon with X mark, muted audio symbol, white color with red X, minimalist design, transparent background, pixel art icon style",
                "UI_Sound_Off.png"
            );
        }

        private static async Task GenerateAsset(string prompt, string outputFilename)
        {
            EditorUtility.DisplayProgressBar("ComfyUI", $"Generating {outputFilename}...", 0.1f);

            try
            {
                // Load workflow template
                if (!File.Exists(WorkflowPath))
                {
                    Debug.LogError($"[ComfyUI] Workflow not found: {WorkflowPath}");
                    return;
                }

                string workflowJson = File.ReadAllText(WorkflowPath);
                
                // Replace %PROMPT% placeholder
                workflowJson = workflowJson.Replace("%PROMPT%", prompt);
                
                // Randomize seed
                var random = new System.Random();
                long newSeed = (long)(random.NextDouble() * 999999999999999);
                workflowJson = System.Text.RegularExpressions.Regex.Replace(
                    workflowJson,
                    @"""seed"":\s*\d+",
                    $"\"seed\": {newSeed}"
                );

                var workflow = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowJson);

                string promptId = await ComfyUIManager.QueuePrompt(workflow);
                if (string.IsNullOrEmpty(promptId))
                {
                    Debug.LogError("[ComfyUI] Failed to queue prompt");
                    return;
                }

                Debug.Log($"[ComfyUI] Prompt queued: {promptId}");
                EditorUtility.DisplayProgressBar("ComfyUI", "Waiting for generation...", 0.3f);

                // Poll for completion
                HistoryData history = null;
                for (int i = 0; i < 120; i++) // Max 2 minutes
                {
                    await Task.Delay(1000);
                    history = await ComfyUIManager.GetHistory(promptId);
                    if (history?.outputs != null && history.outputs.Count > 0)
                        break;
                    EditorUtility.DisplayProgressBar("ComfyUI", $"Generating... ({i}s)", 0.3f + (i / 120f) * 0.5f);
                }

                if (history?.outputs == null)
                {
                    Debug.LogError("[ComfyUI] Generation timed out");
                    return;
                }

                // Find the image output
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

                            // Set import settings for 9-slice
                            SetSpriteImportSettings(path);

                            Debug.Log($"[ComfyUI] Asset saved to: {path}");
                        }
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ComfyUI] Error: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void SetSpriteImportSettings(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(16, 16, 16, 16); // 9-slice border
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
    }
}

