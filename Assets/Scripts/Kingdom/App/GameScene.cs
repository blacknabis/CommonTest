using Common.UI;
using Common.App;
using UnityEngine;

namespace Kingdom.App
{
    /// <summary>
    /// 메인 게임 씬 컨트롤러.
    /// 웨이브 진행, 타워 건설 등을 총괄합니다.
    /// </summary>
    public class GameScene : SceneBase<SCENES>
    {
        public override bool OnInit()
        {
            Debug.Log("[GameScene] Initialized.");
            MainUI = UIHelper.GetOrCreate<GameView>();
            return true;
        }

        public override bool OnStartScene()
        {
            Debug.Log("[GameScene] Started.");
            return true;
        }

        public override void OnEndScene()
        {
            Debug.Log("[GameScene] Ended.");
        }
    }
}
