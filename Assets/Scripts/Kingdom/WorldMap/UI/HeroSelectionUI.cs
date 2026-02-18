using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// 영웅 보유 목록 표시 및 출전 슬롯 배치를 관리하는 UI.
    /// </summary>
    public class HeroSelectionUI : MonoBehaviour
    {
        [Serializable]
        public sealed class HeroData
        {
            public string HeroId;
            public string DisplayName;
            [Min(1)] public int Level = 1;
            [Min(1)] public int Attack = 10;
            [Min(0)] public int Defense = 5;
            [Min(1)] public int Health = 100;
            public Sprite Portrait;
            public bool IsOwned = true;
        }

        [Serializable]
        public sealed class HeroSlotView
        {
            public Button SelectButton;
            public Image Portrait;
            public TextMeshProUGUI TxtName;
            public TextMeshProUGUI TxtLevel;
        }

        [Serializable]
        public sealed class PartySlotView
        {
            public Image Portrait;
            public TextMeshProUGUI TxtName;
            public TextMeshProUGUI TxtLevel;
        }

        [Header("Data")]
        [SerializeField] private List<HeroData> heroRoster = new List<HeroData>();
        [SerializeField] private int maxPartySize = 3;

        [Header("Hero List UI")]
        [SerializeField] private List<HeroSlotView> heroSlotViews = new List<HeroSlotView>();

        [Header("Party UI")]
        [SerializeField] private List<PartySlotView> partySlotViews = new List<PartySlotView>();

        [Header("Detail UI")]
        [SerializeField] private Image imgSelectedPortrait;
        [SerializeField] private TextMeshProUGUI txtSelectedName;
        [SerializeField] private TextMeshProUGUI txtSelectedLevel;
        [SerializeField] private TextMeshProUGUI txtSelectedAttack;
        [SerializeField] private TextMeshProUGUI txtSelectedDefense;
        [SerializeField] private TextMeshProUGUI txtSelectedHealth;
        [SerializeField] private TextMeshProUGUI txtPartyCount;
        [SerializeField] private TextMeshProUGUI txtHint;

        [Header("Action Buttons")]
        [SerializeField] private Button btnAssignToParty;
        [SerializeField] private Button btnRemoveFromParty;

        [Header("Animation")]
        [SerializeField] private float highlightDuration = 0.25f;
        [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.5f, 1f);

        private const string SelectedHeroIdKey = "Kingdom.Hero.SelectedHeroId";
        private const string PartyKeyPrefix = "Kingdom.Hero.Party.";
        private const int DefaultPartySlots = 3;

        private readonly List<string> _partyHeroIds = new List<string>();
        private readonly Dictionary<string, HeroData> _heroMap = new Dictionary<string, HeroData>();
        private readonly Dictionary<string, HeroSlotView> _heroViewMap = new Dictionary<string, HeroSlotView>();

        private string _selectedHeroId;

        private void Awake()
        {
            EnsureRuntimeFallbackUi();
            _heroMap.Clear();
            for (int i = 0; i < heroRoster.Count; i++)
            {
                HeroData hero = heroRoster[i];
                if (hero == null || string.IsNullOrWhiteSpace(hero.HeroId))
                {
                    continue;
                }

                _heroMap[hero.HeroId] = hero;
            }

            BindHeroSlots();

            if (btnAssignToParty != null)
            {
                btnAssignToParty.onClick.RemoveAllListeners();
                btnAssignToParty.onClick.AddListener(OnClickAssign);
            }

            if (btnRemoveFromParty != null)
            {
                btnRemoveFromParty.onClick.RemoveAllListeners();
                btnRemoveFromParty.onClick.AddListener(OnClickRemove);
            }

            LoadSelectionFromPrefs();
        }

        private void OnEnable()
        {
            LoadSelectionFromPrefs();
            if (string.IsNullOrWhiteSpace(_selectedHeroId))
            {
                SelectFirstOwnedHero();
            }

            RefreshAll();
        }

        private void OnDisable()
        {
            SaveSelectionToPrefs();
        }

        public IReadOnlyList<string> GetPartyHeroIds()
        {
            return _partyHeroIds;
        }

        public bool TrySetParty(IReadOnlyList<string> heroIds)
        {
            _partyHeroIds.Clear();
            if (heroIds != null)
            {
                for (int i = 0; i < heroIds.Count && _partyHeroIds.Count < Mathf.Max(1, maxPartySize); i++)
                {
                    string heroId = heroIds[i];
                    if (!_heroMap.TryGetValue(heroId, out HeroData hero) || hero == null || !hero.IsOwned)
                    {
                        continue;
                    }

                    if (!_partyHeroIds.Contains(heroId))
                    {
                        _partyHeroIds.Add(heroId);
                    }
                }
            }

            RefreshAll();
            return true;
        }

        private void BindHeroSlots()
        {
            _heroViewMap.Clear();

            int bindCount = Mathf.Min(heroSlotViews.Count, heroRoster.Count);
            for (int i = 0; i < bindCount; i++)
            {
                HeroData hero = heroRoster[i];
                HeroSlotView view = heroSlotViews[i];
                if (hero == null || view == null)
                {
                    continue;
                }

                if (view.SelectButton != null)
                {
                    string capturedHeroId = hero.HeroId;
                    view.SelectButton.onClick.RemoveAllListeners();
                    view.SelectButton.onClick.AddListener(() => OnClickSelectHero(capturedHeroId));
                }

                _heroViewMap[hero.HeroId] = view;
            }
        }

        private void RefreshAll()
        {
            RefreshHeroList();
            RefreshSelectedHeroDetail();
            RefreshPartyView();
            RefreshButtons();
        }

        private void RefreshHeroList()
        {
            int bindCount = Mathf.Min(heroSlotViews.Count, heroRoster.Count);
            for (int i = 0; i < bindCount; i++)
            {
                HeroData hero = heroRoster[i];
                HeroSlotView view = heroSlotViews[i];
                if (hero == null || view == null)
                {
                    continue;
                }

                bool owned = hero.IsOwned;
                bool selected = string.Equals(hero.HeroId, _selectedHeroId, StringComparison.Ordinal);

                if (view.TxtName != null)
                {
                    view.TxtName.text = owned ? hero.DisplayName : $"{hero.DisplayName} (미보유)";
                }

                if (view.TxtLevel != null)
                {
                    view.TxtLevel.text = owned ? $"Lv.{Mathf.Max(1, hero.Level)}" : "잠김";
                }

                if (view.Portrait != null)
                {
                    view.Portrait.sprite = hero.Portrait;
                    view.Portrait.color = owned ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
                }

                if (view.SelectButton != null)
                {
                    view.SelectButton.interactable = owned;
                    ColorBlock cb = view.SelectButton.colors;
                    cb.normalColor = selected ? highlightColor : Color.white;
                    view.SelectButton.colors = cb;
                }
            }
        }

        private void RefreshSelectedHeroDetail()
        {
            if (!_heroMap.TryGetValue(_selectedHeroId, out HeroData hero) || hero == null)
            {
                SetEmptyDetail();
                return;
            }

            if (imgSelectedPortrait != null)
            {
                imgSelectedPortrait.sprite = hero.Portrait;
                imgSelectedPortrait.color = hero.IsOwned ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
            }

            if (txtSelectedName != null)
            {
                txtSelectedName.text = hero.DisplayName;
            }

            if (txtSelectedLevel != null)
            {
                txtSelectedLevel.text = $"레벨: {Mathf.Max(1, hero.Level)}";
            }

            if (txtSelectedAttack != null)
            {
                txtSelectedAttack.text = $"공격력: {Mathf.Max(1, hero.Attack)}";
            }

            if (txtSelectedDefense != null)
            {
                txtSelectedDefense.text = $"방어력: {Mathf.Max(0, hero.Defense)}";
            }

            if (txtSelectedHealth != null)
            {
                txtSelectedHealth.text = $"체력: {Mathf.Max(1, hero.Health)}";
            }
        }

        private void RefreshPartyView()
        {
            int partySize = Mathf.Max(1, maxPartySize);

            for (int i = 0; i < partySlotViews.Count; i++)
            {
                PartySlotView slot = partySlotViews[i];
                if (slot == null)
                {
                    continue;
                }

                HeroData hero = null;
                bool hasHero = i < _partyHeroIds.Count && _heroMap.TryGetValue(_partyHeroIds[i], out hero) && hero != null;
                if (hasHero)
                {
                    if (slot.Portrait != null)
                    {
                        slot.Portrait.sprite = hero.Portrait;
                        slot.Portrait.color = Color.white;
                    }

                    if (slot.TxtName != null)
                    {
                        slot.TxtName.text = hero.DisplayName;
                    }

                    if (slot.TxtLevel != null)
                    {
                        slot.TxtLevel.text = $"Lv.{Mathf.Max(1, hero.Level)}";
                    }
                }
                else
                {
                    if (slot.Portrait != null)
                    {
                        slot.Portrait.sprite = null;
                        slot.Portrait.color = new Color(1f, 1f, 1f, 0.2f);
                    }

                    if (slot.TxtName != null)
                    {
                        slot.TxtName.text = "빈 슬롯";
                    }

                    if (slot.TxtLevel != null)
                    {
                        slot.TxtLevel.text = "-";
                    }
                }
            }

            if (txtPartyCount != null)
            {
                txtPartyCount.text = $"파티: {_partyHeroIds.Count}/{partySize}";
            }
        }

        private void RefreshButtons()
        {
            bool hasSelected = _heroMap.ContainsKey(_selectedHeroId);
            bool alreadyInParty = hasSelected && _partyHeroIds.Contains(_selectedHeroId);
            bool canAssign = hasSelected && !alreadyInParty && _partyHeroIds.Count < Mathf.Max(1, maxPartySize);
            bool canRemove = hasSelected && alreadyInParty;

            if (btnAssignToParty != null)
            {
                btnAssignToParty.interactable = canAssign;
            }

            if (btnRemoveFromParty != null)
            {
                btnRemoveFromParty.interactable = canRemove;
            }

            if (txtHint != null)
            {
                if (!hasSelected)
                {
                    txtHint.text = "영웅을 선택하세요.";
                }
                else if (alreadyInParty)
                {
                    txtHint.text = "이미 파티에 포함된 영웅입니다.";
                }
                else if (_partyHeroIds.Count >= Mathf.Max(1, maxPartySize))
                {
                    txtHint.text = "파티가 가득 찼습니다.";
                }
                else
                {
                    txtHint.text = "선택한 영웅을 파티에 배치할 수 있습니다.";
                }
            }
        }

        private void OnClickSelectHero(string heroId)
        {
            if (!_heroMap.TryGetValue(heroId, out HeroData hero) || hero == null || !hero.IsOwned)
            {
                return;
            }

            _selectedHeroId = heroId;
            SaveSelectionToPrefs();
            RefreshAll();

            if (_heroViewMap.TryGetValue(heroId, out HeroSlotView view) && view != null && view.SelectButton != null)
            {
                StartCoroutine(CoBlink(view.SelectButton));
            }
        }

        private void OnClickAssign()
        {
            if (string.IsNullOrWhiteSpace(_selectedHeroId))
            {
                return;
            }

            if (_partyHeroIds.Count >= Mathf.Max(1, maxPartySize))
            {
                RefreshButtons();
                return;
            }

            if (_partyHeroIds.Contains(_selectedHeroId))
            {
                RefreshButtons();
                return;
            }

            _partyHeroIds.Add(_selectedHeroId);
            SaveSelectionToPrefs();
            RefreshAll();
        }

        private void EnsureRuntimeFallbackUi()
        {
            EnsureDefaultRoster();

            if (heroSlotViews != null && heroSlotViews.Count > 0 && imgSelectedPortrait != null && btnAssignToParty != null && btnRemoveFromParty != null)
            {
                return;
            }

            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            Image rootImage = GetComponent<Image>();
            if (rootImage == null)
            {
                rootImage = gameObject.AddComponent<Image>();
            }

            rootImage.color = new Color(0.1f, 0.12f, 0.16f, 0.96f);
            rootImage.raycastTarget = true;

            RectTransform listRoot = CreatePanel(root, "HeroListRoot", new Vector2(0f, 0f), new Vector2(0.38f, 1f), new Vector2(24f, 80f), new Vector2(-8f, -24f));
            RectTransform detailRoot = CreatePanel(root, "HeroDetailRoot", new Vector2(0.38f, 0f), new Vector2(1f, 1f), new Vector2(8f, 80f), new Vector2(-24f, -24f));
            RectTransform partyRoot = CreatePanel(root, "PartyRoot", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 24f), new Vector2(-24f, 68f));

            VerticalLayoutGroup listLayout = listRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 6f;
            listLayout.padding = new RectOffset(8, 8, 8, 8);
            listLayout.childControlHeight = true;
            listLayout.childControlWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;

            heroSlotViews = new List<HeroSlotView>();
            for (int i = 0; i < heroRoster.Count; i++)
            {
                HeroData hero = heroRoster[i];
                if (hero == null)
                {
                    continue;
                }

                heroSlotViews.Add(CreateHeroSlot(listRoot, hero));
            }

            BuildDetailUi(detailRoot);
            BuildPartyUi(partyRoot);
            BuildActionButtons(detailRoot);
        }

        private void EnsureDefaultRoster()
        {
            if (heroRoster != null && heroRoster.Count > 0)
            {
                return;
            }

            heroRoster = new List<HeroData>
            {
                new HeroData { HeroId = "DefaultHero", DisplayName = "Knight", Level = 1, Attack = 16, Defense = 8, Health = 120, IsOwned = true },
                new HeroData { HeroId = "ArcherHero", DisplayName = "Ranger", Level = 1, Attack = 14, Defense = 5, Health = 95, IsOwned = true },
                new HeroData { HeroId = "MageHero", DisplayName = "Mage", Level = 1, Attack = 20, Defense = 3, Health = 80, IsOwned = true }
            };
        }

        private static RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = panel.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.05f);
            image.raycastTarget = false;
            return rect;
        }

        private HeroSlotView CreateHeroSlot(RectTransform parent, HeroData hero)
        {
            GameObject slot = new GameObject($"HeroSlot_{hero.HeroId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.SetParent(parent, false);
            slot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            slot.GetComponent<LayoutElement>().preferredHeight = 72f;

            HorizontalLayoutGroup layout = slot.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            Image portrait = CreateImageChild(slotRect, "Portrait", new Vector2(52f, 52f), new Color(1f, 1f, 1f, 0.35f));
            TextMeshProUGUI txtName = CreateTextChild(slotRect, "TxtName", hero.DisplayName, 20f, TextAlignmentOptions.MidlineLeft, 210f);
            TextMeshProUGUI txtLevel = CreateTextChild(slotRect, "TxtLevel", $"Lv.{hero.Level}", 18f, TextAlignmentOptions.MidlineRight, 90f);

            return new HeroSlotView
            {
                SelectButton = slot.GetComponent<Button>(),
                Portrait = portrait,
                TxtName = txtName,
                TxtLevel = txtLevel
            };
        }

        private void BuildDetailUi(RectTransform detailRoot)
        {
            imgSelectedPortrait = CreateImageChild(detailRoot, "imgSelectedPortrait", new Vector2(160f, 160f), new Color(1f, 1f, 1f, 0.25f));
            RectTransform portraitRect = imgSelectedPortrait.rectTransform;
            portraitRect.anchorMin = new Vector2(0f, 1f);
            portraitRect.anchorMax = new Vector2(0f, 1f);
            portraitRect.pivot = new Vector2(0f, 1f);
            portraitRect.anchoredPosition = new Vector2(12f, -12f);

            txtSelectedName = CreateAnchoredText(detailRoot, "txtSelectedName", "영웅 미선택", 26f, new Vector2(190f, -18f));
            txtSelectedLevel = CreateAnchoredText(detailRoot, "txtSelectedLevel", "레벨: -", 20f, new Vector2(190f, -58f));
            txtSelectedAttack = CreateAnchoredText(detailRoot, "txtSelectedAttack", "공격력: -", 20f, new Vector2(190f, -90f));
            txtSelectedDefense = CreateAnchoredText(detailRoot, "txtSelectedDefense", "방어력: -", 20f, new Vector2(190f, -122f));
            txtSelectedHealth = CreateAnchoredText(detailRoot, "txtSelectedHealth", "체력: -", 20f, new Vector2(190f, -154f));
            txtHint = CreateAnchoredText(detailRoot, "txtHint", "영웅을 선택하세요.", 18f, new Vector2(12f, -220f));
        }

        private void BuildPartyUi(RectTransform partyRoot)
        {
            HorizontalLayoutGroup layout = partyRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            partySlotViews = new List<PartySlotView>();
            int slots = Mathf.Max(DefaultPartySlots, maxPartySize);
            for (int i = 0; i < slots; i++)
            {
                partySlotViews.Add(CreatePartySlot(partyRoot, i));
            }

            txtPartyCount = CreateAnchoredText(partyRoot, "txtPartyCount", $"파티: 0/{Mathf.Max(1, maxPartySize)}", 18f, new Vector2(8f, -8f));
        }

        private PartySlotView CreatePartySlot(RectTransform parent, int index)
        {
            GameObject slot = new GameObject($"PartySlot_{index}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.SetParent(parent, false);
            slot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            slot.GetComponent<LayoutElement>().preferredWidth = 170f;

            VerticalLayoutGroup layout = slot.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            Image portrait = CreateImageChild(slotRect, "Portrait", new Vector2(64f, 64f), new Color(1f, 1f, 1f, 0.25f));
            TextMeshProUGUI txtName = CreateTextChild(slotRect, "TxtName", "빈 슬롯", 16f, TextAlignmentOptions.Center, 120f);
            TextMeshProUGUI txtLevel = CreateTextChild(slotRect, "TxtLevel", "-", 14f, TextAlignmentOptions.Center, 120f);

            return new PartySlotView
            {
                Portrait = portrait,
                TxtName = txtName,
                TxtLevel = txtLevel
            };
        }

        private void BuildActionButtons(RectTransform detailRoot)
        {
            btnAssignToParty = CreateButton(detailRoot, "btnAssignToParty", "Assign", new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(12f, 12f), new Vector2(-6f, 56f), new Color(0.2f, 0.52f, 0.31f, 0.95f));
            btnRemoveFromParty = CreateButton(detailRoot, "btnRemoveFromParty", "Remove", new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(6f, 12f), new Vector2(-12f, 56f), new Color(0.5f, 0.23f, 0.23f, 0.95f));
        }

        private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 20f;

            return buttonObject.GetComponent<Button>();
        }

        private static Image CreateImageChild(RectTransform parent, string name, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            LayoutElement layout = imageObject.GetComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateTextChild(RectTransform parent, string name, string value, float fontSize, TextAlignmentOptions align, float preferredWidth)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            LayoutElement layout = textObject.GetComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            return text;
        }

        private static TextMeshProUGUI CreateAnchoredText(RectTransform parent, string name, string value, float fontSize, Vector2 anchoredPos)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(560f, 30f);
            rect.anchoredPosition = anchoredPos;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = Color.white;
            return text;
        }

        private void OnClickRemove()
        {
            if (string.IsNullOrWhiteSpace(_selectedHeroId))
            {
                return;
            }

            if (_partyHeroIds.Remove(_selectedHeroId))
            {
                SaveSelectionToPrefs();
                RefreshAll();
            }
        }

        private void LoadSelectionFromPrefs()
        {
            string selected = PlayerPrefs.GetString(SelectedHeroIdKey, string.Empty);
            _selectedHeroId = selected;

            _partyHeroIds.Clear();
            int partySlots = Mathf.Max(1, maxPartySize);
            for (int i = 0; i < partySlots; i++)
            {
                string heroId = PlayerPrefs.GetString(PartyKeyPrefix + i, string.Empty);
                if (string.IsNullOrWhiteSpace(heroId))
                {
                    continue;
                }

                if (_heroMap.TryGetValue(heroId, out HeroData hero) && hero != null && hero.IsOwned && !_partyHeroIds.Contains(heroId))
                {
                    _partyHeroIds.Add(heroId);
                }
            }

            Debug.Log($"[HeroSelectionUI] Selection loaded. selected={_selectedHeroId}, partyCount={_partyHeroIds.Count}");
        }

        private void SaveSelectionToPrefs()
        {
            PlayerPrefs.SetString(SelectedHeroIdKey, string.IsNullOrWhiteSpace(_selectedHeroId) ? string.Empty : _selectedHeroId);
            int partySlots = Mathf.Max(1, maxPartySize);
            for (int i = 0; i < partySlots; i++)
            {
                string heroId = i < _partyHeroIds.Count ? _partyHeroIds[i] : string.Empty;
                PlayerPrefs.SetString(PartyKeyPrefix + i, heroId);
            }

            PlayerPrefs.Save();
            Debug.Log($"[HeroSelectionUI] Selection saved. selected={_selectedHeroId}, partyCount={_partyHeroIds.Count}");
        }

        private void SelectFirstOwnedHero()
        {
            for (int i = 0; i < heroRoster.Count; i++)
            {
                HeroData hero = heroRoster[i];
                if (hero != null && hero.IsOwned)
                {
                    _selectedHeroId = hero.HeroId;
                    return;
                }
            }

            _selectedHeroId = string.Empty;
        }

        private void SetEmptyDetail()
        {
            if (imgSelectedPortrait != null)
            {
                imgSelectedPortrait.sprite = null;
                imgSelectedPortrait.color = new Color(1f, 1f, 1f, 0.2f);
            }

            if (txtSelectedName != null) txtSelectedName.text = "영웅 미선택";
            if (txtSelectedLevel != null) txtSelectedLevel.text = "레벨: -";
            if (txtSelectedAttack != null) txtSelectedAttack.text = "공격력: -";
            if (txtSelectedDefense != null) txtSelectedDefense.text = "방어력: -";
            if (txtSelectedHealth != null) txtSelectedHealth.text = "체력: -";
        }

        private IEnumerator CoBlink(Selectable selectable)
        {
            if (selectable == null)
            {
                yield break;
            }

            ColorBlock cb = selectable.colors;
            Color original = cb.normalColor;
            cb.normalColor = highlightColor;
            selectable.colors = cb;

            float elapsed = 0f;
            while (elapsed < highlightDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            cb.normalColor = original;
            selectable.colors = cb;
        }
    }
}
