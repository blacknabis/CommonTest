using Common.Extensions;
using Common.Utils;
using Kingdom.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.App
{
    /// <summary>
    /// 오디오 옵션 팝업 뷰.
    /// - BGM/SFX 슬라이더, 음소거 토글을 AudioSettingsService와 동기화
    /// - SFX 조절 시 샘플 효과음을 짧게 재생
    /// </summary>
    public class AudioOptionsPopup : MonoBehaviour
    {
        private const string DefaultSampleSfxResourcePath = "Audio/SFX/UI_Common_Click";

        [Header("UI")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle bgmMuteToggle;
        [SerializeField] private Toggle sfxMuteToggle;
        [SerializeField] private TextMeshProUGUI txtBgmValue;
        [SerializeField] private TextMeshProUGUI txtSfxValue;
        [SerializeField] private Button btnClose;

        [Header("Audio Preview")]
        [SerializeField] private AudioClip sampleSfx;
        [SerializeField, Range(0.05f, 0.5f)] private float sampleSfxMinInterval = 0.2f;

        private bool _suppressUiEvent;
        private float _lastSampleAt = -999f;

        private void Awake()
        {
            // 프리팹/런타임 생성 모두 대응: 누락된 참조를 이름 경로로 자동 바인딩
            EnsureRuntimeBindings();

            if (bgmSlider != null)
            {
                bgmSlider.minValue = 0f;
                bgmSlider.maxValue = 1f;
                bgmSlider.onValueChanged.AddListener(OnChangedBgmSlider);
            }

            if (sfxSlider != null)
            {
                sfxSlider.minValue = 0f;
                sfxSlider.maxValue = 1f;
                sfxSlider.onValueChanged.AddListener(OnChangedSfxSlider);
            }

            if (bgmMuteToggle != null)
            {
                bgmMuteToggle.onValueChanged.AddListener(OnChangedBgmMute);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.onValueChanged.AddListener(OnChangedSfxMute);
            }

            if (btnClose != null)
            {
                btnClose.SetOnClickWithCooldown(Close);
            }

            if (sampleSfx == null)
            {
                // 샘플 SFX 미지정 시 기본 UI 클릭 사운드 사용
                sampleSfx = Resources.Load<AudioClip>(DefaultSampleSfxResourcePath);
            }
        }

        private void OnEnable()
        {
            // 팝업이 열릴 때마다 최신 설정값 반영
            AudioSettingsService.Load();
            RefreshUiFromService();
        }

        private void OnDestroy()
        {
            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveListener(OnChangedBgmSlider);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(OnChangedSfxSlider);
            }

            if (bgmMuteToggle != null)
            {
                bgmMuteToggle.onValueChanged.RemoveListener(OnChangedBgmMute);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.onValueChanged.RemoveListener(OnChangedSfxMute);
            }
        }

        private void RefreshUiFromService()
        {
            _suppressUiEvent = true;

            if (bgmSlider != null)
            {
                bgmSlider.value = AudioSettingsService.BgmVolume;
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = AudioSettingsService.SfxVolume;
            }

            if (bgmMuteToggle != null)
            {
                bgmMuteToggle.isOn = AudioSettingsService.IsBgmMuted;
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.isOn = AudioSettingsService.IsSfxMuted;
            }

            _suppressUiEvent = false;

            RefreshValueTexts();
        }

        private void RefreshValueTexts()
        {
            if (txtBgmValue != null)
            {
                txtBgmValue.text = $"{Mathf.RoundToInt(AudioSettingsService.BgmVolume * 100f)}%";
            }

            if (txtSfxValue != null)
            {
                txtSfxValue.text = $"{Mathf.RoundToInt(AudioSettingsService.SfxVolume * 100f)}%";
            }
        }

        private void OnChangedBgmSlider(float value)
        {
            if (_suppressUiEvent)
            {
                // 서비스 -> UI 반영 중에는 역이벤트 차단
                return;
            }

            AudioSettingsService.SetBgmVolume(value);
            RefreshValueTexts();
        }

        private void OnChangedSfxSlider(float value)
        {
            if (_suppressUiEvent)
            {
                return;
            }

            AudioSettingsService.SetSfxVolume(value);
            RefreshValueTexts();
            PlaySampleSfx();
        }

        private void OnChangedBgmMute(bool isMuted)
        {
            if (_suppressUiEvent)
            {
                return;
            }

            AudioSettingsService.SetBgmMuted(isMuted);
        }

        private void OnChangedSfxMute(bool isMuted)
        {
            if (_suppressUiEvent)
            {
                return;
            }

            AudioSettingsService.SetSfxMuted(isMuted);
            PlaySampleSfx();
        }

        private void PlaySampleSfx()
        {
            if (sampleSfx == null)
            {
                return;
            }

            // 슬라이더 드래그 중 과도한 재생 방지
            float now = Time.unscaledTime;
            if (now - _lastSampleAt < sampleSfxMinInterval)
            {
                return;
            }

            _lastSampleAt = now;
            AudioHelper.Instance?.PlaySFX(sampleSfx, 1f);
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }

        private void EnsureRuntimeBindings()
        {
            // 아래 경로는 WorldMapView 폴백 생성 구조(Panel/Content/*)를 기준으로 탐색
            if (bgmSlider == null)
            {
                Transform tr = transform.Find("Panel/Content/BgmRow/Slider");
                if (tr != null)
                {
                    bgmSlider = tr.GetComponent<Slider>();
                }
            }

            if (sfxSlider == null)
            {
                Transform tr = transform.Find("Panel/Content/SfxRow/Slider");
                if (tr != null)
                {
                    sfxSlider = tr.GetComponent<Slider>();
                }
            }

            if (bgmMuteToggle == null)
            {
                Transform tr = transform.Find("Panel/Content/BgmRow/MuteToggle");
                if (tr != null)
                {
                    bgmMuteToggle = tr.GetComponent<Toggle>();
                }
            }

            if (sfxMuteToggle == null)
            {
                Transform tr = transform.Find("Panel/Content/SfxRow/MuteToggle");
                if (tr != null)
                {
                    sfxMuteToggle = tr.GetComponent<Toggle>();
                }
            }

            if (txtBgmValue == null)
            {
                Transform tr = transform.Find("Panel/Content/BgmRow/Value");
                if (tr != null)
                {
                    txtBgmValue = tr.GetComponent<TextMeshProUGUI>();
                }
            }

            if (txtSfxValue == null)
            {
                Transform tr = transform.Find("Panel/Content/SfxRow/Value");
                if (tr != null)
                {
                    txtSfxValue = tr.GetComponent<TextMeshProUGUI>();
                }
            }

            if (btnClose == null)
            {
                Transform tr = transform.Find("Panel/BtnClose");
                if (tr != null)
                {
                    btnClose = tr.GetComponent<Button>();
                }
            }
        }
    }
}

