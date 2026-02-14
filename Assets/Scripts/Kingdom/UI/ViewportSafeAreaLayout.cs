using UnityEngine;

namespace Kingdom.App
{
    [DisallowMultipleComponent]
    public class ViewportSafeAreaLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaTarget;
        [SerializeField] private bool applySafeArea = true;
        [SerializeField] private bool applyToDirectChildrenIfNoTarget = true;
        [SerializeField] private RectTransform[] bottomLiftTargets;
        [SerializeField, Range(0f, 64f)] private float bottomLift = 10f;

        private Rect _lastSafeArea = Rect.zero;
        private Vector2[] _baseBottomPositions;
        private bool _cachedBasePositions;
        private ChildRectCache[] _childRectCaches;
        private bool _cachedChildRects;

        private struct ChildRectCache
        {
            public RectTransform Rect;
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 OffsetMin;
            public Vector2 OffsetMax;
        }

        private void Awake()
        {
            CacheBasePositions();
            ApplyLayout();
        }

        private void OnEnable()
        {
            ApplyLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            ApplySafeArea();
            ApplyBottomLift();
        }

        private void CacheBasePositions()
        {
            if (_cachedBasePositions)
            {
                return;
            }

            if (bottomLiftTargets == null || bottomLiftTargets.Length == 0)
            {
                RectTransform autoBottomBar = transform.Find("BottomBar") as RectTransform;
                if (autoBottomBar != null)
                {
                    bottomLiftTargets = new[] { autoBottomBar };
                }
            }

            if (bottomLiftTargets == null || bottomLiftTargets.Length == 0)
            {
                _baseBottomPositions = new Vector2[0];
                _cachedBasePositions = true;
                return;
            }

            _baseBottomPositions = new Vector2[bottomLiftTargets.Length];
            for (int i = 0; i < bottomLiftTargets.Length; i++)
            {
                RectTransform target = bottomLiftTargets[i];
                _baseBottomPositions[i] = target != null ? target.anchoredPosition : Vector2.zero;
            }

            _cachedBasePositions = true;
        }

        private void ApplySafeArea()
        {
            if (!applySafeArea)
            {
                return;
            }

            RectTransform target = safeAreaTarget != null ? safeAreaTarget : transform as RectTransform;
            Rect safeArea = Screen.safeArea;
            if (safeArea == _lastSafeArea)
            {
                return;
            }

            if (target != null)
            {
                ApplySafeAreaToTarget(target, safeArea);
                _lastSafeArea = safeArea;
                return;
            }

            ApplySafeAreaToDirectChildren(safeArea);
            _lastSafeArea = safeArea;
        }

        private void ApplySafeAreaToTarget(RectTransform target, Rect safeArea)
        {
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);

            Vector2 min = safeArea.position;
            Vector2 max = safeArea.position + safeArea.size;

            min.x /= screenWidth;
            min.y /= screenHeight;
            max.x /= screenWidth;
            max.y /= screenHeight;

            target.anchorMin = min;
            target.anchorMax = max;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private void ApplySafeAreaToDirectChildren(Rect safeArea)
        {
            if (!applyToDirectChildrenIfNoTarget)
            {
                return;
            }

            CacheChildRects();
            if (_childRectCaches == null || _childRectCaches.Length == 0)
            {
                return;
            }

            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);

            Vector2 safeMin = safeArea.position;
            Vector2 safeMax = safeArea.position + safeArea.size;
            safeMin.x /= screenWidth;
            safeMin.y /= screenHeight;
            safeMax.x /= screenWidth;
            safeMax.y /= screenHeight;
            Vector2 safeSize = safeMax - safeMin;

            for (int i = 0; i < _childRectCaches.Length; i++)
            {
                ChildRectCache cache = _childRectCaches[i];
                if (cache.Rect == null)
                {
                    continue;
                }

                cache.Rect.anchorMin = safeMin + Vector2.Scale(cache.AnchorMin, safeSize);
                cache.Rect.anchorMax = safeMin + Vector2.Scale(cache.AnchorMax, safeSize);
                cache.Rect.offsetMin = cache.OffsetMin;
                cache.Rect.offsetMax = cache.OffsetMax;
            }
        }

        private void CacheChildRects()
        {
            if (_cachedChildRects)
            {
                return;
            }

            int childCount = transform.childCount;
            if (childCount == 0)
            {
                _childRectCaches = new ChildRectCache[0];
                _cachedChildRects = true;
                return;
            }

            var caches = new System.Collections.Generic.List<ChildRectCache>(childCount);
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child is RectTransform rectChild)
                {
                    caches.Add(new ChildRectCache
                    {
                        Rect = rectChild,
                        AnchorMin = rectChild.anchorMin,
                        AnchorMax = rectChild.anchorMax,
                        OffsetMin = rectChild.offsetMin,
                        OffsetMax = rectChild.offsetMax,
                    });
                }
            }

            _childRectCaches = caches.ToArray();
            _cachedChildRects = true;
        }

        private void ApplyBottomLift()
        {
            CacheBasePositions();
            if (bottomLiftTargets == null || _baseBottomPositions == null)
            {
                return;
            }

            for (int i = 0; i < bottomLiftTargets.Length; i++)
            {
                RectTransform target = bottomLiftTargets[i];
                if (target == null)
                {
                    continue;
                }

                target.anchoredPosition = _baseBottomPositions[i] + new Vector2(0f, bottomLift);
            }
        }
    }
}
