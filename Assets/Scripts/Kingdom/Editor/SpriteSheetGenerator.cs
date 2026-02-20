using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using CatSudoku.Editor;

namespace Kingdom.Editor
{
    /// <summary>
    /// Editor tool to generate character sprite sheets using an I2V pipeline.
    /// Step 1: Generate Base Image
    /// Step 2: (External) ComfyUI I2V generation
    /// Step 3: Stitch output frames into a single SpriteSheet.
    /// </summary>
    public static class I2VSpritePipeline
    {
        private const string BaseWorkflowPath = "Assets/Editor/ComfyUI/workflow_hero_base.json";
        private const string I2VWorkflowPath = "Assets/Editor/ComfyUI/workflow_hero_i2v.json";
        private const string OutputFolder = "Assets/Resources/Generated/SpriteSheets";

        // Base style prompts for the right-facing base character
        // 배경색은 프롬프트로 제어 불가 -> C# 후처리(Flood Fill)로 마젠타 교체
        private const string StylePrefix = "masterpiece, best quality, 2d game asset, character design, perfectly side view, facing right, standing still, standard pose, full body, chibi, super deformed, sd character, 3 heads tall, cute proportions, big head, short body, simple solid color background, isolated single character, no UI, no text, no arrows, no reference boxes, clean image, "; 
        private const string NegativePrompt = "facing forward, facing camera, back view, realistic proportions, 8 heads tall, tall character, realistic, photorealistic, 3d, shadow, background scenery, landscape, text, font, letters, typography, watermark, signature, UI, interface, menu, border, frame, cinematic, blurry, cropped, out of frame, dialogue, sketch, bad anatomy, multiple characters, reference sheet, arrows, diagrams, zoom boxes, split screen";
        
        // Use anime model for heroes
        private const string ModelName = "waiIllustriousSDXL_v160.safetensors";

        // 배경 교체 허용 색상 차이 (0~1 범위)
        private const float BackgroundTolerance = 0.25f;

        [MenuItem("Tools/ComfyUI/I2V Pipeline/1. Generate Base Hero (Knight)")]
        public static async void GenerateKnightBaseAsync()
        {
            await GenerateBaseImage(
                "1boy, male knight, silver armor, holding sword and shield, fantasy armor, neutral expression",
                "Hero_Knight_Base.png"
            );
        }

