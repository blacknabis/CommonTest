using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.WorldMap
{
    public sealed class WorldMapPresenter
    {
        private readonly List<StageData> _allStages;
        private readonly Dictionary<int, StageData> _stageById;
        private readonly IStageProgressRepository _progressRepository;
        private readonly IStageUnlockPolicy _unlockPolicy;
        private int _selectedStageId = -1;

        public event Action<int> StageLockedClicked;
        public event Action<int> StageSelected;

        public WorldMapPresenter(
            IReadOnlyList<StageData> stages,
            IStageProgressRepository progressRepository,
            IStageUnlockPolicy unlockPolicy)
        {
            _allStages = new List<StageData>();
            _stageById = new Dictionary<int, StageData>();
            _progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
            _unlockPolicy = unlockPolicy ?? throw new ArgumentNullException(nameof(unlockPolicy));

            if (stages == null)
            {
                return;
            }

            for (int i = 0; i < stages.Count; i++)
            {
                StageData stage = stages[i];
                _allStages.Add(stage);

                if (!_stageById.ContainsKey(stage.StageId))
                {
                    _stageById.Add(stage.StageId, stage);
                }
            }

            _allStages.Sort((a, b) => a.StageId.CompareTo(b.StageId));
        }

        public bool TryBuildViewModel(int stageId, out StageNodeViewModel viewModel)
        {
            if (!_stageById.TryGetValue(stageId, out StageData stage))
            {
                viewModel = default;
                return false;
            }

            IReadOnlyDictionary<int, StageProgressSnapshot> progressMap = _progressRepository.GetProgressMap();
            progressMap.TryGetValue(stageId, out StageProgressSnapshot progress);

            bool isUnlocked = _unlockPolicy.IsUnlocked(
                stageId,
                progressMap,
                _allStages,
                _progressRepository.GetTotalEarnedStars());

            string label = string.IsNullOrWhiteSpace(stage.StageName)
                ? $"STAGE {stage.StageId}"
                : stage.StageName;

            viewModel = new StageNodeViewModel(
                stage.StageId,
                label,
                isUnlocked,
                progress.IsCleared,
                progress.EarnedStars,
                _selectedStageId == stage.StageId,
                stage.IsBoss,
                false,
                null);

            return true;
        }

        public bool HandleNodeClicked(int stageId)
        {
            if (!TryBuildViewModel(stageId, out StageNodeViewModel vm))
            {
                Debug.LogWarning($"[WorldMap] Unknown stageId: {stageId}");
                return false;
            }

            if (!vm.IsUnlocked)
            {
                StageLockedClicked?.Invoke(stageId);
                return false;
            }

            _selectedStageId = stageId;
            StageSelected?.Invoke(stageId);
            return true;
        }
    }
}
