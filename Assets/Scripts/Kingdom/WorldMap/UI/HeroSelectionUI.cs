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

        private readonly List<string> _partyHeroIds = new List<string>();
        private readonly Dictionary<string, HeroData> _heroMap = new Dictionary<string, HeroData>();
        private readonly Dictionary<string, HeroSlotView> _heroViewMap = new Dictionary<string, HeroSlotView>();

        private string _selectedHeroId;

        private void Awake()
        {
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
        }

        private void OnEnable()
        {
            if (string.IsNullOrWhiteSpace(_selectedHeroId))
            {
                SelectFirstOwnedHero();
            }

            RefreshAll();
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
            RefreshAll();
        }

        private void OnClickRemove()
        {
            if (string.IsNullOrWhiteSpace(_selectedHeroId))
            {
                return;
            }

            if (_partyHeroIds.Remove(_selectedHeroId))
            {
                RefreshAll();
            }
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
