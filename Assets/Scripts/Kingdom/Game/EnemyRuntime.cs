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
    public class EnemyRuntime : MonoBehaviour, IDamageable
    {
        private EnemyConfig _config;
        private List<Vector3> _path;
        private int _pathIndex;
        private float _hp;
        private bool _isDead;
        private int _blockerTowerId = -1;
        private Vector3 _blockedAnchor;
        private float _blockedElapsed;
        private float _attackCooldownLeft;
        private float _regenTickAccum;
        private bool _deathBurstTriggered;
        private EnemyMotionState _motionState = EnemyMotionState.Moving;

        public event Action<EnemyRuntime> ReachedGoal;
        public event Action<EnemyRuntime> Killed;
        public event Action<EnemyRuntime, float> Damaged;
        public event Action<EnemyRuntime, float> Healed;
        public event Action<EnemyRuntime> Dodged;
        public event Action<EnemyRuntime, float> AttackPerformed;
        public event Action<EnemyRuntime, float, float, Vector3> DeathBurstTriggered;

        public bool IsDead => _isDead;
        public bool IsAlive => !_isDead;
        public bool IsFlying => _config != null && _config.IsFlyingUnit;
        public bool IsBoss => _config != null && _config.IsBossUnit;
        public bool IsInstaKillImmune => _config != null && _config.IsInstaKillImmune;
        public EnemyConfig Config => _config;
        public EnemyMotionState MotionState => _motionState;
        public bool IsBlocked => _blockerTowerId >= 0;

        public void Initialize(EnemyConfig config, List<Vector3> path)
        {
            _config = config;
            _path = path;
            _pathIndex = 0;
            _hp = config != null ? Mathf.Max(1f, config.MaxHp) : 1f;
            _isDead = false;
            _blockerTowerId = -1;
            _blockedElapsed = 0f;
            _attackCooldownLeft = 0f;
            _regenTickAccum = 0f;
            _deathBurstTriggered = false;
            _motionState = EnemyMotionState.Moving;

            if (_path != null && _path.Count > 0)
            {
                transform.position = _path[0];
            }
        }

        private void Update()
        {
            if (_isDead)
            {
                return;
            }

            if (IsBlocked)
            {
                TickBlockedState();
                TickRegen();
                return;
            }

            if (_path == null || _path.Count == 0 || _pathIndex >= _path.Count)
            {
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
                    _isDead = true;
                    _motionState = EnemyMotionState.Dead;
                    ReachedGoal?.Invoke(this);
                }
            }
        }

        public bool TryEnterBlock(int blockerTowerId, Vector3 blockAnchor)
        {
            if (_isDead || blockerTowerId < 0)
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
            if (_isDead || IsInstaKillImmune || IsBoss)
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
            if (!_isDead)
            {
                _motionState = EnemyMotionState.Moving;
            }
        }

        public void ApplyDamage(float amount, DamageType damageType = DamageType.Physical, bool halfPhysicalArmorPenetration = false)
        {
            if (_isDead)
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

            _hp -= finalDamage;
            Damaged?.Invoke(this, finalDamage);
            if (_hp <= 0f)
            {
                KillAndNotify();
            }
        }

        private void TickBlockedState()
        {
            _blockedElapsed += Time.deltaTime;
            _attackCooldownLeft = Mathf.Max(0f, _attackCooldownLeft - Time.deltaTime);

            if (_blockedElapsed >= 0.2f)
            {
                _motionState = EnemyMotionState.Attacking;
                TryAttackBlocker();
            }
            else
            {
                _motionState = EnemyMotionState.Blocked;
            }

            transform.position = Vector3.Lerp(transform.position, _blockedAnchor, Time.deltaTime * 8f);
        }

        private void TickRegen()
        {
            if (_config == null || _isDead)
            {
                return;
            }

            float regenPerSec = Mathf.Max(0f, _config.RegenHpPerSec);
            if (regenPerSec <= 0f || _hp >= _config.MaxHp)
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
            float before = _hp;
            _hp = Mathf.Min(_config.MaxHp, _hp + (regenPerSec * dt));
            float healed = _hp - before;
            if (healed > 0f)
            {
                Healed?.Invoke(this, healed);
            }
        }

        private void TryAttackBlocker()
        {
            if (_config == null || _attackCooldownLeft > 0f)
            {
                return;
            }

            float min = Mathf.Max(0f, _config.AttackDamageMin);
            float max = Mathf.Max(min, _config.AttackDamageMax);
            float damage = UnityEngine.Random.Range(min, max);
            if (damage > 0f)
            {
                AttackPerformed?.Invoke(this, damage);
            }

            _attackCooldownLeft = Mathf.Max(0.1f, _config.AttackCooldownSec);
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
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            _motionState = EnemyMotionState.Dead;
            TriggerDeathBurstOnce();
            Killed?.Invoke(this);
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
    }
}
