using UnityEngine;

namespace Kingdom.App
{
    /// <summary>
    /// Lightweight pulse effect for important UI buttons.
    /// </summary>
    public sealed class UIButtonPulse : MonoBehaviour
    {
        [SerializeField] private float speed = 2.2f;
        [SerializeField] private float amplitude = 0.06f;
        [SerializeField] private bool useUnscaledTime = true;

        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            _baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            float t = useUnscaledTime ? Time.unscaledTime : Time.time;
            float wave = 1f + Mathf.Sin(t * speed * Mathf.PI * 2f) * amplitude;
            transform.localScale = _baseScale * wave;
        }
    }
}
