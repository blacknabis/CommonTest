using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.WorldMap
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class UIStageNode : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField, Min(1)] private int stageId = 1;

        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI stageNumberText;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject selectedHighlight;
        [SerializeField] private GameObject notificationDot;
        [SerializeField] private Image[] starImages;

        [Header("Visual Style")]
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.58f, 0.58f, 0.58f, 1f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.96f, 0.78f, 1f);
        [SerializeField] private Color stageLabelUnlockedColor = Color.white;
        [SerializeField] private Color stageLabelLockedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color stageLabelClearedColor = new Color(0.92f, 0.98f, 0.92f, 1f);
        [SerializeField] private Color stageLabelSelectedColor = new Color(1f, 0.98f, 0.86f, 1f);
        [SerializeField] private Color starOnColor = Color.white;
        [SerializeField] private Color starOffColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField, Range(0.2f, 1f)] private float lockedAlpha = 0.9f;
        [SerializeField] private bool hideStarsWhenLocked = true;

        public int StageId => stageId;
        public event Action<int> OnNodeClicked;

        private bool _subscribed;

        private void Awake()
        {
            TryAutoWire();
            ValidateRequiredReferences();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 프리팹 편집 중 참조가 비어 있으면 가능한 범위에서 자동 복구한다.
            TryAutoWire();
        }
#endif

        private void OnEnable()
        {
            SubscribeClick();
        }

        private void OnDisable()
        {
            UnsubscribeClick();
        }

        public void Initialize(int value)
        {
            if (value > 0)
            {
                stageId = value;
            }
        }

        public void Bind(in StageNodeViewModel vm)
        {
            if (vm.StageId > 0 && vm.StageId != stageId)
            {
                stageId = vm.StageId;
            }

            if (button != null)
            {
                button.interactable = vm.IsUnlocked;
            }

            if (stageNumberText != null)
            {
                if (!string.IsNullOrWhiteSpace(vm.StageLabel))
                {
                    stageNumberText.text = vm.StageLabel;
                }

                stageNumberText.color = ResolveStageLabelColor(vm);
            }

            if (iconImage != null)
            {
                if (vm.IconSprite != null)
                {
                    iconImage.sprite = vm.IconSprite;
                }

                Color tint = ResolveIconColor(vm);
                tint.a = vm.IsUnlocked ? tint.a : Mathf.Clamp01(lockedAlpha);
                iconImage.color = tint;
            }

            if (lockIcon != null)
            {
                lockIcon.SetActive(!vm.IsUnlocked);
            }

            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(vm.IsSelected);
            }

            if (notificationDot != null)
            {
                notificationDot.SetActive(vm.HasNotification);
            }

            if (starImages != null && starImages.Length > 0)
            {
                bool starVisible = vm.IsUnlocked || !hideStarsWhenLocked;
                int stars = Mathf.Clamp(vm.EarnedStars, 0, starImages.Length);
                for (int i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] == null)
                    {
                        continue;
                    }

                    starImages[i].enabled = starVisible;
                    if (!starVisible)
                    {
                        continue;
                    }

                    bool isOn = i < stars;
                    starImages[i].color = isOn ? starOnColor : starOffColor;
                }
            }
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }

        private void HandleClick()
        {
            OnNodeClicked?.Invoke(stageId);
        }

        private void SubscribeClick()
        {
            if (_subscribed || button == null)
            {
                return;
            }

            button.onClick.AddListener(HandleClick);
            _subscribed = true;
        }

        private void UnsubscribeClick()
        {
            if (!_subscribed || button == null)
            {
                return;
            }

            button.onClick.RemoveListener(HandleClick);
            _subscribed = false;
        }

        private void TryAutoWire()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (iconImage == null)
            {
                // 우선순위: Button TargetGraphic -> Button Image -> 자기 자신의 Image
                if (button != null && button.targetGraphic is Image targetGraphicImage)
                {
                    iconImage = targetGraphicImage;
                }
                else if (button != null)
                {
                    iconImage = button.image;
                }
                else
                {
                    iconImage = GetComponent<Image>();
                }
            }

            if (stageNumberText == null)
            {
                // 이름 기반 탐색 후, 실패하면 하위 TMP 전체 탐색으로 보정한다.
                Transform label = transform.Find("lblStage");
                if (label == null)
                {
                    label = transform.Find("StageLabelRoot/lblStage");
                }

                if (label != null)
                {
                    stageNumberText = label.GetComponent<TextMeshProUGUI>();
                }

                if (stageNumberText == null)
                {
                    stageNumberText = GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }

            if (lockIcon == null)
            {
                Transform lockTransform = transform.Find("LockIcon");
                if (lockTransform != null)
                {
                    lockIcon = lockTransform.gameObject;
                }
            }

            if (selectedHighlight == null)
            {
                Transform selectedTransform = transform.Find("SelectedHighlight");
                if (selectedTransform != null)
                {
                    selectedHighlight = selectedTransform.gameObject;
                }
            }

            if (notificationDot == null)
            {
                Transform notificationTransform = transform.Find("NotificationDot");
                if (notificationTransform != null)
                {
                    notificationDot = notificationTransform.gameObject;
                }
            }

            if (starImages == null || starImages.Length == 0)
            {
                Transform starContainer = transform.Find("StarContainer");
                if (starContainer != null)
                {
                    starImages = starContainer.GetComponentsInChildren<Image>(true);
                }
            }
        }

        private void ValidateRequiredReferences()
        {
            if (button == null)
            {
                Debug.LogError($"[StageNode] Button reference missing: {name}", this);
            }

            if (stageId <= 0)
            {
                Debug.LogError($"[StageNode] Invalid StageId on {name}. StageId must be >= 1.", this);
            }
        }

        // 아이콘은 선택 상태를 가장 우선해서 강조하고, 잠금 상태는 별도 톤을 사용한다.
        private Color ResolveIconColor(in StageNodeViewModel vm)
        {
            if (!vm.IsUnlocked)
            {
                return lockedColor;
            }

            return vm.IsSelected ? selectedColor : unlockedColor;
        }

        // 라벨 색상은 선택 > 잠금 > 클리어 > 기본 순서로 결정한다.
        private Color ResolveStageLabelColor(in StageNodeViewModel vm)
        {
            if (vm.IsSelected)
            {
                return stageLabelSelectedColor;
            }

            if (!vm.IsUnlocked)
            {
                return stageLabelLockedColor;
            }

            return vm.IsCleared ? stageLabelClearedColor : stageLabelUnlockedColor;
        }
    }
}
