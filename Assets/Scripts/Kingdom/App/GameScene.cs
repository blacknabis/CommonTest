using Common.UI;
using Common.App;
using System.Collections.Generic;
using UnityEngine;
using Kingdom.Game;
using Kingdom.WorldMap;
using Kingdom.Save;
using UnityEngine.EventSystems;

namespace Kingdom.App
{
    /// <summary>
    /// 메인 게임 씬 컨트롤러.
    /// 웨이브 진행, 타워 건설 등을 총괄합니다.
    /// </summary>
    public class GameScene : SceneBase<SCENES>
    {
        private const string BattlefieldPrefabResourcePath = "Prefabs/Game/GameBattlefield";
        private const string TowerConfigResourcePath = "Data/TowerConfigs/BasicTower";
        private const string HeroConfigResourcePathPrefix = "Data/HeroConfigs/";
        private const string DefaultHeroId = "DefaultHero";
        private const string SelectedHeroIdPlayerPrefsKey = "Kingdom.Hero.SelectedHeroId";
        private const string HeroPortraitSpriteResourcePathPrefix = "UI/Sprites/Heroes/Portraits/";
        private const string SpellReinforceConfigResourcePath = "Data/SpellConfigs/ReinforceSpell";
        private const string SpellRainConfigResourcePath = "Data/SpellConfigs/RainSpell";
        private const float EarlyCallGoldPerSecond = 2.0f;
        private const float EarlyCallCooldownMinRatio = 0.35f;
        private const float FastForwardTimeScale = 2f;
        private const int DebugGrantGoldAmount = 500;

        private GameStateController _stateController;
        private PathManager _pathManager;
        private SpawnManager _spawnManager;
        private WaveManager _waveManager;
        private InGameEconomyManager _economyManager;
        private TowerManager _towerManager;
        private ProjectileManager _projectileManager;
        private HeroController _heroController;
        private GameBattlefield _battlefield;
        private WaveConfig _activeWaveConfig;
        private HeroConfig _heroConfig;
        private SpellConfig _reinforceSpellConfig;
        private SpellConfig _rainSpellConfig;
        private int _pendingBuildSlotIndex = -1;
        private int _selectedTowerId = -1;
        private bool _isRallyPlacementMode;
        private float _reinforceCooldownLeft;
        private float _rainCooldownLeft;
        private bool _resultPresented;
        private bool _resultFinalized;
        private float _battleStartedAtUnscaled;
        private bool _isFastForward;
        private float _activeTimeScale = 1f;
        private bool _waveKpiActive;
        private int _waveKpiNumber;
        private float _waveKpiStartedAtUnscaled;
        private int _waveKpiStartLives;
        private int _waveKpiStartGold;
        private int _waveEarlyCallAttempts;
        private int _waveEarlyCallSuccess;
        private readonly Dictionary<TowerType, int> _waveTowerBuildCount = new();
        private readonly Dictionary<string, int> _waveLeakByEnemyType = new();

        public override bool OnInit()
        {
            Debug.Log("[GameScene] Initialized.");
            MainUI = UIHelper.GetOrCreate<GameView>();
            return true;
        }

        public override bool OnStartScene()
        {
            Debug.Log("[GameScene] Started.");
            EnsureMainCameraForBattle2D();

            _stateController = FindFirstObjectByType<GameStateController>();
            if (_stateController == null)
            {
                GameObject flowGo = new GameObject("GameStateController");
                _stateController = flowGo.AddComponent<GameStateController>();
            }

            if (MainUI is GameView gameView)
            {
                gameView.Bind(_stateController);
            }

            _pathManager = FindFirstObjectByType<PathManager>();
            if (_pathManager == null)
            {
                _pathManager = new GameObject("PathManager").AddComponent<PathManager>();
            }

            _spawnManager = FindFirstObjectByType<SpawnManager>();
            if (_spawnManager == null)
            {
                _spawnManager = new GameObject("SpawnManager").AddComponent<SpawnManager>();
            }

            _battlefield = EnsureBattlefield();
            if (_battlefield != null)
            {
                _pathManager.SetDefaultPathPoints(_battlefield.GetPathPoints());
                _spawnManager.SetEnemyRoot(_battlefield.EnemyRoot);
            }

            _waveManager = FindFirstObjectByType<WaveManager>();
            if (_waveManager == null)
            {
                _waveManager = new GameObject("WaveManager").AddComponent<WaveManager>();
            }

            _economyManager = FindFirstObjectByType<InGameEconomyManager>();
            if (_economyManager == null)
            {
                _economyManager = new GameObject("InGameEconomyManager").AddComponent<InGameEconomyManager>();
            }

            _towerManager = FindFirstObjectByType<TowerManager>();
            if (_towerManager == null)
            {
                _towerManager = new GameObject("TowerManager").AddComponent<TowerManager>();
            }

            _projectileManager = FindFirstObjectByType<ProjectileManager>();
            if (_projectileManager == null)
            {
                _projectileManager = new GameObject("ProjectileManager").AddComponent<ProjectileManager>();
            }

            _heroController = FindFirstObjectByType<HeroController>();
            if (_heroController == null)
            {
                _heroController = new GameObject("HeroController").AddComponent<HeroController>();
            }

            _activeWaveConfig = ResolveStageWaveConfig();
            _resultPresented = false;
            _resultFinalized = false;
            _battleStartedAtUnscaled = Time.unscaledTime;
            _isFastForward = false;
            _activeTimeScale = 1f;
            _waveKpiActive = false;
            if (_activeWaveConfig != null && _activeWaveConfig.Waves != null && _activeWaveConfig.Waves.Count > 0)
            {
                _stateController.SetTotalWaves(_activeWaveConfig.Waves.Count);
            }

            _waveManager.Configure(_activeWaveConfig, _stateController, _spawnManager, _pathManager);
            ConfigureEconomyAndTower();
            ApplyEffectiveTimeScale();

            return true;
        }

