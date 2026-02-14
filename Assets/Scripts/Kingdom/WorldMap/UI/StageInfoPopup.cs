using System;
using Common.Extensions;
using Common.UI;
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

        [Header("Difficulty Buttons")]
        [SerializeField] private Button btnCasual;
        [SerializeField] private Button btnNormal;
        [SerializeField] private Button btnVeteran;

        [Header("Action Buttons")]
        [SerializeField] private Button btnStart;
        [SerializeField] private Button btnBack;

        private StageInfoPayload _payload;
        private StageDifficulty _selectedDifficulty = StageDifficulty.Normal;

        protected override void OnInit()
        {
            base.OnInit();

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
                RequestClose();
                return;
            }

            _payload = payload;
            _selectedDifficulty = payload.StageData.Difficulty;

            BindStageData();
            RefreshDifficultyButtons();
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
                RequestClose();
                return;
            }

            _payload.OnStartRequested?.Invoke(_payload.StageData.StageId, _selectedDifficulty);
            ClosePopup();
        }
    }
}
