using Common.UI;
using Common.Utils;
using Common.App;
using Common.Extensions;
using System.Collections.Generic;
using UnityEngine;
using Kingdom.Game;
using Kingdom.Game.UI;
using Kingdom.WorldMap;
using Kingdom.Save;
using UnityEngine.EventSystems;

namespace Kingdom.App
{
    /// <summary>
    /// 메인 게임 씬 컨트롤러.
    /// 웨이브 진행, 타워/영웅/스킬 시스템을 초기화합니다.
    /// </summary>
    public class GameScene : SceneBase<SCENES>
    {
        // 리소스 경로 및 밸런스 상수
        private const string BattlefieldPrefabResourcePath = "Prefabs/Game/GameBattlefield"; // 전장 프리팹 경로
        private const string DefaultHeroId = "DefaultHero"; // 기본 영웅 ID
        private const string SelectedHeroIdPlayerPrefsKey = "Kingdom.Hero.SelectedHeroId"; // 선택 영웅 저장 키
        private const string HeroPortraitSpriteResourcePathPrefix = "UI/Sprites/Heroes/Portraits/"; // 영웅 초상화 스프라이트 경로 접두사
        private const string SpellReinforceConfigResourcePath = "Data/SpellConfigs/ReinforceSpell"; // 지원병 스펠 설정 경로
        private const string SpellRainConfigResourcePath = "Data/SpellConfigs/RainSpell"; // 화살비 스펠 설정 경로
        private const float EarlyCallGoldPerSecond = 2.0f; // 웨이브 조기 호출 초당 보너스 골드
        private const float EarlyCallCooldownMinRatio = 0.35f; // 웨이브 조기 호출 최소 쿨다운 비율
        private const float FastForwardTimeScale = 2f; // 배속 시간 배율
        private const int DebugGrantGoldAmount = 500; // 디버그 골드 지급량
        private const string WaveStartSfxResourcePath = "Audio/SFX/UI_WaveStart_Heroic"; // 웨이브 시작 사운드 경로
        private const string WaveStartSfxFallbackResourcePath = "Audio/SFX/UI_Common_Click"; // 웨이브 시작 대체 사운드 경로
        private const string WorldHpBarPrefabResourcePath = "UI/WorldHpBar"; // 월드 HP바 프리팹 경로
        private const string SelectionSystemPrefabResourcePath = "UI/SelectionSystem"; // 선택 시스템 프리팹 경로
        private const float WaveStartBannerDurationSec = 1.2f; // 웨이브 시작 배너 노출 시간
        private const float WaveStartSfxVolumeScale = 0.85f; // 웨이브 시작 사운드 볼륨 스케일
        private const bool RequireCompleteRuntimeVisualData = true; // 필수 비주얼 데이터 누락 시 씬 시작 중단
        private const string HeroInGameSpriteResourcePathPrefix = "UI/Sprites/Heroes/InGame/"; // 인게임 영웅 스프라이트 경로 접두사
        private const string GeneratedHeroManifestResourcePath = "Sprites/Heroes/manifest"; // 생성형 영웅 매니페스트 경로
        private const string GeneratedHeroSpriteResourcePathPrefix = "Sprites/Heroes/"; // 생성형 영웅 스프라이트 경로 접두사
        private static readonly string[] RequiredHeroActions = { "idle", "walk", "attack", "die" }; // 영웅 필수 액션 목록
        private static readonly string[] TowerSpriteResourcePrefixes =
        {
            "UI/Sprites/Towers/",
            "Sprites/Towers/",
            "Kingdom/Towers/Sprites/"
        };
        [System.Serializable]
        private sealed class GeneratedHeroManifestData
        {
            public GeneratedHeroActionRecord[] actions;
        }
        [System.Serializable]
        private sealed class GeneratedHeroActionRecord
        {
            public string actionGroup;
            public string sourceFile;
            public string outputTexture;
        }
        private GameStateController _stateController; // 전투 흐름 상태 머신
        private PathManager _pathManager; // 적 이동 경로 관리
        private SpawnManager _spawnManager; // 적 스폰 관리자
        private WaveManager _waveManager; // 웨이브 진행 관리자
        private InGameEconomyManager _economyManager; // 골드/생명 경제 관리자
        private TowerManager _towerManager; // 타워 건설/강화/판매 관리자
        private ProjectileManager _projectileManager; // 투사체 시뮬레이션 관리자
        private HeroController _heroController; // 영웅 런타임 제어기
        private GameBattlefield _battlefield; // 전장 루트 오브젝트
        private WaveConfig _activeWaveConfig; // 현재 스테이지 웨이브 설정
        private HeroConfig _heroConfig; // 선택된 영웅 설정
        private SpellConfig _reinforceSpellConfig; // 지원병 스펠 설정
        private SpellConfig _rainSpellConfig; // 화살비 스펠 설정
        private int _pendingBuildSlotIndex = -1; // 링 메뉴에서 선택된 대기 슬롯
        private int _selectedTowerId = -1; // 현재 선택된 타워 런타임 ID
        private bool _isRallyPlacementMode; // 집결지 배치 모드 여부
        private float _reinforceCooldownLeft; // 지원병 스펠 남은 쿨다운
        private float _rainCooldownLeft; // 화살비 스펠 남은 쿨다운
        private bool _resultPresented; // 결과 UI 표시 여부
        private bool _resultFinalized; // 결과 처리 완료 여부
        private float _battleStartedAtUnscaled; // 전투 시작 시각(언스케일드)
        private bool _isFastForward; // 배속 모드 여부
        private float _activeTimeScale = 1f; // 현재 적용 시간 배율
        private bool _waveKpiActive; // 웨이브 KPI 수집 활성 여부
        private int _waveKpiNumber; // KPI 대상 웨이브 번호
        private float _waveKpiStartedAtUnscaled; // KPI 수집 시작 시각
        private int _waveKpiStartLives; // KPI 시작 시 생명값
        private int _waveKpiStartGold; // KPI 시작 시 골드값
        private int _waveEarlyCallAttempts; // 조기 호출 시도 횟수
        private int _waveEarlyCallSuccess; // 조기 호출 성공 횟수
        private readonly Dictionary<TowerType, int> _waveTowerBuildCount = new(); // 웨이브별 타워 타입 건설 횟수
        private readonly Dictionary<string, int> _waveLeakByEnemyType = new(); // 웨이브별 적 타입 누수 횟수
        private int _waveStartAnnouncedWave = -1; // 마지막 웨이브 시작 안내 인덱스
        private bool _waveStartSfxResolved; // 웨이브 시작 사운드 해석 완료 여부
        private AudioClip _waveStartSfxClip; // 웨이브 시작 사운드 캐시
        private WorldHpBarManager _worldHpBarManager;
        private SelectionController _selectionController;
        private SelectionCircleVisual _selectionCircleVisual;

        // 씬 초기화 시 UI를 준비한다.
        public override bool OnInit()
        {
            Debug.Log("[GameScene] Initialized.");
            MainUI = UIHelper.GetOrCreate<GameView>();
            return true;
        }

        // 씬 시작 시 전투 시스템을 생성/연결한다.
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

            EnsureWorldHpBarSystem();
            EnsureSelectionSystems();

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
            if (RequireCompleteRuntimeVisualData && !ValidateRequiredRuntimeData(out List<string> missingData))
            {
                return AbortSceneStartForMissingData(missingData);
            }

