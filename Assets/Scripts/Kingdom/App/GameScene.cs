using Common.UI;
using Common.App;
using UnityEngine;
using Kingdom.Game;
using Kingdom.WorldMap;

namespace Kingdom.App
{
    /// <summary>
    /// 메인 게임 씬 컨트롤러.
    /// 웨이브 진행, 타워 건설 등을 총괄합니다.
    /// </summary>
    public class GameScene : SceneBase<SCENES>
    {
        private const string BattlefieldPrefabResourcePath = "Prefabs/Game/GameBattlefield";

        private GameStateController _stateController;
        private PathManager _pathManager;
        private SpawnManager _spawnManager;
        private WaveManager _waveManager;
        private GameBattlefield _battlefield;

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

            WaveConfig stageWaveConfig = ResolveStageWaveConfig();
            if (stageWaveConfig != null && stageWaveConfig.Waves != null && stageWaveConfig.Waves.Count > 0)
            {
                _stateController.SetTotalWaves(stageWaveConfig.Waves.Count);
            }
            _waveManager.Configure(stageWaveConfig, _stateController, _spawnManager, _pathManager);

            return true;
        }

        public override void OnEndScene()
        {
            Time.timeScale = 1f;
            Debug.Log("[GameScene] Ended.");
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

                if (stageData.WaveConfig != null)
                {
                    return stageData.WaveConfig;
                }
            }

            for (int i = 0; i < stageConfig.Stages.Count; i++)
            {
                if (stageConfig.Stages[i].WaveConfig != null)
                {
                    return stageConfig.Stages[i].WaveConfig;
                }
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
    }
}
