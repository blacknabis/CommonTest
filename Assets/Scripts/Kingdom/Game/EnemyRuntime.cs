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
    /// 테스트용 적 런타임 엔티티. 경로를 따라 이동하고 도착/사망 이벤트를 발행한다.
    /// </summary>
    public class EnemyRuntime : MonoBehaviour
    {
        private EnemyConfig _config;
        private List<Vector3> _path;
        private int _pathIndex;
        private float _hp;
        private bool _isDead;
        private int _blockerTowerId = -1;
        private Vector3 _blockedAnchor;
        private float _blockedElapsed;
        private EnemyMotionState _motionState = EnemyMotionState.Moving;

        public event Action<EnemyRuntime> ReachedGoal;
        public event Action<EnemyRuntime> Killed;
        public bool IsDead => _isDead;
        public bool IsFlying => _config != null && _config.IsFlying;
        public bool IsBoss => _config != null && _config.IsBoss;
        public bool IsInstaKillImmune => _config != null && _config.IsInstaKillImmune;
        public EnemyConfig Config => _config;
        public EnemyMotionState MotionState => _motionState;
        public bool IsBlocked => _blockerTowerId >= 0;

        public void Initialize(EnemyConfig config, List<Vector3> path)
        {
            _config = config;
            _path = path;
            _pathIndex = 0;
            _hp = config != null ? Mathf.Max(1f, config.HP) : 1f;
            _isDead = false;
            _blockerTowerId = -1;
            _blockedElapsed = 0f;
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
                return;
            }

            if (_path == null || _path.Count == 0 || _pathIndex >= _path.Count)
            {
                return;
            }

            float speed = _config != null ? Mathf.Max(0.1f, _config.MoveSpeed) : 1f;
            Vector3 target = _path[_pathIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

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

            // Boss/elite forced-control immunity: barracks block is treated as hard CC.
            if (IsBoss)
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
            _motionState = EnemyMotionState.Blocked;
            return true;
        }

        public bool TryApplyInstantKill()
        {
            if (_isDead || IsInstaKillImmune || IsBoss)
            {
                return false;
            }

            _isDead = true;
            _motionState = EnemyMotionState.Dead;
            Killed?.Invoke(this);
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
            if (_hp <= 0f)
            {
                _isDead = true;
                _motionState = EnemyMotionState.Dead;
                Killed?.Invoke(this);
            }
        }

        private void TickBlockedState()
        {
            _blockedElapsed += Time.deltaTime;

            // Blocked 직후 짧은 딜레이 후 Attacking 상태로 전환.
            if (_blockedElapsed >= 0.2f)
            {
                _motionState = EnemyMotionState.Attacking;
            }
            else
            {
                _motionState = EnemyMotionState.Blocked;
            }

            transform.position = Vector3.Lerp(transform.position, _blockedAnchor, Time.deltaTime * 8f);
        }
    }
}
