using Common.Patterns;
using Kingdom.WorldMap;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Kingdom.Save
{
    /// <summary>
    /// UserSaveData 단일 인스턴스를 관리하는 글로벌 저장 매니저.
    /// </summary>
    public class SaveManager : MonoSingleton<SaveManager>
    {
        private UserSaveData _saveData;

        public UserSaveData SaveData
        {
            get
            {
                if (_saveData == null)
                {
                    _saveData = new UserSaveData();
                }

                return _saveData;
            }
        }

        protected override void OnSingletonAwake()
        {
            _saveData = new UserSaveData();
            Debug.Log("[SaveManager] Initialized.");
        }

        public void Reload()
        {
            _saveData = new UserSaveData();
        }

        public void ResetAll()
        {
            SaveData.ResetAll();
        }

        [ContextMenu("Run Save Compatibility SelfTest")]
        private void RunSaveCompatibilitySelfTest()
        {
            string path = SaveData.SaveFilePath;
            string original = File.Exists(path) ? File.ReadAllText(path) : null;
            bool hadOriginal = original != null;

            var tests = new List<(string name, string json, int stageId, int stars)>
            {
                (
                    "CurrentSchema",
                    "{ \"StageProgressList\": [ { \"StageId\": 101, \"IsCleared\": true, \"BestStars\": 3, \"BestClearTimeSeconds\": 77.7, \"BestDifficulty\": 1 } ], \"LastSavedUtcTicks\": 123 }",
                    101,
                    3
                ),
                (
                    "LegacyStageProgress",
                    "{ \"StageProgress\": [ { \"StageId\": 102, \"IsCleared\": true, \"BestStars\": 2, \"BestClearTimeSeconds\": 88.8, \"BestDifficulty\": 0 } ] }",
                    102,
                    2
                ),
                (
                    "LegacyStages",
                    "{ \"Stages\": [ { \"StageId\": 103, \"IsCleared\": true, \"BestStars\": 1, \"BestClearTimeSeconds\": 99.9, \"BestDifficulty\": 2 } ] }",
                    103,
                    1
                ),
                (
                    "RawArray",
                    "[ { \"StageId\": 104, \"IsCleared\": true, \"BestStars\": 3, \"BestClearTimeSeconds\": 66.6, \"BestDifficulty\": 1 } ]",
                    104,
                    3
                ),
                (
                    "CamelCase",
                    "{ \"stageProgress\": [ { \"stageId\": 105, \"isCleared\": true, \"bestStars\": 2, \"bestClearTimeSeconds\": 55.5, \"bestDifficulty\": 0 } ] }",
                    105,
                    2
                ),
            };

            int pass = 0;
            int fail = 0;

            try
            {
                for (int i = 0; i < tests.Count; i++)
                {
                    var test = tests[i];
                    File.WriteAllText(path, test.json);
                    var probe = new UserSaveData();
                    var progress = probe.GetStageProgress(test.stageId);
                    bool ok = progress != null && progress.IsCleared && progress.BestStars == test.stars;
                    if (ok) pass++;
                    else fail++;

                    Debug.Log($"[SaveManager] Compatibility test {test.name}: {(ok ? "PASS" : "FAIL")} stage={test.stageId} stars={progress?.BestStars}");
                }
            }
            catch (Exception e)
            {
                fail++;
                Debug.LogError($"[SaveManager] Compatibility self-test failed with exception: {e}");
            }
            finally
            {
                try
                {
                    if (hadOriginal)
                    {
                        File.WriteAllText(path, original);
                    }
                    else if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception restoreException)
                {
                    Debug.LogError($"[SaveManager] Compatibility self-test restore failed: {restoreException}");
                }

                Reload();
            }

            Debug.Log($"[SaveManager] Compatibility self-test finished. pass={pass}, fail={fail}, total={tests.Count}");
        }
    }
}