        public override void OnEndScene()
        {
            Time.timeScale = 1f;
            _pendingBuildSlotIndex = -1;
            _isRallyPlacementMode = false;
            _resultPresented = false;
            _resultFinalized = false;

            if (MainUI is GameView gameView)
            {
                gameView.BuildTowerRequested -= OnBuildTowerRequested;
                gameView.TowerBuildTypeRequested -= OnTowerBuildTypeRequested;
                gameView.TowerUpgradeRequested -= OnTowerUpgradeRequested;
                gameView.TowerSellRequested -= OnTowerSellRequested;
                gameView.TowerRallyRequested -= OnTowerRallyRequested;
                gameView.NextWaveRequested -= OnNextWaveRequested;
                gameView.SpeedToggleRequested -= OnSpeedToggleRequested;
                gameView.SpellReinforceRequested -= OnSpellReinforceRequested;
                gameView.SpellRainRequested -= OnSpellRainRequested;
            }

            if (_economyManager != null)
            {
                _economyManager.ResourceChanged -= OnResourceChanged;
            }

            if (_stateController != null)
            {
                _stateController.StateChanged -= OnStateChangedForHud;
                _stateController.WaveChanged -= OnWaveChangedForKpi;
                _stateController.WaveReadyTimeChanged -= OnWaveReadyTimeChanged;
            }

            if (_spawnManager != null)
            {
                _spawnManager.EnemyReachedGoal -= OnEnemyReachedGoalForKpi;
            }

            Debug.Log("[GameScene] Ended.");
        }

        private void Update()
        {
            HandleDebugGrantGoldInput();

            if (_towerManager == null || MainUI is not GameView gameView)
            {
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                TickSpellCooldowns();
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                TickSpellCooldowns();
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                TickSpellCooldowns();
                return;
            }

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            if (_isRallyPlacementMode && _selectedTowerId >= 0)
            {
                bool rallySet = _towerManager.TrySetRallyPoint(_selectedTowerId, world);
                _isRallyPlacementMode = false;
                if (!rallySet)
                {
                    Debug.Log($"[GameScene] Rally set ignored. towerId={_selectedTowerId}");
                }

                RefreshAfterTowerAction();
                TickSpellCooldowns();
                return;
            }

            if (_towerManager.TryFindTowerAtWorldPosition(world, 0.7f, out int towerId, out Vector3 towerWorld))
            {
                if (_towerManager.TryGetTowerActionInfo(towerId, out TowerManager.TowerActionInfo towerInfo))
                {
                    _selectedTowerId = towerId;
                    _isRallyPlacementMode = false;
                    _pendingBuildSlotIndex = -1;
                    gameView.HideTowerRingMenuPublic();
                    string infoText = $"{towerInfo.TowerType} Lv.{towerInfo.Level}";
                    bool canUpgrade = towerInfo.Level < towerInfo.MaxLevel && _economyManager != null && _economyManager.Gold >= towerInfo.UpgradeCost;
                    gameView.OpenTowerActionMenuAtWorldPosition(
                        towerWorld,
                        infoText,
                        canUpgrade,
                        towerInfo.UpgradeCost,
                        towerInfo.SellRefund,
                        towerInfo.SupportsRally);
                }

                return;
            }

            if (_towerManager.TryFindBuildableSlotAtWorldPosition(world, 0.65f, out int slotIndex, out Vector3 slotWorld))
            {
                _pendingBuildSlotIndex = slotIndex;
                _selectedTowerId = -1;
                _isRallyPlacementMode = false;
                gameView.HideTowerActionMenuPublic();
                gameView.OpenTowerRingMenuAtWorldPosition(slotWorld);
                return;
            }

            _pendingBuildSlotIndex = -1;
            _selectedTowerId = -1;
            _isRallyPlacementMode = false;
            if (gameView.IsTowerRingMenuOpen)
            {
                gameView.HideTowerRingMenuPublic();
            }
            if (gameView.IsTowerActionMenuOpen)
            {
                gameView.HideTowerActionMenuPublic();
            }

            TickSpellCooldowns();
        }

        private void HandleDebugGrantGoldInput()
        {
#if UNITY_EDITOR
            if (_economyManager == null)
            {
                return;
            }

            if (!Input.GetKeyDown(KeyCode.F6))
            {
                return;
            }

            _economyManager.AddGold(DebugGrantGoldAmount);
            Debug.Log($"[GameScene] Debug grant gold +{DebugGrantGoldAmount}");
#endif
        }

