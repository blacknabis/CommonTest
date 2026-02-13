using Common.UI;
using Common.Extensions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kingdom.App
{
    /// <summary>
    /// 월드맵 화면 UI.
    /// 스테이지 선택, 영웅 관리, 업그레이드 버튼을 제공합니다.
    /// Resources/UI/WorldMapView 프리팹에 바인딩됩니다.
    /// </summary>
    public class WorldMapView : BaseView
    {
        [Header("WorldMap UI")]
        [SerializeField] private Button btnStage1;
        [SerializeField] private Button btnHeroRoom;
        [SerializeField] private Button btnUpgrades;
        [SerializeField] private Button btnBack;
        
        private void Awake()
        {
            if (btnStage1 == null && transform.Find("btnStage1")) btnStage1 = transform.Find("btnStage1").GetComponent<Button>();
            if (btnHeroRoom == null && transform.Find("btnHeroRoom")) btnHeroRoom = transform.Find("btnHeroRoom").GetComponent<Button>();
            if (btnUpgrades == null && transform.Find("btnUpgrades")) btnUpgrades = transform.Find("btnUpgrades").GetComponent<Button>();
            if (btnBack == null && transform.Find("btnBack")) btnBack = transform.Find("btnBack").GetComponent<Button>();
        }

        protected override void OnInit()
        {
            if (btnStage1.IsNotNull())
                btnStage1.SetOnClickWithCooldown(() => OnClickStage(1));
            
            if (btnHeroRoom.IsNotNull())
                btnHeroRoom.SetOnClickWithCooldown(OnClickHeroRoom);
            
            if (btnUpgrades.IsNotNull())
                btnUpgrades.SetOnClickWithCooldown(OnClickUpgrades);

            if (btnBack.IsNotNull())
                btnBack.SetOnClickWithCooldown(OnClickBack);
        }

        private void OnClickStage(int stageId)
        {
            Debug.Log($"[WorldMapView] Stage {stageId} selected. Loading GameScene...");
            KingdomAppManager.Instance.ChangeScene(SCENES.GameScene);
        }

        private void OnClickHeroRoom()
        {
            Debug.Log("[WorldMapView] Hero Room clicked (Not implemented).");
            // TODO: Show Hero Popup
        }

        private void OnClickUpgrades()
        {
            Debug.Log("[WorldMapView] Upgrades clicked (Not implemented).");
            // TODO: Show Upgrade Popup
        }

        private void OnClickBack()
        {
             Debug.Log("[WorldMapView] Back clicked. Returning to Title.");
             KingdomAppManager.Instance.ChangeScene(SCENES.TitleScene);
        }

        public override bool OnBackKey()
        {
            OnClickBack();
            return true;
        }
    }
}
