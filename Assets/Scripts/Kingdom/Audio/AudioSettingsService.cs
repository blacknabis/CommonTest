using Common.Utils;
using UnityEngine;

namespace Kingdom.Audio
{
    /// <summary>
    /// 오디오 설정(BGM/SFX 볼륨, 음소거)을 로드/저장하고 AudioHelper에 반영하는 정적 서비스.
    /// </summary>
    public static class AudioSettingsService
    {
        private const float SaveDebounceSeconds = 0.2f;

        public static float BgmVolume { get; private set; } = AudioSettingsKeys.DefaultBgmVolume;
        public static float SfxVolume { get; private set; } = AudioSettingsKeys.DefaultSfxVolume;
        public static bool IsBgmMuted { get; private set; } = AudioSettingsKeys.DefaultBgmMuted;
        public static bool IsSfxMuted { get; private set; } = AudioSettingsKeys.DefaultSfxMuted;

        private static bool _isLoaded;
        private static bool _isSaveDirty;
        private static float _saveDueTime;
        private static AudioSettingsSaveDebounceDriver _saveDriver;

        public static void InitializeLifecycleHook()
        {
            EnsureSaveDriver();
        }

        public static void Load()
        {
            if (_isLoaded)
            {
                return;
            }

            // 기존 Common 키를 읽어 신규 Kingdom 키의 초기값으로 사용(하위 호환)
            float legacyBgm = PlayerPrefs.GetFloat("Common_BGMVolume", AudioSettingsKeys.DefaultBgmVolume);
            float legacySfx = PlayerPrefs.GetFloat("Common_SFXVolume", AudioSettingsKeys.DefaultSfxVolume);

            BgmVolume = ClampSafe(PlayerPrefs.GetFloat(AudioSettingsKeys.BgmVolume, legacyBgm), AudioSettingsKeys.DefaultBgmVolume);
            SfxVolume = ClampSafe(PlayerPrefs.GetFloat(AudioSettingsKeys.SfxVolume, legacySfx), AudioSettingsKeys.DefaultSfxVolume);
            IsBgmMuted = PlayerPrefs.GetInt(AudioSettingsKeys.BgmMuted, AudioSettingsKeys.DefaultBgmMuted ? 1 : 0) != 0;
            IsSfxMuted = PlayerPrefs.GetInt(AudioSettingsKeys.SfxMuted, AudioSettingsKeys.DefaultSfxMuted ? 1 : 0) != 0;

            _isLoaded = true;
            // 로드 완료 즉시 런타임 오디오 출력에 동기화
            ApplyToAudioHelper();
        }

        public static void SetBgmVolume(float value)
        {
            Load();
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(BgmVolume, clamped))
            {
                return;
            }

            BgmVolume = clamped;
            ApplyToAudioHelper();
            MarkDirtyAndDebounceSave();
        }

        public static void SetSfxVolume(float value)
        {
            Load();
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(SfxVolume, clamped))
            {
                return;
            }

            SfxVolume = clamped;
            ApplyToAudioHelper();
            MarkDirtyAndDebounceSave();
        }

        public static void SetBgmMuted(bool muted)
        {
            Load();
            if (IsBgmMuted == muted)
            {
                return;
            }

            IsBgmMuted = muted;
            ApplyToAudioHelper();
            MarkDirtyAndDebounceSave();
        }

        public static void SetSfxMuted(bool muted)
        {
            Load();
            if (IsSfxMuted == muted)
            {
                return;
            }

            IsSfxMuted = muted;
            ApplyToAudioHelper();
            MarkDirtyAndDebounceSave();
        }

        public static void ApplyToAudioHelper()
        {
            var audio = AudioHelper.Instance;
            if (audio == null)
            {
                // 부팅 초기에는 싱글톤이 아직 없을 수 있으므로 조용히 리턴
                return;
            }

            audio.SetBGMVolume(BgmVolume);
            audio.SetSFXVolume(SfxVolume);
            audio.IsBGMMuted = IsBgmMuted;
            audio.IsSFXMuted = IsSfxMuted;
        }

        public static void Save()
        {
            SaveImmediate();
        }

        public static void FlushPendingSave()
        {
            if (!_isSaveDirty)
            {
                return;
            }

            SaveImmediate();
        }

        internal static void Tick()
        {
            if (!_isSaveDirty)
            {
                return;
            }

            if (Time.unscaledTime < _saveDueTime)
            {
                return;
            }

            SaveImmediate();
        }

        private static void MarkDirtyAndDebounceSave()
        {
            if (!Application.isPlaying)
            {
                SaveImmediate();
                return;
            }

            EnsureSaveDriver();
            _isSaveDirty = true;
            _saveDueTime = Time.unscaledTime + SaveDebounceSeconds;
        }

        private static void SaveImmediate()
        {
            PlayerPrefs.SetFloat(AudioSettingsKeys.BgmVolume, BgmVolume);
            PlayerPrefs.SetFloat(AudioSettingsKeys.SfxVolume, SfxVolume);
            PlayerPrefs.SetInt(AudioSettingsKeys.BgmMuted, IsBgmMuted ? 1 : 0);
            PlayerPrefs.SetInt(AudioSettingsKeys.SfxMuted, IsSfxMuted ? 1 : 0);
            PlayerPrefs.Save();
            _isSaveDirty = false;
        }

        private static void EnsureSaveDriver()
        {
            if (_saveDriver != null)
            {
                return;
            }

            _saveDriver = Object.FindFirstObjectByType<AudioSettingsSaveDebounceDriver>();
            if (_saveDriver != null)
            {
                return;
            }

            var go = new GameObject("[AudioSettings]SaveDebounceDriver");
            Object.DontDestroyOnLoad(go);
            _saveDriver = go.AddComponent<AudioSettingsSaveDebounceDriver>();
        }

        private static float ClampSafe(float value, float fallback)
        {
            // 저장 데이터 오염(NaN/Infinity) 방어
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return fallback;
            }

            return Mathf.Clamp01(value);
        }
    }

    /// <summary>
    /// 오디오 설정 저장 디바운스 갱신 및 앱 종료/백그라운드 강제 저장 훅.
    /// </summary>
    internal sealed class AudioSettingsSaveDebounceDriver : MonoBehaviour
    {
        private void Update()
        {
            AudioSettingsService.Tick();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                AudioSettingsService.FlushPendingSave();
            }
        }

        private void OnApplicationQuit()
        {
            AudioSettingsService.FlushPendingSave();
        }
    }
}