        private static WaveConfig ResolveStageWaveConfig()
        {
            WorldMapManager worldMapManager = WorldMapManager.Instance;
            StageConfig stageConfig = worldMapManager != null ? worldMapManager.CurrentStageConfig : null;
            if (stageConfig == null || stageConfig.Stages == null || stageConfig.Stages.Count == 0)
            {
                return null;
            }

            int selectedStageId = WorldMapScene.SelectedStageId;
            for (int i = 0; i < stageConfig.Stages.Count; i++)
            {
                StageData stageData = stageConfig.Stages[i];
                if (selectedStageId > 0 && stageData.StageId != selectedStageId)
                {
                    continue;
                }

                WaveConfig resolved = TryResolveWaveConfig(stageData);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            for (int i = 0; i < stageConfig.Stages.Count; i++)
            {
                WaveConfig resolved = TryResolveWaveConfig(stageConfig.Stages[i]);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }

        private static WaveConfig TryResolveWaveConfig(StageData stageData)
        {
            if (stageData.WaveConfig != null)
            {
                return stageData.WaveConfig;
            }

            // StageConfig 직렬화 누락 시 Resources 경로에서 보완 로드.
            if (stageData.StageId > 0)
            {
                return Resources.Load<WaveConfig>($"Data/WaveConfigs/Stage_{stageData.StageId}_WaveConfig");
            }

            return null;
        }

        private static void EnsureMainCameraForBattle2D()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        }

        private static GameBattlefield EnsureBattlefield()
        {
            GameBattlefield existing = FindFirstObjectByType<GameBattlefield>();
            if (existing != null)
            {
                existing.EnsureRuntimeDefaults();
                return existing;
            }

            GameObject prefab = Resources.Load<GameObject>(BattlefieldPrefabResourcePath);
            if (prefab != null)
            {
                bool hasMissingScript = false;
                var components = prefab.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        hasMissingScript = true;
                        break;
                    }
                }

                GameBattlefield prefabBattlefield = prefab.GetComponent<GameBattlefield>();
                if (hasMissingScript || prefabBattlefield == null)
                {
                    Debug.LogWarning("[GameScene] GameBattlefield prefab has missing/invalid script reference. Using runtime fallback battlefield.");
                    return GameBattlefield.CreateFallbackRuntime();
                }

                GameObject instance = Object.Instantiate(prefab);
                GameBattlefield fromPrefab = instance.GetComponent<GameBattlefield>();
                if (fromPrefab == null)
                {
                    fromPrefab = instance.AddComponent<GameBattlefield>();
                }

                fromPrefab.EnsureRuntimeDefaults();
                return fromPrefab;
            }

            GameBattlefield fallback = GameBattlefield.CreateFallbackRuntime();
            Debug.LogWarning("[GameScene] GameBattlefield prefab missing. Runtime fallback battlefield created.");
            return fallback;
        }

        private void ConfigureEconomyAndTower()
        {
            int initialGold = _activeWaveConfig != null ? Mathf.Max(0, _activeWaveConfig.InitialGold) : 100;
            int initialLives = _activeWaveConfig != null ? Mathf.Max(1, _activeWaveConfig.InitialLives) : 20;
            _economyManager.Configure(initialGold, initialLives, _spawnManager, _stateController);

            var towerConfig = Resources.Load<TowerConfig>(TowerConfigResourcePath);
            _heroConfig = ResolveSelectedHeroConfig();
            _reinforceSpellConfig = ResolveSpellConfig(SpellReinforceConfigResourcePath, "reinforce", "Reinforce", 20f, 4f);
            _rainSpellConfig = ResolveSpellConfig(SpellRainConfigResourcePath, "rain", "Rain", 30f, 4f);
            var towerSlots = _battlefield != null ? _battlefield.GetTowerSlotPositions() : null;
            Transform towerRoot = _battlefield != null ? _battlefield.TowerRoot : null;
            _projectileManager.Configure(_spawnManager, towerRoot);
            _towerManager.Configure(_spawnManager, _economyManager, _projectileManager, towerRoot, towerSlots, towerConfig);
            ConfigureHeroController(towerSlots);

            if (MainUI is not GameView gameView)
            {
                return;
            }

            gameView.BuildTowerRequested -= OnBuildTowerRequested;
            gameView.BuildTowerRequested += OnBuildTowerRequested;
            gameView.TowerBuildTypeRequested -= OnTowerBuildTypeRequested;
            gameView.TowerBuildTypeRequested += OnTowerBuildTypeRequested;
            gameView.TowerUpgradeRequested -= OnTowerUpgradeRequested;
            gameView.TowerUpgradeRequested += OnTowerUpgradeRequested;
            gameView.TowerSellRequested -= OnTowerSellRequested;
            gameView.TowerSellRequested += OnTowerSellRequested;
            gameView.TowerRallyRequested -= OnTowerRallyRequested;
            gameView.TowerRallyRequested += OnTowerRallyRequested;
            gameView.NextWaveRequested -= OnNextWaveRequested;
            gameView.NextWaveRequested += OnNextWaveRequested;
            gameView.SpeedToggleRequested -= OnSpeedToggleRequested;
            gameView.SpeedToggleRequested += OnSpeedToggleRequested;
            gameView.SpellReinforceRequested -= OnSpellReinforceRequested;
            gameView.SpellReinforceRequested += OnSpellReinforceRequested;
            gameView.SpellRainRequested -= OnSpellRainRequested;
            gameView.SpellRainRequested += OnSpellRainRequested;

            _economyManager.ResourceChanged -= OnResourceChanged;
            _economyManager.ResourceChanged += OnResourceChanged;
            _stateController.StateChanged -= OnStateChangedForHud;
            _stateController.StateChanged += OnStateChangedForHud;
            _stateController.WaveChanged -= OnWaveChangedForKpi;
            _stateController.WaveChanged += OnWaveChangedForKpi;
            _stateController.WaveReadyTimeChanged -= OnWaveReadyTimeChanged;
            _stateController.WaveReadyTimeChanged += OnWaveReadyTimeChanged;
            _spawnManager.EnemyReachedGoal -= OnEnemyReachedGoalForKpi;
            _spawnManager.EnemyReachedGoal += OnEnemyReachedGoalForKpi;

            gameView.UpdateResourceInfo(_economyManager.Lives, _economyManager.Gold);
            gameView.SetTowerInfo(_towerManager.TowerCount, _towerManager.RemainingSlots);
            gameView.SetTowerRingMenuAvailability(
                _economyManager.Gold,
                _towerManager.RemainingSlots,
                _towerManager.GetBuildCost(TowerType.Archer),
                _towerManager.GetBuildCost(TowerType.Barracks),
                _towerManager.GetBuildCost(TowerType.Mage),
                _towerManager.GetBuildCost(TowerType.Artillery));
            gameView.SetNextWaveInteractable(_stateController.CurrentState == GameFlowState.WaveReady);
            gameView.SetSpeedVisual(_isFastForward);
            gameView.SetSpellCooldown("reinforce", 0f);
            gameView.SetSpellCooldown("rain", 0f);
            gameView.SetHeroPortrait(ResolveHeroPortraitSprite(_heroConfig));
        }

