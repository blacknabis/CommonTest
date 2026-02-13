using Common;
using Kingdom.App;
using UnityEngine;

namespace Kingdom.App
{
    /// <summary>
    /// 프로젝트의 통합 매니저.
    /// AppManagerBase를 상속받아 씬 관리 및 프로젝트 전용 매니저들을 총괄합니다.
    /// </summary>
    public class KingdomAppManager : AppManagerBase<SCENES, KingdomAppManager>
    {
        protected override string GetSceneNamespacePrefix()
        {
            return "Kingdom.App.";
        }

        protected override void OnInitializeManagers()
        {
            Debug.Log("[KingdomAppManager] Initializing project specific managers...");
            // TODO: GameManager, GoldManager, TowerManager 등을 여기서 초기화
        }

        protected override void Update()
        {
            base.Update();
            // 전역 단축키나 공통 로직이 필요하면 여기에 작성
        }
    }
}
