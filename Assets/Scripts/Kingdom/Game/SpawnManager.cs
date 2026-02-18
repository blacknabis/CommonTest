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

    /// <summary>
    /// Minimal hero runtime loop: chase nearest enemy and attack on cooldown.
    /// </summary>
    public class HeroController : MonoBehaviour
    {
        private const string HeroInGameSpriteResourcePathPrefix = "UI/Sprites/Heroes/InGame/";
        private const int MaxSequenceFrames = 32;
        private const float DefaultAnimationFps = 10f;

        [SerializeField] private HeroConfig heroConfig;

        private readonly List<EnemyRuntime> _aliveEnemies = new();
        private SpawnManager _spawnManager;
        private EnemyRuntime _currentTarget;
        private float _attackCooldownLeft;
        private SpriteRenderer _renderer;
        private Vector3 _homePosition;
        private float _attackVisualHoldLeft;

        private Sprite[] _idleFrames;
        private Sprite[] _walkFrames;
        private Sprite[] _attackFrames;
        private Sprite[] _dieFrames;
        private Sprite[] _activeFrames;
        private int _activeFrameIndex;
        private float _animationTimer;
        private bool _useAnimatedSprites;
        private HeroVisualState _visualState = HeroVisualState.Idle;

        public string CurrentHeroId => heroConfig != null ? heroConfig.HeroId : string.Empty;

        public void Configure(SpawnManager spawnManager, HeroConfig config, Vector3 spawnPosition)
        {
            _spawnManager = spawnManager;
            heroConfig = config != null ? config : CreateFallbackHeroConfig();
            _homePosition = spawnPosition;
            transform.position = spawnPosition;
            EnsureVisual();
            RebindSpawnEvents();
        }

        private void Update()
        {
            if (heroConfig == null)
            {
                return;
            }

            _attackCooldownLeft = Mathf.Max(0f, _attackCooldownLeft - Time.deltaTime);
            _attackVisualHoldLeft = Mathf.Max(0f, _attackVisualHoldLeft - Time.deltaTime);
            ValidateCurrentTarget();
            bool isMoving = false;
            bool didAttack = false;

            if (_currentTarget == null)
            {
                _currentTarget = FindNearestEnemy(heroConfig.AttackRange * 2.4f);
            }

            if (_currentTarget == null)
            {
                isMoving = MoveTowards(_homePosition, heroConfig.MoveSpeed * 0.75f);
                UpdateVisualState(isMoving, didAttack);
                TickVisualAnimation(Time.deltaTime);
                return;
            }

            Vector3 targetPos = _currentTarget.transform.position;
            float attackRange = Mathf.Max(0.4f, heroConfig.AttackRange);
            float sqrDistance = (targetPos - transform.position).sqrMagnitude;
            if (sqrDistance > attackRange * attackRange)
            {
                isMoving = MoveTowards(targetPos, heroConfig.MoveSpeed);
                UpdateVisualState(isMoving, didAttack);
                TickVisualAnimation(Time.deltaTime);
                return;
            }

            if (_attackCooldownLeft > 0f)
            {
                UpdateVisualState(isMoving, didAttack);
                TickVisualAnimation(Time.deltaTime);
                return;
            }

            _currentTarget.ApplyDamage(Mathf.Max(1f, heroConfig.AttackDamage), DamageType.Physical, false);
            _attackCooldownLeft = Mathf.Max(0.1f, heroConfig.AttackCooldown);
            didAttack = true;
            UpdateVisualState(isMoving, didAttack);
            TickVisualAnimation(Time.deltaTime);
        }

        private void OnDisable()
        {
            UnbindSpawnEvents();
            _aliveEnemies.Clear();
            _currentTarget = null;
        }

        private void RebindSpawnEvents()
        {
            UnbindSpawnEvents();
            if (_spawnManager == null)
            {
                return;
            }

            _spawnManager.EnemySpawned += HandleEnemySpawned;
            _spawnManager.EnemyKilled += HandleEnemyRemoved;
            _spawnManager.EnemyReachedGoal += HandleEnemyRemoved;
        }

        private void UnbindSpawnEvents()
        {
            if (_spawnManager == null)
            {
                return;
            }

            _spawnManager.EnemySpawned -= HandleEnemySpawned;
            _spawnManager.EnemyKilled -= HandleEnemyRemoved;
            _spawnManager.EnemyReachedGoal -= HandleEnemyRemoved;
        }

        private void HandleEnemySpawned(EnemyRuntime enemy, EnemyConfig config)
        {
            if (enemy != null && !_aliveEnemies.Contains(enemy))
            {
                _aliveEnemies.Add(enemy);
            }
        }

        private void HandleEnemyRemoved(EnemyRuntime enemy, EnemyConfig config)
        {
            _aliveEnemies.Remove(enemy);
            if (_currentTarget == enemy)
            {
                _currentTarget = null;
            }
        }

        private void ValidateCurrentTarget()
        {
            if (_currentTarget == null)
            {
                return;
            }

            if (_currentTarget.IsDead || !_aliveEnemies.Contains(_currentTarget))
            {
                _currentTarget = null;
            }
        }

        private EnemyRuntime FindNearestEnemy(float searchRange)
        {
            float range = Mathf.Max(0.5f, searchRange);
            float rangeSqr = range * range;
            float bestSqr = float.MaxValue;
            EnemyRuntime best = null;

            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyRuntime enemy = _aliveEnemies[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
                if (sqr > rangeSqr || sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = enemy;
            }

            return best;
        }

        private bool MoveTowards(Vector3 targetPos, float moveSpeed)
        {
            Vector3 from = transform.position;
            Vector3 to = Vector3.MoveTowards(from, targetPos, Mathf.Max(0.1f, moveSpeed) * Time.deltaTime);
            to.z = 0f;
            transform.position = to;
            return (to - from).sqrMagnitude > 0.000001f;
        }

        private void EnsureVisual()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
                if (_renderer == null)
                {
                    _renderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            LoadHeroAnimationFrames(heroConfig);
            if (_useAnimatedSprites)
            {
                _renderer.color = Color.white;
                SetVisualState(HeroVisualState.Idle, forceReset: true);
            }
            else
            {
                Sprite resolvedSprite = ResolveHeroRuntimeSprite(heroConfig);
                _renderer.sprite = resolvedSprite != null ? resolvedSprite : GetFallbackHeroSprite();
                _renderer.color = resolvedSprite != null
                    ? Color.white
                    : new Color(1f, 0.95f, 0.5f, 1f);
            }

            _renderer.sortingOrder = 35;
            transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        }

        private void LoadHeroAnimationFrames(HeroConfig config)
        {
            string heroId = config != null ? config.HeroId : null;
            _idleFrames = LoadHeroFrameSequence(heroId, "idle");
            _walkFrames = LoadHeroFrameSequence(heroId, "walk");
            _attackFrames = LoadHeroFrameSequence(heroId, "attack");
            _dieFrames = LoadHeroFrameSequence(heroId, "die");
            _useAnimatedSprites = (_idleFrames.Length + _walkFrames.Length + _attackFrames.Length + _dieFrames.Length) > 0;
        }

        private void UpdateVisualState(bool isMoving, bool didAttack)
        {
            if (!_useAnimatedSprites)
            {
                return;
            }

            if (didAttack)
            {
                _attackVisualHoldLeft = GetAttackVisualDuration();
                SetVisualState(HeroVisualState.Attack);
                return;
            }

            if (_attackVisualHoldLeft > 0f)
            {
                SetVisualState(HeroVisualState.Attack);
                return;
            }

            SetVisualState(isMoving ? HeroVisualState.Move : HeroVisualState.Idle);
        }

        private void SetVisualState(HeroVisualState next, bool forceReset = false)
        {
            if (!_useAnimatedSprites && !forceReset)
            {
                return;
            }

            if (!forceReset && _visualState == next)
            {
                return;
            }

            _visualState = next;
            _activeFrames = ResolveFramesForState(next);
            _activeFrameIndex = 0;
            _animationTimer = 0f;
            if (_activeFrames != null && _activeFrames.Length > 0 && _renderer != null)
            {
                _renderer.sprite = _activeFrames[0];
            }
        }

        private void TickVisualAnimation(float deltaTime)
        {
            if (!_useAnimatedSprites || _activeFrames == null || _activeFrames.Length <= 1 || _renderer == null)
            {
                return;
            }

            float frameDuration = 1f / Mathf.Max(1f, DefaultAnimationFps);
            _animationTimer += Mathf.Max(0f, deltaTime);
            if (_animationTimer < frameDuration)
            {
                return;
            }

            int step = Mathf.FloorToInt(_animationTimer / frameDuration);
            _animationTimer -= step * frameDuration;
            _activeFrameIndex = (_activeFrameIndex + step) % _activeFrames.Length;
            _renderer.sprite = _activeFrames[_activeFrameIndex];
        }

        private Sprite[] ResolveFramesForState(HeroVisualState state)
        {
            switch (state)
            {
                case HeroVisualState.Move:
                    return _walkFrames.Length > 0 ? _walkFrames : (_idleFrames.Length > 0 ? _idleFrames : _attackFrames);
                case HeroVisualState.Attack:
                    return _attackFrames.Length > 0 ? _attackFrames : (_idleFrames.Length > 0 ? _idleFrames : _walkFrames);
                case HeroVisualState.Dead:
                    return _dieFrames.Length > 0 ? _dieFrames : (_idleFrames.Length > 0 ? _idleFrames : _walkFrames);
                default:
                    return _idleFrames.Length > 0 ? _idleFrames : (_walkFrames.Length > 0 ? _walkFrames : _attackFrames);
            }
        }

        private float GetAttackVisualDuration()
        {
            if (_attackFrames == null || _attackFrames.Length <= 0)
            {
                return 0.12f;
            }

            return Mathf.Max(0.1f, _attackFrames.Length / Mathf.Max(1f, DefaultAnimationFps));
        }

        private static Sprite ResolveHeroRuntimeSprite(HeroConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.HeroId))
            {
                return null;
            }

            return Resources.Load<Sprite>(HeroInGameSpriteResourcePathPrefix + config.HeroId);
        }

        private static Sprite[] LoadHeroFrameSequence(string heroId, string action)
        {
            if (string.IsNullOrWhiteSpace(heroId) || string.IsNullOrWhiteSpace(action))
            {
                return Array.Empty<Sprite>();
            }

            var frames = new List<Sprite>(MaxSequenceFrames);
            for (int i = 0; i < MaxSequenceFrames; i++)
            {
                string path = $"{HeroInGameSpriteResourcePathPrefix}{heroId}/{action}_{i:00}";
                Sprite frame = Resources.Load<Sprite>(path);
                if (frame == null)
                {
                    break;
                }

                frames.Add(frame);
            }

            return frames.ToArray();
        }

        private static HeroConfig CreateFallbackHeroConfig()
        {
            HeroConfig config = ScriptableObject.CreateInstance<HeroConfig>();
            config.hideFlags = HideFlags.DontSave;
            config.HeroId = "FallbackHero";
            config.DisplayName = "Hero";
            config.MaxHp = 500f;
            config.MoveSpeed = 3.2f;
            config.AttackDamage = 30f;
            config.AttackCooldown = 0.8f;
            config.AttackRange = 1.8f;
            return config;
        }

        private static Sprite GetFallbackHeroSprite()
        {
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
        }

        private enum HeroVisualState
        {
            Idle = 0,
            Move = 1,
            Attack = 2,
            Dead = 3
        }
    }
}
