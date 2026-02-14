using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// 월드맵 보스/라이벌 등장 연출 및 보상 처리를 담당한다.
    /// </summary>
    public class BossEventSystem : MonoBehaviour
    {
        [Serializable]
        public sealed class BossEventData
        {
            public int StageId;
            public string EventId;
            public string Title;
            [TextArea] public string Description;
            public bool IsRivalEvent;
            public int RewardStars;
            public string UnlockSkillId;
        }

        [Header("Event Data")]
        [SerializeField] private List<BossEventData> eventDataList = new List<BossEventData>();

        [Header("Presentation")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private float holdDuration = 1.5f;

        [Header("Callbacks")]
        [SerializeField] private UnityEvent onRivalEventStarted;
        [SerializeField] private UnityEvent onBossEventStarted;
        [SerializeField] private UnityEvent onBossCleared;

        private const string EventShownKeyPrefix = "Kingdom.BossEvent.Shown.";
        private const string BossClearedKeyPrefix = "Kingdom.BossEvent.Cleared.";
        private const string BonusStarKey = "Kingdom.SkillTree.BonusStars";
        private const string SkillUnlockKeyPrefix = "Kingdom.SkillTree.SkillUnlocked.";

        private readonly Dictionary<int, BossEventData> _eventByStage = new Dictionary<int, BossEventData>();
        private Coroutine _presentRoutine;

        private void Awake()
        {
            BuildLookup();

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 0f;
            }
        }

        public bool TryTriggerRivalEvent(int stageId)
        {
            if (!_eventByStage.TryGetValue(stageId, out BossEventData data) || !data.IsRivalEvent)
            {
                return false;
            }

            if (IsEventAlreadyShown(data))
            {
                return false;
            }

            MarkEventShown(data);
            onRivalEventStarted?.Invoke();
            PlayPresentation(data);
            return true;
        }

        public bool TryTriggerBossEvent(int stageId)
        {
            if (!_eventByStage.TryGetValue(stageId, out BossEventData data) || data.IsRivalEvent)
            {
                return false;
            }

            if (IsEventAlreadyShown(data))
            {
                return false;
            }

            MarkEventShown(data);
            onBossEventStarted?.Invoke();
            PlayPresentation(data);
            return true;
        }

        public void NotifyBossStageCleared(int stageId)
        {
            if (!_eventByStage.TryGetValue(stageId, out BossEventData data) || data.IsRivalEvent)
            {
                return;
            }

            string clearKey = GetBossClearKey(data);
            if (PlayerPrefs.GetInt(clearKey, 0) == 1)
            {
                return;
            }

            PlayerPrefs.SetInt(clearKey, 1);

            if (data.RewardStars > 0)
            {
                int current = PlayerPrefs.GetInt(BonusStarKey, 0);
                PlayerPrefs.SetInt(BonusStarKey, current + data.RewardStars);
            }

            if (!string.IsNullOrWhiteSpace(data.UnlockSkillId))
            {
                string skillKey = SkillUnlockKeyPrefix + data.UnlockSkillId;
                PlayerPrefs.SetInt(skillKey, 1);
            }

            PlayerPrefs.Save();

            onBossCleared?.Invoke();
            PlayPresentation(new BossEventData
            {
                EventId = data.EventId + ".clear",
                Title = string.IsNullOrWhiteSpace(data.Title) ? "보스 클리어" : $"{data.Title} 처치",
                Description = BuildRewardMessage(data),
                IsRivalEvent = false,
            });
        }

        private void BuildLookup()
        {
            _eventByStage.Clear();
            for (int i = 0; i < eventDataList.Count; i++)
            {
                BossEventData data = eventDataList[i];
                if (data == null || data.StageId <= 0)
                {
                    continue;
                }

                _eventByStage[data.StageId] = data;
            }
        }

        private void PlayPresentation(BossEventData data)
        {
            if (data == null || overlayRoot == null || overlayCanvasGroup == null)
            {
                return;
            }

            if (_presentRoutine != null)
            {
                StopCoroutine(_presentRoutine);
            }

            _presentRoutine = StartCoroutine(CoPlayPresentation(data));
        }

        private IEnumerator CoPlayPresentation(BossEventData data)
        {
            overlayRoot.SetActive(true);

            if (txtTitle != null)
            {
                txtTitle.text = data.Title;
            }

            if (txtDescription != null)
            {
                txtDescription.text = data.Description;
            }

            yield return CoFade(0f, 1f, fadeDuration);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, holdDuration));
            yield return CoFade(1f, 0f, fadeDuration);

            overlayRoot.SetActive(false);
            _presentRoutine = null;
        }

        private IEnumerator CoFade(float from, float to, float duration)
        {
            if (overlayCanvasGroup == null)
            {
                yield break;
            }

            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            overlayCanvasGroup.alpha = from;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                overlayCanvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            overlayCanvasGroup.alpha = to;
        }

        private bool IsEventAlreadyShown(BossEventData data)
        {
            return PlayerPrefs.GetInt(GetShownKey(data), 0) == 1;
        }

        private void MarkEventShown(BossEventData data)
        {
            PlayerPrefs.SetInt(GetShownKey(data), 1);
            PlayerPrefs.Save();
        }

        private static string GetShownKey(BossEventData data)
        {
            return EventShownKeyPrefix + ResolveEventKey(data);
        }

        private static string GetBossClearKey(BossEventData data)
        {
            return BossClearedKeyPrefix + ResolveEventKey(data);
        }

        private static string ResolveEventKey(BossEventData data)
        {
            if (!string.IsNullOrWhiteSpace(data.EventId))
            {
                return data.EventId;
            }

            return data.StageId.ToString();
        }

        private static string BuildRewardMessage(BossEventData data)
        {
            string rewardStar = data.RewardStars > 0 ? $"별 +{data.RewardStars}" : string.Empty;
            string skill = !string.IsNullOrWhiteSpace(data.UnlockSkillId) ? $"스킬 해제: {data.UnlockSkillId}" : string.Empty;

            if (!string.IsNullOrEmpty(rewardStar) && !string.IsNullOrEmpty(skill))
            {
                return $"보상 획득! {rewardStar}, {skill}";
            }

            if (!string.IsNullOrEmpty(rewardStar))
            {
                return $"보상 획득! {rewardStar}";
            }

            if (!string.IsNullOrEmpty(skill))
            {
                return $"보상 획득! {skill}";
            }

            return "보상 획득!";
        }
    }
}
