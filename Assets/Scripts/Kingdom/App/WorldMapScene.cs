using Common.UI;
using Common.App;
using UnityEngine;

namespace Kingdom.App
{
    /// <summary>
    /// 월드맵 씬 컨트롤러.
    /// 스테이지 선택, 영웅 관리, 타워/스킬 업그레이드를 담당합니다.
    /// </summary>
    public class WorldMapScene : SceneBase<SCENES>
    {
        public override bool OnInit()
        {
            Debug.Log("[WorldMapScene] Initialized.");
            MainUI = UIHelper.GetOrCreate<WorldMapView>();
            return true;
        }

        public override bool OnStartScene()
        {
            Debug.Log("[WorldMapScene] Started. Showing world map.");
            return true;
        }

        public override void OnEndScene()
        {
            Debug.Log("[WorldMapScene] Ended.");
        }

        public override bool ProcessBackKey()
        {
            // 월드맵에서 뒤로가기 → 타이틀로 복귀
            KingdomAppManager.Instance.ChangeScene(SCENES.TitleScene);
            return true;
        }
    }
}
