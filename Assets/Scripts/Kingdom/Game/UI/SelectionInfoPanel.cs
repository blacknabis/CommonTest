using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Common.Extensions;
using Kingdom.Game;

namespace Kingdom.Game.UI
{
    /// <summary>
    /// Displays detailed information of the currently selected object.
    /// Shows Name, HP text, and HP slider.
    /// </summary>
    public class SelectionInfoPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _txtName;
        [SerializeField] private TextMeshProUGUI _txtHp;
        [SerializeField] private Slider _hpSlider;

        private ISelectableTarget _currentTarget;

        private void Start()
        {
            DisablePlaceholderImagesAndRaycasts();

            // 자동 할당 (null일 경우 이름으로 찾음)
            if (_panelRoot.IsNull()) _panelRoot = transform.Find("PanelRoot")?.gameObject;
            if (_txtName.IsNull()) _txtName = GetComponentInChildren<TextMeshProUGUI>();
            if (_txtHp.IsNull())
            {
                TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    TextMeshProUGUI candidate = labels[i];
                    if (candidate.IsNull() || candidate == _txtName)
                    {
                        continue;
                    }

                    _txtHp = candidate;
                    break;
                }
            }

            if (_hpSlider.IsNull()) _hpSlider = GetComponentInChildren<Slider>();
            if (_panelRoot.IsNull()) _panelRoot = gameObject;

            if (SelectionController.Instance.IsNotNull())
            {
                SelectionController.Instance.SelectionChanged += OnSelectionChanged;
            }
            
            // 초기 상태는 숨김
            if (_panelRoot.IsNotNull()) _panelRoot.SetActive(false);
        }

        private void DisablePlaceholderImagesAndRaycasts()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image.IsNull())
                {
                    continue;
                }

                image.raycastTarget = false;
                image.color = new Color(0f, 0f, 0f, 0f);
                image.enabled = false;
            }

            TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMeshProUGUI label = labels[i];
                if (label.IsNull())
                {
                    continue;
                }

                label.raycastTarget = false;
            }

            Slider[] sliders = GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
            {
                Slider slider = sliders[i];
                if (slider.IsNull())
                {
                    continue;
                }

                if (slider.targetGraphic.IsNotNull())
                {
                    slider.targetGraphic.raycastTarget = false;
                }
            }
        }

        private void OnDestroy()
        {
            if (SelectionController.Instance.IsNotNull())
            {
                SelectionController.Instance.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnSelectionChanged(ISelectableTarget target)
        {
            _currentTarget = target;
            
            if (_currentTarget.IsNull() || !_currentTarget.IsAlive)
            {
                if (_panelRoot.IsNotNull()) _panelRoot.SetActive(false);
                return;
            }

            if (_panelRoot.IsNotNull()) _panelRoot.SetActive(true);
            UpdateUI();
        }

        private void Update()
        {
            if (_currentTarget.IsNull() || !_currentTarget.IsAlive)
            {
                if (_panelRoot.IsNotNull() && _panelRoot.activeSelf)
                {
                    _panelRoot.SetActive(false);
                }
                return;
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_currentTarget.IsNull()) return;

            if (_txtName.IsNotNull()) _txtName.text = _currentTarget.DisplayName;
            
            if (_txtHp.IsNotNull())
            {
                _txtHp.text = $"{(int)_currentTarget.CurrentHp} / {(int)_currentTarget.MaxHp}";
            }

            if (_hpSlider.IsNotNull())
            {
                _hpSlider.value = _currentTarget.HpRatio;
            }
        }
    }
}