        private void ConfigureHeroController(System.Collections.Generic.List<Vector3> towerSlots)
        {
            if (_heroController == null || _spawnManager == null)
            {
                return;
            }

            // Start at safe position (bottom-left) instead of near first tower slot
            Vector3 heroSpawn = new Vector3(-6.5f, -3.5f, 0f);

            _heroController.Configure(_spawnManager, _heroConfig, heroSpawn);
        }

        private void OnBuildTowerRequested()
        {
            if (_towerManager == null)
            {
                return;
            }

            bool built;
            if (_pendingBuildSlotIndex >= 0)
            {
                built = _towerManager.TryBuildTowerAtSlot(TowerType.Archer, _pendingBuildSlotIndex);
            }
            else
            {
                built = _towerManager.TryBuildNextTower();
            }

            if (!built)
            {
                Debug.Log("[GameScene] Build tower request ignored. (No slot or not enough gold)");
            }
            else
            {
                _pendingBuildSlotIndex = -1;
                _selectedTowerId = -1;
                _isRallyPlacementMode = false;
                AccumulateTowerBuildKpi(TowerType.Archer);
            }

            if (MainUI is GameView gameView)
            {
                gameView.SetTowerInfo(_towerManager.TowerCount, _towerManager.RemainingSlots);
                if (_economyManager != null)
                {
                    gameView.UpdateResourceInfo(_economyManager.Lives, _economyManager.Gold);
                    gameView.SetTowerRingMenuAvailability(
                        _economyManager.Gold,
                        _towerManager.RemainingSlots,
                        _towerManager.GetBuildCost(TowerType.Archer),
                        _towerManager.GetBuildCost(TowerType.Barracks),
                        _towerManager.GetBuildCost(TowerType.Mage),
                        _towerManager.GetBuildCost(TowerType.Artillery));
                }
            }
        }

        private void OnTowerBuildTypeRequested(TowerType towerType)
        {
            if (_towerManager == null)
            {
                return;
            }

            bool built;
            if (_pendingBuildSlotIndex >= 0)
            {
                built = _towerManager.TryBuildTowerAtSlot(towerType, _pendingBuildSlotIndex);
            }
            else
            {
                built = _towerManager.TryBuildNextTower(towerType);
            }

            if (!built)
            {
                Debug.Log($"[GameScene] Build tower request ignored. type={towerType} (No slot or not enough gold)");
            }
            else
            {
                _pendingBuildSlotIndex = -1;
                _selectedTowerId = -1;
                _isRallyPlacementMode = false;
                AccumulateTowerBuildKpi(towerType);
            }

            if (MainUI is GameView gameView)
            {
                gameView.SetTowerInfo(_towerManager.TowerCount, _towerManager.RemainingSlots);
                if (_economyManager != null)
                {
                    gameView.UpdateResourceInfo(_economyManager.Lives, _economyManager.Gold);
                    gameView.SetTowerRingMenuAvailability(
                        _economyManager.Gold,
                        _towerManager.RemainingSlots,
                        _towerManager.GetBuildCost(TowerType.Archer),
                        _towerManager.GetBuildCost(TowerType.Barracks),
                        _towerManager.GetBuildCost(TowerType.Mage),
                        _towerManager.GetBuildCost(TowerType.Artillery));
                }
            }
        }

