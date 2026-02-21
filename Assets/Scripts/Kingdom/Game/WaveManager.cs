using UnityEngine;
using Kingdom.App;
using Kingdom.WorldMap;

namespace Kingdom.Game
{
    /// <summary>
    /// FSM과 SpawnManager를 연결해 웨이브 스폰을 실행하는 최소 매니저.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private WaveConfig waveConfig;
        [SerializeField] private GameStateController stateController;
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private PathManager pathManager;

        private int _aliveEnemyCount;
        private bool _spawnCompletedForCurrentWave;
        private int _spawnStartedWaveIndex;
        private SpawnManager _subscribedSpawnManager;
        private static bool _fallbackConfigLogged;

        public int AliveEnemyCount => _aliveEnemyCount;

        private void Awake()
        {
            ResolveReferences(includeWaveConfig: false);
        }

        private void OnEnable()
        {
            RebindEvents();
        }

        private void OnDisable()
        {
            if (stateController != null)
            {
                stateController.StateChanged -= OnStateChanged;
            }

            if (_subscribedSpawnManager != null)
            {
                _subscribedSpawnManager.EnemySpawned -= OnEnemySpawned;
                _subscribedSpawnManager.EnemyKilled -= OnEnemyRemoved;
                _subscribedSpawnManager.EnemyReachedGoal -= OnEnemyRemoved;
                _subscribedSpawnManager.WaveSpawnCompleted -= OnWaveSpawnCompleted;
                _subscribedSpawnManager = null;
            }
        }

        private void Update()
        {
            if (stateController == null || stateController.CurrentState != GameFlowState.WaveRunning)
            {
                return;
            }

            if (_spawnCompletedForCurrentWave && _aliveEnemyCount <= 0)
            {
                _spawnCompletedForCurrentWave = false;
                Debug.Log($"[WaveManager] Wave {stateController.CurrentWave} all enemies removed.");

                if (stateController != null)
                {
                    stateController.TryCompleteCurrentWave();
                }
            }
        }

        public void Configure(
            WaveConfig config,
            GameStateController controller,
            SpawnManager spawn,
            PathManager path)
        {
            waveConfig = config;
            stateController = controller;
            spawnManager = spawn;
            pathManager = path;
            RebindEvents();
        }

        private void OnStateChanged(GameFlowState state)
        {
            if (state != GameFlowState.WaveRunning)
            {
                if (state == GameFlowState.Prepare || state == GameFlowState.Result)
                {
                    _spawnStartedWaveIndex = 0;
                }
                return;
            }

            ResolveReferences(includeWaveConfig: true);

            if (waveConfig == null || spawnManager == null || pathManager == null)
            {
                Debug.LogWarning(
                    $"[WaveManager] Missing references. waveConfig:{(waveConfig != null)} spawnManager:{(spawnManager != null)} pathManager:{(pathManager != null)}");
                return;
            }

            int waveIndex = Mathf.Max(1, stateController.CurrentWave) - 1;
            if (_spawnStartedWaveIndex == waveIndex + 1)
            {
                return;
            }

            if (waveConfig.Waves == null || waveIndex >= waveConfig.Waves.Count)
            {
                Debug.LogWarning($"[WaveManager] Wave index out of range: {waveIndex}. Force result.");
                stateController.ForceResult();
                return;
            }

            _spawnCompletedForCurrentWave = false;
            _spawnStartedWaveIndex = waveIndex + 1;
            spawnManager.SpawnWave(waveConfig.Waves[waveIndex], pathManager);
            Debug.Log($"[WaveManager] Spawn started for wave {waveIndex + 1}.");
        }

        private void OnEnemySpawned(EnemyRuntime enemy, EnemyConfig config)
        {
            _aliveEnemyCount++;
        }

        private void OnEnemyRemoved(EnemyRuntime enemy, EnemyConfig config)
        {
            _aliveEnemyCount = Mathf.Max(0, _aliveEnemyCount - 1);
        }

        private void OnWaveSpawnCompleted()
        {
            _spawnCompletedForCurrentWave = true;
        }

