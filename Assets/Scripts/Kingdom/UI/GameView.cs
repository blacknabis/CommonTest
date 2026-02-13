using Common.UI;
using Common.Extensions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kingdom.App
{
    /// <summary>
    /// 인게임 화면 UI (HUD).
    /// 현재는 승리/패배/일시정지 등의 기본 기능만 제공.
    /// Resources/UI/GameView 프리팹에 바인딩됩니다.
    /// </summary>
    public class GameView : BaseView
    {
        [Header("Game UI")]
        [SerializeField] private Button btnVictory; // 디버그용 승리 버튼
        [SerializeField] private Button btnDefeat;  // 디버그용 패배 버튼
        [SerializeField] private Button btnPause;
        [SerializeField] private TextMeshProUGUI txtWaveInfo;

        private void Awake()
        {
            if (btnVictory == null && transform.Find("btnVictory")) btnVictory = transform.Find("btnVictory").GetComponent<Button>();
            if (btnDefeat == null && transform.Find("btnDefeat")) btnDefeat = transform.Find("btnDefeat").GetComponent<Button>();
            if (btnPause == null && transform.Find("btnPause")) btnPause = transform.Find("btnPause").GetComponent<Button>();
            if (txtWaveInfo == null && transform.Find("txtWaveInfo")) txtWaveInfo = transform.Find("txtWaveInfo").GetComponent<TextMeshProUGUI>();
        }

        protected override void OnInit()
        {
            if (btnVictory.IsNotNull())
                btnVictory.SetOnClickWithCooldown(OnVictory);

            if (btnDefeat.IsNotNull())
                btnDefeat.SetOnClickWithCooldown(OnDefeat);

            if (btnPause.IsNotNull())
                btnPause.SetOnClickWithCooldown(OnPause);
        }

        protected override void OnEnter(object[] data)
        {
            base.OnEnter(data);
            UpdateWaveInfo(1, 10);
        }

        public void UpdateWaveInfo(int current, int total)
        {
            if (txtWaveInfo.IsNotNull())
                txtWaveInfo.text = $"WAVE {current} / {total}";
        }

        private void OnVictory()
        {
            Debug.Log("[GameView] Victory! Return to Map.");
            // 원래는 결과 팝업 -> 맵 이동
            KingdomAppManager.Instance.ChangeScene(SCENES.WorldMapScene);
        }

        private void OnDefeat()
        {
            Debug.Log("[GameView] Defeat! Retry?");
            // 원래는 재시도 팝업
            KingdomAppManager.Instance.ChangeScene(SCENES.WorldMapScene); // 임시로 맵 이동
        }

        private void OnPause()
        {
            Debug.Log("[GameView] Pause clicked.");
            // TODO: Pause Popup
        }
    }
}
