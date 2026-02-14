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
        [SerializeField, Range(0.2f, 1f)] private float lockedAlpha = 0.9f;

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
            // 프리팹 편집 중에도 참조를 최대한 자동 보정합니다.
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

            if (stageNumberText != null && !string.IsNullOrWhiteSpace(vm.StageLabel))
            {
                stageNumberText.text = vm.StageLabel;
            }

            if (iconImage != null)
            {
                if (vm.IconSprite != null)
                {
                    iconImage.sprite = vm.IconSprite;
                }

                Color tint = vm.IsUnlocked ? unlockedColor : lockedColor;
                tint.a = vm.IsUnlocked ? unlockedColor.a : Mathf.Clamp01(lockedAlpha);
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
                int stars = Mathf.Clamp(vm.EarnedStars, 0, starImages.Length);
                for (int i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] == null)
                    {
                        continue;
                    }

                    bool isOn = i < stars;
                    starImages[i].enabled = isOn;
                    starImages[i].color = isOn ? Color.white : new Color(1f, 1f, 1f, 0.3f);
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
                // 우선순위: Button TargetGraphic -> Button Image -> 자기 자신 Image
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
                // 계층이 복잡해져도 라벨 이름 우선으로 안정적으로 찾습니다.
                Transform label = transform.Find("lblStage");
                if (label != null)
                {
                    stageNumberText = label.GetComponent<TextMeshProUGUI>();
                }

                if (stageNumberText == null)
                {
                    stageNumberText = GetComponentInChildren<TextMeshProUGUI>(true);
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
    }
}
