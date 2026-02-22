using System;
using Common.Extensions;
using UnityEngine;

namespace Kingdom.Game
{
    public enum BarracksSoldierState
    {
        Idle = 0,
        Moving = 1,
        Blocking = 2,
        Dead = 3,
        Respawning = 4
    }

    /// <summary>
    /// Runtime soldier unit for Barracks tower.
    /// HP / death / respawn / block-target attack loop is driven by TowerManager.
    /// </summary>
    public sealed class BarracksSoldierRuntime : BaseUnit
    {
        private const float DefaultMoveSpeed = 5.5f;
        private const float ArriveDistance = 0.05f;
        private const float AttackVisualHoldSec = 0.14f;
        private static readonly int MotionStateHash = Animator.StringToHash("MotionState");

        private float _attackDamage = 1f;
        private float _attackCooldownSec = 1f;
        private float _attackCooldownLeft;
        private float _attackVisualHoldLeft;
        private float _respawnSec = 10f;
        private float _respawnLeft;
        private float _moveSpeed = DefaultMoveSpeed;
        private Vector3 _rallyPoint;
        private EnemyRuntime _blockTarget;
        private string _displayName = "Barracks Soldier";
        private SpriteRenderer _renderer;
        private Animator _animator;
        private bool _useAnimator;
        private float _animAttackDuration = 0.14f;

        public int OwnerTowerId { get; private set; } = -1;
        public int SoldierIndex { get; private set; }
        public BarracksSoldierState State { get; private set; } = BarracksSoldierState.Idle;
        public EnemyRuntime BlockTarget => _blockTarget;
        public bool CanEngage => IsAlive && State != BarracksSoldierState.Respawning;
        public override string DisplayName => _displayName.IsNullOrWhiteSpace() ? name : _displayName;
        public override float AttackPower => _attackDamage;
        public override float DefensePower => 0f;
        public override string UnitType => "Soldier";

        public event Action<BarracksSoldierRuntime> Died;
        public event Action<BarracksSoldierRuntime> Respawned;

        public void Initialize(
            int ownerTowerId,
            int soldierIndex,
            float maxHp,
            float attackDamage,
            float attackCooldownSec,
            float respawnSec,
            Vector3 rallyPoint,
            SpriteRenderer renderer,
            Animator animator = null,
            string displayName = null)
        {
            OwnerTowerId = ownerTowerId;
            SoldierIndex = soldierIndex;
            _displayName = displayName.IsNullOrWhiteSpace() ? name : displayName.Trim();
            _renderer = renderer;
            _animator = animator.IsNotNull() ? animator : GetComponent<Animator>();
            _useAnimator = _animator.IsNotNull() && _animator.runtimeAnimatorController.IsNotNull();
            if (_useAnimator)
            {
                _animAttackDuration = ResolveAnimatorClipLength("attack", "atk", "slash");
            }

            InitializeHealth(maxHp);
            _attackDamage = Mathf.Max(1f, attackDamage);
            _attackCooldownSec = Mathf.Max(0.1f, attackCooldownSec);
            _respawnSec = Mathf.Max(0.1f, respawnSec);
            _attackCooldownLeft = 0f;
            _attackVisualHoldLeft = 0f;
            _respawnLeft = 0f;
            _rallyPoint = rallyPoint;
            _blockTarget = null;
            State = BarracksSoldierState.Idle;
            SetVisualAlive(true);
            TickVisualAnimation(0f);
        }

        public void UpdateStats(float maxHp, float attackDamage, float attackCooldownSec, float respawnSec)
        {
            float hpRatio = _maxHp > 0f ? Mathf.Clamp01(_currentHp / _maxHp) : 1f;
            _maxHp = Mathf.Max(1f, maxHp);
            _attackDamage = Mathf.Max(1f, attackDamage);
            _attackCooldownSec = Mathf.Max(0.1f, attackCooldownSec);
            _respawnSec = Mathf.Max(0.1f, respawnSec);

            if (IsAlive)
            {
                _currentHp = Mathf.Max(1f, _maxHp * hpRatio);
            }
        }

        public void SetRallyPoint(Vector3 rallyPoint, bool forceMove)
        {
            _rallyPoint = rallyPoint;
            if (forceMove && IsAlive && State != BarracksSoldierState.Respawning)
            {
                State = BarracksSoldierState.Moving;
            }
        }

        public void SetSorting(int sortingLayerId, int sortingOrder)
        {
            if (_renderer.IsNull())
            {
                return;
            }

            _renderer.sortingLayerID = sortingLayerId;
            _renderer.sortingOrder = sortingOrder;
        }

        public void AssignBlockTarget(EnemyRuntime target)
        {
            if (!CanEngage)
            {
                return;
            }

            _blockTarget = target;
            _attackCooldownLeft = 0f;
            State = target.IsNotNull() ? BarracksSoldierState.Blocking : BarracksSoldierState.Idle;
        }

        public void ClearBlockTarget()
        {
            _blockTarget = null;
            _attackCooldownLeft = 0f;
            if (State == BarracksSoldierState.Blocking && IsAlive)
            {
                State = BarracksSoldierState.Moving;
            }
        }