        private void OnResourceChanged(int lives, int gold)
        {
            if (MainUI is GameView gameView)
            {
                gameView.UpdateResourceInfo(lives, gold);
                if (_towerManager != null)
                {
                    gameView.SetTowerRingMenuAvailability(
                        gold,
                        _towerManager.RemainingSlots,
                        _towerManager.GetBuildCost(TowerType.Archer),
                        _towerManager.GetBuildCost(TowerType.Barracks),
                        _towerManager.GetBuildCost(TowerType.Mage),
                        _towerManager.GetBuildCost(TowerType.Artillery));
                }
            }
        }

        private void OnNextWaveRequested()
        {
            if (_stateController == null)
            {
                return;
            }

            float remainingReadySeconds = _stateController.WaveReadyRemaining;
            _waveEarlyCallAttempts++;
            // NOTE:
            // TryEarlyCallNextWave() 내부에서 상태 전이가 즉시 발생하고,
            // 그 콜백에서 KPI Finalize가 먼저 호출될 수 있다.
            // 따라서 성공 카운트를 먼저 올려두고, 실패 시 롤백한다.
            _waveEarlyCallSuccess++;
            if (!_stateController.TryEarlyCallNextWave())
            {
                _waveEarlyCallSuccess = Mathf.Max(0, _waveEarlyCallSuccess - 1);
                return;
            }

            ApplyEarlyCallReward(remainingReadySeconds);
            if (MainUI is GameView gameView)
            {
                gameView.SetNextWaveInteractable(false);
            }
        }

        private void OnSpeedToggleRequested()
        {
            _isFastForward = !_isFastForward;
            _activeTimeScale = _isFastForward ? FastForwardTimeScale : 1f;
            ApplyEffectiveTimeScale();

            if (MainUI is GameView gameView)
            {
                gameView.SetSpeedVisual(_isFastForward);
            }

            Debug.Log($"[KPI] SpeedToggle speed={_activeTimeScale:0.0}x paused={(_stateController != null && _stateController.IsPaused)}");
        }

        private void ApplyEarlyCallReward(float remainingReadySeconds)
        {
            if (_economyManager == null || _stateController == null)
            {
                return;
            }

            int waveIndex = Mathf.Max(1, _stateController.CurrentWave) - 1;
            int baseBonus = 0;
            if (_activeWaveConfig != null && _activeWaveConfig.Waves != null && waveIndex >= 0 && waveIndex < _activeWaveConfig.Waves.Count)
            {
                baseBonus = Mathf.Max(0, _activeWaveConfig.Waves[waveIndex].BonusGoldOnEarlyCall);
            }

            float remain = Mathf.Max(0f, remainingReadySeconds);
            int timeBonus = Mathf.RoundToInt(remain * EarlyCallGoldPerSecond);
            int totalBonus = Mathf.Max(0, baseBonus + timeBonus);
            if (totalBonus > 0)
            {
                _economyManager.AddGold(totalBonus);
            }

            float readyDuration = _stateController.WaveReadyDuration;
            float reinforceReduction = GetScaledEarlyCallCooldownReduction(_reinforceSpellConfig, remainingReadySeconds, readyDuration);
            float rainReduction = GetScaledEarlyCallCooldownReduction(_rainSpellConfig, remainingReadySeconds, readyDuration);
            _reinforceCooldownLeft = Mathf.Max(0f, _reinforceCooldownLeft - reinforceReduction);
            _rainCooldownLeft = Mathf.Max(0f, _rainCooldownLeft - rainReduction);
            TickSpellCooldowns();
            Debug.Log($"[GameScene] EarlyCall reward applied. base={baseBonus} time={timeBonus} total={totalBonus} remain={remainingReadySeconds:0.00}s cdReduce(reinforce={reinforceReduction:0.00}, rain={rainReduction:0.00})");
        }

        private void OnStateChangedForHud(GameFlowState state)
        {
            ApplyEffectiveTimeScale();

            if (state == GameFlowState.WaveBreak || state == GameFlowState.Result)
            {
                FinalizeWaveKpi(state.ToString());
            }

            if (MainUI is GameView gameView)
            {
                bool canEarlyCall = state == GameFlowState.WaveReady;
                gameView.SetNextWaveInteractable(canEarlyCall);
                if (state != GameFlowState.WaveReady)
                {
                    gameView.HideWaveReadyCountdown();
                }

                if (state == GameFlowState.Result)
                {
                    PresentBattleResult(gameView);
                }
                else
                {
                    gameView.HideResult();
                }
            }
        }

        private void OnWaveReadyTimeChanged(float remainingSeconds)
        {
            if (MainUI is not GameView gameView)
            {
                return;
            }

            if (_stateController == null || _stateController.CurrentState != GameFlowState.WaveReady)
            {
                gameView.HideWaveReadyCountdown();
                return;
            }

            gameView.SetWaveReadyCountdown(remainingSeconds);
        }

        private void PresentBattleResult(GameView gameView)
        {
            if (_resultPresented)
            {
                return;
            }

            bool isVictory = EvaluateVictory();
            int stars = CalculateStars(isVictory);
            FinalizeBattleResult(isVictory, stars);
            string message = BuildResultMessage(isVictory, stars);

            gameView.ShowResult(isVictory, message);
            _resultPresented = true;
            Debug.Log($"[GameScene] Result presented. victory={isVictory} stars={stars}");
        }

