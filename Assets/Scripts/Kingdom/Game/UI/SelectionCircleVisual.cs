using UnityEngine;
using Common.Extensions;

namespace Kingdom.Game.UI
{
    /// <summary>
    /// Visual representation of the selection circle.
    /// Follows the selected target and adjusts to its size or position.
    /// </summary>
    public class SelectionCircleVisual : MonoBehaviour
    {
        [SerializeField] private Sprite _circleSprite;
        private SpriteRenderer _renderer;
        private ISelectableTarget _target;
        private const string DefaultCircleSpritePath = "UI/Sprites/Common/SelectionCircle";

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (_circleSprite.IsNull())
            {
                _circleSprite = Resources.Load<Sprite>(DefaultCircleSpritePath);
            }

            if (_circleSprite.IsNotNull())
            {
                _renderer.sprite = _circleSprite;
            }
            
            // 발밑에 표시되도록 설정 (Sorting Order는 유닛보다 낮게)
            _renderer.sortingOrder = 30;
            gameObject.SetActive(false);
        }

        public void SetTarget(ISelectableTarget target)
        {
            _target = target;
            
            if (_target.IsNull())
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            UpdatePosition();
            ApplyScaleFromTarget();
        }

        private void Update()
        {
            if (_target.IsNotNull())
            {
                UpdatePosition();
                
                // 타겟이 죽으면 자동 해제
                if (!_target.IsAlive)
                {
                    SetTarget(null);
                }
            }
        }

        private void UpdatePosition()
        {
            if (_target.IsNotNull())
            {
                Vector3 pos = _target.Position;
                pos.z = 0.05f; // 배경보다 위, 유닛보다 아래
                transform.position = pos;
            }
        }

        private void ApplyScaleFromTarget()
        {
            if (_renderer.IsNull() || _renderer.sprite.IsNull() || _target.IsNull())
            {
                transform.localScale = Vector3.one;
                return;
            }

            float desiredDiameter = ResolveDesiredWorldDiameter(_target);
            float spriteDiameter = Mathf.Max(_renderer.sprite.bounds.size.x, _renderer.sprite.bounds.size.y);
            if (spriteDiameter <= 0.0001f)
            {
                transform.localScale = Vector3.one;
                return;
            }

            float uniform = Mathf.Clamp(desiredDiameter / spriteDiameter, 0.03f, 2.5f);
            transform.localScale = new Vector3(uniform, uniform, 1f);
        }

        private static float ResolveDesiredWorldDiameter(ISelectableTarget target)
        {
            float fallback = target.UnitType switch
            {
                "Tower" => 1.35f,
                "Hero" => 0.95f,
                "Soldier" => 0.82f,
                _ => 0.82f
            };

            if (target is not Component component || component.IsNull())
            {
                return fallback;
            }

            Collider2D collider = component.GetComponentInChildren<Collider2D>();
            if (collider.IsNull())
            {
                return fallback;
            }

            float byCollider = Mathf.Max(collider.bounds.size.x, collider.bounds.size.y) * 1.25f;
            return Mathf.Clamp(byCollider, 0.55f, 1.8f);
        }
    }
}