        public void Tick(float deltaTime, Vector3 desiredPosition)
        {
            if (deltaTime < 0f)
            {
                deltaTime = 0f;
            }

            if (State == BarracksSoldierState.Dead || State == BarracksSoldierState.Respawning || !IsAlive)
            {
                TickRespawn(deltaTime);
                TickVisualAnimation(deltaTime);
                return;
            }

            if (_blockTarget.IsNull() || _blockTarget.IsDead)
            {
                _blockTarget = null;
                if (State == BarracksSoldierState.Blocking)
                {
                    State = BarracksSoldierState.Moving;
                }
            }

            if (_blockTarget.IsNotNull())
            {
                State = BarracksSoldierState.Blocking;
                TickAttack(deltaTime);
            }

            TickMovement(deltaTime, desiredPosition);
            TickVisualAnimation(deltaTime);
        }

        public override void ApplyDamage(float amount, DamageType damageType = DamageType.Physical, bool halfPhysicalArmorPenetration = false)
        {
            if (!IsAlive)
            {
                return;
            }

            float finalDamage = Mathf.Max(0f, amount);
            if (finalDamage <= 0f)
            {
                return;
            }

            _currentHp -= finalDamage;
            if (_currentHp <= 0f)
            {
                _currentHp = 0f;
                Die();
            }
        }

        protected override void Die()
        {
            if (State == BarracksSoldierState.Dead || State == BarracksSoldierState.Respawning)
            {
                return;
            }

            _blockTarget = null;
            _attackCooldownLeft = 0f;
            _attackVisualHoldLeft = 0f;
            _respawnLeft = _respawnSec;
            State = BarracksSoldierState.Dead;
            SetVisualAlive(false);
            TickVisualAnimation(0f);
            Died?.Invoke(this);
        }

        private void TickAttack(float deltaTime)
        {
            if (_blockTarget.IsNull() || _blockTarget.IsDead)
            {
                return;
            }

            _attackCooldownLeft = Mathf.Max(0f, _attackCooldownLeft - deltaTime);
            if (_attackCooldownLeft > 0f)
            {
                return;
            }

            _blockTarget.ApplyDamage(_attackDamage, DamageType.Physical, false);
            _attackCooldownLeft = _attackCooldownSec;
            _attackVisualHoldLeft = Mathf.Max(AttackVisualHoldSec, _animAttackDuration);
        }

        private void TickMovement(float deltaTime, Vector3 desiredPosition)
        {
            desiredPosition.z = -0.5f;

            if (deltaTime <= 0f)
            {
                transform.position = desiredPosition;
                if ((_blockTarget.IsNull() || _blockTarget.IsDead) && (transform.position - desiredPosition).sqrMagnitude <= ArriveDistance * ArriveDistance)
                {
                    State = BarracksSoldierState.Idle;
                }
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, desiredPosition, _moveSpeed * deltaTime);
            if (_blockTarget.IsNull() || _blockTarget.IsDead)
            {
                float sqr = (transform.position - desiredPosition).sqrMagnitude;
                State = sqr <= ArriveDistance * ArriveDistance ? BarracksSoldierState.Idle : BarracksSoldierState.Moving;
            }
        }

        private void TickRespawn(float deltaTime)
        {
            if (State == BarracksSoldierState.Dead)
            {
                State = BarracksSoldierState.Respawning;
            }

            if (State != BarracksSoldierState.Respawning)
            {
                return;
            }

            _respawnLeft -= deltaTime;
            if (_respawnLeft > 0f)
            {
                return;
            }

            RestoreHealthToFull();
            _blockTarget = null;
            _attackCooldownLeft = 0f;
            _attackVisualHoldLeft = 0f;
            transform.position = new Vector3(_rallyPoint.x, _rallyPoint.y, -0.5f);
            State = BarracksSoldierState.Idle;
            SetVisualAlive(true);
            TickVisualAnimation(0f);
            Respawned?.Invoke(this);
        }

        private void SetVisualAlive(bool alive)
        {
            if (_renderer.IsNotNull())
            {
                _renderer.enabled = alive;
            }
        }

        private void TickVisualAnimation(float deltaTime)
        {
            _attackVisualHoldLeft = Mathf.Max(0f, _attackVisualHoldLeft - Mathf.Max(0f, deltaTime));
            int motionState = ToAnimatorMotionState();
            if (_useAnimator && _animator.IsNotNull())
            {
                _animator.SetInteger(MotionStateHash, motionState);
            }
        }

        private int ToAnimatorMotionState()
        {
            if (State == BarracksSoldierState.Dead || State == BarracksSoldierState.Respawning || !IsAlive)
            {
                return 3; // Die
            }

            if (_attackVisualHoldLeft > 0f)
            {
                return 2; // Attack
            }

            return State switch
            {
                BarracksSoldierState.Moving => 1, // Walk
                BarracksSoldierState.Blocking => 0, // Idle
                _ => 0
            };
        }

        private float ResolveAnimatorClipLength(params string[] nameTokens)
        {
            if (_animator.IsNull() || _animator.runtimeAnimatorController.IsNull())
            {
                return AttackVisualHoldSec;
            }

            AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;
            if (clips.IsNull() || clips.Length <= 0 || nameTokens.IsNull() || nameTokens.Length <= 0)
            {
                return AttackVisualHoldSec;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip.IsNull())
                {
                    continue;
                }

                string n = (clip.name ?? string.Empty).ToLowerInvariant();
                for (int tokenIndex = 0; tokenIndex < nameTokens.Length; tokenIndex++)
                {
                    string token = nameTokens[tokenIndex];
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        continue;
                    }

                    if (n.Contains(token.ToLowerInvariant()))
                    {
                        return Mathf.Max(AttackVisualHoldSec, clip.length);
                    }
                }
            }

            return AttackVisualHoldSec;
        }
    }
}