        private void FinalizeBattleResult(bool isVictory, int stars)
        {
            if (_resultFinalized)
            {
                return;
            }

            int stageId = WorldMapScene.SelectedStageId;
            if (stageId <= 0)
            {
                _resultFinalized = true;
                return;
            }

            StageDifficulty difficulty = WorldMapScene.SelectedDifficulty;
            float clearTime = Mathf.Max(0f, Time.unscaledTime - _battleStartedAtUnscaled);
            if (isVictory)
            {
                SaveManager.Instance.SaveData.SetStageCleared(stageId, stars, clearTime, difficulty);
                NotifyBossStageCleared(stageId);
            }

            WorldMapReturnAnimator.SetPendingReturnData(stageId, isVictory, clearTime, difficulty);
            _resultFinalized = true;
            Debug.Log($"[GameScene] Result finalized. stage={stageId} victory={isVictory} stars={stars} clearTime={clearTime:0.00}s");
        }

        private string BuildResultMessage(bool isVictory, int stars)
        {
            int lives = Mathf.Max(0, _economyManager != null ? _economyManager.Lives : 0);
            int stageId = WorldMapScene.SelectedStageId;
            if (!isVictory || stageId <= 0)
            {
                return "방어선 붕괴\n다시 시도해 주세요.";
            }

            UserSaveData.StageProgressData progress = SaveManager.Instance.SaveData.GetStageProgress(stageId);
            int bestStars = progress != null ? Mathf.Clamp(progress.BestStars, 0, 3) : stars;
            float bestTime = progress != null ? Mathf.Max(0f, progress.BestClearTimeSeconds) : 0f;

            string bestTimeText = bestTime > 0f ? $"{bestTime:0.0}s" : "-";
            return $"별 {stars}/3 획득\n남은 생명: {lives}\nBEST 별: {bestStars}/3 / BEST 시간: {bestTimeText}";
        }

        private static void NotifyBossStageCleared(int stageId)
        {
            if (stageId <= 0)
            {
                return;
            }

            BossEventSystem bossEventSystem = FindFirstObjectByType<BossEventSystem>();
            if (bossEventSystem == null)
            {
                return;
            }

            bossEventSystem.NotifyBossStageCleared(stageId);
        }

        private bool EvaluateVictory()
        {
            int lives = _economyManager != null ? _economyManager.Lives : 0;
            if (lives <= 0)
            {
                return false;
            }

            if (_stateController == null)
            {
                return false;
            }

            return _stateController.CurrentWave >= _stateController.TotalWaves;
        }

        private int CalculateStars(bool isVictory)
        {
            if (!isVictory)
            {
                return 0;
            }

            int lives = Mathf.Max(0, _economyManager != null ? _economyManager.Lives : 0);
            int threeStar = 20;
            int twoStar = 15;
            if (_activeWaveConfig != null && _activeWaveConfig.StarThresholds != null)
            {
                if (_activeWaveConfig.StarThresholds.Length > 0)
                {
                    threeStar = Mathf.Max(0, _activeWaveConfig.StarThresholds[0]);
                }

                if (_activeWaveConfig.StarThresholds.Length > 1)
                {
                    twoStar = Mathf.Max(0, _activeWaveConfig.StarThresholds[1]);
                }
            }

            if (lives >= threeStar)
            {
                return 3;
            }

            if (lives >= twoStar)
            {
                return 2;
            }

            return 1;
        }

        private void OnSpellReinforceRequested()
        {
            if (_reinforceCooldownLeft > 0f)
            {
                return;
            }

            _reinforceCooldownLeft = GetSpellCooldownDuration(_reinforceSpellConfig, 20f);
            TickSpellCooldowns();
        }

        private void OnSpellRainRequested()
        {
            if (_rainCooldownLeft > 0f)
            {
                return;
            }

            _rainCooldownLeft = GetSpellCooldownDuration(_rainSpellConfig, 30f);
            TickSpellCooldowns();
        }

        private void TickSpellCooldowns()
        {
            if (MainUI is not GameView gameView)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            if (dt > 0f)
            {
                _reinforceCooldownLeft = Mathf.Max(0f, _reinforceCooldownLeft - dt);
                _rainCooldownLeft = Mathf.Max(0f, _rainCooldownLeft - dt);
            }

            float reinforceDuration = GetSpellCooldownDuration(_reinforceSpellConfig, 20f);
            float rainDuration = GetSpellCooldownDuration(_rainSpellConfig, 30f);
            float reinforceNorm = reinforceDuration > 0f
                ? _reinforceCooldownLeft / reinforceDuration
                : 0f;
            float rainNorm = rainDuration > 0f
                ? _rainCooldownLeft / rainDuration
                : 0f;

            gameView.SetSpellCooldown("reinforce", reinforceNorm);
            gameView.SetSpellCooldown("rain", rainNorm);
        }

