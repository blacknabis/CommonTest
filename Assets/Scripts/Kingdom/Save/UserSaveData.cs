using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Kingdom.WorldMap;

namespace Kingdom.Save
{
    /// <summary>
    /// 유저 진행 데이터 저장/로드 컨테이너.
    /// JSON 파일 기반으로 스테이지 진행 상황을 관리합니다.
    /// </summary>
    [Serializable]
    public class UserSaveData
    {
        [Serializable]
        private class SaveDataContainer
        {
            public List<StageProgressData> StageProgressList = new();
            public long LastSavedUtcTicks;
        }

        [Serializable]
        private class LegacySaveDataContainerV1
        {
            public List<StageProgressData> StageProgress;
            public List<StageProgressData> Stages;
        }

        [Serializable]
        public class StageProgressData
        {
            public int StageId;
            public bool IsCleared;
            public int BestStars;
            public float BestClearTimeSeconds;
            public StageDifficulty BestDifficulty;

            public static StageProgressData CreateDefault(int stageId)
            {
                return new StageProgressData
                {
                    StageId = stageId,
                    IsCleared = false,
                    BestStars = 0,
                    BestClearTimeSeconds = 0f,
                    BestDifficulty = StageDifficulty.Casual
                };
            }
        }

        private const string FileName = "kingdom_user_save.json";
        private const string CorruptBackupPrefix = "kingdom_user_save.corrupt_";
        private const string CorruptBackupSuffix = ".json";

        private readonly Dictionary<int, StageProgressData> _stageProgressMap = new();

        public string SaveFilePath => Path.Combine(Application.persistentDataPath, FileName);

        public UserSaveData()
        {
            Load();
        }

        public StageProgressData GetStageProgress(int stageId)
        {
            if (_stageProgressMap.TryGetValue(stageId, out var data))
            {
                return data;
            }

            var created = StageProgressData.CreateDefault(stageId);
            _stageProgressMap[stageId] = created;
            return created;
        }

        public void SetStageCleared(int stageId, int stars, float clearTimeSeconds, StageDifficulty difficulty)
        {
            var progress = GetStageProgress(stageId);

            progress.IsCleared = true;
            progress.BestStars = Mathf.Max(progress.BestStars, Mathf.Clamp(stars, 0, 3));

            if (progress.BestClearTimeSeconds <= 0f || clearTimeSeconds < progress.BestClearTimeSeconds)
            {
                progress.BestClearTimeSeconds = Mathf.Max(0f, clearTimeSeconds);
                progress.BestDifficulty = difficulty;
            }

            Save();
        }

        public IReadOnlyCollection<StageProgressData> GetAllStageProgress()
        {
            return _stageProgressMap.Values;
        }

        public void Save()
        {
            try
            {
                var container = new SaveDataContainer
                {
                    StageProgressList = new List<StageProgressData>(_stageProgressMap.Values),
                    LastSavedUtcTicks = DateTime.UtcNow.Ticks
                };

                var json = JsonUtility.ToJson(container, true);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserSaveData] Save failed: {e}");
            }
        }

        public void Load()
        {
            _stageProgressMap.Clear();

            if (!File.Exists(SaveFilePath))
            {
                return;
            }

            string json = null;
            try
            {
                json = File.ReadAllText(SaveFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    BackupCorruptSaveFile("empty");
                    Save();
                    return;
                }

                if (!TryDeserializeContainer(json, out SaveDataContainer container) || container == null || container.StageProgressList == null)
                {
                    BackupCorruptSaveFile("invalid_schema");
                    Save();
                    return;
                }

                for (int i = 0; i < container.StageProgressList.Count; i++)
                {
                    var data = container.StageProgressList[i];
                    if (data == null)
                    {
                        continue;
                    }

                    if (data.StageId <= 0)
                    {
                        continue;
                    }

                    data.BestStars = Mathf.Clamp(data.BestStars, 0, 3);
                    data.BestClearTimeSeconds = Mathf.Max(0f, data.BestClearTimeSeconds);
                    _stageProgressMap[data.StageId] = data;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserSaveData] Load failed: {e}");
                BackupCorruptSaveFile("exception");
                Save();
            }
        }

