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
    /// 별(Star) 자원을 소모하여 스킬을 해금/강화하는 스킬 트리 UI.
    /// </summary>
    public class SkillTreeUI : MonoBehaviour
    {
        [Serializable]
        public sealed class SkillNodeView
        {
            [Header("Identity")]
            public string SkillId;
            public string DisplayName;
            [TextArea] public string Description;

            [Header("Progress")]
            [Min(1)] public int MaxLevel = 5;
            [Min(0)] public int BaseCost = 1;
            [Min(0)] public int AdditionalCostPerLevel = 1;

            [Header("Unlock Conditions")]
            public List<string> RequiredSkillIds = new List<string>();
            [Min(0)] public int RequiredStageId;
            [Min(0)] public int RequiredTotalStars;

            [Header("UI")]
            public Button UpgradeButton;
            public TextMeshProUGUI TxtSkillName;
            public TextMeshProUGUI TxtLevel;
            public TextMeshProUGUI TxtCost;
            public TextMeshProUGUI TxtCondition;
            public Image LockOverlay;
        }

        [Header("Skill Nodes")]
        [SerializeField] private List<SkillNodeView> skillNodes = new List<SkillNodeView>();

        [Header("Header UI")]
        [SerializeField] private TextMeshProUGUI txtTotalStars;
        [SerializeField] private TextMeshProUGUI txtSpentStars;
        [SerializeField] private TextMeshProUGUI txtAvailableStars;

        [Header("Animation")]
        [SerializeField] private float popDuration = 0.2f;
        [SerializeField] private Vector3 popScale = new Vector3(1.12f, 1.12f, 1f);

        private const string SpentStarKey = "Kingdom.SkillTree.SpentStars";
        private const string BonusStarKey = "Kingdom.SkillTree.BonusStars";
        private const string SkillLevelKeyPrefix = "Kingdom.SkillTree.SkillLevel.";

        private UserSaveData _saveData;
        private readonly Dictionary<string, SkillNodeView> _nodeMap = new Dictionary<string, SkillNodeView>();

        private void Awake()
        {
            _saveData = new UserSaveData();
            _nodeMap.Clear();

            for (int i = 0; i < skillNodes.Count; i++)
            {
                SkillNodeView node = skillNodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.SkillId))
                {
                    continue;
                }

                _nodeMap[node.SkillId] = node;

                if (node.UpgradeButton != null)
                {
                    string capturedSkillId = node.SkillId;
                    node.UpgradeButton.onClick.RemoveAllListeners();
                    node.UpgradeButton.onClick.AddListener(() => TryUpgrade(capturedSkillId));
                }
            }
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        public void RefreshAll()
        {
            int totalStars = GetTotalEarnedStars();
            int spentStars = GetSpentStars();
            int availableStars = Mathf.Max(0, totalStars - spentStars);

            if (txtTotalStars != null)
            {
                txtTotalStars.text = $"총 별: {totalStars}";
            }

            if (txtSpentStars != null)
            {
                txtSpentStars.text = $"사용 별: {spentStars}";
            }

            if (txtAvailableStars != null)
            {
                txtAvailableStars.text = $"남은 별: {availableStars}";
            }

            for (int i = 0; i < skillNodes.Count; i++)
            {
                RefreshNode(skillNodes[i], availableStars);
            }
        }

        public int GetSkillLevel(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return 0;
            }

            return Mathf.Max(0, PlayerPrefs.GetInt(GetSkillLevelKey(skillId), 0));
        }

        private void TryUpgrade(string skillId)
        {
            if (!_nodeMap.TryGetValue(skillId, out SkillNodeView node) || node == null)
            {
                return;
            }

            int currentLevel = GetSkillLevel(skillId);
            if (currentLevel >= Mathf.Max(1, node.MaxLevel))
            {
                RefreshAll();
                return;
            }

            bool unlocked = IsNodeUnlocked(node, out _);
            if (!unlocked)
            {
                RefreshAll();
                return;
            }

            int cost = GetUpgradeCost(node, currentLevel);
            int availableStars = Mathf.Max(0, GetTotalEarnedStars() - GetSpentStars());
            if (availableStars < cost)
            {
                RefreshAll();
                return;
            }

            PlayerPrefs.SetInt(GetSkillLevelKey(skillId), currentLevel + 1);
            PlayerPrefs.SetInt(SpentStarKey, GetSpentStars() + cost);
            PlayerPrefs.Save();

            if (node.UpgradeButton != null)
            {
                StartCoroutine(CoPop(node.UpgradeButton.transform));
            }

            RefreshAll();
        }

        private void RefreshNode(SkillNodeView node, int availableStars)
        {
            if (node == null)
            {
                return;
            }

            int level = GetSkillLevel(node.SkillId);
            int clampedMax = Mathf.Max(1, node.MaxLevel);
            level = Mathf.Clamp(level, 0, clampedMax);

            bool unlocked = IsNodeUnlocked(node, out string lockReason);
            bool canUpgrade = unlocked && level < clampedMax;
            int cost = GetUpgradeCost(node, level);

            if (node.TxtSkillName != null)
            {
                node.TxtSkillName.text = string.IsNullOrWhiteSpace(node.DisplayName)
                    ? node.SkillId
                    : node.DisplayName;
            }

            if (node.TxtLevel != null)
            {
                node.TxtLevel.text = $"Lv. {level}/{clampedMax}";
            }

            if (node.TxtCost != null)
            {
                node.TxtCost.text = canUpgrade
                    ? $"필요 별: {cost}"
                    : (level >= clampedMax ? "MAX" : "잠김");
            }

            if (node.TxtCondition != null)
            {
                if (level >= clampedMax)
                {
                    node.TxtCondition.text = "강화 완료";
                }
                else
                {
                    node.TxtCondition.text = unlocked ? "강화 가능" : lockReason;
                }
            }

            bool interactable = canUpgrade && availableStars >= cost;
            if (node.UpgradeButton != null)
            {
                node.UpgradeButton.interactable = interactable;
            }

            if (node.LockOverlay != null)
            {
                node.LockOverlay.gameObject.SetActive(!unlocked);
            }
        }

        private bool IsNodeUnlocked(SkillNodeView node, out string lockReason)
        {
            lockReason = string.Empty;
            if (node == null)
            {
                lockReason = "잘못된 노드";
                return false;
            }

            if (node.RequiredStageId > 0)
            {
                UserSaveData.StageProgressData progress = _saveData.GetStageProgress(node.RequiredStageId);
                if (!progress.IsCleared)
                {
                    lockReason = $"스테이지 {node.RequiredStageId} 클리어 필요";
                    return false;
                }
            }

            int totalStars = GetTotalEarnedStars();
            if (totalStars < Mathf.Max(0, node.RequiredTotalStars))
            {
                lockReason = $"총 별 {node.RequiredTotalStars}개 필요";
                return false;
            }

            if (node.RequiredSkillIds != null)
            {
                for (int i = 0; i < node.RequiredSkillIds.Count; i++)
                {
                    string requiredSkillId = node.RequiredSkillIds[i];
                    if (string.IsNullOrWhiteSpace(requiredSkillId))
                    {
                        continue;
                    }

                    if (GetSkillLevel(requiredSkillId) <= 0)
                    {
                        lockReason = $"선행 스킬 필요: {requiredSkillId}";
                        return false;
                    }
                }
            }

            return true;
        }

        private int GetTotalEarnedStars()
        {
            int total = 0;
            var progresses = _saveData.GetAllStageProgress();
            foreach (var progress in progresses)
            {
                if (progress == null)
                {
                    continue;
                }

                total += Mathf.Clamp(progress.BestStars, 0, 3);
            }

            total += Mathf.Max(0, PlayerPrefs.GetInt(BonusStarKey, 0));
            return Mathf.Max(0, total);
        }

        private int GetSpentStars()
        {
            return Mathf.Max(0, PlayerPrefs.GetInt(SpentStarKey, 0));
        }

        private static string GetSkillLevelKey(string skillId)
        {
            return SkillLevelKeyPrefix + skillId;
        }

        private int GetUpgradeCost(SkillNodeView node, int currentLevel)
        {
            int baseCost = Mathf.Max(0, node.BaseCost);
            int additional = Mathf.Max(0, node.AdditionalCostPerLevel) * Mathf.Max(0, currentLevel);
            return Mathf.Max(1, baseCost + additional);
        }

        private IEnumerator CoPop(Transform target)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 baseScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, popDuration));
                float pulse = Mathf.Sin(t * Mathf.PI);
                target.localScale = Vector3.Lerp(baseScale, Vector3.Scale(baseScale, popScale), pulse);
                yield return null;
            }

            target.localScale = baseScale;
        }
    }
}