        private static SpellConfig ResolveSpellConfig(string resourcePath, string spellId, string displayName, float cooldownSeconds, float earlyCallReductionSeconds)
        {
            SpellConfig config = Resources.Load<SpellConfig>(resourcePath);
            if (config != null)
            {
                return config;
            }

            SpellConfig fallback = ScriptableObject.CreateInstance<SpellConfig>();
            fallback.hideFlags = HideFlags.DontSave;
            fallback.SpellId = spellId;
            fallback.DisplayName = displayName;
            fallback.CooldownSeconds = cooldownSeconds;
            fallback.EarlyCallCooldownReductionSeconds = earlyCallReductionSeconds;
            return fallback;
        }

        private static HeroConfig ResolveHeroConfig(string resourcePath)
        {
            HeroConfig config = Resources.Load<HeroConfig>(resourcePath);
            if (config != null)
            {
                return config;
            }

            HeroConfig fallback = ScriptableObject.CreateInstance<HeroConfig>();
            fallback.hideFlags = HideFlags.DontSave;
            fallback.HeroId = "DefaultHero";
            fallback.DisplayName = "Hero";
            fallback.MaxHp = 500f;
            fallback.MoveSpeed = 3.2f;
            fallback.AttackDamage = 30f;
            fallback.AttackCooldown = 0.8f;
            fallback.AttackRange = 1.8f;
            return fallback;
        }

        private static HeroConfig ResolveSelectedHeroConfig()
        {
            string selectedHeroId = PlayerPrefs.GetString(SelectedHeroIdPlayerPrefsKey, DefaultHeroId);
            if (string.IsNullOrWhiteSpace(selectedHeroId))
            {
                selectedHeroId = DefaultHeroId;
            }

            string selectedPath = HeroConfigResourcePathPrefix + selectedHeroId;
            HeroConfig selectedConfig = Resources.Load<HeroConfig>(selectedPath);
            if (selectedConfig != null)
            {
                Debug.Log($"[GameScene] Hero config resolved from selected id. heroId={selectedConfig.HeroId}, path={selectedPath}");
                return selectedConfig;
            }

            string defaultPath = HeroConfigResourcePathPrefix + DefaultHeroId;
            if (!selectedHeroId.Equals(DefaultHeroId))
            {
                Debug.Log($"[GameScene] Selected hero config missing: {selectedPath}. Fallback to {defaultPath}.");
            }

            return ResolveHeroConfig(defaultPath);
        }

