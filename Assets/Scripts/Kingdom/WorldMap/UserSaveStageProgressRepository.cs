using System.Collections.Generic;
using Kingdom.Save;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// UserSaveData를 기반으로 월드맵 Presenter가 읽을 진행 데이터를 제공합니다.
    /// </summary>
    public sealed class UserSaveStageProgressRepository : IStageProgressRepository
    {
        private readonly UserSaveData _saveData;
        private readonly Dictionary<int, StageProgressSnapshot> _progressMap = new Dictionary<int, StageProgressSnapshot>();

        public UserSaveStageProgressRepository(UserSaveData saveData = null)
        {
            _saveData = saveData ?? new UserSaveData();
            BuildCache();
        }

        public IReadOnlyDictionary<int, StageProgressSnapshot> GetProgressMap()
        {
            return _progressMap;
        }

        public int GetTotalEarnedStars()
        {
            int total = 0;
            foreach (var pair in _progressMap)
            {
                total += pair.Value.EarnedStars;
            }

            return total;
        }

        private void BuildCache()
        {
            _progressMap.Clear();
            if (_saveData == null)
            {
                return;
            }

            // 캐시를 한 번 구성해 Presenter 조회 시 추가 할당을 줄입니다.
            foreach (UserSaveData.StageProgressData data in _saveData.GetAllStageProgress())
            {
                if (data == null)
                {
                    continue;
                }

                _progressMap[data.StageId] = new StageProgressSnapshot(
                    data.StageId,
                    data.IsCleared,
                    data.BestStars,
                    data.BestClearTimeSeconds,
                    data.BestDifficulty);
            }
        }
    }
}
