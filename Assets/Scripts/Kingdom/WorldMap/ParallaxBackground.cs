using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.WorldMap
{
    [Serializable]
    public struct ParallaxLayer
    {
        public Transform Target;
        [Range(0f, 1f)] public float Speed;

        [NonSerialized] public Vector3 InitialLocalPosition;
    }

    /// <summary>
    /// 마우스 이동을 기준으로 배경 레이어에 시차 효과를 적용합니다.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Parallax")]
        [SerializeField] private Vector2 maxOffset = new Vector2(1.2f, 0.8f);
        [SerializeField] private float smoothTime = 0.12f;
        [SerializeField] private List<ParallaxLayer> layers = new List<ParallaxLayer>();

        private Vector2 currentNormalized;
        private Vector2 normalizedVelocity;

        private void Awake()
        {
            CacheInitialLayerPositions();
        }

        private void LateUpdate()
        {
            if (layers == null || layers.Count == 0)
            {
                return;
            }

            Vector2 targetNormalized = GetMouseNormalizedPosition();

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            currentNormalized = Vector2.SmoothDamp(currentNormalized, targetNormalized, ref normalizedVelocity, smoothTime, Mathf.Infinity, deltaTime);

            for (int i = 0; i < layers.Count; i++)
            {
                ParallaxLayer layer = layers[i];
                if (layer.Target == null)
                {
                    continue;
                }

                Vector3 offset = new Vector3(
                    currentNormalized.x * maxOffset.x * layer.Speed,
                    currentNormalized.y * maxOffset.y * layer.Speed,
                    0f);

                layer.Target.localPosition = layer.InitialLocalPosition + offset;
                layers[i] = layer;
            }
        }

        private void CacheInitialLayerPositions()
        {
            for (int i = 0; i < layers.Count; i++)
            {
                ParallaxLayer layer = layers[i];
                if (layer.Target == null)
                {
                    continue;
                }

                layer.InitialLocalPosition = layer.Target.localPosition;
                layers[i] = layer;
            }
        }

        private static Vector2 GetMouseNormalizedPosition()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return Vector2.zero;
            }

            float x = Mathf.Clamp((Input.mousePosition.x / Screen.width) * 2f - 1f, -1f, 1f);
            float y = Mathf.Clamp((Input.mousePosition.y / Screen.height) * 2f - 1f, -1f, 1f);
            return new Vector2(x, y);
        }
    }
}
