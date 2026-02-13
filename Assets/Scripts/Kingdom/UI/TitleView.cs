using Common.UI;
using Common.Extensions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kingdom.App
{
    /// <summary>
    /// 타이틀 화면 UI.
    /// 게임 시작, 설정 버튼을 제공합니다.
    /// Resources/UI/TitleView 프리팹에 바인딩됩니다.
    /// </summary>
    public class TitleView : BaseView
    {
        [Header("Title UI")]
        [SerializeField] private Button btnStart;
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private AudioSource bgmSource;

        private void Awake()
        {
            if (btnStart == null && transform.Find("btnStart"))
                btnStart = transform.Find("btnStart").GetComponent<Button>();
        }

        protected override void OnInit()
        {
            if (btnStart.IsNotNull())
            {
                btnStart.SetOnClickWithCooldown(OnClickStart);
            }
        }

        protected override void OnEnter(object[] data)
        {
            Debug.Log("[TitleView] Shown.");
        }

        private void OnClickStart()
        {
            Debug.Log("[TitleView] Start button clicked. Moving to WorldMap.");
            KingdomAppManager.Instance.ChangeScene(SCENES.WorldMapScene);
        }
        public void SetBackgroundImage(Sprite sprite)
        {
            if (backgroundImage != null) backgroundImage.sprite = sprite;
        }

        public void SetBgmClip(AudioClip clip)
        {
            if (bgmSource != null) bgmSource.clip = clip;
        }
    }
}
