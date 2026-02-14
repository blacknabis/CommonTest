using Common.App;
using UnityEngine;

namespace Kingdom.App
{
    /// <summary>
    /// 게임 시작 시 초기화를 담당하는 씬.
    /// </summary>
    public class InitScene : SceneBase<SCENES>
    {
        public override bool OnInit()
        {
            Debug.Log("[InitScene] Initializing...");
            return true;
        }

        public override bool OnStartScene()
        {
            Debug.Log("[InitScene] App initializing complete. Moving to Title.");
            
            // 초기화 작업이 끝난 후 타이틀 씬으로 전환
            KingdomAppManager.Instance.ChangeScene(SCENES.TitleScene, useFade: false);
            return true;
        }
    }
}
