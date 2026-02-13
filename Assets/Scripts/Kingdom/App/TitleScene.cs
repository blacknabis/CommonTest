using Common.App;
using Common.UI;
using UnityEngine;

namespace Kingdom.App
{
    /// <summary>
    /// 타이틀 및 메인 메뉴를 담당하는 씬.
    /// </summary>
    public class TitleScene : SceneBase<SCENES>
    {
        public override bool OnInit()
        {
            Debug.Log("[TitleScene] Initialized.");
            // MainUI를 TitleView로 설정 — SceneBase가 자동으로 Init/Show/Hide를 호출
            MainUI = UIHelper.GetOrCreate<TitleView>();
            return true;
        }

        public override bool OnStartScene()
        {
            Debug.Log("[TitleScene] Started.");
            return true;
        }
    }
}