            _resultPresented = false;
            _resultFinalized = false;
            _battleStartedAtUnscaled = Time.unscaledTime;
            _isFastForward = false;
            _activeTimeScale = 1f;
            _waveKpiActive = false;
            _waveStartAnnouncedWave = -1;
            _waveStartSfxResolved = false;
            _waveStartSfxClip = null;
            if (_activeWaveConfig != null && _activeWaveConfig.Waves != null && _activeWaveConfig.Waves.Count > 0)
            {
                _stateController.SetTotalWaves(_activeWaveConfig.Waves.Count);
            }

            _waveManager.Configure(_activeWaveConfig, _stateController, _spawnManager, _pathManager);
            ConfigureEconomyAndTower();
            ApplyEffectiveTimeScale();

            return true;
        }

        private void EnsureWorldHpBarSystem()
        {
            _worldHpBarManager = FindFirstObjectByType<WorldHpBarManager>();
            if (_worldHpBarManager.IsNull())
            {
                _worldHpBarManager = new GameObject("WorldHpBarManager").AddComponent<WorldHpBarManager>();
            }

            GameObject hpBarPrefab = Resources.Load<GameObject>(WorldHpBarPrefabResourcePath);
            if (hpBarPrefab.IsNotNull())
            {
                _worldHpBarManager.ConfigureRuntime(hpBarPrefab);
            }
            else
            {
                Debug.LogWarning($"[GameScene] WorldHpBar prefab missing: {WorldHpBarPrefabResourcePath}");
            }
        }

        private void EnsureSelectionSystems()
        {
            _selectionController = FindFirstObjectByType<SelectionController>();
            _selectionCircleVisual = FindFirstObjectByType<SelectionCircleVisual>();

            if (_selectionController.IsNull() || _selectionCircleVisual.IsNull())
            {
                GameObject selectionSystemPrefab = Resources.Load<GameObject>(SelectionSystemPrefabResourcePath);
                if (selectionSystemPrefab.IsNotNull())
                {
                    GameObject selectionSystemInstance = Instantiate(selectionSystemPrefab);
                    if (_selectionController.IsNull())
                    {
                        _selectionController = selectionSystemInstance.GetComponentInChildren<SelectionController>(true);
                    }

                    if (_selectionCircleVisual.IsNull())
                    {
                        _selectionCircleVisual = selectionSystemInstance.GetComponentInChildren<SelectionCircleVisual>(true);
                    }

                    if (_selectionController.IsNull() || _selectionCircleVisual.IsNull())
                    {
                        Debug.LogWarning($"[GameScene] SelectionSystem prefab is missing required components. path={SelectionSystemPrefabResourcePath}");
                    }
                }
            }

            if (_selectionController.IsNull())
            {
                _selectionController = new GameObject("SelectionController").AddComponent<SelectionController>();
            }

            if (_selectionCircleVisual.IsNull())
            {
                GameObject circleGo = new GameObject("SelectionCircleVisual");
                _selectionCircleVisual = circleGo.AddComponent<SelectionCircleVisual>();
            }

            if (_selectionController.IsNotNull() && _selectionCircleVisual.IsNotNull())
            {
                _selectionController.SetCircleVisual(_selectionCircleVisual);
            }
        }

        // 씬 종료 시 이벤트 구독과 상태를 정리한다.
        public override void OnEndScene()
        {
            Time.timeScale = 1f;
            _pendingBuildSlotIndex = -1;
            _isRallyPlacementMode = false;
            _resultPresented = false;
            _resultFinalized = false;
            _waveStartAnnouncedWave = -1;

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

        // 입력 처리, 타워 선택, 주문 쿨다운 갱신을 수행한다.
        private void Update()
        {
            HandleDebugGrantGoldInput();
            HandleHeroMoveCommandInput();

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
                    LogRallySetIgnored(_selectedTowerId);
                }

                RefreshAfterTowerAction();
                TickSpellCooldowns();
                SelectionController.SuppressSelectionForCurrentFrame();
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
                    string infoText = BuildTowerActionInfoText(towerInfo);
                    bool canUpgrade = towerInfo.Level < towerInfo.MaxLevel && _economyManager != null && _economyManager.Gold >= towerInfo.UpgradeCost;
                    gameView.OpenTowerActionMenuAtWorldPosition(
                        towerWorld,
                        infoText,
                        canUpgrade,
                        towerInfo.UpgradeCost,
                        towerInfo.SellRefund,
                        towerInfo.SupportsRally);

                    if (_selectionController.IsNotNull()
                        && _towerManager.TryGetTowerSelectableTarget(towerId, out ISelectableTarget towerTarget)
                        && towerTarget.IsNotNull())
                    {
                        _selectionController.Select(towerTarget);
                    }
                }

