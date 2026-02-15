using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Kingdom.Game;

namespace Kingdom.WorldMap
{
    public static class WorldMapStageConfigGenerator
    {
        private const string AssetFolderPath = "Assets/Resources/Data/StageConfigs";
        private const string AssetPath = AssetFolderPath + "/World1_StageConfig.asset";
        private const string WaveConfigFolderPath = "Assets/Resources/Data/WaveConfigs";

        [MenuItem("Kingdom/WorldMap/Generate World1 StageConfig")]
        public static void GenerateWorld1StageConfig()
        {
            EnsureFolderPath(AssetFolderPath);
            EnsureFolderPath(WaveConfigFolderPath);

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
                    WaveConfig = GetOrCreateDefaultWaveConfig(1, false),
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
                    WaveConfig = GetOrCreateDefaultWaveConfig(2, false),
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
                    WaveConfig = GetOrCreateDefaultWaveConfig(3, false),
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
                    WaveConfig = GetOrCreateDefaultWaveConfig(4, false),
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
                    WaveConfig = GetOrCreateDefaultWaveConfig(5, true),
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

        [MenuItem("Kingdom/WorldMap/Bind StageConfig -> WaveConfigs")]
        public static void BindWaveConfigsToStageConfig()
        {
            EnsureFolderPath(WaveConfigFolderPath);

            StageConfig config = AssetDatabase.LoadAssetAtPath<StageConfig>(AssetPath);
            if (config == null || config.Stages == null || config.Stages.Count == 0)
            {
                Debug.LogWarning("[WorldMapStageConfigGenerator] StageConfig not found or empty.");
                return;
            }

            var stages = config.Stages;
            bool changed = false;
            for (int i = 0; i < stages.Count; i++)
            {
                StageData stage = stages[i];
                WaveConfig waveConfig = GetOrCreateDefaultWaveConfig(stage.StageId, stage.IsBoss);
                if (stage.WaveConfig != waveConfig)
                {
                    stage.WaveConfig = waveConfig;
                    stages[i] = stage;
                    changed = true;
                }
            }

            if (changed)
            {
                config.Stages = stages;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.Refresh();
            Debug.Log("[WorldMapStageConfigGenerator] StageConfig -> WaveConfig 바인딩 완료.");
        }

        private static WaveConfig GetOrCreateDefaultWaveConfig(int stageId, bool isBossStage)
        {
            string path = $"{WaveConfigFolderPath}/Stage_{stageId}_WaveConfig.asset";
            WaveConfig waveConfig = AssetDatabase.LoadAssetAtPath<WaveConfig>(path);
            if (waveConfig == null)
            {
                waveConfig = ScriptableObject.CreateInstance<WaveConfig>();
                AssetDatabase.CreateAsset(waveConfig, path);
            }

            waveConfig.StageId = stageId;
            waveConfig.InitialGold = 100;
            waveConfig.InitialLives = 20;
            waveConfig.StarThresholds = new[] { 20, 15 };
            waveConfig.Waves = BuildDefaultWaves(isBossStage);

            EditorUtility.SetDirty(waveConfig);
            return waveConfig;
        }

        private static List<WaveConfig.WaveData> BuildDefaultWaves(bool isBossStage)
        {
            var waves = new List<WaveConfig.WaveData>(3);
            for (int i = 0; i < 3; i++)
            {
                int waveIndex = i + 1;
                bool isBossWave = isBossStage && waveIndex == 3;
                int count = isBossWave ? 1 : (3 + i * 2);
                float interval = isBossWave ? 1.2f : Mathf.Max(0.45f, 0.75f - i * 0.1f);

                waves.Add(new WaveConfig.WaveData
                {
                    WaveIndex = waveIndex,
                    BonusGoldOnEarlyCall = 0,
                    IsBossWave = isBossWave,
                    SpawnEntries = new List<WaveConfig.SpawnEntry>
                    {
                        new WaveConfig.SpawnEntry
                        {
                            Enemy = null,
                            Count = count,
                            SpawnInterval = interval,
                            PathId = 0,
                            SpawnDelay = 0f
                        }
                    }
                });
            }

            return waves;
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
