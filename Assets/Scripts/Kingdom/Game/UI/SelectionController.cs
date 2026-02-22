using UnityEngine;
using UnityEngine.EventSystems;
using Common.Extensions;
using Kingdom.Game;

namespace Kingdom.Game.UI
{
    /// <summary>
    /// Handles object selection via mouse clicks.
    /// Manages the currently selected ISelectableTarget and provides visual feedback.
    /// </summary>
    public class SelectionController : MonoBehaviour
    {
        public static SelectionController Instance { get; private set; }
        private static int _suppressedFrame = -1;

        [Header("Settings")]
        [SerializeField] private LayerMask _selectionLayer;
        [SerializeField] private float _clickRadius = 0.5f;
        [SerializeField] private SelectionCircleVisual _circleVisual;

        private ISelectableTarget _currentSelected;

        public ISelectableTarget CurrentSelected => _currentSelected;

        /// <summary>
        /// Selection change event. Passes the newly selected target (null if deselected).
        /// </summary>
        public event System.Action<ISelectableTarget> SelectionChanged;

        public static void SuppressSelectionForCurrentFrame()
        {
            _suppressedFrame = Time.frameCount;
        }

        public void SetCircleVisual(SelectionCircleVisual visual)
        {
            _circleVisual = visual;
            if (_circleVisual.IsNotNull())
            {
                _circleVisual.SetTarget(_currentSelected);
            }
        }

        private void Awake()
        {
            if (Instance.IsNotNull() && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (_suppressedFrame == Time.frameCount)
            {
                return;
            }

            // 빈 공간 클릭 시 선택 해제, 오브젝트 클릭 시 선택
            if (Input.GetMouseButtonDown(0))
            {
                // UI 위를 클릭한 경우 무시
                if (EventSystem.current.IsNotNull() && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                HandleMouseClick();
            }
        }

        private void HandleMouseClick()
        {
            if (Camera.main.IsNull())
            {
                return;
            }

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // LayerMask 미설정(0)인 경우 모든 레이어를 대상으로 선택 처리.
            int layerMask = _selectionLayer.value == 0 ? Physics2D.AllLayers : _selectionLayer.value;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(mousePos, _clickRadius, layerMask);
            if ((colliders.IsNull() || colliders.Length <= 0) && layerMask != Physics2D.AllLayers)
            {
                colliders = Physics2D.OverlapCircleAll(mousePos, _clickRadius, Physics2D.AllLayers);
            }
            
            ISelectableTarget targetFound = null;
            float closestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                var selectable = col.GetComponentInParent<ISelectableTarget>();
                if (selectable.IsNotNull() && selectable.IsAlive)
                {
                    float dist = Vector2.Distance(mousePos, col.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        targetFound = selectable;
                    }
                }
            }

            Select(targetFound);
        }

        public void Select(ISelectableTarget target)
        {
            if (_currentSelected == target)
            {
                return;
            }

            if (_currentSelected.IsNotNull())
            {
                _currentSelected.OnDeselected();
            }

            _currentSelected = target;

            if (_currentSelected.IsNotNull())
            {
                _currentSelected.OnSelected();
            }

            if (_circleVisual.IsNotNull())
            {
                _circleVisual.SetTarget(_currentSelected);
            }

            SelectionChanged?.Invoke(_currentSelected);
        }

        public void Deselect()
        {
            Select(null);
        }
    }
}
