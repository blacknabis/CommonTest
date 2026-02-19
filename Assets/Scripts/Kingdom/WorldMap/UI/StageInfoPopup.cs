using System;
using Common.Extensions;
using Common.UI;
using Common.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Kingdom.Save;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// 월드맵 스테이지 상세 정보 팝업.
    /// 난이도 선택 후 게임 시작 요청을 전달합니다.
    /// </summary>
    public class StageInfoPopup : BasePopup
    {
        [Serializable]
        public sealed class StageInfoPayload
        {
            public StageData StageData;
            public UserSaveData.StageProgressData ProgressData;
            public Action<int, StageDifficulty> OnStartRequested;

            public StageInfoPayload(
                StageData stageData,
                UserSaveData.StageProgressData progressData,
                Action<int, StageDifficulty> onStartRequested)
            {
                StageData = stageData;
                ProgressData = progressData;
                OnStartRequested = onStartRequested;
            }
        }

        [Header("Info Text")]
        [SerializeField] private TextMeshProUGUI txtStageName;
        [SerializeField] private TextMeshProUGUI txtStarCount;
        [SerializeField] private TextMeshProUGUI txtRecommendedDifficulty;
        [SerializeField] private TextMeshProUGUI txtBestRecord;
        [SerializeField] private Image panelBackground;

        [Header("Difficulty Buttons")]
        [SerializeField] private Button btnCasual;
        [SerializeField] private Button btnNormal;
        [SerializeField] private Button btnVeteran;

        [Header("Action Buttons")]
        [SerializeField] private Button btnStart;
        [SerializeField] private Button btnBack;

        [Header("Audio")]
        [SerializeField] private AudioClip openClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float openVolumeScale = 0.75f;
        [SerializeField, Range(0f, 1f)] private float clickVolumeScale = 0.9f;
        [SerializeField, Range(0f, 0.3f)] private float clickSfxMinInterval = 0.08f;

        private const string PanelSpriteResourcePath = "UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Panel";
        private const string ButtonSpriteResourcePath = "UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Button";
        private const string StartButtonSpriteResourcePath = "UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Button_Start";
        private const string CloseButtonSpriteResourcePath = "UI/Sprites/WorldMap/StageInfoPopup/StageInfoPopup_Button_Close";
        private const string LegacyPanelSpriteResourcePath = "UI/Sprites/WorldMap/ico_stage_bg";
        private const string LegacyButtonSpriteResourcePath = "UI/Sprites/WorldMap/ico_text_bg";
        private const string PopupClickAudioResourcePath = "Audio/SFX/UI_Common_Click";

        private StageInfoPayload _payload;
        private StageDifficulty _selectedDifficulty = StageDifficulty.Normal;
        private float _lastClickSfxAtUnscaledTime = -999f;
        private bool _suppressCloseSfxOnce;

        protected override void OnInit()
        {
            base.OnInit();
            TryApplyVisualSkin();
            TryLoadDefaultAudio();

            if (btnCasual.IsNotNull()) btnCasual.SetOnClick(() => OnClickDifficulty(StageDifficulty.Casual));
            if (btnNormal.IsNotNull()) btnNormal.SetOnClick(() => OnClickDifficulty(StageDifficulty.Normal));
            if (btnVeteran.IsNotNull()) btnVeteran.SetOnClick(() => OnClickDifficulty(StageDifficulty.Veteran));

            if (btnStart.IsNotNull()) btnStart.SetOnClickWithCooldown(OnClickStart, 0.25f);
            if (btnBack.IsNotNull()) btnBack.SetOnClick(RequestClose);
        }

        protected override void OnEnter(object[] data)
        {
            if (data == null || data.Length == 0 || data[0] is not StageInfoPayload payload)
            {
                _suppressCloseSfxOnce = true;
                RequestClose();
                return;
            }

            _payload = payload;
            _selectedDifficulty = payload.StageData.Difficulty;

            BindStageData();
            RefreshDifficultyButtons();
            PlayOpenSfx();
        }

        protected override void OnExit()
        {
            base.OnExit();
            _payload = null;
        }

        private void BindStageData()
        {
            if (_payload == null)
            {
                return;
            }

            if (txtStageName.IsNotNull())
            {
                txtStageName.text = $"{_payload.StageData.StageName} (#{_payload.StageData.StageId})";
            }

            if (txtStarCount.IsNotNull())
            {
                var stars = Mathf.Clamp(_payload.ProgressData.BestStars, 0, 3);
                txtStarCount.text = $"별: {stars}/3";
            }

            if (txtRecommendedDifficulty.IsNotNull())
            {
                txtRecommendedDifficulty.text = $"권장 난이도: {_payload.StageData.Difficulty}";
            }

            if (txtBestRecord.IsNotNull())
            {
                var bestTime = _payload.ProgressData.BestClearTimeSeconds;
                if (bestTime > 0f)
                {
                    txtBestRecord.text = $"최고 기록: {bestTime:0.00}s ({_payload.ProgressData.BestDifficulty})";
                }
                else
                {
                    txtBestRecord.text = "최고 기록: 없음";
                }
            }
        }

        private void OnClickDifficulty(StageDifficulty difficulty)
        {
            PlayClickSfx();
            _selectedDifficulty = difficulty;
            RefreshDifficultyButtons();
        }

        private void RefreshDifficultyButtons()
        {
            if (btnCasual.IsNotNull()) btnCasual.SetInteractable(_selectedDifficulty != StageDifficulty.Casual);
            if (btnNormal.IsNotNull()) btnNormal.SetInteractable(_selectedDifficulty != StageDifficulty.Normal);
            if (btnVeteran.IsNotNull()) btnVeteran.SetInteractable(_selectedDifficulty != StageDifficulty.Veteran);
        }

        private void OnClickStart()
        {
            if (_payload == null)
            {
                _suppressCloseSfxOnce = true;
                RequestClose();
                return;
            }

            PlayClickSfx();
            _payload.OnStartRequested?.Invoke(_payload.StageData.StageId, _selectedDifficulty);
            ClosePopup();
        }

        protected override bool OnCloseRequested()
        {
            if (_suppressCloseSfxOnce)
            {
                _suppressCloseSfxOnce = false;
                return true;
            }

            PlayClickSfx();
            return true;
        }

        private void TryApplyVisualSkin()
        {
            if (panelBackground == null)
            {
                var panel = transform.Find("Panel");
                if (panel != null)
                {
                    panelBackground = panel.GetComponent<Image>();
                }
            }

            Sprite panelSprite = Resources.Load<Sprite>(PanelSpriteResourcePath);
            if (panelSprite == null)
            {
                panelSprite = Resources.Load<Sprite>(LegacyPanelSpriteResourcePath);
            }
            if (panelBackground != null && panelSprite != null)
            {
                panelBackground.sprite = panelSprite;
                panelBackground.type = Image.Type.Sliced;
                panelBackground.color = Color.white;
            }

            Sprite buttonSprite = Resources.Load<Sprite>(ButtonSpriteResourcePath);
            if (buttonSprite == null)
            {
                buttonSprite = Resources.Load<Sprite>(LegacyButtonSpriteResourcePath);
            }
            Sprite startButtonSprite = Resources.Load<Sprite>(StartButtonSpriteResourcePath);
            if (startButtonSprite == null)
            {
                startButtonSprite = buttonSprite;
            }
            Sprite closeButtonSprite = Resources.Load<Sprite>(CloseButtonSpriteResourcePath);
            if (closeButtonSprite == null)
            {
                closeButtonSprite = buttonSprite;
            }

            ApplyButtonSkin(btnCasual, buttonSprite);
            ApplyButtonSkin(btnNormal, buttonSprite);
            ApplyButtonSkin(btnVeteran, buttonSprite);
            ApplyButtonSkin(btnBack, buttonSprite);
            ApplyButtonSkin(btnStart, startButtonSprite);
            ApplyButtonSkin(closeButton, closeButtonSprite);
        }

        private static void ApplyButtonSkin(Button button, Sprite sprite)
        {
            if (button == null || sprite == null || button.image == null)
            {
                return;
            }

            button.image.sprite = sprite;
            button.image.type = Image.Type.Sliced;
        }

        private void TryLoadDefaultAudio()
        {
            if (clickClip == null)
            {
                clickClip = Resources.Load<AudioClip>(PopupClickAudioResourcePath);
            }

            if (openClip == null)
            {
                openClip = clickClip;
            }
        }

        private void PlayOpenSfx()
        {
            if (openClip == null)
            {
                return;
            }

            AudioHelper.Instance?.PlaySFX(openClip, openVolumeScale);
        }

        private void PlayClickSfx()
        {
            if (clickClip == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now - _lastClickSfxAtUnscaledTime < clickSfxMinInterval)
            {
                return;
            }

            _lastClickSfxAtUnscaledTime = now;
            AudioHelper.Instance?.PlaySFX(clickClip, clickVolumeScale);
        }
    }
}
