using System.Collections;
using Common;
using Common.Utils;
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
                Debug.LogWarning("[KingdomAppManager] Scene loop regression is already running.");
                return;
            }

            StartCoroutine(CoRunSceneLoopRegression(30, 0.35f));
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

            Debug.LogWarning($"[KingdomAppManager] Scene loop step timeout. expected={expectedSceneName}, active={SceneManager.GetActiveScene().name}");
            onDone?.Invoke(false);
        }
    }
}
