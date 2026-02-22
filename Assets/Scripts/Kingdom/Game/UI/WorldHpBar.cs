using UnityEngine;
using Common.Extensions;

namespace Kingdom.Game.UI
{
    /// <summary>
    /// Individual HP bar following a target in world space.
    /// Manages the UI Slider and visibility based on HP ratio.
    /// </summary>
    public class WorldHpBar : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _fillRenderer;
        [SerializeField] private Vector3 _offset = new Vector3(0, 0.08f, 0);

        private ISelectableTarget _target;
        private Vector3 _originalFillScale;
        private Vector3 _originalFillLocalPosition;

        private void Awake()
        {
            if (_fillRenderer.IsNull())
            {
                _fillRenderer = ResolveFillRenderer();
            }

            if (_fillRenderer.IsNotNull())
            {
                _originalFillScale = _fillRenderer.transform.localScale;
                _originalFillLocalPosition = _fillRenderer.transform.localPosition;
            }

            if (Mathf.Abs(_offset.y) > 0.3f)
            {
                _offset = new Vector3(_offset.x, 0.08f, _offset.z);
            }
        }

        private SpriteRenderer ResolveFillRenderer()
        {
            Transform fill = transform.Find("Fill");
            if (fill.IsNotNull())
            {
                SpriteRenderer byName = fill.GetComponent<SpriteRenderer>();
                if (byName.IsNotNull())
                {
                    return byName;
                }
            }

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer candidate = renderers[i];
                if (candidate.IsNull() || candidate.transform == transform)
                {
                    continue;
                }

                if (candidate.name.Contains("Fill"))
                {
                    return candidate;
                }
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer candidate = renderers[i];
                if (candidate.IsNull() || candidate.transform == transform)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        public void SetTarget(ISelectableTarget target)
        {
            _target = target;
            if (_target.IsNotNull())
            {
                UpdatePosition();
                UpdateHp();
            }
        }

        private void Update()
        {
            if (_target.IsNull() || !_target.IsAlive)
            {
                gameObject.SetActive(false);
                return;
            }

            UpdatePosition();
            UpdateHp();
        }

        private void UpdatePosition()
        {
            if (_target.IsNotNull())
            {
                transform.position = ResolveHeadAnchorWorldPosition(_target) + _offset;
            }
        }

        private void UpdateHp()
        {
            if (_target.IsNotNull() && _fillRenderer.IsNotNull())
            {
                float ratio = _target.HpRatio;
                float scaledX = _originalFillScale.x * ratio;
                _fillRenderer.transform.localScale = new Vector3(scaledX, _originalFillScale.y, _originalFillScale.z);

                // Keep left edge fixed while shrinking the bar width.
                float shiftX = (_originalFillScale.x - scaledX) * 0.5f;
                _fillRenderer.transform.localPosition = new Vector3(
                    _originalFillLocalPosition.x - shiftX,
                    _originalFillLocalPosition.y,
                    _originalFillLocalPosition.z);
            }
        }

        private static Vector3 ResolveHeadAnchorWorldPosition(ISelectableTarget target)
        {
            Vector3 basePos = target.Position;
            if (target is not Component component || component.IsNull())
            {
                return basePos;
            }

            float maxY = basePos.y;
            float maxHeight = 0f;
            float centerX = basePos.x;

            Collider2D col = component.GetComponentInChildren<Collider2D>();
            if (col.IsNotNull())
            {
                Bounds cb = col.bounds;
                maxY = Mathf.Max(maxY, cb.max.y);
                maxHeight = Mathf.Max(maxHeight, cb.size.y);
                centerX = cb.center.x;
            }

            SpriteRenderer sr = component.GetComponentInChildren<SpriteRenderer>();
            if (sr.IsNotNull())
            {
                Bounds sb = sr.bounds;
                maxY = Mathf.Max(maxY, sb.max.y);
                maxHeight = Mathf.Max(maxHeight, sb.size.y);
                centerX = sb.center.x;
            }

            float extra = Mathf.Clamp(0.06f + (maxHeight * 0.04f), 0.06f, 0.18f);
            return new Vector3(centerX, maxY + extra, basePos.z);
        }
    }
}
