using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Game
{
    /// <summary>
    /// Hero runtime loop: state machine + combat + progression.
    /// </summary>
    public class HeroController : MonoBehaviour, IDamageable
    {
        private const string HeroInGameSpriteResourcePathPrefix = "UI/Sprites/Heroes/InGame/";
        private const int MaxSequenceFrames = 32;
        private const float DefaultAnimationFps = 10f;
        private const float TankGuardBuffSeconds = 3.5f;
        private const float TankGuardDamageReduction = 0.35f;
        private const int SupportSummonMaxCount = 2;
        private const float SupportSummonLifetimeSec = 12f;
        private const float SupportSummonAttackRange = 2.4f;
        private const float SupportSummonAttackCooldown = 0.8f;
        private const float SupportSummonDamageRatio = 0.45f;
        private const float SupportSummonOrbitRadius = 1.2f;

        [SerializeField] private HeroConfig heroConfig;

        private readonly List<EnemyRuntime> _aliveEnemies = new();
        private readonly List<SummonRuntime> _summons = new();
        private SpawnManager _spawnManager;
        private EnemyRuntime _currentTarget;

        private float _attackCooldownLeft;
        private float _activeSkillCooldownLeft;
        private float _attackVisualHoldLeft;
        private float _guardBuffLeft;
        private float _currentHp;
        private float _respawnLeft;

        private int _level = 1;
        private int _xp;
        private int _xpToNext;

        private float _armorPercent;
        private float _magicResistPercent;

        private SpriteRenderer _renderer;
        private Vector3 _homePosition;

        private Sprite[] _idleFrames;
        private Sprite[] _walkFrames;
        private Sprite[] _attackFrames;
        private Sprite[] _dieFrames;
        private Sprite[] _activeFrames;
        private int _activeFrameIndex;
        private float _animationTimer;
        private bool _useAnimatedSprites;
        private HeroVisualState _visualState = HeroVisualState.Idle;
        private HeroRuntimeState _runtimeState = HeroRuntimeState.Idle;
        private static Sprite _fallbackSummonSprite;

        public string CurrentHeroId => heroConfig != null ? heroConfig.HeroId : string.Empty;
        public int CurrentLevel => _level;
        public int CurrentXp => _xp;
        public HeroRuntimeState RuntimeState => _runtimeState;
        public bool IsAlive => _currentHp > 0f;

        public void Configure(SpawnManager spawnManager, HeroConfig config, Vector3 spawnPosition)
        {
            ClearSummons();
            _spawnManager = spawnManager;
            heroConfig = config != null ? config : CreateFallbackHeroConfig();
            _homePosition = spawnPosition;
            transform.position = spawnPosition;

            InitializeProgression();
            _currentHp = heroConfig.GetMaxHp(_level);
            _respawnLeft = 0f;
            _attackCooldownLeft = 0f;
            _activeSkillCooldownLeft = 0f;
            _guardBuffLeft = 0f;
            _runtimeState = HeroRuntimeState.Idle;

            EnsureVisual();
            RebindSpawnEvents();
        }

        private void Update()
        {
            if (heroConfig == null)
            {
                return;
            }

            TickCooldownLoop();
            TickSummons(Time.deltaTime);

            if (!IsAlive)
            {
                TickRespawnLoop();
                TickVisualAnimation(Time.deltaTime);
                return;
            }

            TickCombatLoop();
            TickVisualAnimation(Time.deltaTime);
        }

        private void OnDisable()
        {
            UnbindSpawnEvents();
            _aliveEnemies.Clear();
            _currentTarget = null;
            ClearSummons();
        }

        public void ApplyDamage(float amount, DamageType damageType = DamageType.Physical, bool halfPhysicalArmorPenetration = false)
        {
            if (!IsAlive)
            {
                return;
            }

            float damage = ComputeMitigatedDamage(Mathf.Max(0f, amount), damageType, halfPhysicalArmorPenetration);
            if (damage <= 0f)
            {
                return;
            }

            _currentHp -= damage;
            if (_currentHp <= 0f)
            {
                EnterDeadState();
            }
        }

        private void InitializeProgression()
        {
            _level = heroConfig != null ? heroConfig.ClampLevel(heroConfig.StartLevel) : 1;
            _xp = 0;
            _xpToNext = heroConfig != null ? heroConfig.GetRequiredXpForNextLevel(_level) : 100;
            _armorPercent = heroConfig != null ? heroConfig.GetArmorPercent(_level) : 0f;
            _magicResistPercent = heroConfig != null ? heroConfig.GetMagicResistPercent(_level) : 0f;
        }

        private void TickCooldownLoop()
        {
            float dt = Time.deltaTime;
            _attackCooldownLeft = Mathf.Max(0f, _attackCooldownLeft - dt);
            _activeSkillCooldownLeft = Mathf.Max(0f, _activeSkillCooldownLeft - dt);
            _attackVisualHoldLeft = Mathf.Max(0f, _attackVisualHoldLeft - dt);
            _guardBuffLeft = Mathf.Max(0f, _guardBuffLeft - dt);
        }

        private void TickRespawnLoop()
        {
            if (_runtimeState != HeroRuntimeState.Dead)
            {
                _runtimeState = HeroRuntimeState.Dead;
                if (_respawnLeft <= 0f)
                {
                    _respawnLeft = heroConfig.GetRespawnSec(_level);
                }
            }

            SetVisualState(HeroVisualState.Dead);
            _respawnLeft -= Time.deltaTime;
            if (_respawnLeft > 0f)
            {
                return;
            }

            Respawn();
        }

        private void Respawn()
        {
            _runtimeState = HeroRuntimeState.Respawn;
            _currentHp = heroConfig.GetMaxHp(_level);
            _currentTarget = null;
            _respawnLeft = 0f;
            _attackCooldownLeft = 0.15f;

            transform.position = _homePosition;
            if (_renderer != null)
            {
                _renderer.color = Color.white;
            }

            SetVisualState(HeroVisualState.Idle, forceReset: true);
            _runtimeState = HeroRuntimeState.Idle;
        }

        private void TickCombatLoop()
        {
            ValidateCurrentTarget();
            bool isMoving = false;
            bool didAttack = false;

            if (_currentTarget == null)
            {
                _currentTarget = FindNearestEnemy(heroConfig.GetAttackRange(_level) * 2.4f);
            }

            if (_currentTarget == null)
            {
                isMoving = MoveTowards(_homePosition, heroConfig.GetMoveSpeed(_level) * 0.75f);
                _runtimeState = isMoving ? HeroRuntimeState.Move : HeroRuntimeState.Idle;
                UpdateVisualState(isMoving, didAttack);
                return;
            }

            Vector3 targetPos = _currentTarget.transform.position;
            float attackRange = Mathf.Max(0.4f, heroConfig.GetAttackRange(_level));
            float sqrDistance = (targetPos - transform.position).sqrMagnitude;
            if (sqrDistance > attackRange * attackRange)
            {
                isMoving = MoveTowards(targetPos, heroConfig.GetMoveSpeed(_level));
                _runtimeState = HeroRuntimeState.Move;
                UpdateVisualState(isMoving, didAttack);
                return;
            }

            _runtimeState = HeroRuntimeState.Engage;

            if (_attackCooldownLeft > 0f)
            {
                UpdateVisualState(isMoving, didAttack);
                return;
            }

            float damage = UnityEngine.Random.Range(heroConfig.GetDamageMin(_level), heroConfig.GetDamageMax(_level));
            _currentTarget.ApplyDamage(Mathf.Max(1f, damage), DamageType.Physical, false);
            _attackCooldownLeft = heroConfig.GetAttackCooldown(_level);
            TryExecuteActiveSkill(_currentTarget);

            didAttack = true;
            UpdateVisualState(isMoving, didAttack);
        }

        private float ComputeMitigatedDamage(float amount, DamageType damageType, bool halfPhysicalArmorPenetration)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float resist = 0f;
            switch (damageType)
            {
                case DamageType.Physical:
                    resist = _armorPercent;
                    if (halfPhysicalArmorPenetration)
                    {
                        resist *= 0.5f;
                    }
                    break;
                case DamageType.Magic:
                    resist = _magicResistPercent;
                    break;
                case DamageType.True:
                default:
                    resist = 0f;
                    break;
            }

            float mitigated = amount * (1f - Mathf.Clamp(resist, 0f, EnemyConfig.MaxResistanceCap));
            if (_guardBuffLeft > 0f && damageType != DamageType.True)
            {
                mitigated *= (1f - TankGuardDamageReduction);
            }

            return mitigated;
        }

        private void EnterDeadState()
        {
            _currentHp = 0f;
            _currentTarget = null;
            _runtimeState = HeroRuntimeState.Dead;
            _respawnLeft = heroConfig != null ? heroConfig.GetRespawnSec(_level) : 8f;
            _guardBuffLeft = 0f;
            ClearSummons();

            SetVisualState(HeroVisualState.Dead, forceReset: true);
            if (_renderer != null)
            {
                _renderer.color = new Color(0.45f, 0.45f, 0.45f, 1f);
            }
        }

        private void RebindSpawnEvents()
        {
            UnbindSpawnEvents();
            if (_spawnManager == null)
            {
                return;
            }

            _spawnManager.EnemySpawned += HandleEnemySpawned;
            _spawnManager.EnemyKilled += HandleEnemyKilled;
            _spawnManager.EnemyReachedGoal += HandleEnemyRemoved;
        }

        private void UnbindSpawnEvents()
        {
            if (_spawnManager == null)
            {
                return;
            }

            _spawnManager.EnemySpawned -= HandleEnemySpawned;
            _spawnManager.EnemyKilled -= HandleEnemyKilled;
            _spawnManager.EnemyReachedGoal -= HandleEnemyRemoved;
        }

        private void HandleEnemySpawned(EnemyRuntime enemy, EnemyConfig config)
        {
            if (enemy != null && !_aliveEnemies.Contains(enemy))
            {
                _aliveEnemies.Add(enemy);
            }
        }

        private void HandleEnemyKilled(EnemyRuntime enemy, EnemyConfig config)
        {
            HandleEnemyRemoved(enemy, config);
            GrantExperience(config != null ? Mathf.Max(1, config.BountyGold) : 1);
        }

        private void HandleEnemyRemoved(EnemyRuntime enemy, EnemyConfig config)
        {
            _aliveEnemies.Remove(enemy);
            if (_currentTarget == enemy)
            {
                _currentTarget = null;
            }
        }

        private void GrantExperience(int amount)
        {
            if (heroConfig == null || amount <= 0 || _level >= heroConfig.MaxLevel)
            {
                return;
            }

            _xp += amount;
            while (_level < heroConfig.MaxLevel && _xp >= _xpToNext)
            {
                _xp -= _xpToNext;
                int prevLevel = _level;
                float prevMaxHp = heroConfig.GetMaxHp(prevLevel);

                _level++;
                _xpToNext = heroConfig.GetRequiredXpForNextLevel(_level);
                _armorPercent = heroConfig.GetArmorPercent(_level);
                _magicResistPercent = heroConfig.GetMagicResistPercent(_level);

                if (IsAlive)
                {
                    float newMaxHp = heroConfig.GetMaxHp(_level);
                    float healBonus = (newMaxHp - prevMaxHp) * 0.5f;
                    _currentHp = Mathf.Min(newMaxHp, _currentHp + healBonus);
                }
            }
        }

        private void TryExecuteActiveSkill(EnemyRuntime primaryTarget)
        {
            if (heroConfig == null || _activeSkillCooldownLeft > 0f)
            {
                return;
            }

            string skill = ResolveActiveSkillKey();
            if (string.IsNullOrWhiteSpace(skill))
            {
                return;
            }

            float baseDamage = UnityEngine.Random.Range(heroConfig.GetDamageMin(_level), heroConfig.GetDamageMax(_level));
            bool fired = false;

            if (skill.Contains("multishot"))
            {
                fired = ExecuteMultishot(baseDamage);
            }
            else if (skill.Contains("shield") || skill.Contains("smash"))
            {
                fired = ExecuteShieldSlam(baseDamage);
            }
            else if (skill.Contains("arcane") || skill.Contains("burst") || skill.Contains("ray"))
            {
                fired = ExecuteArcaneBurst(baseDamage, primaryTarget);
            }
            else if (skill.Contains("summon"))
            {
                fired = ExecuteSummonGuardian();
            }
            else if (primaryTarget != null && !primaryTarget.IsDead)
            {
                primaryTarget.ApplyDamage(Mathf.Max(1f, baseDamage * 0.6f), DamageType.Physical, false);
                fired = true;
            }

            if (fired)
            {
                _activeSkillCooldownLeft = heroConfig.GetActiveCooldownSec(_level);
            }
        }

        private string ResolveActiveSkillKey()
        {
            if (heroConfig == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(heroConfig.ActiveSkillId))
            {
                return heroConfig.ActiveSkillId.Trim().ToLowerInvariant();
            }

            return heroConfig.Role switch
            {
                HeroRole.Tank => "shieldslam",
                HeroRole.MagicDps => "arcaneburst",
                HeroRole.Support => "summon_guardian",
                _ => "multishot"
            };
        }

        private bool ExecuteMultishot(float baseDamage)
        {
            int hitCount = 0;
            float range = Mathf.Max(1.2f, heroConfig.ActiveRange > 0f ? heroConfig.ActiveRange : heroConfig.GetAttackRange(_level) * 1.25f);
            float rangeSqr = range * range;
            Vector3 origin = transform.position;

            for (int i = 0; i < _aliveEnemies.Count && hitCount < 3; i++)
            {
                EnemyRuntime enemy = _aliveEnemies[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                float sqr = (enemy.transform.position - origin).sqrMagnitude;
                if (sqr > rangeSqr)
                {
                    continue;
                }

                enemy.ApplyDamage(Mathf.Max(1f, baseDamage * 0.75f), DamageType.Physical, false);
                hitCount++;
            }

            return hitCount > 0;
        }

        private bool ExecuteShieldSlam(float baseDamage)
        {
            int hitCount = 0;
            float radius = Mathf.Max(1.1f, heroConfig.ActiveRange > 0f ? heroConfig.ActiveRange : 1.6f);
            float radiusSqr = radius * radius;
            Vector3 origin = transform.position;

            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyRuntime enemy = _aliveEnemies[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                float sqr = (enemy.transform.position - origin).sqrMagnitude;
                if (sqr > radiusSqr)
                {
                    continue;
                }

                enemy.ApplyDamage(Mathf.Max(1f, baseDamage * 0.7f), DamageType.Physical, true);
                hitCount++;
            }

            if (hitCount > 0)
            {
                _guardBuffLeft = Mathf.Max(_guardBuffLeft, TankGuardBuffSeconds);
                float maxHp = heroConfig.GetMaxHp(_level);
                _currentHp = Mathf.Min(maxHp, _currentHp + maxHp * 0.04f);
            }

            return hitCount > 0;
        }

        private bool ExecuteArcaneBurst(float baseDamage, EnemyRuntime primaryTarget)
        {
            int hitCount = 0;
            Vector3 origin = primaryTarget != null && !primaryTarget.IsDead
                ? primaryTarget.transform.position
                : transform.position;
            float radius = Mathf.Max(1.5f, heroConfig.ActiveRange > 0f ? heroConfig.ActiveRange : heroConfig.GetAttackRange(_level) * 1.4f);
            float radiusSqr = radius * radius;

            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyRuntime enemy = _aliveEnemies[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                float sqr = (enemy.transform.position - origin).sqrMagnitude;
                if (sqr > radiusSqr)
                {
                    continue;
                }

                enemy.ApplyDamage(Mathf.Max(1f, baseDamage * 0.8f), DamageType.Magic, false);
                hitCount++;
            }

            return hitCount > 0;
        }

        private bool ExecuteSummonGuardian()
        {
            if (_summons.Count >= SupportSummonMaxCount)
            {
                return false;
            }

            GameObject summonGo = new GameObject($"HeroSummon_{CurrentHeroId}_{_summons.Count + 1}");
            summonGo.layer = gameObject.layer;
            if (transform.parent != null)
            {
                summonGo.transform.SetParent(transform.parent, true);
            }

            Vector2 random = UnityEngine.Random.insideUnitCircle;
            if (random.sqrMagnitude <= 0.0001f)
            {
                random = Vector2.right;
            }

            summonGo.transform.position = transform.position + new Vector3(random.x, random.y, 0f) * SupportSummonOrbitRadius;
            summonGo.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            SpriteRenderer renderer = summonGo.AddComponent<SpriteRenderer>();
            renderer.sprite = GetFallbackSummonSprite();
            renderer.sortingOrder = 34;
            renderer.color = new Color(0.68f, 0.92f, 1f, 0.95f);

            _summons.Add(new SummonRuntime
            {
                Root = summonGo,
                Renderer = renderer,
                LifeLeft = SupportSummonLifetimeSec,
                AttackCooldownLeft = 0.2f
            });

            return true;
        }

        private void TickSummons(float deltaTime)
        {
            if (_summons.Count <= 0)
            {
                return;
            }

            for (int i = _summons.Count - 1; i >= 0; i--)
            {
                SummonRuntime summon = _summons[i];
                if (summon == null || summon.Root == null)
                {
                    _summons.RemoveAt(i);
                    continue;
                }

                summon.LifeLeft -= Mathf.Max(0f, deltaTime);
                summon.AttackCooldownLeft = Mathf.Max(0f, summon.AttackCooldownLeft - Mathf.Max(0f, deltaTime));
                if (summon.LifeLeft <= 0f)
                {
                    Destroy(summon.Root);
                    _summons.RemoveAt(i);
                    continue;
                }

                Vector3 orbitTarget = transform.position + GetSummonOrbitOffset(i);
                orbitTarget.z = 0f;
                summon.Root.transform.position = Vector3.MoveTowards(
                    summon.Root.transform.position,
                    orbitTarget,
                    3.5f * Mathf.Max(0f, deltaTime));

                if (summon.AttackCooldownLeft > 0f)
                {
                    continue;
                }

                EnemyRuntime target = FindNearestEnemyFrom(summon.Root.transform.position, SupportSummonAttackRange);
                if (target == null)
                {
                    continue;
                }

                float damage = Mathf.Max(1f, heroConfig.GetDamageMin(_level) * SupportSummonDamageRatio);
                target.ApplyDamage(damage, DamageType.Magic, false);
                summon.AttackCooldownLeft = SupportSummonAttackCooldown;
            }
        }

        private Vector3 GetSummonOrbitOffset(int index)
        {
            float angle = Time.time * 1.8f + index * 1.9f;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * SupportSummonOrbitRadius;
        }

        private void ClearSummons()
        {
            for (int i = 0; i < _summons.Count; i++)
            {
                SummonRuntime summon = _summons[i];
                if (summon == null || summon.Root == null)
                {
                    continue;
                }

                Destroy(summon.Root);
            }

            _summons.Clear();
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
            return FindNearestEnemyFrom(transform.position, searchRange);
        }

        private EnemyRuntime FindNearestEnemyFrom(Vector3 origin, float searchRange)
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

                float sqr = (enemy.transform.position - origin).sqrMagnitude;
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
            config.Role = HeroRole.RangedDps;
            config.MaxHp = 500f;
            config.MoveSpeed = 3.2f;
            config.AttackDamage = 30f;
            config.DamageMin = 25f;
            config.DamageMax = 35f;
            config.AttackCooldown = 0.8f;
            config.AttackRange = 1.8f;
            config.RespawnSec = 15f;
            config.StartLevel = 1;
            config.MaxLevel = 10;
            config.ActiveCooldownSec = 12f;
            config.ActiveRange = 2.1f;
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

        private static Sprite GetFallbackSummonSprite()
        {
            if (_fallbackSummonSprite != null)
            {
                return _fallbackSummonSprite;
            }

            Texture2D tex = new Texture2D(10, 10, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[10 * 10];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.9f, 0.98f, 1f, 1f);
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _fallbackSummonSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                10f);

            return _fallbackSummonSprite;
        }

        private sealed class SummonRuntime
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public float LifeLeft;
            public float AttackCooldownLeft;
        }

        private enum HeroVisualState
        {
            Idle = 0,
            Move = 1,
            Attack = 2,
            Dead = 3
        }

        public enum HeroRuntimeState
        {
            Idle = 0,
            Move = 1,
            Engage = 2,
            Dead = 3,
            Respawn = 4
        }
    }
}