        private static async Task GenerateBaseImage(string corePrompt, string outputFilename)
        {
            EditorUtility.DisplayProgressBar("ComfyUI Base Generator", $"Generating {outputFilename}...", 0.1f);

            try
            {
                if (!File.Exists(BaseWorkflowPath))
                {
                    Debug.LogError($"[I2VSpritePipeline] Workflow not found: {BaseWorkflowPath}");
                    return;
                }

                string workflowJson = File.ReadAllText(BaseWorkflowPath);

                string fullPrompt = $"{StylePrefix}{corePrompt}";
                workflowJson = workflowJson.Replace("%PROMPT%", fullPrompt);
                workflowJson = workflowJson.Replace("%NEGATIVE_PROMPT%", NegativePrompt);
                workflowJson = workflowJson.Replace("%MODEL_NAME%", ModelName);

                var random = new System.Random();
                long newSeed = (long)(random.NextDouble() * 999999999999999);
                workflowJson = workflowJson.Replace("\"%SEED%\"", newSeed.ToString());

                var workflow = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowJson);
                string promptId = await ComfyUIManager.QueuePrompt(workflow);

                if (string.IsNullOrEmpty(promptId))
                {
                    Debug.LogError("[I2VSpritePipeline] Failed to queue prompt. Is ComfyUI running?");
                    return;
                }

                Debug.Log($"[I2VSpritePipeline] Prompt queued: {promptId}");
                EditorUtility.DisplayProgressBar("ComfyUI Base Generator", "Waiting for generation...", 0.3f);

                HistoryData history = null;
                for (int i = 0; i < 180; i++)
                {
                    await Task.Delay(1000);
                    history = await ComfyUIManager.GetHistory(promptId);
                    if (history?.outputs != null && history.outputs.Count > 0)
                        break;
                    EditorUtility.DisplayProgressBar("ComfyUI Base Generator", $"Generating... ({i}s)", 0.3f + (i / 180f) * 0.5f);
                }

                if (history?.outputs == null)
                {
                    Debug.LogError("[I2VSpritePipeline] Generation timed out.");
                    return;
                }

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

                            // 후처리: 배경을 마젠타(FF00FF)로 자동 교체
                            EditorUtility.DisplayProgressBar("ComfyUI Base Generator", "Removing background...", 0.9f);
                            ReplaceBackgroundWithMagenta(path);

                            AssetDatabase.Refresh();
                            SetTextureImportSettings(path);

                            Debug.Log($"[I2VSpritePipeline] Base Image saved to: {path} (background removed)");
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
                Debug.LogError($"[I2VSpritePipeline] Error: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// 이미지의 네 꼭짓점에서 Flood Fill을 수행하여 배경 영역을 투명(Alpha=0)으로 제거합니다.
        /// AI 모델이 프롬프트로 배경색을 제어할 수 없는 한계를 극복하기 위한 후처리입니다.
        /// </summary>
        private static void ReplaceBackgroundWithMagenta(string imagePath)
        {
            byte[] rawData = File.ReadAllBytes(imagePath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(rawData);

            int w = tex.width;
            int h = tex.height;
            Color[] pixels = tex.GetPixels();
            bool[] visited = new bool[w * h];
            Color transparent = new Color(0f, 0f, 0f, 0f); // 완전 투명

            // 네 꼭짓점에서 Flood Fill 시작
            Vector2Int[] corners = new Vector2Int[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(w - 1, 0),
                new Vector2Int(0, h - 1),
                new Vector2Int(w - 1, h - 1)
            };

            foreach (var corner in corners)
            {
                int idx = corner.y * w + corner.x;
                if (visited[idx]) continue;

                Color seedColor = pixels[idx];
                FloodFill(pixels, visited, w, h, corner.x, corner.y, seedColor, transparent);
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] pngBytes = tex.EncodeToPNG();
            File.WriteAllBytes(imagePath, pngBytes);

            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log($"[I2VSpritePipeline] Background removed (transparent) for: {imagePath}");
        }

        /// <summary>
        /// 스택 기반 Flood Fill. seedColor와 유사한 색상을 모두 replacementColor로 교체합니다.
        /// </summary>
        private static void FloodFill(Color[] pixels, bool[] visited, int w, int h, int startX, int startY, Color seedColor, Color replacementColor)
        {
            Stack<Vector2Int> stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(startX, startY));

            while (stack.Count > 0)
            {
                var pos = stack.Pop();
                int x = pos.x;
                int y = pos.y;

                if (x < 0 || x >= w || y < 0 || y >= h) continue;

                int idx = y * w + x;
                if (visited[idx]) continue;
                visited[idx] = true;

                Color current = pixels[idx];
                if (!IsColorSimilar(current, seedColor, BackgroundTolerance)) continue;

                pixels[idx] = replacementColor;

                // 상하좌우 4방향 탐색
                stack.Push(new Vector2Int(x + 1, y));
                stack.Push(new Vector2Int(x - 1, y));
                stack.Push(new Vector2Int(x, y + 1));
                stack.Push(new Vector2Int(x, y - 1));
            }
        }

        /// <summary>
        /// 두 색상이 허용 범위 내에서 유사한지 비교합니다.
        /// </summary>
        private static bool IsColorSimilar(Color a, Color b, float tolerance)
        {
            return Mathf.Abs(a.r - b.r) < tolerance &&
                   Mathf.Abs(a.g - b.g) < tolerance &&
                   Mathf.Abs(a.b - b.b) < tolerance;
        }

        private static void SetTextureImportSettings(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point; 
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.isReadable = true; // Important for using it as an input to I2V or processor
                importer.SaveAndReimport();
            }
        }

        [MenuItem("Tools/ComfyUI/I2V Pipeline/2. Generate Walk Animation (Knight)")]
        public static async void GenerateKnightWalkI2VAsync()
        {
            await GenerateI2VSequence(
                "Hero_Knight_Base.png",
                "walking forward, swinging arms, stepping, dynamic movement",
                "Hero_Knight_Walk"
            );
        }

        private static async Task GenerateI2VSequence(string baseImageFilename, string actionPrompt, string outputPrefix)
        {
            EditorUtility.DisplayProgressBar("ComfyUI I2V Animation", $"Generating {outputPrefix} sequence...", 0.1f);

            try
            {
                if (!File.Exists(I2VWorkflowPath))
                {
                    Debug.LogError($"[I2VSpritePipeline] Workflow not found: {I2VWorkflowPath}");
                    return;
                }

                string baseImagePath = $"{OutputFolder}/{baseImageFilename}";
                if (!File.Exists(baseImagePath))
                {
                    Debug.LogError($"[I2VSpritePipeline] Base image not found: {baseImagePath}. Please run Step 1 first.");
                    return;
                }

                // Upload base image to ComfyUI
                byte[] baseImageData = File.ReadAllBytes(baseImagePath);
                bool uploaded = await ComfyUIManager.UploadImage(baseImageData, baseImageFilename);
                if (!uploaded) return;

                string workflowJson = File.ReadAllText(I2VWorkflowPath);

                string fullPrompt = $"{StylePrefix}{actionPrompt}";
                workflowJson = workflowJson.Replace("%PROMPT%", fullPrompt);
                workflowJson = workflowJson.Replace("%NEGATIVE_PROMPT%", NegativePrompt);
                
                // For AnimateDiff SD1.5, we use checkpoint & motion module combinations typically seen in LCM setups.
                workflowJson = workflowJson.Replace("%MODEL_NAME%", "DreamShaper_8_pruned.safetensors"); 
                workflowJson = workflowJson.Replace("%MOTION_MODEL%", "AnimateLCM_sd15_t2v.ckpt");

                var random = new System.Random();
                long newSeed = (long)(random.NextDouble() * 999999999999999);
                workflowJson = workflowJson.Replace("\"%SEED%\"", newSeed.ToString());

                var workflow = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowJson);
                string promptId = await ComfyUIManager.QueuePrompt(workflow);

                if (string.IsNullOrEmpty(promptId))
                {
                    Debug.LogError("[I2VSpritePipeline] Failed to queue I2V prompt.");
                    return;
                }

                Debug.Log($"[I2VSpritePipeline] I2V Prompt queued: {promptId}");
                EditorUtility.DisplayProgressBar("ComfyUI I2V Animation", "Waiting for video generation...", 0.3f);

                HistoryData history = null;
                for (int i = 0; i < 300; i++) // I2V usually takes a bit longer
                {
                    await Task.Delay(1000);
                    history = await ComfyUIManager.GetHistory(promptId);
                    if (history?.outputs != null && history.outputs.Count > 0)
                        break;
                    EditorUtility.DisplayProgressBar("ComfyUI I2V Animation", $"Generating Frames... ({i}s)", 0.3f + (i / 300f) * 0.5f);
                }

                if (history?.outputs == null)
                {
                    Debug.LogError("[I2VSpritePipeline] Generation timed out.");
                    return;
                }

                string outputDir = $"{OutputFolder}/{outputPrefix}";
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                foreach (var nodeOutput in history.outputs.Values)
                {
                    if (nodeOutput.images != null && nodeOutput.images.Length > 0)
                    {
                        for (int i = 0; i < nodeOutput.images.Length; i++)
                        {
                            var img = nodeOutput.images[i];
                            byte[] imageData = await ComfyUIManager.GetImage(img.filename, img.subfolder, img.type);

                            if (imageData != null)
                            {
                                string path = $"{outputDir}/{outputPrefix}_{i:D2}.png";
                                File.WriteAllBytes(path, imageData);
                            }
                        }
                        
                        AssetDatabase.Refresh();
                        Debug.Log($"[I2VSpritePipeline] Sequence saved to: {outputDir}");
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputDir);
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[I2VSpritePipeline] Error: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/ComfyUI/I2V Pipeline/3. Stitch Frames to SpriteSheet")]
        public static void StitchFrames()
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Folder containing Image Frames", "Assets", "");
            if (string.IsNullOrEmpty(folderPath)) return;

            string[] files = Directory.GetFiles(folderPath, "*.png");
            if (files.Length == 0)
            {
                Debug.LogError("[I2VSpritePipeline] No PNG frames found in the selected folder.");
                return;
            }

            Array.Sort(files); // Ensure frames are stitched in order

            List<Texture2D> frames = new List<Texture2D>();
            foreach (var file in files)
            {
                byte[] fileData = File.ReadAllBytes(file);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
                frames.Add(tex);
            }

            if (frames.Count == 0 || frames[0] == null) return;

            int width = frames[0].width;
            int height = frames[0].height;

            Texture2D sheet = new Texture2D(width * frames.Count, height, TextureFormat.RGBA32, false);

            for (int i = 0; i < frames.Count; i++)
            {
                Color[] pixels = frames[i].GetPixels();
                sheet.SetPixels(i * width, 0, width, height, pixels);
            }
            sheet.Apply();

            byte[] bytes = sheet.EncodeToPNG();
            
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);

            string directoryName = new DirectoryInfo(folderPath).Name;
            string outputPath = $"{OutputFolder}/{directoryName}_Sheet.png";
            
            File.WriteAllBytes(outputPath, bytes);
            AssetDatabase.Refresh();

            // Set Import Settings for Multiple
            AssetDatabase.ImportAsset(outputPath);
            var importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                
                // Set Sprite Metadata
                SpriteMetaData[] metaData = new SpriteMetaData[frames.Count];
                for (int i = 0; i < frames.Count; i++)
                {
                    metaData[i] = new SpriteMetaData
                    {
                        name = $"{directoryName}_{i}",
                        rect = new Rect(i * width, 0, width, height),
                        alignment = 0,
                        pivot = new Vector2(0.5f, 0.5f)
                    };
                }
                importer.spritesheet = metaData;
                importer.SaveAndReimport();
            }

            Debug.Log($"[I2VSpritePipeline] Successfully stitched {frames.Count} frames into {outputPath}");
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputPath);
            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }
    }
}
