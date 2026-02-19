using Common;
using Common.Utils;
using Cysharp.Threading.Tasks;
using Kingdom.Audio;
using Kingdom.Game;
using Kingdom.Save;
using System;
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
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                Scene active = SceneManager.GetActiveScene();
                if (string.Equals(active.name, expectedSceneName, System.StringComparison.Ordinal))
                {
                    return true;
                }

                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            Debug.Log($"[KingdomAppManager] Scene loop step timeout. expected={expectedSceneName}, active={SceneManager.GetActiveScene().name}");
            return false;
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
