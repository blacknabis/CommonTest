using Common.UI;
using Common.App;
using UnityEngine;
using Kingdom.WorldMap;
using Kingdom.Save;

namespace Kingdom.App
{
    /// <summary>
    /// 월드맵 씬 컨트롤러.
    /// 스테이지 선택, 진척도 관리, 시작 팝업 표시를 담당합니다.
    /// </summary>
    public class WorldMapScene : SceneBase<SCENES>
    {
        public static int SelectedStageId { get; private set; } = -1;
        public static StageDifficulty SelectedDifficulty { get; private set; } = StageDifficulty.Normal;

        private UserSaveData _userSaveData;

        public override bool OnInit()
        {
            Debug.Log("[WorldMapScene] Initialized.");
            MainUI = UIHelper.GetOrCreate<WorldMapView>();

            _userSaveData = SaveManager.Instance.SaveData;

            return true;
        }

        public override bool OnStartScene()
        {
            Debug.Log("[WorldMapScene] Started. Showing world map.");
            EnsureWorldMapManager();

            if (MainUI is WorldMapView worldMapView)
            {
                worldMapView.RefreshStageNodeProgress();
            }

            if (WorldMapManager.Instance != null)
            {
                WorldMapManager.Instance.StageNodeClicked -= OnStageNodeClicked;
                WorldMapManager.Instance.StageNodeClicked += OnStageNodeClicked;
            }

            return true;
        }

        public override void OnEndScene()
        {
            if (WorldMapManager.Instance != null)
            {
                WorldMapManager.Instance.StageNodeClicked -= OnStageNodeClicked;
            }

            Debug.Log("[WorldMapScene] Ended.");
        }

        public override bool ProcessBackKey()
        {
            // 월드맵에서는 뒤로가기 시 타이틀로 복귀
            KingdomAppManager.Instance.ChangeScene(SCENES.TitleScene);
            return true;
        }

        private void OnStageNodeClicked(int stageId)
        {
            if (WorldMapManager.Instance == null || WorldMapManager.Instance.CurrentStageConfig == null)
            {
                return;
            }

            var stageList = WorldMapManager.Instance.CurrentStageConfig.Stages;
            for (int i = 0; i < stageList.Count; i++)
            {
                if (stageList[i].StageId != stageId)
                {
                    continue;
                }

                var progress = _userSaveData != null
                    ? _userSaveData.GetStageProgress(stageId)
                    : UserSaveData.StageProgressData.CreateDefault(stageId);

                var payload = new StageInfoPopup.StageInfoPayload(stageList[i], progress, HandleStartStage);
                StageInfoPopup popupPrefab = Resources.Load<StageInfoPopup>("UI/StageInfoPopup");
                if (popupPrefab != null)
                {
                    UIHelper.ShowPopup<StageInfoPopup>(new object[] { payload });
                }
                else
                {
                    Debug.LogWarning("[WorldMapScene] StageInfoPopup prefab missing. Start stage directly.");
                    HandleStartStage(stageId, stageList[i].Difficulty);
                }
                return;
            }
        }

        private void HandleStartStage(int stageId, StageDifficulty difficulty)
        {
            SetSelectedStageContext(stageId, difficulty);

            Debug.Log($"[WorldMapScene] Start Stage: {stageId}, Difficulty: {difficulty}");
            KingdomAppManager.Instance.ChangeScene(SCENES.GameScene);
        }

        public static void SetSelectedStageContext(int stageId, StageDifficulty difficulty)
        {
            SelectedStageId = stageId;
            SelectedDifficulty = difficulty;
        }

        private static void EnsureWorldMapManager()
        {
            if (WorldMapManager.Instance != null)
            {
                return;
            }

            GameObject managerGo = new GameObject("WorldMapManager");
            managerGo.AddComponent<WorldMapManager>();
            Debug.Log("[WorldMapScene] WorldMapManager was missing. Created runtime instance.");
        }
    }
}

