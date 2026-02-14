using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kingdom.WorldMap
{
    public enum StageNodeState
    {
        Locked,
        Unlocked,
        Completed,
    }

    [Serializable]
    public class StageNodeClickEvent : UnityEvent<int>
    {
    }

    /// <summary>
    /// 월드맵 스테이지 노드 단위 컴포넌트.
    /// StageData 기반으로 상태를 계산하고 상태별 스프라이트를 표시합니다.
    /// </summary>
    public class StageNode : MonoBehaviour, IPointerClickHandler
    {
        [Header("Stage Data")]
        [SerializeField] private StageData stageData;

        [Header("State")]
        [SerializeField] private StageNodeState currentState;

        [Header("State Sprites")]
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Sprite unlockedSprite;
        [SerializeField] private Sprite completedSprite;

        [Header("Visual Targets")]
        [SerializeField] private Image targetImage;
        [SerializeField] private SpriteRenderer targetSpriteRenderer;

        [Header("Events")]
        [SerializeField] private StageNodeClickEvent onNodeClicked = new StageNodeClickEvent();

        public StageData Data => stageData;
        public StageNodeState CurrentState => currentState;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (targetSpriteRenderer == null)
            {
                targetSpriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Start()
        {
            SetState(CalculateStateFromData(stageData));
        }

        private void OnValidate()
        {
            ApplyVisualState(currentState);
        }

        public void UpdateStageData(StageData data)
        {
            stageData = data;
            SetState(CalculateStateFromData(stageData));
        }

        public void SetState(StageNodeState state)
        {
            currentState = state;
            ApplyVisualState(currentState);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleClick();
        }

        private void OnMouseDown()
        {
            HandleClick();
        }

        private void HandleClick()
        {
            if (currentState == StageNodeState.Locked)
            {
                return;
            }

            if (IsDefaultStageData(stageData))
            {
                return;
            }

            onNodeClicked?.Invoke(stageData.StageId);

            if (WorldMapManager.Instance != null)
            {
                WorldMapManager.Instance.OnNodeClicked(stageData.StageId);
            }
        }

        private StageNodeState CalculateStateFromData(StageData data)
        {
            if (IsDefaultStageData(data))
            {
                return StageNodeState.Locked;
            }

            if (!data.IsUnlocked)
            {
                return StageNodeState.Locked;
            }

            if (data.BestTime > 0f)
            {
                return StageNodeState.Completed;
            }

            return StageNodeState.Unlocked;
        }

        private static bool IsDefaultStageData(StageData data)
        {
            return EqualityComparer<StageData>.Default.Equals(data, default);
        }

        private void ApplyVisualState(StageNodeState state)
        {
            Sprite targetSprite = GetSpriteForState(state);

            if (targetImage != null)
            {
                targetImage.sprite = targetSprite;
            }

            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.sprite = targetSprite;
            }
        }

        private Sprite GetSpriteForState(StageNodeState state)
        {
            switch (state)
            {
                case StageNodeState.Locked:
                    return lockedSprite;
                case StageNodeState.Completed:
                    return completedSprite;
                case StageNodeState.Unlocked:
                default:
                    return unlockedSprite;
            }
        }
    }
}
