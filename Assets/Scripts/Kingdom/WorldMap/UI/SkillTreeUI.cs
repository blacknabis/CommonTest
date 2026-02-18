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
        
        [Header("Category Tabs")]
        [SerializeField] private RectTransform tabRoot;
        [SerializeField] private List<Button> categoryTabButtons = new List<Button>();
        [SerializeField] private Color categoryTabNormalColor = new Color(0.2f, 0.24f, 0.3f, 0.9f);
        [SerializeField] private Color categoryTabSelectedColor = new Color(0.22f, 0.52f, 0.31f, 0.95f);

        private const string SpentStarKey = "Kingdom.SkillTree.SpentStars";
        private const string BonusStarKey = "Kingdom.SkillTree.BonusStars";
        private const string SkillLevelKeyPrefix = "Kingdom.SkillTree.SkillLevel.";
        private static readonly string[] CategoryKeys =
        {
            "archers",
            "barracks",
            "mages",
            "artillery",
            "rain",
            "reinforce"
        };
        private static readonly string[] CategoryLabels =
        {
            "Archers",
            "Barracks",
            "Mages",
            "Artillery",
            "Rain",
            "Reinforce"
        };

        private UserSaveData _saveData;
        private readonly Dictionary<string, SkillNodeView> _nodeMap = new Dictionary<string, SkillNodeView>();
        private readonly Dictionary<string, List<SkillNodeView>> _categoryMap = new Dictionary<string, List<SkillNodeView>>(StringComparer.OrdinalIgnoreCase);
        private string _activeCategoryKey = CategoryKeys[0];

        private void Awake()
        {
            EnsureRuntimeFallbackUi();
            _saveData = new UserSaveData();
            _nodeMap.Clear();
            _categoryMap.Clear();

            for (int i = 0; i < skillNodes.Count; i++)
            {
                SkillNodeView node = skillNodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.SkillId))
                {
                    continue;
                }

                _nodeMap[node.SkillId] = node;
                AddNodeToCategory(node);

                if (node.UpgradeButton != null)
                {
                    string capturedSkillId = node.SkillId;
                    node.UpgradeButton.onClick.RemoveAllListeners();
                    node.UpgradeButton.onClick.AddListener(() => TryUpgrade(capturedSkillId));
                }
            }

            EnsureCategoryTabs();
            ApplyCategoryVisibility();
        }

        private void EnsureRuntimeFallbackUi()
        {
            if (txtTotalStars != null && txtSpentStars != null && txtAvailableStars != null && skillNodes != null && skillNodes.Count > 0)
            {
                return;
            }

            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            EnsureBackground(root);
            RectTransform contentRoot = EnsureContentRoot(root);
            EnsureHeader(contentRoot);
            EnsureDefaultNodes(contentRoot);
        }

        private static void EnsureBackground(RectTransform root)
        {
            Image image = root.GetComponent<Image>();
            if (image == null)
            {
                image = root.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0.1f, 0.12f, 0.16f, 0.96f);
            image.raycastTarget = true;
        }

        private RectTransform EnsureContentRoot(RectTransform root)
        {
            Transform existing = root.Find("SkillTreeContent");
            if (existing != null)
            {
                return existing as RectTransform;
            }

            GameObject contentObject = new GameObject("SkillTreeContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.SetParent(root, false);
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.offsetMin = new Vector2(24f, 24f);
            contentRect.offsetMax = new Vector2(-24f, -96f);

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return contentRect;
        }

        private void EnsureHeader(RectTransform contentRoot)
        {
            Transform headerTransform = contentRoot.Find("Header");
            RectTransform headerRect;
            if (headerTransform == null)
            {
                GameObject headerObject = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                headerRect = headerObject.GetComponent<RectTransform>();
                headerRect.SetParent(contentRoot, false);

                HorizontalLayoutGroup headerLayout = headerObject.GetComponent<HorizontalLayoutGroup>();
                headerLayout.spacing = 12f;
                headerLayout.childControlWidth = true;
                headerLayout.childControlHeight = true;
                headerLayout.childForceExpandWidth = true;
                headerLayout.childForceExpandHeight = false;

                LayoutElement element = headerObject.GetComponent<LayoutElement>();
                element.preferredHeight = 48f;
            }
            else
            {
                headerRect = headerTransform as RectTransform;
            }

            if (txtTotalStars == null)
            {
                txtTotalStars = CreateHeaderText(headerRect, "txtTotalStars", "총 별: 0");
            }

            if (txtSpentStars == null)
            {
                txtSpentStars = CreateHeaderText(headerRect, "txtSpentStars", "사용 별: 0");
            }

            if (txtAvailableStars == null)
            {
                txtAvailableStars = CreateHeaderText(headerRect, "txtAvailableStars", "남은 별: 0");
            }
        }

        private static TextMeshProUGUI CreateHeaderText(RectTransform parent, string name, string text)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(parent, false);

            LayoutElement element = textObject.GetComponent<LayoutElement>();
            element.preferredHeight = 40f;

            TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return tmp;
        }

        private void EnsureDefaultNodes(RectTransform contentRoot)
        {
            if (skillNodes != null && skillNodes.Count > 0)
            {
                return;
            }

            skillNodes = new List<SkillNodeView>();
            string[] categories = { "Archers", "Barracks", "Mages", "Artillery", "Rain", "Reinforce" };

            for (int c = 0; c < categories.Length; c++)
            {
                string category = categories[c];
                string previousSkillId = null;
                for (int tier = 1; tier <= 5; tier++)
                {
                    string skillId = $"{category.ToLowerInvariant()}_t{tier}";
                    SkillNodeView node = CreateFallbackNodeView(contentRoot, category, skillId, tier, previousSkillId);
                    skillNodes.Add(node);
                    previousSkillId = skillId;
                }
            }
        }

        private void AddNodeToCategory(SkillNodeView node)
        {
            string categoryKey = ResolveCategoryKey(node != null ? node.SkillId : null);
            if (!_categoryMap.TryGetValue(categoryKey, out List<SkillNodeView> nodes))
            {
                nodes = new List<SkillNodeView>();
                _categoryMap[categoryKey] = nodes;
            }

            nodes.Add(node);
        }

        private static string ResolveCategoryKey(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return CategoryKeys[0];
            }

            string lowered = skillId.ToLowerInvariant();
            for (int i = 0; i < CategoryKeys.Length; i++)
            {
                string key = CategoryKeys[i];
                if (lowered.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    return key;
                }
            }

            return CategoryKeys[0];
        }

        private void EnsureCategoryTabs()
        {
            if (tabRoot == null)
            {
                tabRoot = transform.Find("CategoryTabs") as RectTransform;
            }

            if (tabRoot == null)
            {
                GameObject tabsObject = new GameObject("CategoryTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                tabRoot = tabsObject.GetComponent<RectTransform>();
                tabRoot.SetParent(transform, false);
                tabRoot.anchorMin = new Vector2(0f, 1f);
                tabRoot.anchorMax = new Vector2(1f, 1f);
                tabRoot.pivot = new Vector2(0.5f, 1f);
                tabRoot.offsetMin = new Vector2(24f, -84f);
                tabRoot.offsetMax = new Vector2(-24f, -36f);

                HorizontalLayoutGroup layout = tabsObject.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 8f;
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;

                LayoutElement element = tabsObject.GetComponent<LayoutElement>();
                element.preferredHeight = 48f;
            }

            if (categoryTabButtons == null)
            {
                categoryTabButtons = new List<Button>();
            }

            for (int i = categoryTabButtons.Count - 1; i >= 0; i--)
            {
                if (categoryTabButtons[i] == null)
                {
                    categoryTabButtons.RemoveAt(i);
                }
            }

            for (int i = 0; i < CategoryKeys.Length; i++)
            {
                Button button = FindOrCreateTabButton(CategoryKeys[i], CategoryLabels[i]);
                if (button == null)
                {
                    continue;
                }

                string capturedKey = CategoryKeys[i];
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnCategoryTabClicked(capturedKey));
                if (!categoryTabButtons.Contains(button))
                {
                    categoryTabButtons.Add(button);
                }
            }
        }

        private Button FindOrCreateTabButton(string categoryKey, string label)
        {
            string buttonName = $"btnTab_{categoryKey}";
            Transform existing = tabRoot != null ? tabRoot.Find(buttonName) : null;
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            if (tabRoot == null)
            {
                return null;
            }

            GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(tabRoot, false);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 44f;
            layout.preferredWidth = 0f;
            layout.flexibleWidth = 1f;

            Image image = buttonObject.GetComponent<Image>();
            image.color = categoryTabNormalColor;

            GameObject textObject = new GameObject("txtLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(buttonRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 18f;
            text.color = Color.white;

            return buttonObject.GetComponent<Button>();
        }

        private void OnCategoryTabClicked(string categoryKey)
        {
            _activeCategoryKey = categoryKey;
            ApplyCategoryVisibility();
            RefreshAll();
        }

        private void ApplyCategoryVisibility()
        {
            if (skillNodes == null || skillNodes.Count == 0)
            {
                return;
            }

            bool hasCategory = !string.IsNullOrWhiteSpace(_activeCategoryKey);
            for (int i = 0; i < skillNodes.Count; i++)
            {
                SkillNodeView node = skillNodes[i];
                if (node == null)
                {
                    continue;
                }

                Transform nodeRoot = ResolveNodeRoot(node);
                if (nodeRoot == null)
                {
                    continue;
                }

                bool visible = !hasCategory || string.Equals(ResolveCategoryKey(node.SkillId), _activeCategoryKey, StringComparison.OrdinalIgnoreCase);
                nodeRoot.gameObject.SetActive(visible);
            }
        }

        private static Transform ResolveNodeRoot(SkillNodeView node)
        {
            if (node == null)
            {
                return null;
            }

            if (node.UpgradeButton != null)
            {
                return node.UpgradeButton.transform.parent;
            }

            if (node.TxtSkillName != null)
            {
                return node.TxtSkillName.transform.parent;
            }

            return null;
        }

        private void UpdateCategoryTabVisual()
        {
            if (categoryTabButtons == null)
            {
                return;
            }

            for (int i = 0; i < categoryTabButtons.Count; i++)
            {
                Button button = categoryTabButtons[i];
                if (button == null || button.image == null)
                {
                    continue;
                }

                string key = string.Empty;
                string name = button.name;
                int index = name.IndexOf("btnTab_", StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    key = name.Substring(index + "btnTab_".Length);
                }

                bool selected = string.Equals(key, _activeCategoryKey, StringComparison.OrdinalIgnoreCase);
                button.image.color = selected ? categoryTabSelectedColor : categoryTabNormalColor;
            }
        }

        private SkillNodeView CreateFallbackNodeView(RectTransform contentRoot, string category, string skillId, int tier, string requiredSkillId)
        {
            GameObject rowObject = new GameObject($"Node_{category}_T{tier}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.SetParent(contentRoot, false);

            LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 64f;

            Image rowImage = rowObject.GetComponent<Image>();
            rowImage.color = new Color(1f, 1f, 1f, 0.06f);

            HorizontalLayoutGroup rowGroup = rowObject.AddComponent<HorizontalLayoutGroup>();
            rowGroup.spacing = 8f;
            rowGroup.padding = new RectOffset(10, 10, 8, 8);
            rowGroup.childControlWidth = false;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childForceExpandHeight = true;

            TextMeshProUGUI nameText = CreateRowText(rowRect, "TxtName", $"{category} T{tier}", 220f);
            TextMeshProUGUI levelText = CreateRowText(rowRect, "TxtLevel", "Lv. 0/5", 120f);
            TextMeshProUGUI costText = CreateRowText(rowRect, "TxtCost", $"필요 별: {tier}", 130f);
            TextMeshProUGUI condText = CreateRowText(rowRect, "TxtCondition", requiredSkillId == null ? "강화 가능" : $"선행: {requiredSkillId}", 250f);

            Button upgradeButton = CreateRowButton(rowRect, "UpgradeButton", "강화");
            Image lockOverlay = CreateLockOverlay(rowRect);

            var node = new SkillNodeView
            {
                SkillId = skillId,
                DisplayName = $"{category} Tier {tier}",
                Description = $"{category} 강화 단계 {tier}",
                MaxLevel = 5,
                BaseCost = tier,
                AdditionalCostPerLevel = 1,
                RequiredSkillIds = new List<string>(),
                RequiredStageId = 0,
                RequiredTotalStars = Mathf.Max(0, tier - 1),
                UpgradeButton = upgradeButton,
                TxtSkillName = nameText,
                TxtLevel = levelText,
                TxtCost = costText,
                TxtCondition = condText,
                LockOverlay = lockOverlay
            };

            if (!string.IsNullOrWhiteSpace(requiredSkillId))
            {
                node.RequiredSkillIds.Add(requiredSkillId);
            }

            return node;
        }

        private static TextMeshProUGUI CreateRowText(RectTransform parent, string name, string text, float preferredWidth)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(parent, false);

            LayoutElement element = textObject.GetComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = 0f;

            TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateRowButton(RectTransform parent, string name, string label)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(parent, false);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = 96f;

            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.52f, 0.28f, 0.95f);

            GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(buttonRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 18f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return buttonObject.GetComponent<Button>();
        }

        private static Image CreateLockOverlay(RectTransform parent)
        {
            GameObject overlayObject = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.SetParent(parent, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.4f);
            overlayImage.raycastTarget = false;
            overlayImage.gameObject.SetActive(false);
            return overlayImage;
        }

        private void OnEnable()
        {
            ApplyCategoryVisibility();
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

            if (_categoryMap.TryGetValue(_activeCategoryKey, out List<SkillNodeView> activeNodes) && activeNodes != null && activeNodes.Count > 0)
            {
                for (int i = 0; i < activeNodes.Count; i++)
                {
                    RefreshNode(activeNodes[i], availableStars);
                }
            }
            else
            {
                for (int i = 0; i < skillNodes.Count; i++)
                {
                    RefreshNode(skillNodes[i], availableStars);
                }
            }

            UpdateCategoryTabVisual();
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
                Debug.Log($"[SkillTreeUI] Upgrade blocked (insufficient stars). skillId={skillId}, available={availableStars}, cost={cost}");
                RefreshAll();
                return;
            }

            PlayerPrefs.SetInt(GetSkillLevelKey(skillId), currentLevel + 1);
            PlayerPrefs.SetInt(SpentStarKey, GetSpentStars() + cost);
            PlayerPrefs.Save();
            Debug.Log($"[SkillTreeUI] Upgrade saved. skillId={skillId}, newLevel={currentLevel + 1}, spent+={cost}");

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
            bool hasEnoughStars = availableStars >= cost;

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
                else if (unlocked && !hasEnoughStars)
                {
                    node.TxtCondition.text = "별 부족";
                }
                else
                {
                    node.TxtCondition.text = unlocked ? "강화 가능" : lockReason;
                }
            }

            bool interactable = canUpgrade && hasEnoughStars;
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
