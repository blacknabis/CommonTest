using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Game
{
    /// <summary>
    /// 전투 맵(배경/경로/루트)을 한 번에 묶는 런타임 구성요소.
    /// </summary>
    public class GameBattlefield : MonoBehaviour
    {
        private const string BackgroundResourcePath = "UI/Sprites/WorldMap/WorldMap_Background";

        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private Transform pathRoot;
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private List<Transform> pathPoints = new();

        public Transform EnemyRoot => enemyRoot;

        public IReadOnlyList<Transform> GetPathPoints()
        {
            RebuildPathPointsIfNeeded();
            return pathPoints;
        }

        public void EnsureRuntimeDefaults()
        {
            if (backgroundRenderer == null)
            {
                var bgGo = new GameObject("Background", typeof(SpriteRenderer));
                bgGo.transform.SetParent(transform, false);
                bgGo.transform.localPosition = new Vector3(0f, 0f, 5f);
                backgroundRenderer = bgGo.GetComponent<SpriteRenderer>();
                backgroundRenderer.sortingOrder = -100;
            }

            if (pathRoot == null)
            {
                pathRoot = new GameObject("PathRoot").transform;
                pathRoot.SetParent(transform, false);
            }

            if (enemyRoot == null)
            {
                enemyRoot = new GameObject("EnemyRoot").transform;
                enemyRoot.SetParent(transform, false);
            }

            if (pathRoot.childCount == 0)
            {
                CreateDefaultPathPoints(pathRoot);
            }

            ApplyBackgroundSprite();
            RebuildPathPointsIfNeeded();
        }

        public static GameBattlefield CreateFallbackRuntime()
        {
            var go = new GameObject("GameBattlefield_Runtime");
            var battlefield = go.AddComponent<GameBattlefield>();
            battlefield.EnsureRuntimeDefaults();
            return battlefield;
        }

        private void ApplyBackgroundSprite()
        {
            if (backgroundRenderer == null)
            {
                return;
            }

            Sprite sprite = Resources.Load<Sprite>(BackgroundResourcePath);
            if (sprite == null)
            {
                backgroundRenderer.sprite = CreateSolidSprite();
                backgroundRenderer.color = new Color(0.22f, 0.28f, 0.17f, 1f);
                backgroundRenderer.transform.localScale = new Vector3(20f, 12f, 1f);
                return;
            }

            backgroundRenderer.sprite = sprite;
            backgroundRenderer.color = Color.white;

            float ppu = Mathf.Max(1f, sprite.pixelsPerUnit);
            float width = sprite.rect.width / ppu;
            float height = sprite.rect.height / ppu;
            float fitScale = Mathf.Max(14f / width, 8f / height);
            backgroundRenderer.transform.localScale = new Vector3(fitScale, fitScale, 1f);
        }

        private void RebuildPathPointsIfNeeded()
        {
            if (pathPoints != null && pathPoints.Count > 0)
            {
                return;
            }

            pathPoints = new List<Transform>();
            if (pathRoot == null)
            {
                return;
            }

            for (int i = 0; i < pathRoot.childCount; i++)
            {
                Transform child = pathRoot.GetChild(i);
                if (child != null)
                {
                    pathPoints.Add(child);
                }
            }
        }

        private static void CreateDefaultPathPoints(Transform root)
        {
            Vector3[] points =
            {
                new Vector3(-5.2f, 0.4f, 0f),
                new Vector3(-3.4f, 0.35f, 0f),
                new Vector3(-1.7f, 0.3f, 0f),
                new Vector3(0.1f, 0.25f, 0f),
                new Vector3(1.9f, 0.2f, 0f),
                new Vector3(3.8f, 0.15f, 0f),
                new Vector3(5.5f, 0.1f, 0f)
            };

            for (int i = 0; i < points.Length; i++)
            {
                var point = new GameObject($"P{i:00}").transform;
                point.SetParent(root, false);
                point.localPosition = points[i];
            }
        }

        private static Sprite CreateSolidSprite()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
