using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Game
{
    public enum EnemyMotionState
    {
        Moving = 0,
        Blocked = 1,
        Attacking = 2,
        Dead = 3
    }

    /// <summary>
    /// Enemy runtime: path movement + block/attack states + on-hit traits.
    /// </summary>
    public class EnemyRuntime : BaseUnit
    {
        private EnemyConfig _config;
        private List<Vector3> _path;
        private int _pathIndex;
        private int _blockerTowerId = -1;
        private Vector3 _blockedAnchor;
        private float _blockedElapsed;
        private float _attackCooldownLeft;
        private float _attackVisualHoldLeft;
        private float _regenTickAccum;
        private bool _deathBurstTriggered;
        private EnemyMotionState _motionState = EnemyMotionState.Moving;
        private SpriteRenderer _renderer;
        private Sprite[] _idleFrames = Array.Empty<Sprite>();
        private Sprite[] _moveFrames = Array.Empty<Sprite>();
        private Sprite[] _attackFrames = Array.Empty<Sprite>();
        private Sprite[] _dieFrames = Array.Empty<Sprite>();
        private Sprite[] _activeFrames = Array.Empty<Sprite>();
        private int _activeFrameIndex;
        private float _animationTimer;
        private float _animationFps = 8f;
        private EnemyMotionState _animationState = EnemyMotionState.Moving;

        public event Action<EnemyRuntime> ReachedGoal;
        public event Action<EnemyRuntime> Killed;
        public event Action<EnemyRuntime, float> Damaged;
        public event Action<EnemyRuntime, float> Healed;
        public event Action<EnemyRuntime> Dodged;
        public event Action<EnemyRuntime, float> AttackPerformed;
        public event Action<EnemyRuntime, float, float, Vector3> DeathBurstTriggered;

        public bool IsDead => !IsAlive;
        public bool IsFlying => _config != null && _config.IsFlyingUnit;
        public bool IsBoss => _config != null && _config.IsBossUnit;
        public bool IsInstaKillImmune => _config != null && _config.IsInstaKillImmune;
        public EnemyConfig Config => _config;
        public EnemyMotionState MotionState => _motionState;
        public bool IsBlocked => _blockerTowerId >= 0;

        public float GetDeathVisualDuration()
        {
            if (_dieFrames == null || _dieFrames.Length <= 1)
            {
                return 0f;
            }

            return (_dieFrames.Length - 1) / Mathf.Max(1f, _animationFps);
        }

        public void Initialize(EnemyConfig config, List<Vector3> path)
        {
            Initialize(config, path, GetComponent<SpriteRenderer>(), null, null, null, null);
        }

        public void Initialize(EnemyConfig config, List<Vector3> path, SpriteRenderer renderer, Sprite[] moveFrames)
        {
            Initialize(config, path, renderer, null, moveFrames, null, null);
        }

        public void Initialize(
            EnemyConfig config,
            List<Vector3> path,
            SpriteRenderer renderer,
            Sprite[] idleFrames,
            Sprite[] moveFrames,
            Sprite[] attackFrames,
            Sprite[] dieFrames)
        {
            _config = config;
            _path = path;
            _pathIndex = 0;
            _blockerTowerId = -1;
            _blockedElapsed = 0f;
            _attackCooldownLeft = 0f;
            _attackVisualHoldLeft = 0f;
            _regenTickAccum = 0f;
            _deathBurstTriggered = false;
            _motionState = EnemyMotionState.Moving;
            _renderer = renderer != null ? renderer : GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            SetAnimationFrames(idleFrames, moveFrames, attackFrames, dieFrames);

            float startHp = config != null ? Mathf.Max(1f, config.MaxHp) : 1f;
            InitializeHealth(startHp);

            if (_path != null && _path.Count > 0)
            {
                transform.position = _path[0];
            }
        }

        private void Update()
        {
            _attackVisualHoldLeft = Mathf.Max(0f, _attackVisualHoldLeft - Time.deltaTime);

            if (!IsAlive)
            {
                TickVisualAnimation(Time.deltaTime);
                return;
            }

            if (IsBlocked)
            {
                TickBlockedState();
                TickRegen();
                TickVisualAnimation(Time.deltaTime);
                return;
            }

            if (_path == null || _path.Count == 0 || _pathIndex >= _path.Count)
            {
                TickVisualAnimation(Time.deltaTime);
                return;
            }

            float speed = _config != null ? Mathf.Max(0.1f, _config.MoveSpeed) : 1f;
            Vector3 target = _path[_pathIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            TickRegen();

            if ((transform.position - target).sqrMagnitude <= 0.0001f)
            {
                _pathIndex++;
                if (_pathIndex >= _path.Count)
                {
                    _currentHp = 0f;
                    _motionState = EnemyMotionState.Dead;
                    ReachedGoal?.Invoke(this);
                }
            }

            TickVisualAnimation(Time.deltaTime);
        }

        public bool TryEnterBlock(int blockerTowerId, Vector3 blockAnchor)
        {
            if (!IsAlive || blockerTowerId < 0)
            {
                return false;
            }

            if (IsBoss || IsFlying || (_config != null && !_config.CanBeBlocked))
            {
                return false;
            }

            if (_blockerTowerId >= 0 && _blockerTowerId != blockerTowerId)
            {
                return false;
            }

            _blockerTowerId = blockerTowerId;
            _blockedAnchor = blockAnchor;
            _blockedElapsed = 0f;
            _attackCooldownLeft = 0f;
            _motionState = EnemyMotionState.Blocked;
            return true;
        }

        public bool TryApplyInstantKill()
        {
            if (!IsAlive || IsInstaKillImmune || IsBoss)
            {
                return false;
            }

            KillAndNotify();
            return true;
        }

        public void ReleaseBlock(int blockerTowerId)
        {
            if (_blockerTowerId < 0)
            {
                return;
            }

            if (blockerTowerId >= 0 && _blockerTowerId != blockerTowerId)
            {
                return;
            }

            _blockerTowerId = -1;
            _blockedElapsed = 0f;
            _attackCooldownLeft = 0f;
            if (IsAlive)
            {
                _motionState = EnemyMotionState.Moving;
            }
        }

        public override void ApplyDamage(float amount, DamageType damageType = DamageType.Physical, bool halfPhysicalArmorPenetration = false)
        {
            if (!IsAlive)
            {
                return;
            }

            if (TryDodge())
            {
                Dodged?.Invoke(this);
                return;
            }

            float finalDamage = DamageCalculator.CalculateFinalDamage(
                amount,
                _config,
                damageType,
                halfPhysicalArmorPenetration);

            if (finalDamage <= 0f)
            {
                return;
            }

            _currentHp -= finalDamage;
            Damaged?.Invoke(this, finalDamage);
            if (_currentHp <= 0f)
            {
                _currentHp = 0f;
                Die();
            }
        }

        protected override void Die()
        {
            if (_motionState == EnemyMotionState.Dead)
            {
                return;
            }

            _motionState = EnemyMotionState.Dead;
            TriggerDeathBurstOnce();
            Killed?.Invoke(this);
        }

        private void TickBlockedState()
        {
            _blockedElapsed += Time.deltaTime;
            _attackCooldownLeft = Mathf.Max(0f, _attackCooldownLeft - Time.deltaTime);
            _motionState = EnemyMotionState.Blocked;

            if (_blockedElapsed >= 0.2f && TryAttackBlocker())
            {
                _attackVisualHoldLeft = GetAttackVisualDuration();
            }

            if (_attackVisualHoldLeft > 0f)
            {
                _motionState = EnemyMotionState.Attacking;
            }

            transform.position = Vector3.Lerp(transform.position, _blockedAnchor, Time.deltaTime * 8f);
        }

        private void TickRegen()
        {
            if (_config == null || !IsAlive)
            {
                return;
            }

            float regenPerSec = Mathf.Max(0f, _config.RegenHpPerSec);
            if (regenPerSec <= 0f || _currentHp >= _config.MaxHp)
            {
                return;
            }

            _regenTickAccum += Time.deltaTime;
            if (_regenTickAccum < 0.1f)
            {
                return;
            }

            float dt = _regenTickAccum;
            _regenTickAccum = 0f;
            float before = _currentHp;
            _currentHp = Mathf.Min(_config.MaxHp, _currentHp + (regenPerSec * dt));
            float healed = _currentHp - before;
            if (healed > 0f)
            {
                Healed?.Invoke(this, healed);
            }
        }

        private bool TryAttackBlocker()
        {
            if (_config == null || _attackCooldownLeft > 0f)
            {
                return false;
            }

            float min = Mathf.Max(0f, _config.AttackDamageMin);
            float max = Mathf.Max(min, _config.AttackDamageMax);
            float damage = UnityEngine.Random.Range(min, max);
            if (damage > 0f)
            {
                AttackPerformed?.Invoke(this, damage);
            }

            _attackCooldownLeft = Mathf.Max(0.1f, _config.AttackCooldownSec);
            return true;
        }

        private bool TryDodge()
        {
            if (_config == null)
            {
                return false;
            }

            float dodgeChance = Mathf.Clamp01(_config.DodgeChance);
            if (dodgeChance <= 0f)
            {
                return false;
            }

            return UnityEngine.Random.value < dodgeChance;
        }

        private void KillAndNotify()
        {
            if (!IsAlive)
            {
                return;
            }

            _currentHp = 0f;
            Die();
        }

        private void TriggerDeathBurstOnce()
        {
            if (_deathBurstTriggered || _config == null)
            {
                return;
            }

            float radius = Mathf.Max(0f, _config.DeathExplosionRadius);
            float damage = Mathf.Max(0f, _config.DeathExplosionDamage);
            if (radius <= 0f || damage <= 0f)
            {
                return;
            }

            _deathBurstTriggered = true;
            DeathBurstTriggered?.Invoke(this, radius, damage, transform.position);
        }

        private float GetAttackVisualDuration()
        {
            if (_attackFrames == null || _attackFrames.Length <= 1)
            {
                return 0.1f;
            }

            return (_attackFrames.Length - 1) / Mathf.Max(1f, _animationFps);
        }

        private void SetAnimationFrames(Sprite[] idleFrames, Sprite[] moveFrames, Sprite[] attackFrames, Sprite[] dieFrames)
        {
            if (moveFrames != null && moveFrames.Length > 0)
            {
                _moveFrames = moveFrames;
            }
            else if (_renderer != null && _renderer.sprite != null)
            {
                _moveFrames = new[] { _renderer.sprite };
            }
            else
            {
                _moveFrames = Array.Empty<Sprite>();
            }

            _idleFrames = idleFrames != null && idleFrames.Length > 0 ? idleFrames : _moveFrames;
            _attackFrames = attackFrames != null && attackFrames.Length > 0 ? attackFrames : _moveFrames;
            _dieFrames = dieFrames != null && dieFrames.Length > 0 ? dieFrames : _attackFrames;

            if (_idleFrames == null || _idleFrames.Length <= 0)
            {
                _idleFrames = _moveFrames;
            }

            if (_attackFrames == null || _attackFrames.Length <= 0)
            {
                _attackFrames = _moveFrames;
            }

            if (_dieFrames == null || _dieFrames.Length <= 0)
            {
                _dieFrames = _attackFrames;
            }

            _activeFrames = ResolveFramesForState(_motionState);
            _activeFrameIndex = 0;
            _animationTimer = 0f;
            _animationState = _motionState;
            if (_renderer != null && _activeFrames.Length > 0 && _activeFrames[0] != null)
            {
                _renderer.sprite = _activeFrames[0];
            }
        }

        private void TickVisualAnimation(float deltaTime)
        {
            if (_renderer == null)
            {
                return;
            }

            if (_animationState != _motionState)
            {
                _animationState = _motionState;
                _activeFrames = ResolveFramesForState(_motionState);
                _activeFrameIndex = 0;
                _animationTimer = 0f;
                if (_activeFrames.Length > 0 && _activeFrames[0] != null)
                {
                    _renderer.sprite = _activeFrames[0];
                }
            }

            if (_activeFrames == null || _activeFrames.Length <= 0)
            {
                return;
            }

            if (_activeFrames.Length == 1)
            {
                if (_renderer.sprite != _activeFrames[0])
                {
                    _renderer.sprite = _activeFrames[0];
                }

                return;
            }

            float frameDuration = 1f / Mathf.Max(1f, _animationFps);
            _animationTimer += Mathf.Max(0f, deltaTime);
            if (_animationTimer < frameDuration)
            {
                return;
            }

            int step = Mathf.FloorToInt(_animationTimer / frameDuration);
            _animationTimer -= step * frameDuration;
            if (ShouldLoopCurrentAnimation())
            {
                _activeFrameIndex = (_activeFrameIndex + Mathf.Max(1, step)) % _activeFrames.Length;
            }
            else
            {
                _activeFrameIndex = Mathf.Min(_activeFrameIndex + Mathf.Max(1, step), _activeFrames.Length - 1);
            }

            Sprite next = _activeFrames[_activeFrameIndex];
            if (next != null)
            {
                _renderer.sprite = next;
            }
        }

        private Sprite[] ResolveFramesForState(EnemyMotionState state)
        {
            switch (state)
            {
                case EnemyMotionState.Attacking:
                    return _attackFrames;
                case EnemyMotionState.Dead:
                    return _dieFrames;
                case EnemyMotionState.Blocked:
                    return _idleFrames;
                case EnemyMotionState.Moving:
                default:
                    return _moveFrames;
            }
        }

        private bool ShouldLoopCurrentAnimation()
        {
            return _motionState == EnemyMotionState.Moving || _motionState == EnemyMotionState.Blocked;
        }
    }
}
