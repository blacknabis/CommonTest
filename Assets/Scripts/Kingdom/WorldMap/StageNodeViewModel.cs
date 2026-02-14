using UnityEngine;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// 단일 스테이지 노드 UI 표현을 위한 불변 데이터입니다.
    /// </summary>
    public readonly struct StageNodeViewModel
    {
        public readonly int StageId;
        public readonly string StageLabel;
        public readonly bool IsUnlocked;
        public readonly bool IsCleared;
        public readonly int EarnedStars;
        public readonly bool IsSelected;
        public readonly bool IsBoss;
        public readonly bool HasNotification;
        public readonly Sprite IconSprite;

        public StageNodeViewModel(
            int stageId,
            string stageLabel,
            bool isUnlocked,
            bool isCleared,
            int earnedStars,
            bool isSelected,
            bool isBoss,
            bool hasNotification,
            Sprite iconSprite)
        {
            StageId = stageId;
            StageLabel = stageLabel;
            IsUnlocked = isUnlocked;
            IsCleared = isCleared;
            EarnedStars = Mathf.Clamp(earnedStars, 0, 3);
            IsSelected = isSelected;
            IsBoss = isBoss;
            HasNotification = hasNotification;
            IconSprite = iconSprite;
        }
    }
}
