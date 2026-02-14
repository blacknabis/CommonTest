using System.Collections.Generic;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// 스테이지 잠금 해제 여부 계산 규칙 인터페이스입니다.
    /// </summary>
    public interface IStageUnlockPolicy
    {
        bool IsUnlocked(
            int stageId,
            IReadOnlyDictionary<int, StageProgressSnapshot> progressMap,
            IReadOnlyList<StageData> allStages,
            int totalEarnedStars);
    }
}
