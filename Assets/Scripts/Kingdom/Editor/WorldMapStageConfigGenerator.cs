using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kingdom.WorldMap
{
    public static class WorldMapStageConfigGenerator
    {
        private const string AssetFolderPath = "Assets/Resources/Data/StageConfigs";
        private const string AssetPath = AssetFolderPath + "/World1_StageConfig.asset";

        [MenuItem("Kingdom/WorldMap/Generate World1 StageConfig")]
        public static void GenerateWorld1StageConfig()
        {
            EnsureFolderPath(AssetFolderPath);

            StageConfig config = AssetDatabase.LoadAssetAtPath<StageConfig>(AssetPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<StageConfig>();
                AssetDatabase.CreateAsset(config, AssetPath);
            }

            config.WorldId = 1;
            config.WorldName = "World 1";
            config.Stages = BuildWorld1Stages();

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WorldMapStageConfigGenerator] 생성 완료: {AssetPath}");
        }

        private static List<StageData> BuildWorld1Stages()
        {
            return new List<StageData>
            {
                new StageData
                {
                    StageId = 1,
                    StageName = "Stage 1",
                    Difficulty = StageDifficulty.Normal,
                    Position = new Vector2(-5f, 0f),
                    NextStageIds = new List<int> { 2 },
                    StarRequirements = new List<float> { 30f, 45f, 60f },
                    IsBoss = false,
                    IsUnlocked = true,
                    BestTime = 0f,
                },
                new StageData
                {
                    StageId = 2,
                    StageName = "Stage 2",
                    Difficulty = StageDifficulty.Casual,
                    Position = new Vector2(-2f, 2f),
                    NextStageIds = new List<int> { 3 },
                    StarRequirements = new List<float> { 30f, 45f, 60f },
                    IsBoss = false,
                    IsUnlocked = false,
                    BestTime = 0f,
                },
                new StageData
                {
                    StageId = 3,
                    StageName = "Stage 3",
                    Difficulty = StageDifficulty.Normal,
                    Position = new Vector2(1f, 0f),
                    NextStageIds = new List<int> { 4 },
                    StarRequirements = new List<float> { 30f, 45f, 60f },
                    IsBoss = false,
                    IsUnlocked = false,
                    BestTime = 0f,
                },
                new StageData
                {
                    StageId = 4,
                    StageName = "Stage 4",
                    Difficulty = StageDifficulty.Veteran,
                    Position = new Vector2(4f, 2f),
                    NextStageIds = new List<int> { 5 },
                    StarRequirements = new List<float> { 30f, 45f, 60f },
                    IsBoss = false,
                    IsUnlocked = false,
                    BestTime = 0f,
                },
                new StageData
                {
                    StageId = 5,
                    StageName = "Stage 5",
                    Difficulty = StageDifficulty.Normal,
                    Position = new Vector2(7f, 0f),
                    NextStageIds = new List<int>(),
                    StarRequirements = new List<float> { 30f, 45f, 60f },
                    IsBoss = true,
                    IsUnlocked = false,
                    BestTime = 0f,
                },
            };
        }

        private static void EnsureFolderPath(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}