        private void RebindEvents()
        {
            ResolveReferences(includeWaveConfig: false);

            if (stateController != null)
            {
                stateController.StateChanged -= OnStateChanged;
                stateController.StateChanged += OnStateChanged;
            }

            if (_subscribedSpawnManager != spawnManager)
            {
                if (_subscribedSpawnManager != null)
                {
                    _subscribedSpawnManager.EnemySpawned -= OnEnemySpawned;
                    _subscribedSpawnManager.EnemyKilled -= OnEnemyRemoved;
                    _subscribedSpawnManager.EnemyReachedGoal -= OnEnemyRemoved;
                    _subscribedSpawnManager.WaveSpawnCompleted -= OnWaveSpawnCompleted;
                }

                _subscribedSpawnManager = spawnManager;
            }

            if (_subscribedSpawnManager != null)
            {
                _subscribedSpawnManager.EnemySpawned -= OnEnemySpawned;
                _subscribedSpawnManager.EnemyKilled -= OnEnemyRemoved;
                _subscribedSpawnManager.EnemyReachedGoal -= OnEnemyRemoved;
                _subscribedSpawnManager.WaveSpawnCompleted -= OnWaveSpawnCompleted;

                _subscribedSpawnManager.EnemySpawned += OnEnemySpawned;
                _subscribedSpawnManager.EnemyKilled += OnEnemyRemoved;
                _subscribedSpawnManager.EnemyReachedGoal += OnEnemyRemoved;
                _subscribedSpawnManager.WaveSpawnCompleted += OnWaveSpawnCompleted;
            }
        }

        private void ResolveReferences(bool includeWaveConfig)
        {
            if (stateController == null)
            {
                stateController = FindFirstObjectByType<GameStateController>();
            }

            if (spawnManager == null)
            {
                spawnManager = FindFirstObjectByType<SpawnManager>();
            }

            if (pathManager == null)
            {
                pathManager = FindFirstObjectByType<PathManager>();
            }

            if (!includeWaveConfig || waveConfig != null)
            {
                return;
            }

            waveConfig = TryResolveWaveConfigFromWorldMap();
            if (waveConfig == null)
            {
                int fallbackWaveCount = stateController != null ? stateController.TotalWaves : 1;
                waveConfig = CreateFallbackWaveConfig(fallbackWaveCount);
            }
        }

        private static WaveConfig TryResolveWaveConfigFromWorldMap()
        {
            WorldMapManager manager = WorldMapManager.Instance;
            if (manager == null || manager.CurrentStageConfig == null || manager.CurrentStageConfig.Stages == null)
            {
                return null;
            }

            int selectedStageId = WorldMapScene.SelectedStageId;
            for (int i = 0; i < manager.CurrentStageConfig.Stages.Count; i++)
            {
                StageData stage = manager.CurrentStageConfig.Stages[i];
                if (selectedStageId > 0 && stage.StageId != selectedStageId)
                {
                    continue;
                }

                WaveConfig resolved = TryResolveWaveConfig(stage);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            for (int i = 0; i < manager.CurrentStageConfig.Stages.Count; i++)
            {
                StageData stage = manager.CurrentStageConfig.Stages[i];
                WaveConfig resolved = TryResolveWaveConfig(stage);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }

        private static WaveConfig TryResolveWaveConfig(StageData stage)
        {
            if (stage.WaveConfig != null)
            {
                return stage.WaveConfig;
            }

            if (stage.StageId > 0)
            {
                return ConfigResourcePaths.LoadWaveConfigByStageId(stage.StageId);
            }

            return null;
        }

        private static WaveConfig CreateFallbackWaveConfig(int waveCount)
        {
            var config = ScriptableObject.CreateInstance<WaveConfig>();
            config.hideFlags = HideFlags.DontSave;
            config.StageId = WorldMapScene.SelectedStageId > 0 ? WorldMapScene.SelectedStageId : 0;
            config.InitialGold = 100;
            config.InitialLives = 20;
            config.StarThresholds = new[] { 20, 15 };
            config.Waves = new System.Collections.Generic.List<WaveConfig.WaveData>();

            int clampedWaveCount = Mathf.Max(1, waveCount);
            for (int i = 0; i < clampedWaveCount; i++)
            {
                config.Waves.Add(new WaveConfig.WaveData
                {
                    WaveIndex = i + 1,
                    BonusGoldOnEarlyCall = 0,
                    IsBossWave = false,
                    SpawnEntries = new System.Collections.Generic.List<WaveConfig.SpawnEntry>
                    {
                        new WaveConfig.SpawnEntry
                        {
                            Enemy = null,
                            Count = 3,
                            SpawnInterval = 0.75f,
                            PathId = 0,
                            SpawnDelay = 0f
                        }
                    }
                });
            }

            if (!_fallbackConfigLogged)
            {
                Debug.Log($"[WaveManager] WaveConfig missing in StageConfig. Using fallback runtime config (Waves={clampedWaveCount}).");
                _fallbackConfigLogged = true;
            }
            return config;
        }
    }
}
