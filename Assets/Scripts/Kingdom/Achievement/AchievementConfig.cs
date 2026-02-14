using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Achievement
{
    public enum AchievementType
    {
        ClearStageCount,
        ReachTotalStars,
        ClearSpecificBossStage,
        UnlockSkillCount,
        OwnHeroCount,
    }

    [Serializable]
    public struct AchievementData
    {
        public string AchievementId;
        public string Title;
        [TextArea] public string Description;
        public AchievementType Type;
        public int TargetValue;
        public int RewardStars;
        public int RewardSkillPoints;
        public int StageId;
    }

    [CreateAssetMenu(fileName = "AchievementConfig", menuName = "Kingdom/Achievement Config")]
    public class AchievementConfig : ScriptableObject
    {
        public List<AchievementData> Achievements = new List<AchievementData>();
    }
}
