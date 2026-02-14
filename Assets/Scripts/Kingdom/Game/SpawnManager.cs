using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Game
{
    /// <summary>
    /// Wave 데이터 기반 적 스폰 담당.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private Transform enemyRoot;
        private readonly Dictionary<EnemyRuntime, EnemyConfig> _enemyConfigMap = new();
        private static Sprite _fallbackEnemySprite;

        public event Action<EnemyRuntime, EnemyConfig> EnemySpawned;
        public event Action<EnemyRuntime, EnemyConfig> EnemyReachedGoal;
        public event Action<EnemyRuntime, EnemyConfig> EnemyKilled;
        public event Action WaveSpawnCompleted;

        public void SetEnemyRoot(Transform root)
        {
            enemyRoot = root;
        }

        public Coroutine SpawnWave(
            WaveConfig.WaveData wave,
            PathManager pathManager)
        {
            return StartCoroutine(CoSpawnWave(wave, pathManager));
        }

        private IEnumerator CoSpawnWave(WaveConfig.WaveData wave, PathManager pathManager)
        {
            if (wave.SpawnEntries == null)
            {
                yield break;
            }

            for (int i = 0; i < wave.SpawnEntries.Count; i++)
            {
                WaveConfig.SpawnEntry entry = wave.SpawnEntries[i];

                if (entry.SpawnDelay > 0f)
                {
                    yield return new WaitForSeconds(entry.SpawnDelay);
                }

                int spawnCount = Mathf.Max(0, entry.Count);
                float spawnInterval = Mathf.Max(0.05f, entry.SpawnInterval);

                for (int j = 0; j < spawnCount; j++)
                {
                    SpawnOne(entry, pathManager);
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            WaveSpawnCompleted?.Invoke();
        }

        private void SpawnOne(WaveConfig.SpawnEntry entry, PathManager pathManager)
        {
            List<Vector3> path;
            if (pathManager == null || !pathManager.TryGetPath(entry.PathId, out path))
            {
                Debug.LogWarning($"[SpawnManager] Path not found. PathId={entry.PathId}");
                return;
            }

            GameObject enemyGo = new GameObject($"Enemy_{entry.Enemy?.EnemyId ?? "Unknown"}");
            if (enemyRoot != null)
            {
                enemyGo.transform.SetParent(enemyRoot, true);
            }

            // 최소 전투 가시성을 위해 기본 SpriteRenderer를 부여한다.
            var renderer = enemyGo.AddComponent<SpriteRenderer>();
            EnemyConfig config = entry.Enemy;
            renderer.sprite = ResolveEnemySprite(config);
            renderer.color = ResolveEnemyColor(config);
            renderer.sortingOrder = 20;
            enemyGo.transform.localScale = Vector3.one * ResolveEnemyScale(config);

            EnemyRuntime enemy = enemyGo.AddComponent<EnemyRuntime>();
            enemy.Initialize(config, path);
            enemy.ReachedGoal += OnEnemyReachedGoal;
            enemy.Killed += OnEnemyKilled;
            _enemyConfigMap[enemy] = config;

            EnemySpawned?.Invoke(enemy, config);
        }

        private void OnEnemyReachedGoal(EnemyRuntime enemy)
        {
            _enemyConfigMap.TryGetValue(enemy, out EnemyConfig config);
            enemy.ReachedGoal -= OnEnemyReachedGoal;
            enemy.Killed -= OnEnemyKilled;
            _enemyConfigMap.Remove(enemy);
            EnemyReachedGoal?.Invoke(enemy, config);
            Destroy(enemy.gameObject);
        }

        private void OnEnemyKilled(EnemyRuntime enemy)
        {
            _enemyConfigMap.TryGetValue(enemy, out EnemyConfig config);
            enemy.ReachedGoal -= OnEnemyReachedGoal;
            enemy.Killed -= OnEnemyKilled;
            _enemyConfigMap.Remove(enemy);
            EnemyKilled?.Invoke(enemy, config);
            Destroy(enemy.gameObject);
        }

        private static Sprite GetFallbackEnemySprite()
        {
            if (_fallbackEnemySprite != null)
            {
                return _fallbackEnemySprite;
            }

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _fallbackEnemySprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                16f);
            return _fallbackEnemySprite;
        }

        private static Sprite ResolveEnemySprite(EnemyConfig config)
        {
            if (config != null && config.Sprite != null)
            {
                return config.Sprite;
            }

            if (config != null && !string.IsNullOrWhiteSpace(config.EnemyId))
            {
                // 규칙 기반 리소스 경로: Resources/UI/Sprites/Enemies/{EnemyId}.png
                Sprite byId = Resources.Load<Sprite>($"UI/Sprites/Enemies/{config.EnemyId}");
                if (byId != null)
                {
                    return byId;
                }
            }

            return GetFallbackEnemySprite();
        }

        private static Color ResolveEnemyColor(EnemyConfig config)
        {
            if (config == null)
            {
                return new Color(0.75f, 0.85f, 1f, 1f);
            }

            if (config.Tint.a > 0f && config.Tint != Color.white)
            {
                return config.Tint;
            }

            if (config.IsBoss)
            {
                return new Color(0.75f, 0.2f, 0.2f, 1f);
            }

            // EnemyId 기반 고정 색상으로 흰 박스 군집을 피한다.
            int hash = Mathf.Abs((config.EnemyId ?? "Enemy").GetHashCode());
            float h = (hash % 360) / 360f;
            Color c = Color.HSVToRGB(h, 0.45f, 0.95f);
            c.a = 1f;
            return c;
        }

        private static float ResolveEnemyScale(EnemyConfig config)
        {
            if (config == null)
            {
                return 0.6f;
            }

            return Mathf.Clamp(config.VisualScale, 0.2f, 2f);
        }
    }
}
