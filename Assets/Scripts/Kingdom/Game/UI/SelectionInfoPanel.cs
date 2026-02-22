using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Common.Extensions;
using Kingdom.Game;

namespace Kingdom.Game.UI
{
    /// <summary>
    /// Displays detailed information of the currently selected object.
    /// Shows Name, HP text, and combat stats.
    /// </summary>
    public class SelectionInfoPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _txtName;
        [SerializeField] private TextMeshProUGUI _txtHp;
        [SerializeField] private TextMeshProUGUI _txtCombat;

        private ISelectableTarget _currentTarget;

        private void Start()
        {
            DisablePlaceholderImagesAndRaycasts();

            // 자동 할당 (null일 경우 이름으로 찾음)
            if (_panelRoot.IsNull()) _panelRoot = transform.Find("PanelRoot")?.gameObject;
            if (_txtName.IsNull()) _txtName = GetComponentInChildren<TextMeshProUGUI>();
            if (_txtHp.IsNull())
            {
                _txtHp = transform.Find("PanelRoot/txtHp")?.GetComponent<TextMeshProUGUI>();
            }

            if (_txtCombat.IsNull())
            {
                _txtCombat = transform.Find("PanelRoot/txtCombat")?.GetComponent<TextMeshProUGUI>();
            }

            if (_panelRoot.IsNull()) _panelRoot = gameObject;
            if (_txtHp.IsNull())
            {
                _txtHp = CreateRuntimeHpLabel();
            }
            if (_txtCombat.IsNull())
            {
                _txtCombat = CreateRuntimeCombatLabel();
            }

            NormalizeLayoutForCompactSidePanel();

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
                if (image.sprite.IsNull())
                {
                    image.color = new Color(0f, 0f, 0f, 0f);
                    image.enabled = false;
                }
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
                _txtHp.text = $"HP {(int)_currentTarget.CurrentHp} / {(int)_currentTarget.MaxHp}";
            }

            if (_txtCombat.IsNotNull())
            {
                string attackText = FormatAttackValue(_currentTarget.AttackPower);
                string defenseText = FormatDefenseValue(_currentTarget.DefensePower);
                _txtCombat.text = $"ATK {attackText}\nDEF {defenseText}";
            }
        }

        private TextMeshProUGUI CreateRuntimeHpLabel()
        {
            Transform parent = _panelRoot.IsNotNull() ? _panelRoot.transform : transform;
            if (parent.IsNull())
            {
                return null;
            }

            GameObject labelGo = new GameObject("txtHpRuntime", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(parent, false);

            TextMeshProUGUI hpLabel = labelGo.GetComponent<TextMeshProUGUI>();
            if (hpLabel.IsNull())
            {
                return null;
            }

            if (_txtName.IsNotNull())
            {
                hpLabel.font = _txtName.font;
                hpLabel.fontSharedMaterial = _txtName.fontSharedMaterial;
                hpLabel.color = _txtName.color;
            }
            else
            {
                hpLabel.color = Color.white;
            }

            hpLabel.fontSize = 16f;
            hpLabel.alignment = TextAlignmentOptions.Left;
            hpLabel.raycastTarget = false;

            RectTransform rect = hpLabel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -58f);
            rect.sizeDelta = new Vector2(180f, 22f);

            return hpLabel;
        }

        private TextMeshProUGUI CreateRuntimeCombatLabel()
        {
            Transform parent = _panelRoot.IsNotNull() ? _panelRoot.transform : transform;
            if (parent.IsNull())
            {
                return null;
            }

            GameObject labelGo = new GameObject("txtCombat", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(parent, false);

            TextMeshProUGUI combatLabel = labelGo.GetComponent<TextMeshProUGUI>();
            if (combatLabel.IsNull())
            {
                return null;
            }

            if (_txtHp.IsNotNull())
            {
                combatLabel.font = _txtHp.font;
                combatLabel.fontSharedMaterial = _txtHp.fontSharedMaterial;
                combatLabel.color = _txtHp.color;
            }
            else if (_txtName.IsNotNull())
            {
                combatLabel.font = _txtName.font;
                combatLabel.fontSharedMaterial = _txtName.fontSharedMaterial;
                combatLabel.color = _txtName.color;
            }
            else
            {
                combatLabel.color = Color.white;
            }

            combatLabel.fontSize = 14f;
            combatLabel.alignment = TextAlignmentOptions.Left;
            combatLabel.raycastTarget = false;

            RectTransform rect = combatLabel.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -82f);
            rect.sizeDelta = new Vector2(180f, 38f);
            return combatLabel;
        }

        private void NormalizeLayoutForCompactSidePanel()
        {
            if (_panelRoot.IsNotNull())
            {
                RectTransform panelRect = _panelRoot.GetComponent<RectTransform>();
                if (panelRect.IsNotNull())
                {
                    if (panelRect.sizeDelta.y < 132f)
                    {
                        panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 132f);
                    }
                }
            }

            if (_txtName.IsNotNull())
            {
                _txtName.fontSize = 20f;
                _txtName.textWrappingMode = TextWrappingModes.Normal;
                _txtName.overflowMode = TextOverflowModes.Ellipsis;
                _txtName.maxVisibleLines = 2;
                _txtName.alignment = TextAlignmentOptions.TopLeft;

                RectTransform nameRect = _txtName.rectTransform;
                nameRect.anchorMin = new Vector2(0f, 1f);
                nameRect.anchorMax = new Vector2(0f, 1f);
                nameRect.pivot = new Vector2(0f, 1f);
                nameRect.anchoredPosition = new Vector2(10f, -8f);
                nameRect.sizeDelta = new Vector2(180f, 44f);
            }

            if (_txtHp.IsNotNull())
            {
                _txtHp.alignment = TextAlignmentOptions.Left;
                _txtHp.fontSize = 16f;
                RectTransform hpRect = _txtHp.rectTransform;
                hpRect.anchorMin = new Vector2(0f, 1f);
                hpRect.anchorMax = new Vector2(0f, 1f);
                hpRect.pivot = new Vector2(0f, 1f);
                hpRect.anchoredPosition = new Vector2(10f, -58f);
                hpRect.sizeDelta = new Vector2(180f, 22f);
            }

            if (_txtCombat.IsNotNull())
            {
                _txtCombat.alignment = TextAlignmentOptions.Left;
                _txtCombat.fontSize = 14f;
                RectTransform combatRect = _txtCombat.rectTransform;
                combatRect.anchorMin = new Vector2(0f, 1f);
                combatRect.anchorMax = new Vector2(0f, 1f);
                combatRect.pivot = new Vector2(0f, 1f);
                combatRect.anchoredPosition = new Vector2(10f, -82f);
                combatRect.sizeDelta = new Vector2(180f, 38f);
            }
        }

        private static string FormatAttackValue(float attackPower)
        {
            return attackPower > 0f ? Mathf.RoundToInt(attackPower).ToString() : "-";
        }

        private static string FormatDefenseValue(float defensePower)
        {
            if (defensePower <= 0f)
            {
                return "0%";
            }

            return $"{Mathf.RoundToInt(defensePower)}%";
        }
    }
}