        public void ResetAll()
        {
            _stageProgressMap.Clear();

            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserSaveData] Reset failed: {e}");
            }
        }

        private void BackupCorruptSaveFile(string reason)
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    return;
                }

                string directory = Path.GetDirectoryName(SaveFilePath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    return;
                }

                string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(directory, $"{CorruptBackupPrefix}{stamp}_{reason}{CorruptBackupSuffix}");
                File.Move(SaveFilePath, backupPath);
                Debug.LogWarning($"[UserSaveData] Corrupt save file moved to backup: {backupPath}");
            }
            catch (Exception backupException)
            {
                Debug.LogError($"[UserSaveData] Corrupt backup failed: {backupException}");
            }
        }

        private static bool TryDeserializeContainer(string rawJson, out SaveDataContainer container)
        {
            container = null;
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return false;
            }

            string trimmed = rawJson.Trim();
            bool hasCurrentKey = rawJson.IndexOf("\"StageProgressList\"", StringComparison.Ordinal) >= 0;
            bool hasLegacyStageProgressKey = rawJson.IndexOf("\"StageProgress\"", StringComparison.Ordinal) >= 0;
            bool hasLegacyStagesKey = rawJson.IndexOf("\"Stages\"", StringComparison.Ordinal) >= 0;

            // Current schema (object only).
            if (!trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                if (TryParseJson(rawJson, out SaveDataContainer current) &&
                    current != null &&
                    current.StageProgressList != null &&
                    (hasCurrentKey || current.StageProgressList.Count > 0))
                {
                    container = current;
                    return true;
                }
            }

            // Legacy schema: { "StageProgress": [...] } or { "Stages": [...] }
            LegacySaveDataContainerV1 legacy = null;
            if (!trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                TryParseJson(rawJson, out legacy);
            }

            if (legacy != null)
            {
                if (legacy.StageProgress != null && (hasLegacyStageProgressKey || legacy.StageProgress.Count > 0))
                {
                    container = new SaveDataContainer
                    {
                        StageProgressList = legacy.StageProgress,
                        LastSavedUtcTicks = DateTime.UtcNow.Ticks
                    };
                    return true;
                }

                if (legacy.Stages != null && (hasLegacyStagesKey || legacy.Stages.Count > 0))
                {
                    container = new SaveDataContainer
                    {
                        StageProgressList = legacy.Stages,
                        LastSavedUtcTicks = DateTime.UtcNow.Ticks
                    };
                    return true;
                }
            }

            // Legacy raw array schema: [ { ...stageProgressData... } ]
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                string wrapped = "{\"StageProgressList\":" + trimmed + "}";
                if (TryParseJson(wrapped, out SaveDataContainer wrappedContainer) &&
                    wrappedContainer != null &&
                    wrappedContainer.StageProgressList != null)
                {
                    container = wrappedContainer;
                    return true;
                }
            }

            // Try common camelCase legacy keys.
            string normalized = NormalizeLegacyJsonKeys(rawJson);
            if (!string.Equals(normalized, rawJson, StringComparison.Ordinal))
            {
                string normalizedTrimmed = normalized.Trim();
                bool normalizedHasCurrentKey = normalized.IndexOf("\"StageProgressList\"", StringComparison.Ordinal) >= 0;
                if (!normalizedTrimmed.StartsWith("[", StringComparison.Ordinal))
                {
                    if (TryParseJson(normalized, out SaveDataContainer normalizedCurrent) &&
                        normalizedCurrent != null &&
                        normalizedCurrent.StageProgressList != null &&
                        (normalizedHasCurrentKey || normalizedCurrent.StageProgressList.Count > 0))
                    {
                        container = normalizedCurrent;
                        return true;
                    }
                }

                if (!normalizedTrimmed.StartsWith("[", StringComparison.Ordinal))
                {
                    TryParseJson(normalized, out legacy);
                }

                if (legacy != null && legacy.StageProgress != null)
                {
                    container = new SaveDataContainer
                    {
                        StageProgressList = legacy.StageProgress,
                        LastSavedUtcTicks = DateTime.UtcNow.Ticks
                    };
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseJson<T>(string json, out T result) where T : class
        {
            result = null;
            try
            {
                result = JsonUtility.FromJson<T>(json);
                return result != null;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeLegacyJsonKeys(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

            // Minimal key normalization for known legacy variants.
            string normalized = json
                .Replace("\"stageProgressList\"", "\"StageProgressList\"")
                .Replace("\"stageProgress\"", "\"StageProgress\"")
                .Replace("\"stages\"", "\"Stages\"")
                .Replace("\"lastSavedUtcTicks\"", "\"LastSavedUtcTicks\"")
                .Replace("\"stageId\"", "\"StageId\"")
                .Replace("\"isCleared\"", "\"IsCleared\"")
                .Replace("\"bestStars\"", "\"BestStars\"")
                .Replace("\"bestClearTimeSeconds\"", "\"BestClearTimeSeconds\"")
                .Replace("\"bestDifficulty\"", "\"BestDifficulty\"");
            return normalized;
        }
    }
}