                SelectionController.SuppressSelectionForCurrentFrame();
                return;
            }

            if (_towerManager.TryFindBuildableSlotAtWorldPosition(world, 0.65f, out int slotIndex, out Vector3 slotWorld))
            {
                _pendingBuildSlotIndex = slotIndex;
                _selectedTowerId = -1;
                _isRallyPlacementMode = false;
                gameView.HideTowerActionMenuPublic();
                gameView.OpenTowerRingMenuAtWorldPosition(slotWorld);
                SelectionController.SuppressSelectionForCurrentFrame();
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

        private void HandleHeroMoveCommandInput()
        {
            if (!Input.GetMouseButtonDown(1))
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (_selectionController.IsNull() || _heroController.IsNull())
            {
                return;
            }

            if (!(_selectionController.CurrentSelected is HeroController selectedHero) || selectedHero != _heroController)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            if (!_heroController.TrySetMoveTarget(world))
            {
                return;
            }

            _isRallyPlacementMode = false;
            _selectedTowerId = -1;
            _pendingBuildSlotIndex = -1;

            if (MainUI is GameView gameView)
            {
                gameView.HideTowerRingMenuPublic();
                gameView.HideTowerActionMenuPublic();
            }

            SelectionController.SuppressSelectionForCurrentFrame();
            Debug.Log($"[GameScene] Hero move command accepted. target={world}");
        }

        // Update에서 문자열 보간 할당을 피하기 위한 타워 정보 텍스트 생성 헬퍼.
        private static string BuildTowerActionInfoText(TowerManager.TowerActionInfo towerInfo)
        {
            return $"{towerInfo.TowerType} Lv.{towerInfo.Level}";
        }

        // Update에서 문자열 보간 할당을 피하기 위한 로그 헬퍼.
        private static void LogRallySetIgnored(int towerId)
        {
            Debug.Log($"[GameScene] Rally set ignored. towerId={towerId}");
        }

        // 에디터 디버그 골드 지급 입력(F6)을 처리한다.
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

        // 현재 선택 스테이지의 웨이브 설정을 해석한다.
        private static WaveConfig ResolveStageWaveConfig()
        {
            WorldMapManager worldMapManager = WorldMapManager.Instance;
            StageConfig stageConfig = worldMapManager != null ? worldMapManager.CurrentStageConfig : null;
            if (stageConfig == null || stageConfig.Stages == null || stageConfig.Stages.Count == 0)
            {
                int selectedStageIdFallback = WorldMapScene.SelectedStageId;
                if (selectedStageIdFallback > 0)
                {
                    WaveConfig directById = ConfigResourcePaths.LoadWaveConfigByStageId(selectedStageIdFallback);
                    if (directById != null)
                    {
                        return directById;
                    }
                }

                return ConfigResourcePaths.LoadWaveConfigByStageId(1);
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

        // StageData에서 웨이브 설정을 직접/경로 기반으로 찾는다.
        private static WaveConfig TryResolveWaveConfig(StageData stageData)
        {
            if (stageData.WaveConfig != null)
            {
                return stageData.WaveConfig;
            }

            // StageConfig 직렬화 참조가 비어 있으면 Resources 경로에서 보완 로드한다.
            if (stageData.StageId > 0)
            {
                return ConfigResourcePaths.LoadWaveConfigByStageId(stageData.StageId);
            }

            return null;
        }

        // 메인 카메라를 2D 전투 기준값으로 고정한다.
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

        // 전장 오브젝트를 확보하고 없으면 폴백을 생성한다.
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

        // 씬 시작 전에 필수 런타임 비주얼 데이터를 검증한다.
        private bool ValidateRequiredRuntimeData(out List<string> missingData)
        {
            missingData = new List<string>();

            string selectedHeroId = GetSelectedHeroIdForRuntimeValidation();
            HeroConfig selectedHeroConfig = LoadSelectedHeroConfigForRuntimeValidation(selectedHeroId, missingData);
            if (selectedHeroConfig != null)
            {
                ValidateHeroVisualBindings(selectedHeroConfig, missingData);
            }

            ValidateWaveEnemyVisualBindings(_activeWaveConfig, missingData);
            ValidateTowerVisualBindings(missingData);

            return missingData.Count <= 0;
        }

        // 검증용 선택 영웅 ID를 안전하게 반환한다.
        private static string GetSelectedHeroIdForRuntimeValidation()
        {
            string selectedHeroId = PlayerPrefs.GetString(SelectedHeroIdPlayerPrefsKey, DefaultHeroId);
            if (string.IsNullOrWhiteSpace(selectedHeroId))
            {
                return DefaultHeroId;
            }

            return selectedHeroId.Trim();
        }

        // 검증용 선택 영웅 설정을 로드한다.
        private static HeroConfig LoadSelectedHeroConfigForRuntimeValidation(string selectedHeroId, List<string> missingData)
        {
            HeroConfig config = ConfigResourcePaths.LoadHeroConfig(selectedHeroId);
            if (config == null)
            {
                missingData.Add($"HeroConfig missing: {ConfigResourcePaths.HeroPrefix}{selectedHeroId} (legacy fallback also checked)");
                return null;
            }

            if (string.IsNullOrWhiteSpace(config.HeroId))
            {
                missingData.Add($"HeroConfig invalid HeroId: {ConfigResourcePaths.HeroPrefix}{selectedHeroId}");
            }

            return config;
        }

        // 영웅 필수 액션 스프라이트 바인딩을 검증한다.
        private static void ValidateHeroVisualBindings(HeroConfig heroConfig, List<string> missingData)
        {
            if (heroConfig == null)
            {
                missingData.Add("Hero visual validation failed: selected hero config is null.");
                return;
            }

            string heroId = string.IsNullOrWhiteSpace(heroConfig.HeroId)
                ? string.Empty
                : heroConfig.HeroId.Trim();
            if (heroId.Length <= 0)
            {
                missingData.Add($"Hero visual validation failed: HeroId is empty. config={heroConfig.name}");
                return;
            }

            GeneratedHeroManifestData manifest = LoadGeneratedHeroManifestForRuntimeValidation();
            for (int i = 0; i < RequiredHeroActions.Length; i++)
            {
                string action = RequiredHeroActions[i];
                string directPath = $"{HeroInGameSpriteResourcePathPrefix}{heroId}/{action}_00";
                if (TryResolveSpriteResourcePath(directPath))
                {
                    continue;
                }

                if (TryResolveHeroActionFromManifest(heroId, action, manifest, out _, out string reason))
                {
                    continue;
                }

                missingData.Add(
                    $"Hero sprite missing: heroId={heroId}, action={action}, directPath={directPath}, manifest={GeneratedHeroManifestResourcePath}, reason={reason}");
            }
        }

        // 생성형 영웅 매니페스트를 로드한다.
        private static GeneratedHeroManifestData LoadGeneratedHeroManifestForRuntimeValidation()
        {
            TextAsset manifestAsset = Resources.Load<TextAsset>(GeneratedHeroManifestResourcePath);
            if (manifestAsset == null || string.IsNullOrWhiteSpace(manifestAsset.text))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<GeneratedHeroManifestData>(manifestAsset.text);
            }
            catch
            {
                return null;
            }
        }

        // 매니페스트에서 영웅 액션 경로를 해석한다.
        private static bool TryResolveHeroActionFromManifest(
            string heroId,
            string action,
            GeneratedHeroManifestData manifest,
            out string textureResourcePath,
            out string reason)
        {
            textureResourcePath = string.Empty;
            reason = string.Empty;

            if (manifest == null || manifest.actions == null || manifest.actions.Length <= 0)
            {
                reason = "manifest missing or empty.";
                return false;
            }

            GeneratedHeroActionRecord best = null;
            for (int i = 0; i < manifest.actions.Length; i++)
            {
                GeneratedHeroActionRecord candidate = manifest.actions[i];
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.actionGroup))
                {
                    continue;
                }

                if (!IsManifestActionMatch(candidate.actionGroup, action))
                {
                    continue;
                }

                if (best == null)
                {
                    best = candidate;
                }

                if (ContainsIgnoreCase(candidate.outputTexture, heroId) || ContainsIgnoreCase(candidate.sourceFile, heroId))
                {
                    best = candidate;
                    break;
                }
            }

            if (best == null)
            {
                reason = $"action record missing for action={action}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(best.outputTexture))
            {
                reason = $"outputTexture is empty for action={action}.";
                return false;
            }

            textureResourcePath = GeneratedHeroSpriteResourcePathPrefix + StripExtension(best.outputTexture);
            if (!TryResolveSpriteResourcePath(textureResourcePath))
            {
                reason = $"output texture unresolved at {textureResourcePath}.";
                return false;
            }

            if (IsMultiActionGroup(best.actionGroup) && !HasActionFramesInTexture(textureResourcePath, action))
            {
                reason = $"multi action record exists but no frames matched action={action} in texture={textureResourcePath}.";
                return false;
            }

            return true;
        }

        private static bool IsManifestActionMatch(string actionGroup, string action)
        {
            if (string.IsNullOrWhiteSpace(actionGroup) || string.IsNullOrWhiteSpace(action))
            {
                return false;
            }

            if (string.Equals(actionGroup, action, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return IsMultiActionGroup(actionGroup);
        }

        private static bool IsMultiActionGroup(string actionGroup)
        {
            return string.Equals(actionGroup, "multi", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasActionFramesInTexture(string textureResourcePath, string action)
        {
            if (string.IsNullOrWhiteSpace(textureResourcePath) || string.IsNullOrWhiteSpace(action))
            {
                return false;
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(textureResourcePath);
            if (sprites == null || sprites.Length <= 0)
            {
                return false;
            }

            string token = $"_{action.ToLowerInvariant()}_";
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite.IsNull() || string.IsNullOrWhiteSpace(sprite.name))
                {
                    continue;
                }

                if (sprite.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        // 웨이브에 등장하는 적의 비주얼 바인딩을 검증한다.
        private static void ValidateWaveEnemyVisualBindings(WaveConfig waveConfig, List<string> missingData)
        {
            if (waveConfig == null)
            {
                missingData.Add("WaveConfig missing: current stage has no wave config.");
                return;
            }

            if (waveConfig.Waves == null || waveConfig.Waves.Count <= 0)
            {
                missingData.Add($"WaveConfig has no waves: {waveConfig.name}");
                return;
            }

            var checkedConfigs = new HashSet<EnemyConfig>();
            for (int waveIndex = 0; waveIndex < waveConfig.Waves.Count; waveIndex++)
            {
                WaveConfig.WaveData wave = waveConfig.Waves[waveIndex];
                int displayWave = wave.WaveIndex > 0 ? wave.WaveIndex : waveIndex + 1;
                if (wave.SpawnEntries == null || wave.SpawnEntries.Count <= 0)
                {
                    missingData.Add($"Wave spawn entry missing: wave={displayWave}");
                    continue;
                }

                for (int entryIndex = 0; entryIndex < wave.SpawnEntries.Count; entryIndex++)
                {
                    WaveConfig.SpawnEntry spawnEntry = wave.SpawnEntries[entryIndex];
                    if (spawnEntry.Enemy == null)
                    {
                        missingData.Add($"EnemyConfig reference missing: wave={displayWave}, entry={entryIndex + 1}");
                        continue;
                    }

                    if (!checkedConfigs.Add(spawnEntry.Enemy))
                    {
                        continue;
                    }

                    if (!TryResolveEnemySpriteBinding(spawnEntry.Enemy, out string detail))
                    {
                        missingData.Add(detail);
                    }
                }
            }
        }

        // 적 Animator 바인딩을 검증한다.
        private static bool TryResolveEnemySpriteBinding(EnemyConfig config, out string detail)
        {
            detail = string.Empty;
            if (config == null)
            {
                detail = "Enemy animator missing: EnemyConfig is null.";
                return false;
            }

            string enemyId = string.IsNullOrWhiteSpace(config.EnemyId) ? "(empty)" : config.EnemyId.Trim();
            if (!TryResolveEnemyAnimatorBinding(config, out string animatorResolvedPath, out string animatorReason))
            {
                detail =
                    $"Enemy animator missing: enemyId={enemyId}, animatorPath={NormalizeForLog(animatorResolvedPath)}, reason={animatorReason}";
                return false;
            }
            // Enemy visual validation is now animator-driven.
            // Sprite-path based validation is intentionally skipped.
            return true;
        }

        // 적 Animator 컨트롤러 바인딩을 확인한다. (현재 런타임은 Animator를 필수로 사용)
        private static bool TryResolveEnemyAnimatorBinding(EnemyConfig config, out string resolvedPath, out string reason)
        {
            resolvedPath = string.Empty;
            reason = string.Empty;

            if (config == null)
            {
                reason = "EnemyConfig is null.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(config.RuntimeAnimatorControllerPath))
            {
                string configuredPath = config.RuntimeAnimatorControllerPath.Trim();
                RuntimeAnimatorController byConfig = Resources.Load<RuntimeAnimatorController>(configuredPath);
                if (byConfig != null)
                {
                    resolvedPath = configuredPath;
                    return true;
                }
            }

            if (string.IsNullOrWhiteSpace(config.EnemyId))
            {
                reason = "EnemyId is empty.";
                return false;
            }

            string enemyId = config.EnemyId.Trim();
            string conventionalPath = $"Animations/Enemies/{enemyId}/{enemyId}";
            RuntimeAnimatorController byConvention = Resources.Load<RuntimeAnimatorController>(conventionalPath);
            if (byConvention != null)
            {
                resolvedPath = conventionalPath;
                return true;
            }

            reason = $"configured={NormalizeForLog(config.RuntimeAnimatorControllerPath)}, conventional={conventionalPath}";
            return false;
        }

        // 타워/배럭 병사 스프라이트 바인딩을 검증한다.
        private static void ValidateTowerVisualBindings(List<string> missingData)
        {
            foreach (TowerType towerType in System.Enum.GetValues(typeof(TowerType)))
            {
                string configPath = $"{ConfigResourcePaths.TowerPrefix}{towerType}";
                TowerConfig config = ConfigResourcePaths.LoadTowerConfig(towerType.ToString());
                if (config == null)
                {
                    missingData.Add($"TowerConfig missing: {configPath}");
                    continue;
                }

                if (config.Levels == null || config.Levels.Length <= 0)
                {
                    missingData.Add($"Tower level data missing: towerType={towerType}, config={configPath}");
                    continue;
                }

                for (int levelIndex = 0; levelIndex < config.Levels.Length; levelIndex++)
                {
                    if (!HasTowerLevelSpriteBinding(config, towerType, levelIndex, out string detail))
                    {
                        missingData.Add(detail);
                    }
                }

                if (towerType == TowerType.Barracks && !HasBarracksSoldierSpriteBinding(config, out string barracksDetail))
                {
                    missingData.Add(barracksDetail);
                }
            }
        }

        // 타워 레벨별 스프라이트 바인딩 존재 여부를 확인한다.
        private static bool HasTowerLevelSpriteBinding(TowerConfig config, TowerType towerType, int levelIndex, out string detail)
        {
            detail = string.Empty;
            if (config == null || config.Levels == null || config.Levels.Length <= levelIndex || levelIndex < 0)
            {
                detail = $"Tower sprite missing: towerType={towerType}, invalid level index={levelIndex}";
                return false;
            }

            int safeLevelIndex = Mathf.Max(0, levelIndex);
            int displayLevel = safeLevelIndex + 1;
            TowerLevelData levelData = config.Levels[safeLevelIndex];

            if (levelData.SpriteOverride != null)
            {
                return true;
            }

            if (TryResolveSpriteResourcePath(levelData.SpriteResourcePath))
            {
                return true;
            }

            string expandedRuntimePath = ExpandTowerTemplatePath(config.RuntimeSpriteResourcePath, towerType, safeLevelIndex);
            if (TryResolveSpriteResourcePath(expandedRuntimePath))
            {
                return true;
            }

            List<string> candidates = BuildTowerSpritePathCandidates(config.TowerId, towerType, safeLevelIndex);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (TryResolveSpriteResourcePath(candidates[i]))
                {
                    return true;
                }
            }

            string candidateText = candidates.Count > 0 ? string.Join(", ", candidates) : "(none)";
            detail =
                $"Tower sprite missing: towerType={towerType}, towerId={NormalizeForLog(config.TowerId)}, level=L{displayLevel}, " +
                $"levelPath={NormalizeForLog(levelData.SpriteResourcePath)}, runtimePath={NormalizeForLog(expandedRuntimePath)}, candidates={candidateText}";
            return false;
        }

        // 배럭 병사 스프라이트 바인딩 존재 여부를 확인한다.
        private static bool HasBarracksSoldierSpriteBinding(TowerConfig config, out string detail)
        {
            detail = string.Empty;
            if (config == null)
            {
                detail = "Barracks soldier sprite missing: TowerConfig is null.";
                return false;
            }

            string soldierConfigPath = config.BarracksSoldierConfig != null
                ? config.BarracksSoldierConfig.RuntimeSpriteResourcePath
                : string.Empty;
            if (TryResolveSpriteResourcePath(soldierConfigPath))
            {
                return true;
            }

            if (TryResolveSpriteResourcePath(config.BarracksData.SoldierSpriteResourcePath))
            {
                return true;
            }

            var candidates = new List<string>
            {
                "UI/Sprites/Barracks/Soldier",
                "Sprites/Barracks/Soldier",
                "UI/Sprites/Towers/Barracks/Soldier",
                "Sprites/Towers/Barracks/Soldier",
                "Kingdom/Towers/Sprites/Barracks/Soldier"
            };

            if (!string.IsNullOrWhiteSpace(config.TowerId))
            {
                string towerId = config.TowerId.Trim();
                candidates.Add($"UI/Sprites/Towers/{towerId}/Soldier");
                candidates.Add($"Sprites/Towers/{towerId}/Soldier");
                candidates.Add($"Kingdom/Towers/Sprites/{towerId}/Soldier");
            }

            if (config.BarracksSoldierConfig != null && !string.IsNullOrWhiteSpace(config.BarracksSoldierConfig.SoldierId))
            {
                string soldierId = config.BarracksSoldierConfig.SoldierId.Trim();
                candidates.Add($"UI/Sprites/Barracks/Soldiers/{soldierId}");
                candidates.Add($"Sprites/Barracks/Soldiers/{soldierId}");
                candidates.Add($"Kingdom/Barracks/Soldiers/{soldierId}");
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (TryResolveSpriteResourcePath(candidates[i]))
                {
                    return true;
                }
            }

            detail =
                $"Barracks soldier sprite missing: towerId={NormalizeForLog(config.TowerId)}, " +
                $"configPath={NormalizeForLog(soldierConfigPath)}, legacyPath={NormalizeForLog(config.BarracksData.SoldierSpriteResourcePath)}, " +
                $"candidates={string.Join(", ", candidates)}";
            return false;
        }

        // 타워 식별자 기반 스프라이트 후보 경로를 생성한다.
        private static List<string> BuildTowerSpritePathCandidates(string towerId, TowerType towerType, int levelIndex)
        {
            string typeName = towerType.ToString();
            string levelToken = Mathf.Max(1, levelIndex + 1).ToString();
            var candidates = new List<string>(24);

            for (int i = 0; i < TowerSpriteResourcePrefixes.Length; i++)
            {
                string prefix = TowerSpriteResourcePrefixes[i];
                AddTowerNameCandidates(candidates, prefix, towerId, levelToken);
                AddTowerNameCandidates(candidates, prefix, typeName, levelToken);
            }

            return candidates;
        }

        // 타워 이름 조합 후보를 경로 목록에 추가한다.
        private static void AddTowerNameCandidates(List<string> candidates, string prefix, string towerName, string levelToken)
        {
            if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(towerName))
            {
                return;
            }

            string trimmed = towerName.Trim();
            TryAddCandidate(candidates, $"{prefix}{trimmed}_L{levelToken}");
            TryAddCandidate(candidates, $"{prefix}{trimmed}/L{levelToken}");
            TryAddCandidate(candidates, $"{prefix}{trimmed}/Level{levelToken}");
            TryAddCandidate(candidates, $"{prefix}{trimmed}");
        }

        // 일반 후보 경로를 중복 없이 추가한다.
        private static void TryAddCandidate(List<string> candidates, string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate) || candidates.Contains(candidate))
            {
                return;
            }

            candidates.Add(candidate);
        }

        // 타워 템플릿 경로의 토큰을 실제 값으로 확장한다.
        private static string ExpandTowerTemplatePath(string templatePath, TowerType towerType, int levelIndex)
        {
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                return string.Empty;
            }

            int level = Mathf.Max(1, levelIndex + 1);
            string expanded = templatePath.Replace("{tower}", towerType.ToString());
            expanded = expanded.Replace("{level}", level.ToString());
            return expanded;
        }

        // Resources/디스크 기준으로 스프라이트 경로를 검증한다.
        private static bool TryResolveSpriteResourcePath(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return false;
            }

            Sprite single = Resources.Load<Sprite>(resourcePath);
            if (single != null)
            {
                return true;
            }

            Sprite[] multiple = Resources.LoadAll<Sprite>(resourcePath);
            if (multiple != null && multiple.Length > 0)
            {
                return true;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                return true;
            }

            return TryResolveTextureOnResourcesDisk(resourcePath);
        }

        // Resources 폴더의 텍스처 파일 존재를 검사한다.
        private static bool TryResolveTextureOnResourcesDisk(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return false;
            }

            string normalized = resourcePath.Replace('\\', '/').TrimStart('/');
            if (normalized.Length <= 0)
            {
                return false;
            }

            string basePath = System.IO.Path.Combine(Application.dataPath, "Resources", normalized);
            if (System.IO.File.Exists(basePath + ".png"))
            {
                return true;
            }

            if (System.IO.File.Exists(basePath + ".jpg"))
            {
                return true;
            }

            if (System.IO.File.Exists(basePath + ".jpeg"))
            {
                return true;
            }

            return false;
        }

        // 대소문자 무시 포함 여부를 확인한다.
        private static bool ContainsIgnoreCase(string source, string token)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            return source.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // 파일명에서 확장자를 제거한다.
        private static string StripExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            return System.IO.Path.GetFileNameWithoutExtension(fileName);
        }

        // 로그 출력용 문자열을 정규화한다.
        private static string NormalizeForLog(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(empty)" : value.Trim();
        }

        // 누락 상세 메시지를 Hero/Enemy/Tower/Barracks/Other 분류로 변환한다.
        private static string ClassifyMissingDataCategory(string detail)
        {
            if (ContainsIgnoreCase(detail, "barracks"))
            {
                return "Barracks";
            }

            if (ContainsIgnoreCase(detail, "hero"))
            {
                return "Hero";
            }

            if (ContainsIgnoreCase(detail, "enemy"))
            {
                return "Enemy";
            }

            if (ContainsIgnoreCase(detail, "tower"))
            {
                return "Tower";
            }

            return "Other";
        }

        // 누락 데이터 카테고리 요약을 로그 빌더에 추가한다.
        private static void AppendMissingDataCategorySummary(System.Text.StringBuilder builder, List<string> missingData)
        {
            if (builder == null || missingData == null || missingData.Count <= 0)
            {
                return;
            }

            var counts = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < missingData.Count; i++)
            {
                string category = ClassifyMissingDataCategory(missingData[i]);
                counts.TryGetValue(category, out int current);
                counts[category] = current + 1;
            }

            string[] order = { "Hero", "Enemy", "Barracks", "Tower", "Other" };
            builder.AppendLine("[Category Summary]");
            for (int i = 0; i < order.Length; i++)
            {
                string key = order[i];
                if (!counts.TryGetValue(key, out int count) || count <= 0)
                {
                    continue;
                }

                builder.Append(" - ");
                builder.Append(key);
                builder.Append(": ");
                builder.AppendLine(count.ToString());
            }
        }

        // 누락 상세 메시지에서 실제 리소스 경로를 추출해 요약으로 출력한다.
        private static void AppendMissingResourcePathSummary(System.Text.StringBuilder builder, List<string> missingData)
        {
            if (builder == null || missingData == null || missingData.Count <= 0)
            {
                return;
            }

            var paths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < missingData.Count; i++)
            {
                CollectMissingResourcePaths(missingData[i], paths);
            }

            if (paths.Count <= 0)
            {
                return;
            }

            builder.AppendLine("[Missing Resource Paths]");
            foreach (string path in paths)
            {
                builder.Append(" - ");
                builder.AppendLine(path);
            }
        }

        // 단일 누락 메시지에서 경로 후보를 추출한다.
        private static void CollectMissingResourcePaths(string detail, HashSet<string> output)
        {
            if (output == null || string.IsNullOrWhiteSpace(detail))
            {
                return;
            }

            const string heroConfigMissingPrefix = "HeroConfig missing:";
            if (detail.StartsWith(heroConfigMissingPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                AddNormalizedMissingPath(output, detail.Substring(heroConfigMissingPrefix.Length));
            }

            CollectKeyValuePath(detail, "requiredPath=", output);
            CollectKeyValuePath(detail, "runtimePath=", output);
            CollectKeyValuePath(detail, "move=", output);

            // candidates=a, b, c 형태와 missing=[action=..., candidates=...] 형태를 모두 처리한다.
            System.Text.RegularExpressions.MatchCollection candidateMatches =
                System.Text.RegularExpressions.Regex.Matches(detail, @"candidates=([^\]\|]+)");
            for (int i = 0; i < candidateMatches.Count; i++)
            {
                string candidateGroup = candidateMatches[i].Groups[1].Value;
                if (string.IsNullOrWhiteSpace(candidateGroup))
                {
                    continue;
                }

                string[] split = candidateGroup.Split(',');
                for (int splitIndex = 0; splitIndex < split.Length; splitIndex++)
                {
                    AddNormalizedMissingPath(output, split[splitIndex]);
                }
            }
        }

        // key=value 형태에서 value를 추출해 경로 목록에 추가한다.
        private static void CollectKeyValuePath(string detail, string key, HashSet<string> output)
        {
            if (string.IsNullOrWhiteSpace(detail) || string.IsNullOrWhiteSpace(key) || output == null)
            {
                return;
            }

            int searchIndex = 0;
            while (searchIndex < detail.Length)
            {
                int keyIndex = detail.IndexOf(key, searchIndex, System.StringComparison.OrdinalIgnoreCase);
                if (keyIndex < 0)
                {
                    return;
                }

                int valueStart = keyIndex + key.Length;
                if (valueStart >= detail.Length)
                {
                    return;
                }

                int valueEnd = detail.Length;
                int comma = detail.IndexOf(',', valueStart);
                if (comma >= 0)
                {
                    valueEnd = System.Math.Min(valueEnd, comma);
                }

                int pipe = detail.IndexOf('|', valueStart);
                if (pipe >= 0)
                {
                    valueEnd = System.Math.Min(valueEnd, pipe);
                }

                int bracket = detail.IndexOf(']', valueStart);
                if (bracket >= 0)
                {
                    valueEnd = System.Math.Min(valueEnd, bracket);
                }

                if (valueEnd > valueStart)
                {
                    string value = detail.Substring(valueStart, valueEnd - valueStart);
                    AddNormalizedMissingPath(output, value);
                }

                searchIndex = valueStart;
            }
        }

        // 경로 후보 문자열을 정규화해 유효한 항목만 추가한다.
        private static void AddNormalizedMissingPath(HashSet<string> output, string rawPath)
        {
            if (output == null || string.IsNullOrWhiteSpace(rawPath))
            {
                return;
            }

            string path = rawPath.Trim();
            path = path.Trim('\"', '\'', '[', ']', '(', ')');
            if (path.EndsWith(".", System.StringComparison.Ordinal))
            {
                path = path.Substring(0, path.Length - 1).Trim();
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (string.Equals(path, "(empty)", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, "(none)", System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (path.IndexOf("no candidates", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("EnemyConfig is null", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            output.Add(path);
        }

        // 누락 상세 메시지를 기반으로 자동 수정 힌트를 생성한다.
        private static void AppendMissingFixHintSummary(System.Text.StringBuilder builder, List<string> missingData)
        {
            if (builder == null || missingData == null || missingData.Count <= 0)
            {
                return;
            }

            var hints = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < missingData.Count; i++)
            {
                CollectMissingFixHints(missingData[i], hints);
            }

            if (hints.Count <= 0)
            {
                return;
            }

            builder.AppendLine("[Auto Fix Hints]");
            foreach (string hint in hints)
            {
                builder.Append(" - ");
                builder.AppendLine(hint);
            }
        }

        // 단일 누락 메시지에서 수정 힌트를 추출한다.
        private static void CollectMissingFixHints(string detail, HashSet<string> hints)
        {
            if (hints == null || string.IsNullOrWhiteSpace(detail))
            {
                return;
            }

            if (detail.StartsWith("HeroConfig missing:", System.StringComparison.OrdinalIgnoreCase))
            {
                hints.Add("HeroConfig 리소스를 생성/복구하세요. 경로 예: Assets/Resources/Kingdom/Configs/Heroes/<HeroId>.asset");
                return;
            }

            if (detail.StartsWith("HeroConfig invalid HeroId:", System.StringComparison.OrdinalIgnoreCase) ||
                detail.IndexOf("HeroId is empty", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hints.Add("HeroConfig.HeroId 값을 채우세요.");
                return;
            }

            if (detail.StartsWith("Hero sprite missing:", System.StringComparison.OrdinalIgnoreCase))
            {
                hints.Add("영웅 스프라이트를 추가하세요: UI/Sprites/Heroes/InGame/<HeroId>/<action>_00");
                hints.Add("또는 Assets/Resources/Sprites/Heroes/manifest.json의 action/outputTexture 매핑을 보완하세요.");
                return;
            }

            if (detail.StartsWith("WaveConfig missing:", System.StringComparison.OrdinalIgnoreCase))
            {
                hints.Add("현재 스테이지에 WaveConfig를 연결하거나 Resources/Kingdom/Configs/Waves/Stage_<id>_WaveConfig를 생성하세요.");
                return;
            }

            if (detail.StartsWith("WaveConfig has no waves:", System.StringComparison.OrdinalIgnoreCase))
            {
                hints.Add("WaveConfig.Waves에 최소 1개 이상의 웨이브 데이터를 추가하세요.");
                return;
            }

            if (detail.StartsWith("Wave spawn entry missing:", System.StringComparison.OrdinalIgnoreCase))
            {
                hints.Add("WaveConfig.Waves[*].SpawnEntries에 최소 1개 이상의 엔트리를 추가하세요.");
                return;
            }

            if (detail.StartsWith("EnemyConfig reference missing:", System.StringComparison.OrdinalIgnoreCase))
            {
                hints.Add("WaveConfig.Waves[*].SpawnEntries[*].Enemy 참조를 지정하세요.");
                return;
            }

            if (detail.StartsWith("Enemy animator missing:", System.StringComparison.OrdinalIgnoreCase))
            {
                hints.Add("EnemyConfig.RuntimeAnimatorControllerPath를 유효한 Resources 경로로 지정하세요.");
                hints.Add("또는 관례 경로 Resources/Animations/Enemies/<EnemyId>/<EnemyId>.controller를 생성하세요.");
                return;
            }

            if (detail.StartsWith("TowerConfig missing:", System.StringComparison.OrdinalIgnoreCase))
            {
                hints.Add("TowerConfig 리소스를 생성/복구하세요. 경로 예: Assets/Resources/Kingdom/Configs/Towers/<TowerType>.asset");
                return;
            }

            if (detail.StartsWith("Tower level data missing:", System.StringComparison.OrdinalIgnoreCase))
            {
                hints.Add("TowerConfig.Levels 배열에 레벨 데이터를 추가하세요.");
                return;
            }

            if (detail.StartsWith("Tower sprite missing:", System.StringComparison.OrdinalIgnoreCase))
            {
                hints.Add("TowerConfig.Levels[i].SpriteResourcePath를 지정하거나 TowerConfig.RuntimeSpriteResourcePath 템플릿을 설정하세요.");
                return;
            }

            if (detail.StartsWith("Barracks soldier sprite missing:", System.StringComparison.OrdinalIgnoreCase))
            {
                hints.Add("TowerConfig.BarracksSoldierConfig.RuntimeSpriteResourcePath를 지정하세요.");
                hints.Add("TowerConfig.BarracksData.SoldierSpriteResourcePath를 지정하세요.");
                return;
            }
        }

        // 필수 데이터 누락 시 시작을 중단하고 사유를 출력한다.
        private bool AbortSceneStartForMissingData(List<string> missingData)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("[GameScene] Required runtime data is missing. Scene start aborted.");
            AppendMissingDataCategorySummary(builder, missingData);
            AppendMissingResourcePathSummary(builder, missingData);
            AppendMissingFixHintSummary(builder, missingData);
            if (missingData != null)
            {
                for (int i = 0; i < missingData.Count; i++)
                {
                    builder.Append(" - ");
                    builder.AppendLine(missingData[i]);
                }
            }

            Debug.LogError(builder.ToString());

            if (_stateController != null)
            {
                _stateController.enabled = false;
            }

            if (_waveManager != null)
            {
                _waveManager.enabled = false;
            }

            if (_spawnManager != null)
            {
                _spawnManager.enabled = false;
            }

            if (_towerManager != null)
            {
                _towerManager.enabled = false;
            }

            if (_heroController != null)
            {
                _heroController.enabled = false;
            }

            enabled = false;
            Time.timeScale = 0f;

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
#endif
            return false;
        }

        // 경제/타워/웨이브 UI와 이벤트를 초기 구성한다.
        private void ConfigureEconomyAndTower()
        {
            int initialGold = _activeWaveConfig != null ? Mathf.Max(0, _activeWaveConfig.InitialGold) : 100;
            int initialLives = _activeWaveConfig != null ? Mathf.Max(1, _activeWaveConfig.InitialLives) : 20;
            _economyManager.Configure(initialGold, initialLives, _spawnManager, _stateController);

            var towerConfig = ConfigResourcePaths.LoadTowerConfig(TowerType.Archer.ToString());
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

        // 영웅 컨트롤러를 스폰 위치와 함께 설정한다.
        private void ConfigureHeroController(System.Collections.Generic.List<Vector3> towerSlots)
        {
            if (_heroController == null || _spawnManager == null)
            {
                return;
            }

            // 첫 타워 슬롯 근처 대신 안전한 좌하단 위치에서 시작한다.
            Vector3 heroSpawn = new Vector3(-6.5f, -3.5f, 0f);

            _heroController.Configure(_spawnManager, _heroConfig, heroSpawn);
        }

        // 기본 건설 요청을 처리한다.
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

        // 타워 타입별 건설 요청을 처리한다.
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

        // 생명/골드 변경을 UI에 반영한다.
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

        // 다음 웨이브 조기 호출 요청을 처리한다.
        private void OnNextWaveRequested()
        {
            if (_stateController == null)
            {
                return;
            }

            float remainingReadySeconds = _stateController.WaveReadyRemaining;
            _waveEarlyCallAttempts++;
            // 주의:
            // TryEarlyCallNextWave() 내부에서 상태 전이가 즉시 발생하고,
            // 같은 프레임에서 KPI Finalize가 먼저 호출될 수 있다.
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

        // 배속 토글 입력을 처리한다.
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

        // 웨이브 조기 호출 보상을 적용한다.
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

        // 게임 상태 변화에 따라 HUD를 갱신한다.
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

                if (state == GameFlowState.WaveRunning)
                {
                    TryPresentWaveStartCue(gameView);
                }
                else
                {
                    gameView.HideWaveStartBanner();
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

        // 웨이브 대기 카운트다운 UI를 갱신한다.
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

        // 웨이브 시작 배너/사운드를 1회 표시한다.
        private void TryPresentWaveStartCue(GameView gameView)
        {
            if (_stateController == null)
            {
                return;
            }

            int currentWave = Mathf.Max(1, _stateController.CurrentWave);
            if (_waveStartAnnouncedWave == currentWave)
            {
                return;
            }

            _waveStartAnnouncedWave = currentWave;
            gameView.ShowWaveStartBanner(currentWave, _stateController.TotalWaves, WaveStartBannerDurationSec);

            AudioClip clip = ResolveWaveStartSfxClip();
            if (clip != null)
            {
                AudioHelper.Instance?.PlaySFX(clip, WaveStartSfxVolumeScale);
            }
        }

        // 웨이브 시작 사운드를 로드(폴백 포함)한다.
        private AudioClip ResolveWaveStartSfxClip()
        {
            if (_waveStartSfxResolved)
            {
                return _waveStartSfxClip;
            }

            _waveStartSfxResolved = true;
            _waveStartSfxClip = Resources.Load<AudioClip>(WaveStartSfxResourcePath);
            if (_waveStartSfxClip != null)
            {
                return _waveStartSfxClip;
            }

            _waveStartSfxClip = Resources.Load<AudioClip>(WaveStartSfxFallbackResourcePath);
            if (_waveStartSfxClip == null)
            {
                Debug.LogWarning($"[GameScene] Wave start SFX not found. path={WaveStartSfxResourcePath}, fallback={WaveStartSfxFallbackResourcePath}");
            }

            return _waveStartSfxClip;
        }

        // 전투 결과를 계산해 UI에 표시한다.
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

        // 전투 결과를 저장 데이터에 확정 반영한다.
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

        // 결과 팝업에 표시할 메시지를 생성한다.
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
            return $"별 {stars}/3 획득\n남은 생명: {lives}\nBEST 별 {bestStars}/3 / BEST 시간: {bestTimeText}";
        }

        // 보스 이벤트 시스템에 스테이지 클리어를 통지한다.
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

        // 현재 전투의 승리 여부를 판정한다.
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

        // 생명값 기준으로 별 개수를 계산한다.
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

        // 지원병 스펠 사용 요청을 처리한다.
        private void OnSpellReinforceRequested()
        {
            if (_reinforceCooldownLeft > 0f)
            {
                return;
            }

            _reinforceCooldownLeft = GetSpellCooldownDuration(_reinforceSpellConfig, 20f);
            TickSpellCooldowns();
        }

        // 화살비 스펠 사용 요청을 처리한다.
        private void OnSpellRainRequested()
        {
            if (_rainCooldownLeft > 0f)
            {
                return;
            }

            _rainCooldownLeft = GetSpellCooldownDuration(_rainSpellConfig, 30f);
            TickSpellCooldowns();
        }

        // 주문 쿨다운을 진행시키고 UI를 갱신한다.
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

        // 스펠 설정을 로드하고 없으면 런타임 폴백을 생성한다.
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

        // 영웅 설정을 로드하고 없으면 ID 기반 폴백을 반환한다.
        private static HeroConfig ResolveHeroConfig(string heroId, string fallbackHeroId = DefaultHeroId)
        {
            HeroConfig config = ConfigResourcePaths.LoadHeroConfig(heroId);
            if (config != null)
            {
                return config;
            }

            return CreateFallbackHeroConfigById(fallbackHeroId);
        }

        // 영웅 ID에 맞는 기본 능력치 프로파일을 생성한다.
        private static HeroConfig CreateFallbackHeroConfigById(string heroId)
        {
            HeroConfig fallback = ScriptableObject.CreateInstance<HeroConfig>();
            fallback.hideFlags = HideFlags.DontSave;
            string normalizedId = string.IsNullOrWhiteSpace(heroId) ? DefaultHeroId : heroId.Trim();

            fallback.HeroId = normalizedId;
            fallback.DisplayName = normalizedId;
            fallback.StartLevel = 1;
            fallback.MaxLevel = 10;

            switch (normalizedId.ToLowerInvariant())
            {
                case "archerhero":
                    fallback.Role = HeroRole.RangedDps;
                    fallback.DisplayName = "Ranger";
                    fallback.MaxHp = 460f;
                    fallback.MoveSpeed = 3.5f;
                    fallback.AttackDamage = 28f;
                    fallback.DamageMin = 24f;
                    fallback.DamageMax = 33f;
                    fallback.AttackCooldown = 0.72f;
                    fallback.AttackRange = 2.5f;
                    fallback.ArmorPercent = 0.08f;
                    fallback.MagicResistPercent = 0.12f;
                    fallback.HpGrowthPerLevel = 35f;
                    fallback.DamageGrowthPerLevel = 3.2f;
                    fallback.ArmorGrowthPerLevel = 0.003f;
                    fallback.MagicResistGrowthPerLevel = 0.004f;
                    fallback.RespawnSec = 14f;
                    fallback.ActiveSkillId = "multishot";
                    fallback.ActiveCooldownSec = 9f;
                    fallback.ActiveRange = 3f;
                    break;

                case "magehero":
                    fallback.Role = HeroRole.MagicDps;
                    fallback.DisplayName = "Mage";
                    fallback.MaxHp = 420f;
                    fallback.MoveSpeed = 3.2f;
                    fallback.AttackDamage = 33f;
                    fallback.DamageMin = 28f;
                    fallback.DamageMax = 39f;
                    fallback.AttackCooldown = 0.85f;
                    fallback.AttackRange = 2.2f;
                    fallback.ArmorPercent = 0.06f;
                    fallback.MagicResistPercent = 0.2f;
                    fallback.HpGrowthPerLevel = 33f;
                    fallback.DamageGrowthPerLevel = 3.8f;
                    fallback.ArmorGrowthPerLevel = 0.0025f;
                    fallback.MagicResistGrowthPerLevel = 0.0055f;
                    fallback.RespawnSec = 15f;
                    fallback.ActiveSkillId = "arcaneburst";
                    fallback.ActiveCooldownSec = 10.5f;
                    fallback.ActiveRange = 2.8f;
                    break;

                default:
                    fallback.Role = HeroRole.Tank;
                    fallback.DisplayName = "Knight";
                    fallback.HeroId = DefaultHeroId;
                    fallback.MaxHp = 620f;
                    fallback.MoveSpeed = 3f;
                    fallback.AttackDamage = 26f;
                    fallback.DamageMin = 23f;
                    fallback.DamageMax = 31f;
                    fallback.AttackCooldown = 0.9f;
                    fallback.AttackRange = 1.7f;
                    fallback.ArmorPercent = 0.22f;
                    fallback.MagicResistPercent = 0.14f;
                    fallback.HpGrowthPerLevel = 52f;
                    fallback.DamageGrowthPerLevel = 2.6f;
                    fallback.ArmorGrowthPerLevel = 0.006f;
                    fallback.MagicResistGrowthPerLevel = 0.004f;
                    fallback.RespawnSec = 12.5f;
                    fallback.ActiveSkillId = "shieldslam";
                    fallback.ActiveCooldownSec = 8.5f;
                    fallback.ActiveRange = 2.1f;
                    break;
            }

            return fallback;
        }

        // 선택 영웅 설정을 우선 로드하고 폴백을 적용한다.
        private static HeroConfig ResolveSelectedHeroConfig()
        {
            string selectedHeroId = PlayerPrefs.GetString(SelectedHeroIdPlayerPrefsKey, DefaultHeroId);
            if (string.IsNullOrWhiteSpace(selectedHeroId))
            {
                selectedHeroId = DefaultHeroId;
            }

            string selectedPath = ConfigResourcePaths.HeroPrefix + selectedHeroId;
            HeroConfig selectedConfig = ConfigResourcePaths.LoadHeroConfig(selectedHeroId);
            if (selectedConfig != null)
            {
                Debug.Log($"[GameScene] Hero config resolved from selected id. heroId={selectedConfig.HeroId}, path={selectedPath}");
                return selectedConfig;
            }

            if (!selectedHeroId.Equals(DefaultHeroId))
            {
                Debug.Log($"[GameScene] Selected hero config missing: {selectedPath}. Fallback to runtime profile.");
                return CreateFallbackHeroConfigById(selectedHeroId);
            }

            HeroConfig defaultConfig = ResolveHeroConfig(DefaultHeroId, DefaultHeroId);
            if (defaultConfig != null)
            {
                return defaultConfig;
            }

            return CreateFallbackHeroConfigById(DefaultHeroId);
        }

        // 영웅 초상화 스프라이트를 로드한다.
        private static Sprite ResolveHeroPortraitSprite(HeroConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.HeroId))
            {
                return null;
            }

            return Resources.Load<Sprite>(HeroPortraitSpriteResourcePathPrefix + config.HeroId);
        }

        // 스펠 쿨다운 시간을 안전값과 함께 반환한다.
        private static float GetSpellCooldownDuration(SpellConfig config, float fallbackSeconds)
        {
            if (config == null)
            {
                return Mathf.Max(0.1f, fallbackSeconds);
            }

            return Mathf.Max(0.1f, config.CooldownSeconds);
        }

        // 조기 호출 쿨다운 감소량을 반환한다.
        private static float GetEarlyCallCooldownReduction(SpellConfig config)
        {
            if (config == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, config.EarlyCallCooldownReductionSeconds);
        }

        // 남은 대기시간 비율에 맞춰 감소량을 스케일링한다.
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

        // 타워 업그레이드 요청을 처리한다.
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

        // 타워 판매 요청을 처리한다.
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

        // 타워 액션 이후 UI/선택 상태를 갱신한다.
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

        // 집결지 지정 모드를 시작한다.
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

        // 일시정지/배속 상태를 반영해 TimeScale을 적용한다.
        private void ApplyEffectiveTimeScale()
        {
            if (_stateController != null && _stateController.IsPaused)
            {
                Time.timeScale = 0f;
                return;
            }

            Time.timeScale = Mathf.Clamp(_activeTimeScale, 0.1f, FastForwardTimeScale);
        }

        // 웨이브 변경 시 KPI 수집 구간을 전환한다.
        private void OnWaveChangedForKpi(int currentWave, int totalWave)
        {
            FinalizeWaveKpi("WaveChanged");
            BeginWaveKpi(currentWave);
        }

        // 웨이브 KPI 수집을 시작한다.
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

        // 웨이브 KPI를 계산해 로그로 기록한다.
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

        // 웨이브 내 타워 건설 통계를 누적한다.
        private void AccumulateTowerBuildKpi(TowerType towerType)
        {
            if (!_waveKpiActive)
            {
                return;
            }

            _waveTowerBuildCount.TryGetValue(towerType, out int current);
            _waveTowerBuildCount[towerType] = current + 1;
        }

        // 웨이브 내 적 누수 통계를 누적한다.
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

        // 타워 건설 비율 문자열을 생성한다.
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

        // 적 누수 집계 문자열을 생성한다.
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




