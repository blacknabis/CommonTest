using System;
using Common.Extensions;
using Common.UI;
using Common.Utils;
using Kingdom.Game;
using Kingdom.Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.App
{
    /// <summary>
    /// 인게임 HUD 뷰.
    /// KR 스타일 최소 슬롯(Lives/Gold/Wave/NextWave/Hero/Spell/Result)을 제공한다.
    /// </summary>
    public class GameView : BaseView
    {
        private const float HudMargin = 24f;
        private const string HeroPortraitWidgetResourcePath = "UI/Widgets/HeroPortraitWidget";
        private const string AudioOptionsPopupResourcePath = "UI/WorldMap/AudioOptionsPopup";
        private const string SelectionInfoPanelResourcePath = "UI/SelectionInfoPanel";
        private static readonly Color TopLeftBackdropColor = new(0f, 0f, 0f, 0.32f);
        private static readonly Color BottomLeftBackdropColor = new(0f, 0f, 0f, 0.30f);
        private static readonly Color RightBackdropColor = new(0f, 0f, 0f, 0.28f);

        [Header("Debug Buttons")]
        [SerializeField] private Button btnVictory;
        [SerializeField] private Button btnDefeat;

        [Header("Top HUD")]
        [SerializeField] private Button btnPause;
        [SerializeField] private Button btnAudioOptions;
        [SerializeField] private Button btnSpeed;
        [SerializeField] private Button btnNextWave;
        [SerializeField] private TextMeshProUGUI txtWaveInfo;
        [SerializeField] private TextMeshProUGUI txtWaveTimer;
        [SerializeField] private TextMeshProUGUI txtStateInfo;
        [SerializeField] private TextMeshProUGUI txtWaveStartBanner;
        [SerializeField] private TextMeshProUGUI txtLives;
        [SerializeField] private TextMeshProUGUI txtGold;

        [Header("Hero / Spell")]
        [SerializeField] private Image imgHeroPortrait;
        [SerializeField] private HeroPortraitWidget heroPortraitWidget;
        [SerializeField] private Button btnBuildTower;
        [SerializeField] private GameObject towerRingRoot;
        [SerializeField] private Button btnTowerArcher;
        [SerializeField] private Button btnTowerBarracks;
        [SerializeField] private Button btnTowerMage;
        [SerializeField] private Button btnTowerArtillery;
        [SerializeField] private Button btnTowerCancel;
        [SerializeField] private GameObject towerActionRoot;
        [SerializeField] private TextMeshProUGUI txtTowerActionInfo;
        [SerializeField] private Button btnTowerUpgrade;
        [SerializeField] private Button btnTowerSell;
        [SerializeField] private Button btnTowerRally;
        [SerializeField] private Button btnTowerActionClose;
        [SerializeField] private Button btnSpellReinforce;
        [SerializeField] private Button btnSpellRain;
        [SerializeField] private Image imgSpellReinforceCooldown;
        [SerializeField] private Image imgSpellRainCooldown;
        [SerializeField] private TextMeshProUGUI txtTowerInfo;
        [SerializeField] private SelectionInfoPanel selectionInfoPanel;

        [Header("Result UI")]
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private TextMeshProUGUI txtResultTitle;
        [SerializeField] private TextMeshProUGUI txtResultMessage;
        [SerializeField] private Button btnRetry;
        [SerializeField] private Button btnExit;

        private GameStateController _stateController;
        private RectTransform _hudRoot;
        private Image _topLeftBackdrop;
        private Image _bottomLeftBackdrop;
        private Image _rightBackdrop;
        private bool _isPausedVisual;
        private int _ringGold;
        private int _ringRemainingSlots;
        private int _costArcher;
        private int _costBarracks;
        private int _costMage;
        private int _costArtillery;
        private GameObject _audioOptionsPopupRoot;

        public event Action NextWaveRequested;
        public event Action SpeedToggleRequested;
        public event Action BuildTowerRequested;
        public event Action<TowerType> TowerBuildTypeRequested;
        public event Action TowerUpgradeRequested;
        public event Action TowerSellRequested;
        public event Action TowerRallyRequested;
        public event Action SpellReinforceRequested;
        public event Action SpellRainRequested;
        public bool IsTowerRingMenuOpen => towerRingRoot != null && towerRingRoot.activeSelf;
        public bool IsTowerActionMenuOpen => towerActionRoot != null && towerActionRoot.activeSelf;

        private void Awake()
        {
            BindLegacyRefs();
            EnsureHudRoot();
            CleanupLegacyRootChildren();
            EnsureCoreSlots();
            EnsureSelectionInfoPanel();
            EnsureHudBackdrops();
            NormalizeLayout();
            EnsureResultSlot();
        }

        protected override void OnInit()
        {
            BindButton(btnVictory, OnVictory);
            BindButton(btnDefeat, OnDefeat);
            BindPauseButton(btnPause, OnPause);
            BindButton(btnAudioOptions, OnAudioOptions);
            BindButton(btnSpeed, OnSpeedToggle);
            BindButton(btnNextWave, OnNextWave);
            BindButton(btnBuildTower, OnBuildTower);
            BindButton(btnTowerArcher, () => OnTowerTypeSelected(TowerType.Archer));
            BindButton(btnTowerBarracks, () => OnTowerTypeSelected(TowerType.Barracks));
            BindButton(btnTowerMage, () => OnTowerTypeSelected(TowerType.Mage));
            BindButton(btnTowerArtillery, () => OnTowerTypeSelected(TowerType.Artillery));
            BindButton(btnTowerCancel, HideTowerRingMenu);
            BindButton(btnTowerUpgrade, OnTowerUpgrade);
            BindButton(btnTowerSell, OnTowerSell);
            BindButton(btnTowerRally, OnTowerRally);
            BindButton(btnTowerActionClose, HideTowerActionMenu);
            BindButton(btnSpellReinforce, OnSpellReinforce);
            BindButton(btnSpellRain, OnSpellRain);
            BindButton(btnRetry, OnRetry);
            BindButton(btnExit, OnExitToMap);

            HideResult();
            SetPauseVisual(false);
            SetSpeedVisual(false);
            UpdateWaveInfo(1, 1);
            HideWaveReadyCountdown();
            HideWaveStartBanner();
            UpdateResourceInfo(20, 100);
            SetHeroPortrait(null);
            SetSpellCooldown("reinforce", 0f);
            SetSpellCooldown("rain", 0f);
            SetTowerInfo(0, 0);
            SetTowerRingMenuAvailability(100, 0, 70, 77, 84, 95);
            HideTowerRingMenu();
            HideTowerActionMenu();
        }

        protected override void OnEnter(object[] data)
        {
            base.OnEnter(data);
            SetStateText("Prepare");
            HideResult();
            AudioHelper.Instance.PlayBGM("BGM_GameScene");
        }

        protected override void OnExit()
        {
            if (_stateController != null)
            {
                _stateController.WaveChanged -= OnWaveChanged;
                _stateController.StateChanged -= OnStateChanged;
            }

            if (_audioOptionsPopupRoot != null)
            {
                Destroy(_audioOptionsPopupRoot);
                _audioOptionsPopupRoot = null;
            }

            CancelInvoke(nameof(HideWaveStartBanner));
            HideWaveStartBanner();
            Time.timeScale = 1f;
            base.OnExit();
        }

        public void Bind(GameStateController controller)
        {
            if (_stateController != null)
            {
                _stateController.WaveChanged -= OnWaveChanged;
                _stateController.StateChanged -= OnStateChanged;
            }

            _stateController = controller;
            if (_stateController == null)
            {
                return;
            }

            _stateController.WaveChanged += OnWaveChanged;
            _stateController.StateChanged += OnStateChanged;
            int currentWave = _stateController.CurrentWave <= 0 ? 1 : _stateController.CurrentWave;
            UpdateWaveInfo(currentWave, _stateController.TotalWaves);
            OnStateChanged(_stateController.CurrentState);
        }

        public void UpdateWaveInfo(int current, int total)
        {
            if (txtWaveInfo != null)
            {
                txtWaveInfo.text = $"WAVE {Mathf.Max(1, current)} / {Mathf.Max(1, total)}";
            }
        }

        public void UpdateResourceInfo(int lives, int gold)
        {
            if (txtLives != null) txtLives.text = $"LIVES {Mathf.Max(0, lives)}";
            if (txtGold != null) txtGold.text = $"GOLD {Mathf.Max(0, gold)}";
        }

        public void SetHeroPortrait(Sprite portrait)
        {
            if (heroPortraitWidget == null && imgHeroPortrait == null)
            {
                return;
            }

            if (heroPortraitWidget != null)
            {
                heroPortraitWidget.SetPortrait(portrait);
                imgHeroPortrait = heroPortraitWidget.PortraitImage;
                return;
            }

            imgHeroPortrait.sprite = portrait;
            imgHeroPortrait.preserveAspect = true;
            imgHeroPortrait.color = portrait != null
                ? Color.white
                : new Color(1f, 1f, 1f, 0.18f);
        }

        public void SetNextWaveInteractable(bool interactable)
        {
            if (btnNextWave != null)
            {
                btnNextWave.interactable = interactable;
            }
        }

        public void SetWaveReadyCountdown(float remainingSeconds)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
            if (txtWaveTimer != null)
            {
                txtWaveTimer.gameObject.SetActive(true);
                txtWaveTimer.text = $"NEXT WAVE IN {seconds}";
                return;
            }

            if (txtStateInfo != null && !_isPausedVisual)
            {
                txtStateInfo.text = $"WaveReady {seconds}";
            }
        }

        public void HideWaveReadyCountdown()
        {
            if (txtWaveTimer != null)
            {
                txtWaveTimer.gameObject.SetActive(false);
            }
        }

        public void ShowWaveStartBanner(int currentWave, int totalWave, float durationSec = 1.2f)
        {
            if (txtWaveStartBanner == null)
            {
                return;
            }

            int safeCurrent = Mathf.Max(1, currentWave);
            int safeTotal = Mathf.Max(1, totalWave);
            txtWaveStartBanner.text = $"WAVE {safeCurrent}/{safeTotal} START!";
            txtWaveStartBanner.gameObject.SetActive(true);
            txtWaveStartBanner.alpha = 1f;

            CancelInvoke(nameof(HideWaveStartBanner));
            if (durationSec > 0f)
            {
                Invoke(nameof(HideWaveStartBanner), durationSec);
            }
        }

        public void HideWaveStartBanner()
        {
            if (txtWaveStartBanner == null)
            {
                return;
            }

            txtWaveStartBanner.gameObject.SetActive(false);
        }

        public void SetSpellCooldown(string spellId, float normalized01)
        {
            float clamped = Mathf.Clamp01(normalized01);
            if (string.Equals(spellId, "reinforce", StringComparison.OrdinalIgnoreCase))
            {
                ApplyCooldownVisual(imgSpellReinforceCooldown, clamped);
                return;
            }

            ApplyCooldownVisual(imgSpellRainCooldown, clamped);
        }

        public void SetTowerInfo(int builtCount, int remainSlots)
        {
            if (txtTowerInfo != null)
            {
                txtTowerInfo.text = $"TOWER {Mathf.Max(0, builtCount)} / SLOT {Mathf.Max(0, remainSlots)}";
            }
        }

        public void SetTowerRingMenuAvailability(
            int gold,
            int remainingSlots,
            int archerCost,
            int barracksCost,
            int mageCost,
            int artilleryCost)
        {
            _ringGold = Mathf.Max(0, gold);
            _ringRemainingSlots = Mathf.Max(0, remainingSlots);
            _costArcher = Mathf.Max(1, archerCost);
            _costBarracks = Mathf.Max(1, barracksCost);
            _costMage = Mathf.Max(1, mageCost);
            _costArtillery = Mathf.Max(1, artilleryCost);

            bool hasSlot = _ringRemainingSlots > 0;
            bool archerAffordable = _ringGold >= _costArcher;
            bool barracksAffordable = _ringGold >= _costBarracks;
            bool mageAffordable = _ringGold >= _costMage;
            bool artilleryAffordable = _ringGold >= _costArtillery;
            UpdateTowerRingButton(btnTowerArcher, "Archer", _costArcher, hasSlot, archerAffordable, new Color(0.22f, 0.42f, 0.72f, 0.95f));
            UpdateTowerRingButton(btnTowerBarracks, "Barracks", _costBarracks, hasSlot, barracksAffordable, new Color(0.35f, 0.55f, 0.22f, 0.95f));
            UpdateTowerRingButton(btnTowerMage, "Mage", _costMage, hasSlot, mageAffordable, new Color(0.45f, 0.3f, 0.72f, 0.95f));
            UpdateTowerRingButton(btnTowerArtillery, "Artillery", _costArtillery, hasSlot, artilleryAffordable, new Color(0.68f, 0.42f, 0.2f, 0.95f));
        }

        public void ShowResult(bool isVictory, string message = null)
        {
            if (resultRoot != null) resultRoot.SetActive(true);
            if (txtResultTitle != null) txtResultTitle.text = isVictory ? "VICTORY" : "DEFEAT";
            if (txtResultMessage != null)
            {
                txtResultMessage.text = string.IsNullOrWhiteSpace(message)
                    ? (isVictory ? "스테이지 클리어!" : "방어선이 돌파되었습니다.")
                    : message;
            }
        }

        public void HideResult()
        {
            if (resultRoot != null) resultRoot.SetActive(false);
        }

        public void SetPauseVisual(bool isPaused)
        {
            _isPausedVisual = isPaused;
            if (txtStateInfo != null && isPaused)
            {
                txtStateInfo.text = "Pause";
            }

            SetPauseButtonLabel(isPaused ? "Resume" : "Pause");
        }

        public void SetSpeedVisual(bool isFastForward)
        {
            if (btnSpeed == null)
            {
                return;
            }

            var text = btnSpeed.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = isFastForward ? "x2" : "x1";
            }
        }

        private void OnVictory()
        {
            ShowResult(true);
        }

        private void OnDefeat()
        {
            ShowResult(false);
        }

        private void OnPause()
        {
            if (_stateController == null)
            {
                Debug.Log("[GameView] Pause clicked, but state controller is not bound.");
                return;
            }

            _stateController.TogglePause();
        }

        private void OnAudioOptions()
        {
            OpenAudioOptionsPopup();
        }

        private void OnNextWave()
        {
            NextWaveRequested?.Invoke();
        }

        private void OnSpeedToggle()
        {
            SpeedToggleRequested?.Invoke();
        }

        private void OnBuildTower()
        {
            if (towerRingRoot == null || btnTowerArcher == null || btnTowerBarracks == null || btnTowerMage == null || btnTowerArtillery == null)
            {
                BuildTowerRequested?.Invoke();
                return;
            }

            if (_ringRemainingSlots <= 0)
            {
                SetStateText("No Slot");
                return;
            }

            OpenTowerRingMenuAtLocalPosition(new Vector2(HudMargin + 390f, HudMargin + 150f));
        }

        private void OnTowerTypeSelected(TowerType towerType)
        {
            HideTowerRingMenu();
            TowerBuildTypeRequested?.Invoke(towerType);
        }

        private void OnTowerUpgrade()
        {
            TowerUpgradeRequested?.Invoke();
        }

        private void OnTowerSell()
        {
            TowerSellRequested?.Invoke();
        }

        private void OnTowerRally()
        {
            TowerRallyRequested?.Invoke();
        }

        private void OnSpellReinforce()
        {
            SpellReinforceRequested?.Invoke();
        }

        private void OnSpellRain()
        {
            SpellRainRequested?.Invoke();
        }

        private void OnRetry()
        {
            KingdomAppManager.Instance.ChangeScene(SCENES.GameScene);
        }

        private void OnExitToMap()
        {
            KingdomAppManager.Instance.ChangeScene(SCENES.WorldMapScene);
        }

        private void OnWaveChanged(int current, int total)
        {
            UpdateWaveInfo(current, total);
        }

        private void OnStateChanged(GameFlowState state)
        {
            SetPauseVisual(state == GameFlowState.Pause);
            SetStateText(state.ToString());

            if (state != GameFlowState.WaveReady)
            {
                HideWaveReadyCountdown();
            }

            if (state == GameFlowState.Result)
            {
                ShowResult(true, "결과 처리 연결 대기 중");
            }
        }

        private void SetStateText(string state)
        {
            if (txtStateInfo != null && !_isPausedVisual)
            {
                txtStateInfo.text = state;
            }
            else if (txtStateInfo != null && state != "Pause")
            {
                txtStateInfo.text = state;
            }
        }

        private void ApplyCooldownVisual(Image image, float amount01)
        {
            if (image == null) return;
            image.fillAmount = amount01;
            image.gameObject.SetActive(amount01 > 0f);
        }

        private void BindButton(Button button, Action callback)
        {
            if (button != null)
            {
                button.SetOnClickWithCooldown(callback);
            }
        }

        private static void BindPauseButton(Button button, Action callback)
        {
            if (button == null)
            {
                return;
            }

            // Pause/Resume 토글은 timescale과 무관하게 즉시 입력 가능해야 한다.
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }

        private void SetPauseButtonLabel(string label)
        {
            if (btnPause == null)
            {
                return;
            }

            var text = btnPause.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = label;
            }
        }

        private void BindLegacyRefs()
        {
            btnVictory = btnVictory != null ? btnVictory : FindButton("btnVictory");
            btnDefeat = btnDefeat != null ? btnDefeat : FindButton("btnDefeat");
            btnPause = btnPause != null ? btnPause : FindButton("btnPause");
            btnAudioOptions = btnAudioOptions != null ? btnAudioOptions : FindButton("btnAudioOptions");
            btnSpeed = btnSpeed != null ? btnSpeed : FindButton("btnSpeed");
            btnNextWave = btnNextWave != null ? btnNextWave : FindButton("btnNextWave");
            btnBuildTower = btnBuildTower != null ? btnBuildTower : FindButton("btnBuildTower");
            btnTowerArcher = btnTowerArcher != null ? btnTowerArcher : FindButton("TowerRingMenuRoot/btnTowerArcher");
            btnTowerBarracks = btnTowerBarracks != null ? btnTowerBarracks : FindButton("TowerRingMenuRoot/btnTowerBarracks");
            btnTowerMage = btnTowerMage != null ? btnTowerMage : FindButton("TowerRingMenuRoot/btnTowerMage");
            btnTowerArtillery = btnTowerArtillery != null ? btnTowerArtillery : FindButton("TowerRingMenuRoot/btnTowerArtillery");
            btnTowerCancel = btnTowerCancel != null ? btnTowerCancel : FindButton("TowerRingMenuRoot/btnTowerCancel");
            btnTowerUpgrade = btnTowerUpgrade != null ? btnTowerUpgrade : FindButton("TowerActionMenuRoot/btnTowerUpgrade");
            btnTowerSell = btnTowerSell != null ? btnTowerSell : FindButton("TowerActionMenuRoot/btnTowerSell");
            btnTowerRally = btnTowerRally != null ? btnTowerRally : FindButton("TowerActionMenuRoot/btnTowerRally");
            btnTowerActionClose = btnTowerActionClose != null ? btnTowerActionClose : FindButton("TowerActionMenuRoot/btnTowerActionClose");
            btnSpellReinforce = btnSpellReinforce != null ? btnSpellReinforce : FindButton("btnSpellReinforce");
            btnSpellRain = btnSpellRain != null ? btnSpellRain : FindButton("btnSpellRain");
            btnRetry = btnRetry != null ? btnRetry : FindButton("ResultRoot/btnRetry");
            btnExit = btnExit != null ? btnExit : FindButton("ResultRoot/btnExit");

            txtWaveInfo = txtWaveInfo != null ? txtWaveInfo : FindText("txtWaveInfo");
            txtWaveTimer = txtWaveTimer != null ? txtWaveTimer : FindText("txtWaveTimer");
            txtStateInfo = txtStateInfo != null ? txtStateInfo : FindText("txtStateInfo");
            txtWaveStartBanner = txtWaveStartBanner != null ? txtWaveStartBanner : FindText("txtWaveStartBanner");
            txtLives = txtLives != null ? txtLives : FindText("txtLives");
            txtGold = txtGold != null ? txtGold : FindText("txtGold");
            txtTowerInfo = txtTowerInfo != null ? txtTowerInfo : FindText("txtTowerInfo");
            txtResultTitle = txtResultTitle != null ? txtResultTitle : FindText("ResultRoot/txtResultTitle");
            txtResultMessage = txtResultMessage != null ? txtResultMessage : FindText("ResultRoot/txtResultMessage");
            txtTowerActionInfo = txtTowerActionInfo != null ? txtTowerActionInfo : FindText("TowerActionMenuRoot/txtTowerActionInfo");

            imgHeroPortrait = imgHeroPortrait != null ? imgHeroPortrait : FindImage("imgHeroPortrait");
            if (heroPortraitWidget == null)
            {
                var tr = transform.Find("HeroPortraitWidget");
                if (tr != null)
                {
                    heroPortraitWidget = tr.GetComponent<HeroPortraitWidget>();
                }
            }

            if (heroPortraitWidget == null)
            {
                heroPortraitWidget = GetComponentInChildren<HeroPortraitWidget>(true);
            }

            if (imgHeroPortrait == null && heroPortraitWidget != null)
            {
                imgHeroPortrait = heroPortraitWidget.PortraitImage;
            }
            imgSpellReinforceCooldown = imgSpellReinforceCooldown != null
                ? imgSpellReinforceCooldown
                : FindImage("btnSpellReinforce/CooldownFill");
            imgSpellRainCooldown = imgSpellRainCooldown != null
                ? imgSpellRainCooldown
                : FindImage("btnSpellRain/CooldownFill");

            if (resultRoot == null)
            {
                var tr = transform.Find("ResultRoot");
                if (tr != null) resultRoot = tr.gameObject;
            }

            if (towerRingRoot == null)
            {
                var tr = transform.Find("TowerRingMenuRoot");
                if (tr != null) towerRingRoot = tr.gameObject;
            }

            if (towerActionRoot == null)
            {
                var tr = transform.Find("TowerActionMenuRoot");
                if (tr != null) towerActionRoot = tr.gameObject;
            }

            if (selectionInfoPanel.IsNull())
            {
                selectionInfoPanel = GetComponentInChildren<SelectionInfoPanel>(true);
            }
        }

        private void EnsureHudRoot()
        {
            var found = transform.Find("HUDRoot");
            if (found != null)
            {
                _hudRoot = found.GetComponent<RectTransform>();
            }

            if (_hudRoot == null)
            {
                _hudRoot = new GameObject("HUDRoot", typeof(RectTransform)).GetComponent<RectTransform>();
                _hudRoot.SetParent(transform, false);
            }

            Stretch(_hudRoot);
        }

        private void EnsureCoreSlots()
        {
            txtWaveInfo ??= CreateText("txtWaveInfo");
            txtStateInfo ??= CreateText("txtStateInfo");
            txtWaveStartBanner ??= CreateText("txtWaveStartBanner");
            txtLives ??= CreateText("txtLives");
            txtGold ??= CreateText("txtGold");

            btnPause ??= CreateButton("btnPause", "Pause");
            btnAudioOptions ??= CreateButton("btnAudioOptions", "Audio");
            btnSpeed ??= CreateButton("btnSpeed", "x1");
            btnNextWave ??= CreateButton("btnNextWave", "Next Wave");
            btnBuildTower ??= CreateButton("btnBuildTower", "Build Tower");
            btnVictory ??= CreateButton("btnVictory", "Victory");
            btnDefeat ??= CreateButton("btnDefeat", "Defeat");
            btnSpellReinforce ??= CreateButton("btnSpellReinforce", "Reinforce");
            btnSpellRain ??= CreateButton("btnSpellRain", "Rain");

            EnsureHeroPortraitWidget();

            EnsureCooldownOverlay(btnSpellReinforce, ref imgSpellReinforceCooldown);
            EnsureCooldownOverlay(btnSpellRain, ref imgSpellRainCooldown);
            txtTowerInfo ??= CreateText("txtTowerInfo");
            EnsureTowerRingMenu();
            EnsureTowerActionMenu();
        }

        private void EnsureSelectionInfoPanel()
        {
            RemoveLegacySelectionInfoPanels();

            GameObject panelPrefab = Resources.Load<GameObject>(SelectionInfoPanelResourcePath);
            if (panelPrefab.IsNull())
            {
                Debug.LogError($"[GameView] SelectionInfoPanel prefab missing: {SelectionInfoPanelResourcePath}");
                return;
            }

            GameObject instance = Instantiate(panelPrefab, _hudRoot, false);
            instance.name = "SelectionInfoPanel";
            selectionInfoPanel = instance.GetComponent<SelectionInfoPanel>();
            if (selectionInfoPanel.IsNull())
            {
                Debug.LogError("[GameView] SelectionInfoPanel component missing on prefab instance.");
            }
        }

        private void RemoveLegacySelectionInfoPanels()
        {
            SelectionInfoPanel[] panels = GetComponentsInChildren<SelectionInfoPanel>(true);
            for (int i = 0; i < panels.Length; i++)
            {
                SelectionInfoPanel panel = panels[i];
                if (panel.IsNull())
                {
                    continue;
                }

                Destroy(panel.gameObject);
            }

            selectionInfoPanel = null;
        }

        private void NormalizeLayout()
        {
            Reparent(btnVictory);
            Reparent(btnDefeat);
            Reparent(btnPause);
            Reparent(btnAudioOptions);
            Reparent(btnSpeed);
            Reparent(btnNextWave);
            Reparent(btnBuildTower);
            Reparent(btnSpellReinforce);
            Reparent(btnSpellRain);
            Reparent(txtWaveInfo);
            Reparent(txtStateInfo);
            Reparent(txtWaveStartBanner);
            Reparent(txtLives);
            Reparent(txtGold);
            Reparent(txtTowerInfo);
            if (heroPortraitWidget != null)
            {
                Reparent(heroPortraitWidget);
            }
            else
            {
                Reparent(imgHeroPortrait);
            }

            PlaceTextTopLeft(txtWaveInfo, new Vector2(HudMargin, -HudMargin), 40);
            PlaceTextTopLeft(txtStateInfo, new Vector2(HudMargin, -HudMargin - 50f), 30);
            PlaceTextTopLeft(txtLives, new Vector2(HudMargin, -HudMargin - 95f), 28);
            PlaceTextTopLeft(txtGold, new Vector2(HudMargin, -HudMargin - 135f), 28);
            PlaceTextTopCenter(txtWaveStartBanner, new Vector2(0f, -96f), 56f);

            PlaceButton(btnPause, new Vector2(1f, 1f), new Vector2(-HudMargin, -HudMargin), new Vector2(120f, 56f), new Color(0.18f, 0.2f, 0.25f, 0.9f), "Pause");
            PlaceButton(btnAudioOptions, new Vector2(1f, 1f), new Vector2(-HudMargin - 132f, -HudMargin), new Vector2(120f, 56f), new Color(0.2f, 0.32f, 0.24f, 0.9f), "Audio");
            PlaceButton(btnSpeed, new Vector2(1f, 1f), new Vector2(-HudMargin - 264f, -HudMargin), new Vector2(120f, 56f), new Color(0.16f, 0.24f, 0.36f, 0.9f), "x1");
            PlaceButton(btnNextWave, new Vector2(1f, 1f), new Vector2(-HudMargin, -HudMargin - 66f), new Vector2(180f, 56f), new Color(0.45f, 0.2f, 0.2f, 0.9f), "Next Wave");
            PlaceButton(btnVictory, new Vector2(1f, 0f), new Vector2(-HudMargin, HudMargin + 70f), new Vector2(150f, 56f), new Color(0.2f, 0.55f, 0.2f, 0.9f), "Victory");
            PlaceButton(btnDefeat, new Vector2(1f, 0f), new Vector2(-HudMargin, HudMargin), new Vector2(150f, 56f), new Color(0.65f, 0.2f, 0.2f, 0.9f), "Defeat");
            PlaceButton(btnSpellReinforce, new Vector2(0f, 0f), new Vector2(HudMargin, HudMargin), new Vector2(160f, 56f), new Color(0.2f, 0.35f, 0.6f, 0.9f), "Reinforce");
            PlaceButton(btnSpellRain, new Vector2(0f, 0f), new Vector2(HudMargin + 170f, HudMargin), new Vector2(140f, 56f), new Color(0.55f, 0.35f, 0.2f, 0.9f), "Rain");
            PlaceButton(btnBuildTower, new Vector2(0f, 0f), new Vector2(HudMargin + 320f, HudMargin), new Vector2(170f, 56f), new Color(0.25f, 0.55f, 0.35f, 0.9f), "Build");
            PlaceTextTopLeft(txtTowerInfo, new Vector2(HudMargin, -HudMargin - 175f), 24);
            LayoutTowerRingMenu();
            LayoutTowerActionMenu();

            RectTransform portraitRect = null;
            if (heroPortraitWidget != null)
            {
                portraitRect = heroPortraitWidget.transform as RectTransform;
            }
            else if (imgHeroPortrait != null)
            {
                portraitRect = imgHeroPortrait.GetComponent<RectTransform>();
            }

            if (portraitRect != null)
            {
                portraitRect.anchorMin = new Vector2(0f, 0f);
                portraitRect.anchorMax = new Vector2(0f, 0f);
                portraitRect.pivot = new Vector2(0f, 0f);
                portraitRect.anchoredPosition = new Vector2(HudMargin, HudMargin + 72f);
                portraitRect.sizeDelta = new Vector2(84f, 84f);
            }

            LayoutHudBackdrops();
        }

        private void EnsureResultSlot()
        {
            if (resultRoot != null)
            {
                resultRoot.transform.SetParent(_hudRoot, false);
                SetupResultRootRect(resultRoot.GetComponent<RectTransform>());
                return;
            }

            var root = new GameObject("ResultRoot", typeof(RectTransform));
            root.transform.SetParent(_hudRoot, false);
            resultRoot = root;
            SetupResultRootRect(root.GetComponent<RectTransform>());

            txtResultTitle = txtResultTitle != null ? txtResultTitle : CreateResultText(root.transform, "txtResultTitle", 42f, new Vector2(0f, -36f), new Vector2(600f, 80f), true);
            txtResultMessage = txtResultMessage != null ? txtResultMessage : CreateResultText(root.transform, "txtResultMessage", 28f, new Vector2(0f, 20f), new Vector2(640f, 120f), false);
            btnRetry = btnRetry != null ? btnRetry : CreateResultButton(root.transform, "btnRetry", "Retry", new Vector2(-120f, 36f));
            btnExit = btnExit != null ? btnExit : CreateResultButton(root.transform, "btnExit", "Exit", new Vector2(120f, 36f));
        }

        private TextMeshProUGUI CreateText(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_hudRoot, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.fontSize = 28;
            return text;
        }

        private Button CreateButton(string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_hudRoot, false);
            var button = go.GetComponent<Button>();

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var text = txtGo.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 28;
            text.color = Color.white;

            var txtRect = txtGo.GetComponent<RectTransform>();
            Stretch(txtRect);
            return button;
        }

        private Button CreateButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var button = go.GetComponent<Button>();

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var text = txtGo.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 22;
            text.color = Color.white;

            Stretch(txtGo.GetComponent<RectTransform>());
            return button;
        }

        private static TextMeshProUGUI CreateResultText(Transform parent, string name, float size, Vector2 anchoredPos, Vector2 box, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = size;

            var rect = text.GetComponent<RectTransform>();
            if (top)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = box;
            return text;
        }

        private static Button CreateResultButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(180f, 70f);

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var text = txtGo.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 30;
            text.color = Color.black;
            Stretch(txtGo.GetComponent<RectTransform>());

            return go.GetComponent<Button>();
        }

        private void Reparent(Component component)
        {
            if (component == null) return;
            component.transform.SetParent(_hudRoot, false);
        }

        private void EnsureHeroPortraitWidget()
        {
            if (_hudRoot == null)
            {
                return;
            }

            if (heroPortraitWidget == null)
            {
                GameObject prefab = Resources.Load<GameObject>(HeroPortraitWidgetResourcePath);
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, _hudRoot, false);
                    instance.name = "HeroPortraitWidget";
                    heroPortraitWidget = instance.GetComponent<HeroPortraitWidget>();
                }
            }

            if (heroPortraitWidget == null)
            {
                heroPortraitWidget = HeroPortraitWidget.CreateFallback(_hudRoot);
            }

            heroPortraitWidget.EnsureRuntimeDefaults();
            imgHeroPortrait = heroPortraitWidget.PortraitImage;
        }

        private static void PlaceTextTopLeft(TextMeshProUGUI text, Vector2 pos, float fontSize)
        {
            if (text == null) return;
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(560f, 46f);
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Left;
        }

        private static void PlaceTextTopCenter(TextMeshProUGUI text, Vector2 pos, float fontSize)
        {
            if (text == null)
            {
                return;
            }

            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(840f, 68f);
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.93f, 0.66f, 1f);
            text.raycastTarget = false;
        }

        private static void PlaceButton(Button button, Vector2 anchor, Vector2 pos, Vector2 size, Color color, string label)
        {
            if (button == null) return;
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var image = button.GetComponent<Image>();
            if (image != null) image.color = color;

            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = label;
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 26f;
            }
        }

        private static void EnsureCooldownOverlay(Button owner, ref Image cooldownImage)
        {
            if (owner == null) return;
            var ownerRect = owner.GetComponent<RectTransform>();
            Transform existing = ownerRect.Find("CooldownFill");
            if (existing != null)
            {
                cooldownImage = existing.GetComponent<Image>();
            }

            if (cooldownImage == null)
            {
                var go = new GameObject("CooldownFill", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(ownerRect, false);
                cooldownImage = go.GetComponent<Image>();
                cooldownImage.color = new Color(0f, 0f, 0f, 0.45f);
                cooldownImage.type = Image.Type.Filled;
                cooldownImage.fillMethod = Image.FillMethod.Vertical;
                cooldownImage.fillOrigin = (int)Image.OriginVertical.Top;
            }

            Stretch(cooldownImage.GetComponent<RectTransform>());
            cooldownImage.fillAmount = 0f;
            cooldownImage.gameObject.SetActive(false);
        }

        private void EnsureTowerRingMenu()
        {
            if (towerRingRoot == null)
            {
                var go = new GameObject("TowerRingMenuRoot", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_hudRoot, false);
                towerRingRoot = go;

                var bg = go.GetComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.45f);
            }

            var rootRect = towerRingRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(260f, 260f);

            btnTowerArcher ??= CreateButton(towerRingRoot.transform, "btnTowerArcher", "Archer");
            btnTowerBarracks ??= CreateButton(towerRingRoot.transform, "btnTowerBarracks", "Barracks");
            btnTowerMage ??= CreateButton(towerRingRoot.transform, "btnTowerMage", "Mage");
            btnTowerArtillery ??= CreateButton(towerRingRoot.transform, "btnTowerArtillery", "Artillery");
            btnTowerCancel ??= CreateButton(towerRingRoot.transform, "btnTowerCancel", "X");
        }

        private void EnsureTowerActionMenu()
        {
            if (towerActionRoot == null)
            {
                var go = new GameObject("TowerActionMenuRoot", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_hudRoot, false);
                towerActionRoot = go;

                var bg = go.GetComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.52f);
            }

            var rootRect = towerActionRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(260f, 180f);

            if (txtTowerActionInfo == null)
            {
                var go = new GameObject("txtTowerActionInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(towerActionRoot.transform, false);
                txtTowerActionInfo = go.GetComponent<TextMeshProUGUI>();
                txtTowerActionInfo.alignment = TextAlignmentOptions.Center;
                txtTowerActionInfo.fontSize = 20f;
                txtTowerActionInfo.color = Color.white;
            }

            btnTowerUpgrade ??= CreateButton(towerActionRoot.transform, "btnTowerUpgrade", "Upgrade");
            btnTowerSell ??= CreateButton(towerActionRoot.transform, "btnTowerSell", "Sell");
            btnTowerRally ??= CreateButton(towerActionRoot.transform, "btnTowerRally", "Rally");
            btnTowerActionClose ??= CreateButton(towerActionRoot.transform, "btnTowerActionClose", "X");
        }

        private void LayoutTowerRingMenu()
        {
            if (towerRingRoot == null)
            {
                return;
            }

            towerRingRoot.transform.SetParent(_hudRoot, false);
            var rootRect = towerRingRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(-320f, -180f);
            rootRect.sizeDelta = new Vector2(260f, 260f);

            PlaceRingButton(btnTowerArcher, new Vector2(0f, 1f), new Vector2(60f, -130f), new Vector2(120f, 52f), new Color(0.22f, 0.42f, 0.72f, 0.95f));
            PlaceRingButton(btnTowerBarracks, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(120f, 52f), new Color(0.35f, 0.55f, 0.22f, 0.95f));
            PlaceRingButton(btnTowerMage, new Vector2(1f, 1f), new Vector2(-60f, -130f), new Vector2(120f, 52f), new Color(0.45f, 0.3f, 0.72f, 0.95f));
            PlaceRingButton(btnTowerArtillery, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(120f, 52f), new Color(0.68f, 0.42f, 0.2f, 0.95f));
            PlaceRingButton(btnTowerCancel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f), new Color(0.12f, 0.12f, 0.12f, 0.95f));
            SetTowerRingMenuAvailability(_ringGold, _ringRemainingSlots, _costArcher, _costBarracks, _costMage, _costArtillery);

            HideTowerRingMenu();
        }

        private void LayoutTowerActionMenu()
        {
            if (towerActionRoot == null)
            {
                return;
            }

            towerActionRoot.transform.SetParent(_hudRoot, false);
            var rootRect = towerActionRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(-120f, -80f);
            rootRect.sizeDelta = new Vector2(260f, 180f);

            if (txtTowerActionInfo != null)
            {
                var textRect = txtTowerActionInfo.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.5f, 1f);
                textRect.anchorMax = new Vector2(0.5f, 1f);
                textRect.pivot = new Vector2(0.5f, 1f);
                textRect.anchoredPosition = new Vector2(0f, -14f);
                textRect.sizeDelta = new Vector2(220f, 54f);
            }

            PlaceRingButton(btnTowerUpgrade, new Vector2(0.5f, 0f), new Vector2(-84f, 12f), new Vector2(80f, 44f), new Color(0.23f, 0.52f, 0.33f, 0.95f));
            PlaceRingButton(btnTowerRally, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(80f, 44f), new Color(0.24f, 0.45f, 0.72f, 0.95f));
            PlaceRingButton(btnTowerSell, new Vector2(0.5f, 0f), new Vector2(84f, 12f), new Vector2(80f, 44f), new Color(0.62f, 0.34f, 0.21f, 0.95f));
            PlaceRingButton(btnTowerActionClose, new Vector2(1f, 1f), new Vector2(-18f, -16f), new Vector2(34f, 34f), new Color(0.2f, 0.2f, 0.2f, 0.95f));

            HideTowerActionMenu();
        }

        public void OpenTowerRingMenuAtScreenPosition(Vector2 screenPosition)
        {
            if (towerRingRoot == null || _hudRoot == null)
            {
                return;
            }

            Camera camera = null;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = canvas.worldCamera;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_hudRoot, screenPosition, camera, out Vector2 localPoint))
            {
                return;
            }

            OpenTowerRingMenuAtLocalPosition(localPoint);
        }

        public void OpenTowerRingMenuAtWorldPosition(Vector3 worldPosition)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            OpenTowerRingMenuAtScreenPosition(camera.WorldToScreenPoint(worldPosition));
        }

        public void OpenTowerActionMenuAtWorldPosition(Vector3 worldPosition, string infoText, bool canUpgrade, int upgradeCost, int sellRefund, bool canRally)
        {
            if (towerActionRoot == null || _hudRoot == null)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector2 screen = camera.WorldToScreenPoint(worldPosition);
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_hudRoot, screen, uiCamera, out Vector2 localPoint))
            {
                return;
            }

            var rootRect = towerActionRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = localPoint + new Vector2(0f, 120f);
            towerActionRoot.SetActive(true);

            if (txtTowerActionInfo != null)
            {
                txtTowerActionInfo.text = $"{infoText}\nUP {upgradeCost}G / SELL {sellRefund}G";
            }

            if (btnTowerUpgrade != null)
            {
                btnTowerUpgrade.interactable = canUpgrade;
            }

            if (btnTowerRally != null)
            {
                btnTowerRally.gameObject.SetActive(canRally);
                btnTowerRally.interactable = canRally;
            }
        }

        public void HideTowerRingMenuPublic()
        {
            HideTowerRingMenu();
        }

        public void HideTowerActionMenuPublic()
        {
            HideTowerActionMenu();
        }

        private void OpenTowerRingMenuAtLocalPosition(Vector2 localPosition)
        {
            if (towerRingRoot == null || _hudRoot == null)
            {
                return;
            }

            var rootRect = towerRingRoot.GetComponent<RectTransform>();
            Rect hudRect = _hudRoot.rect;
            Vector2 half = rootRect.sizeDelta * 0.5f;
            float clampedX = Mathf.Clamp(localPosition.x, -hudRect.width * 0.5f + half.x, hudRect.width * 0.5f - half.x);
            float clampedY = Mathf.Clamp(localPosition.y, -hudRect.height * 0.5f + half.y, hudRect.height * 0.5f - half.y);
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(clampedX, clampedY);
            towerRingRoot.SetActive(true);
        }

        private void HideTowerRingMenu()
        {
            if (towerRingRoot != null)
            {
                towerRingRoot.SetActive(false);
            }
        }

        private void HideTowerActionMenu()
        {
            if (towerActionRoot != null)
            {
                towerActionRoot.SetActive(false);
            }
        }

        private static void SetupResultRootRect(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(760f, 360f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void EnsureHudBackdrops()
        {
            EnsureBackdrop(ref _topLeftBackdrop, "HUDBackdropTopLeft", TopLeftBackdropColor);
            EnsureBackdrop(ref _bottomLeftBackdrop, "HUDBackdropBottomLeft", BottomLeftBackdropColor);
            EnsureBackdrop(ref _rightBackdrop, "HUDBackdropRight", RightBackdropColor);
        }

        private void EnsureBackdrop(ref Image image, string name, Color color)
        {
            if (_hudRoot == null)
            {
                return;
            }

            if (image == null)
            {
                var found = _hudRoot.Find(name);
                if (found != null)
                {
                    image = found.GetComponent<Image>();
                }
            }

            if (image == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_hudRoot, false);
                image = go.GetComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = false;
            image.transform.SetAsFirstSibling();
        }

        private void LayoutHudBackdrops()
        {
            LayoutBackdrop(_topLeftBackdrop, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(610f, 232f));
            LayoutBackdrop(_bottomLeftBackdrop, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(530f, 196f));
            LayoutBackdrop(_rightBackdrop, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(230f, 370f));
        }

        private static void LayoutBackdrop(
            Image image,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            if (image == null)
            {
                return;
            }

            var rect = image.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void PlaceRingButton(Button button, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void UpdateTowerRingButton(Button button, string name, int cost, bool hasSlot, bool enoughGold, Color activeColor)
        {
            if (button == null)
            {
                return;
            }

            bool interactable = hasSlot && enoughGold;
            button.interactable = interactable;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = interactable
                    ? activeColor
                    : new Color(0.25f, 0.25f, 0.25f, 0.9f);
            }

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = hasSlot ? $"{name}\n{cost}G" : $"{name}\nNO SLOT";
                text.fontSize = 18f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = interactable
                    ? Color.white
                    : (hasSlot ? new Color(1f, 0.45f, 0.45f, 1f) : new Color(0.75f, 0.75f, 0.75f, 1f));
            }
        }

        private Button FindButton(string path)
        {
            var tr = transform.Find(path);
            return tr != null ? tr.GetComponent<Button>() : null;
        }

        private TextMeshProUGUI FindText(string path)
        {
            var tr = transform.Find(path);
            return tr != null ? tr.GetComponent<TextMeshProUGUI>() : null;
        }

        private Image FindImage(string path)
        {
            var tr = transform.Find(path);
            return tr != null ? tr.GetComponent<Image>() : null;
        }

        private void CleanupLegacyRootChildren()
        {
            string[] legacyNames =
            {
                "btnVictory",
                "btnDefeat",
                "btnPause",
                "btnAudioOptions",
                "btnSpeed",
                "btnNextWave",
                "btnBuildTower",
                "txtWaveInfo",
                "txtStateInfo",
                "txtWaveStartBanner",
                "txtLives",
                "txtGold",
                "txtTowerInfo",
                "imgHeroPortrait",
                "HeroPortraitWidget",
                "btnSpellReinforce",
                "btnSpellRain"
            };

            for (int i = 0; i < legacyNames.Length; i++)
            {
                var tr = transform.Find(legacyNames[i]);
                if (tr == null)
                {
                    continue;
                }

                if (IsBoundTransform(tr))
                {
                    // 현재 바인딩된 오브젝트면 유지
                    continue;
                }

                Destroy(tr.gameObject);
            }
        }

        private bool IsBoundTransform(Transform tr)
        {
            if (tr == null)
            {
                return false;
            }

            return tr == GetTransform(btnVictory)
                || tr == GetTransform(btnDefeat)
                || tr == GetTransform(btnPause)
                || tr == GetTransform(btnAudioOptions)
                || tr == GetTransform(btnSpeed)
                || tr == GetTransform(btnNextWave)
                || tr == GetTransform(btnBuildTower)
                || tr == GetTransform(txtWaveInfo)
                || tr == GetTransform(txtStateInfo)
                || tr == GetTransform(txtWaveStartBanner)
                || tr == GetTransform(txtLives)
                || tr == GetTransform(txtGold)
                || tr == GetTransform(txtTowerInfo)
                || tr == GetTransform(imgHeroPortrait)
                || tr == GetTransform(heroPortraitWidget)
                || tr == GetTransform(btnSpellReinforce)
                || tr == GetTransform(btnSpellRain);
        }

        private void OpenAudioOptionsPopup()
        {
            EnsureAudioOptionsPopupRoot();
            if (_audioOptionsPopupRoot == null)
            {
                Debug.LogWarning("[GameView] AudioOptionsPopup prefab is missing.");
                return;
            }

            _audioOptionsPopupRoot.SetActive(true);
            _audioOptionsPopupRoot.transform.SetAsLastSibling();
        }

        private void EnsureAudioOptionsPopupRoot()
        {
            if (_audioOptionsPopupRoot != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(AudioOptionsPopupResourcePath);
            if (prefab == null)
            {
                return;
            }

            Transform parent = ResolveAudioOptionsPopupParent();
            _audioOptionsPopupRoot = Instantiate(prefab, parent, false);
            _audioOptionsPopupRoot.name = "AudioOptionsPopup_Game";
            _audioOptionsPopupRoot.SetActive(false);
        }

        private Transform ResolveAudioOptionsPopupParent()
        {
            if (UIManager.Instance != null)
            {
                Canvas popupCanvas = UIManager.Instance.GetLayerCanvas(UILayer.Popup);
                if (popupCanvas != null)
                {
                    return popupCanvas.transform;
                }
            }

            return _hudRoot != null ? _hudRoot : transform;
        }

        private static Transform GetTransform(Component c)
        {
            return c != null ? c.transform : null;
        }
    }
}
