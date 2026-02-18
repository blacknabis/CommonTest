using System.Collections;
using Common;
using Common.Utils;
using Kingdom.Game;
using Kingdom.Save;
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

            StartCoroutine(CoRunSceneLoopRegression(30, 0.35f));
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

        public bool StartWorldMapMetaPopupRegression(int loopCount = 20, float dwellSeconds = 0.08f)
        {
            if (isWorldMapMetaRegressionRunning)
            {
                Debug.Log("[KingdomAppManager] WorldMap meta popup regression is already running.");
                return false;
            }

            StartCoroutine(CoRunWorldMapMetaPopupRegression(loopCount, dwellSeconds));
            return true;
        }

        public bool StartMetaPersistenceAndHeroApplyRegression()
        {
            if (isMetaPersistenceRegressionRunning)
            {
                Debug.Log("[KingdomAppManager] Meta persistence regression is already running.");
                return false;
            }

            StartCoroutine(CoRunMetaPersistenceAndHeroApplyRegression());
            return true;
        }

        private IEnumerator CoRunSceneLoopRegression(int loopCount, float dwellSeconds)
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
                bool toWorld = false;
                yield return CoTryChangeAndWait(SCENES.WorldMapScene, "WorldMapScene", value => toWorld = value);
                if (toWorld) successCount++;
                else failCount++;

                yield return new WaitForSecondsRealtime(safeDwell);

                bool toGame = false;
                yield return CoTryChangeAndWait(SCENES.GameScene, "GameScene", value => toGame = value);
                if (toGame) successCount++;
                else failCount++;

                long currentMemory = Profiler.GetTotalAllocatedMemoryLong();
                if (currentMemory > memoryPeak)
                {
                    memoryPeak = currentMemory;
                }

                yield return new WaitForSecondsRealtime(safeDwell);
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

        private IEnumerator CoTryChangeAndWait(SCENES scene, string expectedSceneName, System.Action<bool> onDone)
        {
            ChangeScene(scene);

            const float timeout = 8f;
            float elapsed = 0f;
            while (elapsed < timeout)
            {
                Scene active = SceneManager.GetActiveScene();
                if (string.Equals(active.name, expectedSceneName, System.StringComparison.Ordinal))
                {
                    onDone?.Invoke(true);
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.Log($"[KingdomAppManager] Scene loop step timeout. expected={expectedSceneName}, active={SceneManager.GetActiveScene().name}");
            onDone?.Invoke(false);
        }

        private IEnumerator CoRunWorldMapMetaPopupRegression(int loopCount, float dwellSeconds)
        {
            isWorldMapMetaRegressionRunning = true;
            int successCount = 0;
            int failCount = 0;
            float safeDwell = Mathf.Max(0.03f, dwellSeconds);

            bool toWorld = false;
            yield return CoTryChangeAndWait(SCENES.WorldMapScene, "WorldMapScene", value => toWorld = value);
            if (!toWorld)
            {
                Debug.Log("[KingdomAppManager] WorldMap meta popup regression aborted. Failed to enter WorldMapScene.");
                isWorldMapMetaRegressionRunning = false;
                yield break;
            }

            yield return null;
            WorldMapView worldMapView = FindFirstObjectByType<WorldMapView>();
            if (worldMapView == null)
            {
                Debug.Log("[KingdomAppManager] WorldMap meta popup regression aborted. WorldMapView not found.");
                isWorldMapMetaRegressionRunning = false;
                yield break;
            }

            Debug.Log($"[KingdomAppManager] WorldMap meta popup regression started. loopCount={loopCount}");

            int loops = Mathf.Max(1, loopCount);
            for (int i = 0; i < loops; i++)
            {
                worldMapView.OpenUpgradesPopup();
                yield return new WaitForSecondsRealtime(safeDwell);
                if (worldMapView.CloseOverlay()) successCount++;
                else failCount++;

                worldMapView.OpenHeroRoomPopup();
                yield return new WaitForSecondsRealtime(safeDwell);
                if (worldMapView.CloseOverlay()) successCount++;
                else failCount++;
            }

            Debug.Log($"[KingdomAppManager] WorldMap meta popup regression finished. success={successCount}, fail={failCount}, loops={loops}");
            isWorldMapMetaRegressionRunning = false;
        }

        private IEnumerator CoRunMetaPersistenceAndHeroApplyRegression()
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

            bool toGame = false;
            yield return CoTryChangeAndWait(SCENES.GameScene, "GameScene", value => toGame = value);
            if (toGame) successCount++;
            else failCount++;

            yield return null;
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

            bool toWorld = false;
            yield return CoTryChangeAndWait(SCENES.WorldMapScene, "WorldMapScene", value => toWorld = value);
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
