using System;
using System.Collections;
using System.Collections.Generic;
using Kingdom.Save;
using Kingdom.WorldMap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.Achievement
{
    /// <summary>
    /// 업적 달성 체크, 팝업 노출, 보상 수령을 처리하는 시스템.
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        [Serializable]
        public sealed class AchievementItemView
        {
            public string AchievementId;
            public TextMeshProUGUI TxtTitle;
            public TextMeshProUGUI TxtDescription;
            public TextMeshProUGUI TxtProgress;
            public TextMeshProUGUI TxtReward;
            public Button BtnClaim;
            public Image ClaimedOverlay;
        }

        [Serializable]
        private sealed class RuntimeAchievementState
        {
            public AchievementData Data;
            public bool IsUnlocked;
            public bool IsClaimed;
            public int CurrentValue;
        }

        [Header("Config")]
        [SerializeField] private AchievementConfig achievementConfig;

        [Header("List UI")]
        [SerializeField] private List<AchievementItemView> itemViews = new List<AchievementItemView>();

        [Header("Popup UI")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TextMeshProUGUI txtPopupTitle;
        [SerializeField] private TextMeshProUGUI txtPopupDesc;
        [SerializeField] private TextMeshProUGUI txtPopupReward;
        [SerializeField] private float popupAutoCloseDelay = 2.2f;

        private const string ClaimedKeyPrefix = "Kingdom.Achievement.Claimed.";
        private const string BonusStarKey = "Kingdom.SkillTree.BonusStars";
        private const string BonusSkillPointKey = "Kingdom.SkillTree.BonusSkillPoints";

        private readonly Dictionary<string, RuntimeAchievementState> _runtimeMap = new Dictionary<string, RuntimeAchievementState>();
        private UserSaveData _saveData;
        private Coroutine _popupRoutine;

        private void Awake()
        {
            _saveData = new UserSaveData();
            BuildRuntimeState();
            BindViews();

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EvaluateAll();
            RefreshAllViews();
        }

        public void EvaluateAll()
        {
            foreach (var pair in _runtimeMap)
            {
                RuntimeAchievementState state = pair.Value;
                state.CurrentValue = EvaluateCurrentValue(state.Data);
                state.IsUnlocked = state.CurrentValue >= Mathf.Max(1, state.Data.TargetValue);
            }
        }

        private void BuildRuntimeState()
        {
            _runtimeMap.Clear();
            if (achievementConfig == null || achievementConfig.Achievements == null)
            {
                return;
            }

            for (int i = 0; i < achievementConfig.Achievements.Count; i++)
            {
                AchievementData data = achievementConfig.Achievements[i];
                if (string.IsNullOrWhiteSpace(data.AchievementId))
                {
                    continue;
                }

                RuntimeAchievementState state = new RuntimeAchievementState
                {
                    Data = data,
                    IsClaimed = PlayerPrefs.GetInt(GetClaimedKey(data.AchievementId), 0) == 1,
                };

                _runtimeMap[data.AchievementId] = state;
            }
        }

        private void BindViews()
        {
            for (int i = 0; i < itemViews.Count; i++)
            {
                AchievementItemView view = itemViews[i];
                if (view == null || string.IsNullOrWhiteSpace(view.AchievementId))
                {
                    continue;
                }

                if (view.BtnClaim != null)
                {
                    string capturedId = view.AchievementId;
                    view.BtnClaim.onClick.RemoveAllListeners();
                    view.BtnClaim.onClick.AddListener(() => ClaimReward(capturedId));
                }
            }
        }

        private void RefreshAllViews()
        {
            for (int i = 0; i < itemViews.Count; i++)
            {
                RefreshView(itemViews[i]);
            }
        }

        private void RefreshView(AchievementItemView view)
        {
            if (view == null || !_runtimeMap.TryGetValue(view.AchievementId, out RuntimeAchievementState state))
            {
                return;
            }

            AchievementData data = state.Data;
            int target = Mathf.Max(1, data.TargetValue);
            int current = Mathf.Clamp(state.CurrentValue, 0, target);

            if (view.TxtTitle != null)
            {
                view.TxtTitle.text = data.Title;
            }

            if (view.TxtDescription != null)
            {
                view.TxtDescription.text = data.Description;
            }

            if (view.TxtProgress != null)
            {
                view.TxtProgress.text = $"진행도: {current}/{target}";
            }

            if (view.TxtReward != null)
            {
                view.TxtReward.text = $"보상: 별 +{Mathf.Max(0, data.RewardStars)}, 스킬포인트 +{Mathf.Max(0, data.RewardSkillPoints)}";
            }

            bool canClaim = state.IsUnlocked && !state.IsClaimed;
            if (view.BtnClaim != null)
            {
                view.BtnClaim.interactable = canClaim;
            }

            if (view.ClaimedOverlay != null)
            {
                view.ClaimedOverlay.gameObject.SetActive(state.IsClaimed);
            }
        }

        private int EvaluateCurrentValue(AchievementData data)
        {
            switch (data.Type)
            {
                case AchievementType.ClearStageCount:
                    return CountClearedStages();

                case AchievementType.ReachTotalStars:
                    return CountTotalStars();

                case AchievementType.ClearSpecificBossStage:
                    return IsStageCleared(data.StageId) ? 1 : 0;

                case AchievementType.UnlockSkillCount:
                    return CountUnlockedSkills();

                case AchievementType.OwnHeroCount:
                    return CountOwnedHeroes();

                default:
                    return 0;
            }
        }

        private int CountClearedStages()
        {
            int count = 0;
            var progresses = _saveData.GetAllStageProgress();
            foreach (var progress in progresses)
            {
                if (progress != null && progress.IsCleared)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountTotalStars()
        {
            int stars = 0;
            var progresses = _saveData.GetAllStageProgress();
            foreach (var progress in progresses)
            {
                if (progress == null)
                {
                    continue;
                }

                stars += Mathf.Clamp(progress.BestStars, 0, 3);
            }

            return stars;
        }

        private bool IsStageCleared(int stageId)
        {
            if (stageId <= 0)
            {
                return false;
            }

            return _saveData.GetStageProgress(stageId).IsCleared;
        }

        private int CountUnlockedSkills()
        {
            int count = 0;
            if (achievementConfig == null || achievementConfig.Achievements == null)
            {
                return 0;
            }

            for (int i = 0; i < achievementConfig.Achievements.Count; i++)
            {
                AchievementData data = achievementConfig.Achievements[i];
                if (data.Type != AchievementType.UnlockSkillCount)
                {
                    continue;
                }

                string key = $"Kingdom.SkillTree.SkillLevel.{data.AchievementId}";
                if (PlayerPrefs.GetInt(key, 0) > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountOwnedHeroes()
        {
            return Mathf.Max(0, PlayerPrefs.GetInt("Kingdom.Hero.OwnedCount", 0));
        }

        private void ClaimReward(string achievementId)
        {
            if (!_runtimeMap.TryGetValue(achievementId, out RuntimeAchievementState state))
            {
                return;
            }

            if (!state.IsUnlocked || state.IsClaimed)
            {
                return;
            }

            state.IsClaimed = true;
            PlayerPrefs.SetInt(GetClaimedKey(achievementId), 1);

            int rewardStars = Mathf.Max(0, state.Data.RewardStars);
            int rewardSkillPoints = Mathf.Max(0, state.Data.RewardSkillPoints);

            if (rewardStars > 0)
            {
                PlayerPrefs.SetInt(BonusStarKey, PlayerPrefs.GetInt(BonusStarKey, 0) + rewardStars);
            }

            if (rewardSkillPoints > 0)
            {
                PlayerPrefs.SetInt(BonusSkillPointKey, PlayerPrefs.GetInt(BonusSkillPointKey, 0) + rewardSkillPoints);
            }

            PlayerPrefs.Save();

            ShowRewardPopup(state.Data);
            RefreshAllViews();
        }

        private void ShowRewardPopup(AchievementData data)
        {
            if (popupRoot == null)
            {
                return;
            }

            if (txtPopupTitle != null)
            {
                txtPopupTitle.text = $"업적 달성: {data.Title}";
            }

            if (txtPopupDesc != null)
            {
                txtPopupDesc.text = data.Description;
            }

            if (txtPopupReward != null)
            {
                txtPopupReward.text = $"별 +{Mathf.Max(0, data.RewardStars)}, 스킬포인트 +{Mathf.Max(0, data.RewardSkillPoints)}";
            }

            popupRoot.SetActive(true);

            if (_popupRoutine != null)
            {
                StopCoroutine(_popupRoutine);
            }

            _popupRoutine = StartCoroutine(CoHidePopup());
        }

        private IEnumerator CoHidePopup()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, popupAutoCloseDelay));

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            _popupRoutine = null;
        }

        private static string GetClaimedKey(string achievementId)
        {
            return ClaimedKeyPrefix + achievementId;
        }
    }
}
