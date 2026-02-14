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

            try
            {
                var json = File.ReadAllText(SaveFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var container = JsonUtility.FromJson<SaveDataContainer>(json);
                if (container == null || container.StageProgressList == null)
                {
                    return;
                }

                for (int i = 0; i < container.StageProgressList.Count; i++)
                {
                    var data = container.StageProgressList[i];
                    if (data == null)
                    {
                        continue;
                    }

                    _stageProgressMap[data.StageId] = data;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserSaveData] Load failed: {e}");
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
    }
}
