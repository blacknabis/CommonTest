using Common;
using Common.Utils;
using Cysharp.Threading.Tasks;
using Kingdom.Audio;
using Kingdom.Game;
using Kingdom.Save;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace Kingdom.App
{
    /// <summary>
    /// Project-level app manager.
    /// </summary>
    public class KingdomAppManager : AppManagerBase<SCENES, KingdomAppManager>
    {
        [Header("Regression Runner")]
        [SerializeField] private bool isSceneLoopRegressionRunning;
        [SerializeField] private bool isWorldMapMetaRegressionRunning;
        [SerializeField] private bool isMetaPersistenceRegressionRunning;
        [SerializeField] private bool isAudioSettingsRegressionRunning;
        [SerializeField] private bool isHeroRoleSmokeRegressionRunning;
        [SerializeField] private bool isCombatIntegrationSmokeRegressionRunning;
        [SerializeField] private bool isBarracksMeleeSmokeRegressionRunning;

        protected override string GetSceneNamespacePrefix()
        {
            return "Kingdom.App.";
        }

        protected override void OnInitializeManagers()
        {
            Debug.Log("[KingdomAppManager] Initializing project specific managers...");
            var saveManager = SaveManager.Instance;
            Debug.Log($"[KingdomAppManager] SaveManager initialized: {saveManager.name}");

            var soundManager = SoundManager.Instance;
            Debug.Log($"[KingdomAppManager] SoundManager initialized: {soundManager.name}");

            AudioSettingsService.InitializeLifecycleHook();
            AudioSettingsService.Load();
            Debug.Log("[KingdomAppManager] AudioSettingsService loaded and applied.");
        }

        protected override void Update()
        {
            base.Update();
        }

        [ContextMenu("Run Scene Loop Regression (30)")]
        private void RunSceneLoopRegression30()
        {
            if (isSceneLoopRegressionRunning)
            {
                Debug.Log("[KingdomAppManager] Scene loop regression is already running.");
                return;
            }

            // 코루틴 대신 UniTask 실행
            RunSceneLoopRegressionAsync(30, 0.35f).Forget();
        }

        [ContextMenu("Run WorldMap Meta Popup Regression (20)")]
        private void RunWorldMapMetaPopupRegression20()
        {
            StartWorldMapMetaPopupRegression(20, 0.08f);
        }

        [ContextMenu("Run Meta Persistence + Hero Apply Regression")]
        private void RunMetaPersistenceAndHeroApplyRegression()
        {
            StartMetaPersistenceAndHeroApplyRegression();
        }

        [ContextMenu("Run Audio Settings Regression")]
        private void RunAudioSettingsRegression()
        {
            StartAudioSettingsRegression();
        }

        [ContextMenu("Run Hero Role Smoke Regression")]
        private void RunHeroRoleSmokeRegression()
        {
            StartHeroRoleSmokeRegression();
        }

        [ContextMenu("Run Combat Integration Smoke Regression")]
        private void RunCombatIntegrationSmokeRegression()
        {
            StartCombatIntegrationSmokeRegression();
        }

        [ContextMenu("Run Barracks Melee Smoke Regression")]
        private void RunBarracksMeleeSmokeRegression()
        {
            StartBarracksMeleeSmokeRegression();
        }

        public bool StartWorldMapMetaPopupRegression(int loopCount = 20, float dwellSeconds = 0.08f)
        {
            if (isWorldMapMetaRegressionRunning)
            {
                Debug.Log("[KingdomAppManager] WorldMap meta popup regression is already running.");
                return false;
            }

            // 코루틴 대신 UniTask 실행
            RunWorldMapMetaPopupRegressionAsync(loopCount, dwellSeconds).Forget();
            return true;
        }

        public bool StartMetaPersistenceAndHeroApplyRegression()
        {
            if (isMetaPersistenceRegressionRunning)
            {
                Debug.Log("[KingdomAppManager] Meta persistence regression is already running.");
                return false;
            }

            // 코루틴 대신 UniTask 실행
            RunMetaPersistenceAndHeroApplyRegressionAsync().Forget();
            return true;
        }

        public bool StartAudioSettingsRegression()
        {
            if (isAudioSettingsRegressionRunning)
            {
                Debug.Log("[KingdomAppManager] Audio settings regression is already running.");
                return false;
            }

            RunAudioSettingsRegressionAsync().Forget();
            return true;
        }

        public bool StartHeroRoleSmokeRegression()
        {
            if (isHeroRoleSmokeRegressionRunning)
            {
                Debug.Log("[KingdomAppManager] Hero role smoke regression is already running.");
                return false;
            }

            RunHeroRoleSmokeRegressionAsync().Forget();
            return true;
        }

        public bool StartCombatIntegrationSmokeRegression()
        {
            if (isCombatIntegrationSmokeRegressionRunning)
            {
                Debug.Log("[KingdomAppManager] Combat integration smoke regression is already running.");
                return false;
            }

            RunCombatIntegrationSmokeRegressionAsync().Forget();
            return true;
        }

        public bool StartBarracksMeleeSmokeRegression()
        {
            if (isBarracksMeleeSmokeRegressionRunning)
            {
                Debug.Log("[KingdomAppManager] Barracks melee smoke regression is already running.");
                return false;
            }

            RunBarracksMeleeSmokeRegressionAsync().Forget();
            return true;
        }

        private async UniTaskVoid RunSceneLoopRegressionAsync(int loopCount, float dwellSeconds)
        {
            isSceneLoopRegressionRunning = true;
            int successCount = 0;
            int failCount = 0;
            float safeDwell = Mathf.Max(0.1f, dwellSeconds);
            long memoryBefore = Profiler.GetTotalAllocatedMemoryLong();
            long memoryPeak = memoryBefore;

            Debug.Log($"[KingdomAppManager] Scene loop regression started. loopCount={loopCount}");

            for (int i = 0; i < Mathf.Max(1, loopCount); i++)
            {
                bool toWorld = await TryChangeAndWaitAsync(SCENES.WorldMapScene, "WorldMapScene");
                if (toWorld) successCount++;
                else failCount++;

                await UniTask.Delay(TimeSpan.FromSeconds(safeDwell), DelayType.UnscaledDeltaTime);

                bool toGame = await TryChangeAndWaitAsync(SCENES.GameScene, "GameScene");
                if (toGame) successCount++;
                else failCount++;

                long currentMemory = Profiler.GetTotalAllocatedMemoryLong();
                if (currentMemory > memoryPeak)
                {
                    memoryPeak = currentMemory;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(safeDwell), DelayType.UnscaledDeltaTime);
            }

            ChangeScene(SCENES.WorldMapScene);
            long memoryAfter = Profiler.GetTotalAllocatedMemoryLong();
            long memoryDelta = memoryAfter - memoryBefore;
            long memoryPeakDelta = memoryPeak - memoryBefore;
            Debug.Log(
                $"[KingdomAppManager] Scene loop regression finished. success={successCount}, fail={failCount}, loops={loopCount}, " +
                $"memBefore={memoryBefore}, memAfter={memoryAfter}, memDelta={memoryDelta}, memPeakDelta={memoryPeakDelta}");
            isSceneLoopRegressionRunning = false;
        }

        private async UniTask<bool> TryChangeAndWaitAsync(SCENES scene, string expectedSceneName)
        {
            ChangeScene(scene);

            const float timeout = 8f;
            float startedAtRealtime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startedAtRealtime < timeout)
            {
                Scene active = SceneManager.GetActiveScene();
                if (string.Equals(active.name, expectedSceneName, System.StringComparison.Ordinal))
                {
                    return true;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            Debug.Log($"[KingdomAppManager] Scene loop step timeout. expected={expectedSceneName}, active={SceneManager.GetActiveScene().name}");
            return false;
        }

        private async UniTask<bool> TryChangeAndWaitResilientAsync(SCENES scene, string expectedSceneName)
        {
            if (await TryChangeAndWaitAsync(scene, expectedSceneName))
            {
                return true;
            }

            Debug.Log($"[KingdomAppManager] Scene change fallback load. expected={expectedSceneName}");
            AsyncOperation op = SceneManager.LoadSceneAsync(expectedSceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.Log($"[KingdomAppManager] Scene fallback load failed. expected={expectedSceneName}");
                return false;
            }

            const float timeout = 8f;
            float startedAtRealtime = Time.realtimeSinceStartup;
            while (!op.isDone && Time.realtimeSinceStartup - startedAtRealtime < timeout)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            bool loaded = string.Equals(
                SceneManager.GetActiveScene().name,
                expectedSceneName,
                StringComparison.Ordinal);
            if (!loaded)
            {
                Debug.Log($"[KingdomAppManager] Scene fallback load timeout. expected={expectedSceneName}, active={SceneManager.GetActiveScene().name}");
            }

            return loaded;
        }

        private async UniTaskVoid RunWorldMapMetaPopupRegressionAsync(int loopCount, float dwellSeconds)
        {
            isWorldMapMetaRegressionRunning = true;
            int successCount = 0;
            int failCount = 0;
            float safeDwell = Mathf.Max(0.03f, dwellSeconds);

            bool toWorld = await TryChangeAndWaitAsync(SCENES.WorldMapScene, "WorldMapScene");
            if (!toWorld)
            {
                Debug.Log("[KingdomAppManager] WorldMap meta popup regression aborted. Failed to enter WorldMapScene.");
                isWorldMapMetaRegressionRunning = false;
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.Update);
            WorldMapView worldMapView = FindFirstObjectByType<WorldMapView>();
            if (worldMapView == null)
            {
                Debug.Log("[KingdomAppManager] WorldMap meta popup regression aborted. WorldMapView not found.");
                isWorldMapMetaRegressionRunning = false;
                return;
            }

            Debug.Log($"[KingdomAppManager] WorldMap meta popup regression started. loopCount={loopCount}");

            int loops = Mathf.Max(1, loopCount);
            for (int i = 0; i < loops; i++)
            {
                worldMapView.OpenUpgradesPopup();
                await UniTask.Delay(TimeSpan.FromSeconds(safeDwell), DelayType.UnscaledDeltaTime);
                if (worldMapView.CloseOverlay()) successCount++;
                else failCount++;

                worldMapView.OpenHeroRoomPopup();
                await UniTask.Delay(TimeSpan.FromSeconds(safeDwell), DelayType.UnscaledDeltaTime);
                if (worldMapView.CloseOverlay()) successCount++;
                else failCount++;

                // 오디오 옵션 팝업 회귀 검증: 오픈/클로즈 루프 정상 동작 확인
                worldMapView.OpenAudioOptionsPopup();
                await UniTask.Delay(TimeSpan.FromSeconds(safeDwell), DelayType.UnscaledDeltaTime);
                if (worldMapView.CloseOverlay()) successCount++;
                else failCount++;
            }

            Debug.Log($"[KingdomAppManager] WorldMap meta popup regression finished. success={successCount}, fail={failCount}, loops={loops}");
            isWorldMapMetaRegressionRunning = false;
        }

        private async UniTaskVoid RunMetaPersistenceAndHeroApplyRegressionAsync()
        {
            isMetaPersistenceRegressionRunning = true;
            int successCount = 0;
            int failCount = 0;

            const string selectedHeroKey = "Kingdom.Hero.SelectedHeroId";
            const string skillKey = "Kingdom.SkillTree.SkillLevel.archers_t1";

            string previousHeroId = PlayerPrefs.GetString(selectedHeroKey, string.Empty);
            int previousSkillLevel = PlayerPrefs.GetInt(skillKey, 0);
            string testHeroId = ResolveTestHeroId();
            int testSkillLevel = Mathf.Max(0, previousSkillLevel) + 1;

            PlayerPrefs.SetString(selectedHeroKey, testHeroId);
            PlayerPrefs.SetInt(skillKey, testSkillLevel);
            PlayerPrefs.Save();

            if (PlayerPrefs.GetString(selectedHeroKey, string.Empty) == testHeroId)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }

            if (PlayerPrefs.GetInt(skillKey, -1) == testSkillLevel)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }

            bool toGame = await TryChangeAndWaitAsync(SCENES.GameScene, "GameScene");
            if (toGame) successCount++;
            else failCount++;

            await UniTask.Yield(PlayerLoopTiming.Update);
            HeroController heroController = FindFirstObjectByType<HeroController>();
            string runtimeHeroId = heroController != null ? heroController.CurrentHeroId : string.Empty;
            if (heroController != null && string.Equals(runtimeHeroId, testHeroId, System.StringComparison.Ordinal))
            {
                successCount++;
            }
            else
            {
                failCount++;
            }

            bool toWorld = await TryChangeAndWaitAsync(SCENES.WorldMapScene, "WorldMapScene");
            if (toWorld) successCount++;
            else failCount++;

            if (PlayerPrefs.GetString(selectedHeroKey, string.Empty) == testHeroId)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }

            if (PlayerPrefs.GetInt(skillKey, -1) == testSkillLevel)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }

            Debug.Log(
                $"[KingdomAppManager] Meta persistence regression finished. success={successCount}, fail={failCount}, " +
                $"heroTest={testHeroId}, heroRuntime={runtimeHeroId}, skillKey={skillKey}, skillLevel={testSkillLevel}, " +
                $"prevHero={previousHeroId}, prevSkill={previousSkillLevel}");

            isMetaPersistenceRegressionRunning = false;
        }

        private async UniTaskVoid RunAudioSettingsRegressionAsync()
        {
            isAudioSettingsRegressionRunning = true;
            int successCount = 0;
            int failCount = 0;

            // 기존 설정 백업
            float prevBgm = PlayerPrefs.GetFloat(AudioSettingsKeys.BgmVolume, AudioSettingsKeys.DefaultBgmVolume);
            float prevSfx = PlayerPrefs.GetFloat(AudioSettingsKeys.SfxVolume, AudioSettingsKeys.DefaultSfxVolume);
            bool prevBgmMuted = PlayerPrefs.GetInt(AudioSettingsKeys.BgmMuted, AudioSettingsKeys.DefaultBgmMuted ? 1 : 0) != 0;
            bool prevSfxMuted = PlayerPrefs.GetInt(AudioSettingsKeys.SfxMuted, AudioSettingsKeys.DefaultSfxMuted ? 1 : 0) != 0;

            // 테스트 값 적용
            const float testBgm = 0.23f;
            const float testSfx = 0.67f;
            const bool testBgmMuted = true;
            const bool testSfxMuted = false;

            AudioSettingsService.SetBgmVolume(testBgm);
            AudioSettingsService.SetSfxVolume(testSfx);
            AudioSettingsService.SetBgmMuted(testBgmMuted);
            AudioSettingsService.SetSfxMuted(testSfxMuted);

            await UniTask.Yield(PlayerLoopTiming.Update);

            // 1) PlayerPrefs 저장 검증
            bool savedBgm = Mathf.Abs(PlayerPrefs.GetFloat(AudioSettingsKeys.BgmVolume, -1f) - testBgm) < 0.001f;
            bool savedSfx = Mathf.Abs(PlayerPrefs.GetFloat(AudioSettingsKeys.SfxVolume, -1f) - testSfx) < 0.001f;
            bool savedBgmMuted = (PlayerPrefs.GetInt(AudioSettingsKeys.BgmMuted, 0) != 0) == testBgmMuted;
            bool savedSfxMuted = (PlayerPrefs.GetInt(AudioSettingsKeys.SfxMuted, 0) != 0) == testSfxMuted;

            if (savedBgm) successCount++; else failCount++;
            if (savedSfx) successCount++; else failCount++;
            if (savedBgmMuted) successCount++; else failCount++;
            if (savedSfxMuted) successCount++; else failCount++;

            // 2) 런타임 반영 검증
            var audio = AudioHelper.Instance;
            bool runtimeBgm = audio != null && Mathf.Abs(audio.BGMVolume - testBgm) < 0.001f;
            bool runtimeSfx = audio != null && Mathf.Abs(audio.SFXVolume - testSfx) < 0.001f;
            bool runtimeBgmMuted = audio != null && audio.IsBGMMuted == testBgmMuted;
            bool runtimeSfxMuted = audio != null && audio.IsSFXMuted == testSfxMuted;

            if (runtimeBgm) successCount++; else failCount++;
            if (runtimeSfx) successCount++; else failCount++;
            if (runtimeBgmMuted) successCount++; else failCount++;
            if (runtimeSfxMuted) successCount++; else failCount++;

            // 3) 씬 왕복 후 값 유지 검증
            bool toWorld = await TryChangeAndWaitAsync(SCENES.WorldMapScene, "WorldMapScene");
            if (toWorld) successCount++; else failCount++;

            bool toGame = await TryChangeAndWaitAsync(SCENES.GameScene, "GameScene");
            if (toGame) successCount++; else failCount++;

            bool persistedAfterSceneChange =
                Mathf.Abs(PlayerPrefs.GetFloat(AudioSettingsKeys.BgmVolume, -1f) - testBgm) < 0.001f &&
                Mathf.Abs(PlayerPrefs.GetFloat(AudioSettingsKeys.SfxVolume, -1f) - testSfx) < 0.001f &&
                ((PlayerPrefs.GetInt(AudioSettingsKeys.BgmMuted, 0) != 0) == testBgmMuted) &&
                ((PlayerPrefs.GetInt(AudioSettingsKeys.SfxMuted, 0) != 0) == testSfxMuted);

            if (persistedAfterSceneChange) successCount++; else failCount++;

            Debug.Log($"[KingdomAppManager] Audio settings regression finished. success={successCount}, fail={failCount}, " +
                      $"testBgm={testBgm}, testSfx={testSfx}, testBgmMuted={testBgmMuted}, testSfxMuted={testSfxMuted}");

            // 원복
            AudioSettingsService.SetBgmVolume(prevBgm);
            AudioSettingsService.SetSfxVolume(prevSfx);
            AudioSettingsService.SetBgmMuted(prevBgmMuted);
            AudioSettingsService.SetSfxMuted(prevSfxMuted);

            isAudioSettingsRegressionRunning = false;
        }

        private async UniTaskVoid RunHeroRoleSmokeRegressionAsync()
        {
            isHeroRoleSmokeRegressionRunning = true;
            int successCount = 0;
            int failCount = 0;
            const string selectedHeroKey = "Kingdom.Hero.SelectedHeroId";
            string previousHeroId = PlayerPrefs.GetString(selectedHeroKey, string.Empty);
            string[] candidates = { "DefaultHero", "ArcherHero", "MageHero" };
            int testedHeroCount = 0;

            try
            {
                for (int i = 0; i < candidates.Length; i++)
                {
                    string heroId = candidates[i];
                    HeroConfig config = Resources.Load<HeroConfig>($"Data/HeroConfigs/{heroId}");
                    if (config == null)
                    {
                        failCount++;
                        Debug.Log($"[KingdomAppManager] Hero role smoke: missing HeroConfig for {heroId}");
                        continue;
                    }

                    testedHeroCount++;
                    PlayerPrefs.SetString(selectedHeroKey, heroId);
                    PlayerPrefs.Save();

                    if (PlayerPrefs.GetString(selectedHeroKey, string.Empty) == heroId)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }

                    bool toGame = await TryChangeAndWaitResilientAsync(SCENES.GameScene, "GameScene");
                    if (toGame) successCount++;
                    else failCount++;

                    await UniTask.Yield(PlayerLoopTiming.Update);
                    HeroController heroController = FindFirstObjectByType<HeroController>();
                    string runtimeHeroId = heroController != null ? heroController.CurrentHeroId : string.Empty;
                    if (heroController != null && string.Equals(runtimeHeroId, heroId, StringComparison.Ordinal))
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        Debug.Log($"[KingdomAppManager] Hero role smoke mismatch. expected={heroId}, runtime={runtimeHeroId}");
                    }

                    bool toWorld = await TryChangeAndWaitResilientAsync(SCENES.WorldMapScene, "WorldMapScene");
                    if (toWorld) successCount++;
                    else failCount++;
                }
            }
            finally
            {
                PlayerPrefs.SetString(selectedHeroKey, previousHeroId);
                PlayerPrefs.Save();
                isHeroRoleSmokeRegressionRunning = false;
            }

            Debug.Log(
                $"[KingdomAppManager] Hero role smoke regression finished. success={successCount}, fail={failCount}, testedHeroes={testedHeroCount}, prevHero={previousHeroId}");
        }

        private async UniTaskVoid RunCombatIntegrationSmokeRegressionAsync()
        {
            isCombatIntegrationSmokeRegressionRunning = true;
            int successCount = 0;
            int failCount = 0;
            const string selectedHeroKey = "Kingdom.Hero.SelectedHeroId";
            string previousHeroId = PlayerPrefs.GetString(selectedHeroKey, string.Empty);

            IntegrationScenario[] scenarios =
            {
                new IntegrationScenario("TankAndSpank", "DefaultHero", TowerType.Barracks, TowerType.Archer),
            };

            try
            {
                for (int i = 0; i < scenarios.Length; i++)
                {
                    IntegrationScenario scenario = scenarios[i];
                    if (!await RunSingleIntegrationScenarioAsync(scenario))
                    {
                        failCount++;
                        continue;
                    }

                    successCount++;
                }
            }
            finally
            {
                PlayerPrefs.SetString(selectedHeroKey, previousHeroId);
                PlayerPrefs.Save();
                isCombatIntegrationSmokeRegressionRunning = false;
            }

            Debug.Log(
                $"[KingdomAppManager] Combat integration smoke regression finished. success={successCount}, fail={failCount}, scenarios={scenarios.Length}, prevHero={previousHeroId}");
        }

        private async UniTask<bool> RunSingleIntegrationScenarioAsync(IntegrationScenario scenario)
        {
            int localPass = 0;
            int localFail = 0;
            const string selectedHeroKey = "Kingdom.Hero.SelectedHeroId";

            PlayerPrefs.SetString(selectedHeroKey, scenario.HeroId);
            PlayerPrefs.Save();
            if (PlayerPrefs.GetString(selectedHeroKey, string.Empty) == scenario.HeroId)
            {
                localPass++;
            }
            else
            {
                localFail++;
            }

            bool toGame = await TryChangeAndWaitResilientAsync(SCENES.GameScene, "GameScene");
            if (toGame)
            {
                localPass++;
            }
            else
            {
                localFail++;
            }

            await UniTask.Yield(PlayerLoopTiming.Update);

            TowerManager towerManager = FindFirstObjectByType<TowerManager>();
            InGameEconomyManager economyManager = FindFirstObjectByType<InGameEconomyManager>();
            SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();
            GameStateController stateController = FindFirstObjectByType<GameStateController>();
            HeroController heroController = FindFirstObjectByType<HeroController>();

            bool refsOk = towerManager != null &&
                          economyManager != null &&
                          spawnManager != null &&
                          stateController != null &&
                          heroController != null;
            if (refsOk)
            {
                localPass++;
            }
            else
            {
                localFail++;
            }

            if (heroController != null && string.Equals(heroController.CurrentHeroId, scenario.HeroId, StringComparison.Ordinal))
            {
                localPass++;
            }
            else
            {
                localFail++;
            }

            if (!refsOk)
            {
                bool backToWorld = await TryChangeAndWaitResilientAsync(SCENES.WorldMapScene, "WorldMapScene");
                if (backToWorld)
                {
                    localPass++;
                }
                else
                {
                    localFail++;
                }

                Debug.Log(
                    $"[KingdomAppManager] Combat integration scenario finished. name={scenario.Name}, pass={localPass}, fail={localFail}, refsOk={refsOk}");
                return localFail == 0;
            }

            economyManager.AddGold(3000);
            int builtCount = 0;
            if (towerManager.TryBuildNextTower(scenario.PrimaryTower))
            {
                builtCount++;
            }

            if (towerManager.TryBuildNextTower(scenario.SecondaryTower))
            {
                builtCount++;
            }

            if (builtCount > 0)
            {
                localPass++;
            }
            else
            {
                localFail++;
            }

            int spawnedCount = 0;
            int killedCount = 0;
            void OnEnemySpawned(EnemyRuntime _, EnemyConfig __) => spawnedCount++;
            void OnEnemyKilled(EnemyRuntime _, EnemyConfig __) => killedCount++;

            spawnManager.EnemySpawned += OnEnemySpawned;
            spawnManager.EnemyKilled += OnEnemyKilled;

            Time.timeScale = 1f;
            const float timeout = 12f;
            float startedAtRealtime = Time.realtimeSinceStartup;
            bool reachedCombat = false;
            while (Time.realtimeSinceStartup - startedAtRealtime < timeout)
            {
                if (stateController.CurrentState == GameFlowState.WaveReady)
                {
                    stateController.TryEarlyCallNextWave();
                }

                if (spawnedCount > 0 && killedCount > 0)
                {
                    reachedCombat = true;
                    break;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            spawnManager.EnemySpawned -= OnEnemySpawned;
            spawnManager.EnemyKilled -= OnEnemyKilled;

            if (reachedCombat)
            {
                localPass++;
            }
            else
            {
                localFail++;
            }

            if (spawnedCount > 0 && killedCount > 0)
            {
                localPass++;
            }
            else
            {
                localFail++;
            }

            bool toWorld = await TryChangeAndWaitResilientAsync(SCENES.WorldMapScene, "WorldMapScene");
            if (toWorld)
            {
                localPass++;
            }
            else
            {
                localFail++;
            }

            Debug.Log(
                $"[KingdomAppManager] Combat integration scenario finished. name={scenario.Name}, hero={scenario.HeroId}, " +
                $"towerA={scenario.PrimaryTower}, towerB={scenario.SecondaryTower}, built={builtCount}, spawned={spawnedCount}, killed={killedCount}, " +
                $"combat={reachedCombat}, pass={localPass}, fail={localFail}");

            return localFail == 0;
        }

        private async UniTaskVoid RunBarracksMeleeSmokeRegressionAsync()
        {
            isBarracksMeleeSmokeRegressionRunning = true;
            int successCount = 0;
            int failCount = 0;
            int spawnedCount = 0;
            int killedCount = 0;
            int enemyAttackEvents = 0;
            int rallySetSuccessCount = 0;
            int maxDeadSoldier = 0;
            int maxRespawningSoldier = 0;
            int maxBlockingSoldier = 0;
            int maxAssignedEnemy = 0;
            int totalSoldierDeaths = 0;
            int totalSoldierRespawns = 0;

            SpawnManager spawnManager = null;
            Action<EnemyRuntime, float> onEnemyAttack = null;
            Action<EnemyRuntime, EnemyConfig> onEnemySpawned = null;
            Action<EnemyRuntime, EnemyConfig> onEnemyKilled = null;
            Action<EnemyRuntime, EnemyConfig> onEnemyReachedGoal = null;

            HashSet<int> trackedEnemyIds = new HashSet<int>();

            try
            {
                bool toGame = await TryChangeAndWaitResilientAsync(SCENES.GameScene, "GameScene");
                if (toGame)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    Debug.Log("[KingdomAppManager] Barracks melee smoke aborted. Failed to enter GameScene.");
                    return;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);

                TowerManager towerManager = FindFirstObjectByType<TowerManager>();
                InGameEconomyManager economyManager = FindFirstObjectByType<InGameEconomyManager>();
                spawnManager = FindFirstObjectByType<SpawnManager>();
                GameStateController stateController = FindFirstObjectByType<GameStateController>();

                bool refsOk = towerManager != null && economyManager != null && spawnManager != null && stateController != null;
                if (refsOk)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    Debug.Log("[KingdomAppManager] Barracks melee smoke aborted. Missing scene references.");
                    return;
                }

                economyManager.AddGold(3000);
                bool built = towerManager.TryBuildNextTower(TowerType.Barracks);
                if (built)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    Debug.Log("[KingdomAppManager] Barracks melee smoke aborted. Failed to build Barracks.");
                    return;
                }

                int barracksTowerId = FindTowerIdByType(towerManager, TowerType.Barracks);
                if (barracksTowerId > 0)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                    Debug.Log("[KingdomAppManager] Barracks melee smoke aborted. Barracks tower id not found.");
                    return;
                }

                onEnemyAttack = (_, damage) =>
                {
                    if (damage > 0f)
                    {
                        enemyAttackEvents++;
                    }
                };

                onEnemySpawned = (enemy, _) =>
                {
                    spawnedCount++;
                    if (enemy == null)
                    {
                        return;
                    }

                    int id = enemy.GetInstanceID();
                    if (trackedEnemyIds.Add(id))
                    {
                        enemy.AttackPerformed += onEnemyAttack;
                    }
                };

                void DetachEnemyAttack(EnemyRuntime enemy)
                {
                    if (enemy == null)
                    {
                        return;
                    }

                    int id = enemy.GetInstanceID();
                    if (!trackedEnemyIds.Remove(id))
                    {
                        return;
                    }

                    enemy.AttackPerformed -= onEnemyAttack;
                }

                onEnemyKilled = (enemy, _) =>
                {
                    killedCount++;
                    DetachEnemyAttack(enemy);
                };

                onEnemyReachedGoal = (enemy, _) =>
                {
                    DetachEnemyAttack(enemy);
                };

                spawnManager.EnemySpawned += onEnemySpawned;
                spawnManager.EnemyKilled += onEnemyKilled;
                spawnManager.EnemyReachedGoal += onEnemyReachedGoal;

                if (towerManager.TryGetTowerActionInfo(barracksTowerId, out var infoA))
                {
                    Vector3 targetA = infoA.WorldPosition + new Vector3(0.7f, 0.15f, 0f);
                    if (towerManager.TrySetRallyPoint(barracksTowerId, targetA))
                    {
                        rallySetSuccessCount++;
                    }
                }

                Time.timeScale = 1f;
                const float timeout = 26f;
                float startedAtRealtime = Time.realtimeSinceStartup;
                float nextRallyMoveAt = Time.realtimeSinceStartup + 2.5f;
                int rallyMoveStep = 0;

                while (Time.realtimeSinceStartup - startedAtRealtime < timeout)
                {
                    if (stateController.CurrentState == GameFlowState.WaveReady)
                    {
                        stateController.TryEarlyCallNextWave();
                    }

                    if (Time.realtimeSinceStartup >= nextRallyMoveAt &&
                        towerManager.TryGetTowerActionInfo(barracksTowerId, out var infoLoop))
                    {
                        Vector3 moveOffset = rallyMoveStep % 2 == 0
                            ? new Vector3(0.65f, 0.30f, 0f)
                            : new Vector3(-0.65f, 0.20f, 0f);
                        if (towerManager.TrySetRallyPoint(barracksTowerId, infoLoop.WorldPosition + moveOffset))
                        {
                            rallySetSuccessCount++;
                        }

                        rallyMoveStep++;
                        nextRallyMoveAt = Time.realtimeSinceStartup + 2.5f;
                    }

                    if (towerManager.TryGetBarracksDebugInfo(barracksTowerId, out var debug))
                    {
                        maxDeadSoldier = Mathf.Max(maxDeadSoldier, debug.SoldierDead);
                        maxRespawningSoldier = Mathf.Max(maxRespawningSoldier, debug.SoldierRespawning);
                        maxBlockingSoldier = Mathf.Max(maxBlockingSoldier, debug.SoldierBlocking);
                        maxAssignedEnemy = Mathf.Max(maxAssignedEnemy, debug.AssignedEnemies);
                        totalSoldierDeaths = Mathf.Max(totalSoldierDeaths, debug.TotalDeaths);
                        totalSoldierRespawns = Mathf.Max(totalSoldierRespawns, debug.TotalRespawns);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update);
                }

                // Respawn completion can occur near the tail of the combat window.
                // Give a short grace period to observe at least one completed respawn.
                if (totalSoldierDeaths > 0 && totalSoldierRespawns <= 0)
                {
                    const float respawnGraceSec = 10f;
                    float graceStarted = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - graceStarted < respawnGraceSec)
                    {
                        if (!towerManager.TryGetBarracksDebugInfo(barracksTowerId, out var debug))
                        {
                            break;
                        }

                        maxDeadSoldier = Mathf.Max(maxDeadSoldier, debug.SoldierDead);
                        maxRespawningSoldier = Mathf.Max(maxRespawningSoldier, debug.SoldierRespawning);
                        maxBlockingSoldier = Mathf.Max(maxBlockingSoldier, debug.SoldierBlocking);
                        maxAssignedEnemy = Mathf.Max(maxAssignedEnemy, debug.AssignedEnemies);
                        totalSoldierDeaths = Mathf.Max(totalSoldierDeaths, debug.TotalDeaths);
                        totalSoldierRespawns = Mathf.Max(totalSoldierRespawns, debug.TotalRespawns);

                        if (totalSoldierRespawns > 0)
                        {
                            break;
                        }

                        await UniTask.Yield(PlayerLoopTiming.Update);
                    }
                }

                if (spawnedCount > 0)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }

                if (enemyAttackEvents > 0)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }

                if (totalSoldierDeaths > 0 && maxDeadSoldier > 0)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }

                if (totalSoldierRespawns > 0 && maxRespawningSoldier > 0)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }

                if (rallySetSuccessCount >= 2)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }

                bool toWorld = await TryChangeAndWaitResilientAsync(SCENES.WorldMapScene, "WorldMapScene");
                if (toWorld)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }

                Debug.Log(
                    $"[KingdomAppManager] Barracks melee smoke regression finished. success={successCount}, fail={failCount}, " +
                    $"spawned={spawnedCount}, killed={killedCount}, enemyAttackEvents={enemyAttackEvents}, " +
                    $"rallySet={rallySetSuccessCount}, maxBlocking={maxBlockingSoldier}, maxAssigned={maxAssignedEnemy}, " +
                    $"maxDead={maxDeadSoldier}, maxRespawning={maxRespawningSoldier}, totalDeaths={totalSoldierDeaths}, totalRespawns={totalSoldierRespawns}");
            }
            finally
            {
                if (spawnManager != null)
                {
                    if (onEnemySpawned != null) spawnManager.EnemySpawned -= onEnemySpawned;
                    if (onEnemyKilled != null) spawnManager.EnemyKilled -= onEnemyKilled;
                    if (onEnemyReachedGoal != null) spawnManager.EnemyReachedGoal -= onEnemyReachedGoal;
                }

                isBarracksMeleeSmokeRegressionRunning = false;
            }
        }

        private static int FindTowerIdByType(TowerManager towerManager, TowerType towerType)
        {
            if (towerManager == null)
            {
                return -1;
            }

            const int maxProbe = 64;
            for (int id = 1; id <= maxProbe; id++)
            {
                if (!towerManager.TryGetTowerActionInfo(id, out var info))
                {
                    continue;
                }

                if (info.TowerType == towerType)
                {
                    return id;
                }
            }

            return -1;
        }

        private readonly struct IntegrationScenario
        {
            public readonly string Name;
            public readonly string HeroId;
            public readonly TowerType PrimaryTower;
            public readonly TowerType SecondaryTower;

            public IntegrationScenario(string name, string heroId, TowerType primaryTower, TowerType secondaryTower)
            {
                Name = name;
                HeroId = heroId;
                PrimaryTower = primaryTower;
                SecondaryTower = secondaryTower;
            }
        }


        private static string ResolveTestHeroId()
        {
            string[] candidates = { "DefaultHero", "ArcherHero", "MageHero" };
            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                HeroConfig config = Resources.Load<HeroConfig>($"Data/HeroConfigs/{candidate}");
                if (config != null)
                {
                    return candidate;
                }
            }

            return "DefaultHero";
        }
    }
}
