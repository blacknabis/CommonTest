using UnityEngine;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// Presenter/Policy에서 사용하는 스테이지 진행 상태 스냅샷입니다.
    /// </summary>
    public readonly struct StageProgressSnapshot
    {
        public readonly int StageId;
        public readonly bool IsCleared;
        public readonly int EarnedStars;
        public readonly float BestClearTimeSeconds;
        public readonly StageDifficulty BestDifficulty;

        public StageProgressSnapshot(
            int stageId,
            bool isCleared,
            int earnedStars,
            float bestClearTimeSeconds,
            StageDifficulty bestDifficulty)
        {
            StageId = stageId;
            IsCleared = isCleared;
            EarnedStars = Mathf.Clamp(earnedStars, 0, 3);
            BestClearTimeSeconds = Mathf.Max(0f, bestClearTimeSeconds);
            BestDifficulty = bestDifficulty;
        }
    }
}
