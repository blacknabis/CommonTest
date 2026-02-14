using System;
using System.Collections;
using System.Collections.Generic;
using Kingdom.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// 스테이지 별 획득/표시/애니메이션을 담당하는 매니저.
    /// </summary>
    public class StarUIManager : MonoBehaviour
    {
        [Serializable]
        private sealed class PendingStageResult
        {
            public int StageId;
            public bool IsCleared;
            public float ClearTimeSeconds;
            public StageDifficulty Difficulty;
            public List<float> StarRequirements;
        }

        [Serializable]
        public sealed class NodeStarBinding
        {
            public int StageId;
            public Transform Root;
            public Image[] StarIcons;
            public Sprite EmptySprite;
            public Sprite FilledSprite;
            public Color EmptyColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            public Color FilledColor = Color.white;
        }

        private static PendingStageResult s_pendingResult;

        [Header("Node Star Bindings")]
        [SerializeField] private List<NodeStarBinding> nodeBindings = new List<NodeStarBinding>();

        [Header("Focused Stage UI")]
        [SerializeField] private TextMeshProUGUI txtBestTime;
        [SerializeField] private TextMeshProUGUI txtStarCount;

        [Header("Animation")]
        [SerializeField] private float perStarAnimDuration = 0.2f;
        [SerializeField] private float betweenStarDelay = 0.08f;
        [SerializeField] private Vector3 popScale = new Vector3(1.25f, 1.25f, 1f);

        private readonly Dictionary<int, NodeStarBinding> _bindingMap = new Dictionary<int, NodeStarBinding>();
        private UserSaveData _saveData;
        private int _focusedStageId = -1;

        public static void SetPendingResult(int stageId, bool isCleared, float clearTimeSeconds, StageDifficulty difficulty, List<float> starRequirements)
        {
            s_pendingResult = new PendingStageResult
            {
                StageId = stageId,
                IsCleared = isCleared,
                ClearTimeSeconds = clearTimeSeconds,
                Difficulty = difficulty,
                StarRequirements = starRequirements != null ? new List<float>(starRequirements) : null,
            };
        }

        private void Awake()
        {
            _saveData = new UserSaveData();

            _bindingMap.Clear();
            for (int i = 0; i < nodeBindings.Count; i++)
            {
                NodeStarBinding binding = nodeBindings[i];
                if (binding == null)
                {
                    continue;
                }

                _bindingMap[binding.StageId] = binding;
            }
        }

        private void OnEnable()
        {
            RefreshAll();

            if (s_pendingResult != null)
            {
                ApplyPendingResult();
            }
        }

        public void BindFocusedStage(int stageId)
        {
            _focusedStageId = stageId;
            RefreshFocusedStageInfo();
        }

        public void ApplyStageResult(int stageId, bool isCleared, float clearTimeSeconds, StageDifficulty difficulty, List<float> starRequirements)
        {
            if (_saveData == null)
            {
                _saveData = new UserSaveData();
            }

            UserSaveData.StageProgressData before = _saveData.GetStageProgress(stageId);
            int beforeStars = Mathf.Clamp(before.BestStars, 0, 3);

            if (isCleared)
            {
                int earnedStars = EvaluateStars(clearTimeSeconds, starRequirements);
                _saveData.SetStageCleared(stageId, earnedStars, clearTimeSeconds, difficulty);

                UserSaveData.StageProgressData after = _saveData.GetStageProgress(stageId);
                int afterStars = Mathf.Clamp(after.BestStars, 0, 3);
                RefreshStage(stageId);
                RefreshFocusedStageInfo();

                if (afterStars > beforeStars)
                {
                    StartCoroutine(CoPlayStarAcquireAnimation(stageId, beforeStars, afterStars));
                }
            }
            else
            {
                RefreshStage(stageId);
                RefreshFocusedStageInfo();
            }
        }

        public void RefreshAll()
        {
            if (_saveData == null)
            {
                _saveData = new UserSaveData();
            }

            foreach (var pair in _bindingMap)
            {
                RefreshStage(pair.Key);
            }

            RefreshFocusedStageInfo();
        }

        public void RefreshStage(int stageId)
        {
            if (!_bindingMap.TryGetValue(stageId, out NodeStarBinding binding) || binding == null)
            {
                return;
            }

            int bestStars = 0;
            if (_saveData != null)
            {
                bestStars = Mathf.Clamp(_saveData.GetStageProgress(stageId).BestStars, 0, 3);
            }

            if (binding.StarIcons == null)
            {
                return;
            }

            for (int i = 0; i < binding.StarIcons.Length; i++)
            {
                Image icon = binding.StarIcons[i];
                if (icon == null)
                {
                    continue;
                }

                bool filled = i < bestStars;
                icon.sprite = filled ? binding.FilledSprite : binding.EmptySprite;
                icon.color = filled ? binding.FilledColor : binding.EmptyColor;
            }
        }

        private void RefreshFocusedStageInfo()
        {
            if (_focusedStageId <= 0 || _saveData == null)
            {
                if (txtBestTime != null)
                {
                    txtBestTime.text = "최고 기록: -";
                }

                if (txtStarCount != null)
                {
                    txtStarCount.text = "별: 0/3";
                }

                return;
            }

            UserSaveData.StageProgressData progress = _saveData.GetStageProgress(_focusedStageId);
            int stars = Mathf.Clamp(progress.BestStars, 0, 3);

            if (txtStarCount != null)
            {
                txtStarCount.text = $"별: {stars}/3";
            }

            if (txtBestTime != null)
            {
                if (progress.BestClearTimeSeconds > 0f)
                {
                    txtBestTime.text = $"최고 기록: {progress.BestClearTimeSeconds:0.00}s ({progress.BestDifficulty})";
                }
                else
                {
                    txtBestTime.text = "최고 기록: 없음";
                }
            }
        }

        private int EvaluateStars(float clearTimeSeconds, List<float> starRequirements)
        {
            if (clearTimeSeconds <= 0f)
            {
                return 0;
            }

            if (starRequirements == null || starRequirements.Count == 0)
            {
                return 1;
            }

            int stars = 0;
            for (int i = 0; i < starRequirements.Count; i++)
            {
                float req = starRequirements[i];
                if (req <= 0f)
                {
                    continue;
                }

                if (clearTimeSeconds <= req)
                {
                    stars++;
                }
            }

            return Mathf.Clamp(stars, 1, 3);
        }

        private IEnumerator CoPlayStarAcquireAnimation(int stageId, int beforeStars, int afterStars)
        {
            if (!_bindingMap.TryGetValue(stageId, out NodeStarBinding binding) || binding == null || binding.StarIcons == null)
            {
                yield break;
            }

            int from = Mathf.Clamp(beforeStars, 0, 3);
            int to = Mathf.Clamp(afterStars, 0, 3);

            for (int i = from; i < to; i++)
            {
                if (i < 0 || i >= binding.StarIcons.Length)
                {
                    continue;
                }

                Image icon = binding.StarIcons[i];
                if (icon == null)
                {
                    continue;
                }

                Transform target = icon.transform;
                Vector3 baseScale = target.localScale;

                float elapsed = 0f;
                while (elapsed < perStarAnimDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, perStarAnimDuration));
                    float pulse = Mathf.Sin(t * Mathf.PI);
                    target.localScale = Vector3.Lerp(baseScale, Vector3.Scale(baseScale, popScale), pulse);
                    yield return null;
                }

                target.localScale = baseScale;

                if (betweenStarDelay > 0f)
                {
                    yield return new WaitForSecondsRealtime(betweenStarDelay);
                }
            }
        }

        private void ApplyPendingResult()
        {
            if (s_pendingResult == null)
            {
                return;
            }

            ApplyStageResult(
                s_pendingResult.StageId,
                s_pendingResult.IsCleared,
                s_pendingResult.ClearTimeSeconds,
                s_pendingResult.Difficulty,
                s_pendingResult.StarRequirements);

            s_pendingResult = null;
        }
    }
}