        private static Sprite ResolveHeroPortraitSprite(HeroConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.HeroId))
            {
                return null;
            }

            return Resources.Load<Sprite>(HeroPortraitSpriteResourcePathPrefix + config.HeroId);
        }

        private static float GetSpellCooldownDuration(SpellConfig config, float fallbackSeconds)
        {
            if (config == null)
            {
                return Mathf.Max(0.1f, fallbackSeconds);
            }

            return Mathf.Max(0.1f, config.CooldownSeconds);
        }

        private static float GetEarlyCallCooldownReduction(SpellConfig config)
        {
            if (config == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, config.EarlyCallCooldownReductionSeconds);
        }

        private static float GetScaledEarlyCallCooldownReduction(
            SpellConfig config,
            float remainingReadySeconds,
            float waveReadyDuration)
        {
            float baseReduction = GetEarlyCallCooldownReduction(config);
            if (baseReduction <= 0f)
            {
                return 0f;
            }

            if (waveReadyDuration <= 0.0001f)
            {
                return baseReduction;
            }

            float t = Mathf.Clamp01(Mathf.Max(0f, remainingReadySeconds) / waveReadyDuration);
            float ratio = Mathf.Lerp(EarlyCallCooldownMinRatio, 1f, t);
            return baseReduction * ratio;
        }

        private void OnTowerUpgradeRequested()
        {
            if (_towerManager == null || _selectedTowerId < 0)
            {
                return;
            }

            bool upgraded = _towerManager.TryUpgradeTower(_selectedTowerId);
            if (!upgraded)
            {
                Debug.Log($"[GameScene] Upgrade tower request ignored. towerId={_selectedTowerId}");
                return;
            }

            RefreshAfterTowerAction();
        }

        private void OnTowerSellRequested()
        {
            if (_towerManager == null || _selectedTowerId < 0)
            {
                return;
            }

            bool sold = _towerManager.TrySellTower(_selectedTowerId);
            if (!sold)
            {
                Debug.Log($"[GameScene] Sell tower request ignored. towerId={_selectedTowerId}");
                return;
            }

            _selectedTowerId = -1;
            _isRallyPlacementMode = false;
            if (MainUI is GameView view)
            {
                view.HideTowerActionMenuPublic();
            }

            RefreshAfterTowerAction();
        }

        private void RefreshAfterTowerAction()
        {
            if (MainUI is not GameView gameView || _economyManager == null || _towerManager == null)
            {
                return;
            }

            gameView.UpdateResourceInfo(_economyManager.Lives, _economyManager.Gold);
            gameView.SetTowerInfo(_towerManager.TowerCount, _towerManager.RemainingSlots);
            gameView.SetTowerRingMenuAvailability(
                _economyManager.Gold,
                _towerManager.RemainingSlots,
                _towerManager.GetBuildCost(TowerType.Archer),
                _towerManager.GetBuildCost(TowerType.Barracks),
                _towerManager.GetBuildCost(TowerType.Mage),
                _towerManager.GetBuildCost(TowerType.Artillery));

            if (_selectedTowerId >= 0 && _towerManager.TryGetTowerActionInfo(_selectedTowerId, out TowerManager.TowerActionInfo info))
            {
                string text = $"{info.TowerType} Lv.{info.Level}";
                bool canUpgrade = info.Level < info.MaxLevel && _economyManager.Gold >= info.UpgradeCost;
                gameView.OpenTowerActionMenuAtWorldPosition(info.WorldPosition, text, canUpgrade, info.UpgradeCost, info.SellRefund, info.SupportsRally);
            }
        }

        private void OnTowerRallyRequested()
        {
            if (_towerManager == null || _selectedTowerId < 0 || MainUI is not GameView gameView)
            {
                return;
            }

            if (!_towerManager.TryGetTowerActionInfo(_selectedTowerId, out TowerManager.TowerActionInfo info) || !info.SupportsRally)
            {
                return;
            }

            _isRallyPlacementMode = true;
            gameView.HideTowerActionMenuPublic();
            Debug.Log($"[GameScene] Rally placement mode started. towerId={_selectedTowerId}");
        }

        private void ApplyEffectiveTimeScale()
        {
            if (_stateController != null && _stateController.IsPaused)
            {
                Time.timeScale = 0f;
                return;
            }

            Time.timeScale = Mathf.Clamp(_activeTimeScale, 0.1f, FastForwardTimeScale);
        }

        private void OnWaveChangedForKpi(int currentWave, int totalWave)
        {
            FinalizeWaveKpi("WaveChanged");
            BeginWaveKpi(currentWave);
        }

        private void BeginWaveKpi(int waveNumber)
        {
            if (_economyManager == null)
            {
                return;
            }

            _waveKpiNumber = Mathf.Max(1, waveNumber);
            _waveKpiStartedAtUnscaled = Time.unscaledTime;
            _waveKpiStartLives = _economyManager.Lives;
            _waveKpiStartGold = _economyManager.Gold;
            _waveEarlyCallAttempts = 0;
            _waveEarlyCallSuccess = 0;
            _waveTowerBuildCount.Clear();
            _waveLeakByEnemyType.Clear();
            _waveKpiActive = true;
        }

        private void FinalizeWaveKpi(string reason)
        {
            if (!_waveKpiActive || _economyManager == null)
            {
                return;
            }

            float clearTime = Mathf.Max(0f, Time.unscaledTime - _waveKpiStartedAtUnscaled);
            int lifeLost = Mathf.Max(0, _waveKpiStartLives - _economyManager.Lives);
            int netGoldDelta = _economyManager.Gold - _waveKpiStartGold;
            float earlyCallUsageRate = _waveEarlyCallAttempts > 0
                ? (float)_waveEarlyCallSuccess / _waveEarlyCallAttempts
                : 0f;

            string buildRate = BuildTowerRateString();
            string leakRate = BuildLeakRateString();
            Debug.Log(
                $"[KPI] WaveSummary wave={_waveKpiNumber} clearTime={clearTime:0.00}s lifeLost={lifeLost} netGoldDelta={netGoldDelta} " +
                $"towerBuildRateByType={buildRate} enemyLeakCountByType={leakRate} earlyCallUsageRate={earlyCallUsageRate:0.00} reason={reason}");

            _waveKpiActive = false;
        }

        private void AccumulateTowerBuildKpi(TowerType towerType)
        {
            if (!_waveKpiActive)
            {
                return;
            }

            _waveTowerBuildCount.TryGetValue(towerType, out int current);
            _waveTowerBuildCount[towerType] = current + 1;
        }

        private void OnEnemyReachedGoalForKpi(EnemyRuntime enemy, EnemyConfig config)
        {
            if (!_waveKpiActive)
            {
                return;
            }

            string enemyId = "Unknown";
            if (config != null && !string.IsNullOrWhiteSpace(config.EnemyId))
            {
                enemyId = config.EnemyId;
            }

            _waveLeakByEnemyType.TryGetValue(enemyId, out int leakCount);
            _waveLeakByEnemyType[enemyId] = leakCount + 1;
        }

        private string BuildTowerRateString()
        {
            if (_waveTowerBuildCount.Count <= 0)
            {
                return "none";
            }

            int totalBuildCount = 0;
            foreach (KeyValuePair<TowerType, int> pair in _waveTowerBuildCount)
            {
                totalBuildCount += pair.Value;
            }

            if (totalBuildCount <= 0)
            {
                return "none";
            }

            string result = string.Empty;
            bool first = true;
            foreach (KeyValuePair<TowerType, int> pair in _waveTowerBuildCount)
            {
                float rate = (float)pair.Value / totalBuildCount;
                if (!first)
                {
                    result += ",";
                }

                result += $"{pair.Key}:{rate:0.00}";
                first = false;
            }

            return result;
        }

        private string BuildLeakRateString()
        {
            if (_waveLeakByEnemyType.Count <= 0)
            {
                return "none";
            }

            string result = string.Empty;
            bool first = true;
            foreach (KeyValuePair<string, int> pair in _waveLeakByEnemyType)
            {
                if (!first)
                {
                    result += ",";
                }

                result += $"{pair.Key}:{pair.Value}";
                first = false;
            }

            return result;
        }
    }
}
