using System;
using System.Collections.Generic;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// 기본 잠금 해제 규칙:
    /// 1번 스테이지는 항상 해제, 그 외는 직전 스테이지 클리어 시 해제합니다.
    /// 필요 시 StageData.StarRequirements[0](1~3)을 별 조건으로 함께 사용합니다.
    /// </summary>
    public sealed class DefaultStageUnlockPolicy : IStageUnlockPolicy
    {
        public bool IsUnlocked(
            int stageId,
            IReadOnlyDictionary<int, StageProgressSnapshot> progressMap,
            IReadOnlyList<StageData> allStages,
            int totalEarnedStars)
        {
            if (stageId <= 0 || allStages == null || allStages.Count == 0)
            {
                return false;
            }

            if (stageId == 1)
            {
                return true;
            }

            StageData? currentStage = null;
            for (int i = 0; i < allStages.Count; i++)
            {
                if (allStages[i].StageId == stageId)
                {
                    currentStage = allStages[i];
                    break;
                }
            }

            if (currentStage == null)
            {
                return false;
            }

            StageData stage = currentStage.Value;
            if (stage.IsUnlocked)
            {
                return true;
            }

            if (TryReadRequiredStars(stage, out int requiredStars) && totalEarnedStars < requiredStars)
            {
                return false;
            }

            // 기본 순차 해제 규칙.
            int previousStageId = stageId - 1;
            if (!progressMap.TryGetValue(previousStageId, out StageProgressSnapshot previousProgress))
            {
                return false;
            }

            return previousProgress.IsCleared;
        }

        private static bool TryReadRequiredStars(in StageData stage, out int requiredStars)
        {
            requiredStars = 0;
            if (stage.StarRequirements == null || stage.StarRequirements.Count == 0)
            {
                return false;
            }

            float firstRule = stage.StarRequirements[0];
            if (firstRule <= 0f || firstRule > 3f)
            {
                return false;
            }

            requiredStars = Math.Max(0, (int)Math.Ceiling(firstRule));
            return requiredStars > 0;
        }
    }
}
