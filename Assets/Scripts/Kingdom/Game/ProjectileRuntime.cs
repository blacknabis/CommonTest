using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Game
{
    public readonly struct ProjectileSpawnData
    {
        public readonly Vector3 StartPosition;
        public readonly EnemyRuntime Target;
        public readonly Vector3 TargetPoint;
        public readonly bool UseTargetPoint;
        public readonly TowerType TowerType;
        public readonly DamageType DamageType;
        public readonly float Damage;
        public readonly bool HalfPhysicalArmorPenetration;
        public readonly bool CanTargetAir;
        public readonly float ReacquireRange;
        public readonly string ProjectileProfileId;

        public ProjectileSpawnData(
            Vector3 startPosition,
            EnemyRuntime target,
            Vector3 targetPoint,
            bool useTargetPoint,
            TowerType towerType,
            DamageType damageType,
            float damage,
            bool halfPhysicalArmorPenetration,
            bool canTargetAir,
            float reacquireRange,
            string projectileProfileId)
        {
            StartPosition = startPosition;
            Target = target;
            TargetPoint = targetPoint;
            UseTargetPoint = useTargetPoint;
            TowerType = towerType;
            DamageType = damageType;
            Damage = damage;
            HalfPhysicalArmorPenetration = halfPhysicalArmorPenetration;
            CanTargetAir = canTargetAir;
            ReacquireRange = reacquireRange;
            ProjectileProfileId = projectileProfileId;
        }
    }

    public sealed class ProjectileRuntime
    {
        private readonly int _id;
        private readonly Transform _transform;
        private readonly ProjectileMoveType _moveType;
        private readonly TowerType _towerType;
        private readonly DamageType _damageType;
        private readonly float _damage;
        private readonly bool _halfPhysicalArmorPenetration;
        private readonly bool _canTargetAir;
        private readonly float _reacquireRange;
        private readonly float _speed;
        private readonly float _maxLifetime;
        private readonly float _hitRadius;
        private readonly float _explosionRadius;
        private readonly bool _canPierce;
        private readonly int _maxHitCount;
        private readonly bool _allowSingleRetarget;

        private readonly HashSet<int> _hitTargets = new();
        private EnemyRuntime _target;
        private Vector3 _targetPoint;
        private bool _useTargetPoint;
        private bool _retargetTried;
        private float _elapsed;
        public Transform Transform => _transform;

        public ProjectileRuntime(int id, Transform transform, ProjectileProfile profile, in ProjectileSpawnData data)
        {
            _id = id;
            _transform = transform;
            _moveType = profile != null ? profile.MoveType : ProjectileMoveType.Homing;
            _towerType = data.TowerType;
            _damageType = data.DamageType;
            _damage = Mathf.Max(0f, data.Damage);
            _halfPhysicalArmorPenetration = data.HalfPhysicalArmorPenetration;
            _canTargetAir = data.CanTargetAir;
            _reacquireRange = Mathf.Max(0.25f, data.ReacquireRange);
            _speed = profile != null ? Mathf.Max(0.5f, profile.Speed) : 10f;
            _maxLifetime = profile != null ? Mathf.Max(0.15f, profile.MaxLifetime) : 1.8f;
            _hitRadius = profile != null ? Mathf.Max(0.05f, profile.HitRadius) : 0.12f;
            _explosionRadius = profile != null ? Mathf.Max(0f, profile.ExplosionRadius) : 0f;
            _canPierce = profile != null && profile.CanPierce;
            _maxHitCount = profile != null ? Mathf.Max(1, profile.MaxHitCount) : 1;
            _target = data.Target;
            _targetPoint = data.TargetPoint;
            _useTargetPoint = data.UseTargetPoint;
            _allowSingleRetarget = data.TowerType == TowerType.Mage && !_useTargetPoint;

            if (_transform != null)
            {
                _transform.position = data.StartPosition;
            }
        }

        public bool Tick(float deltaTime, ProjectileManager manager)
        {
            if (_transform == null || manager == null)
            {
                return false;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            if (_elapsed > _maxLifetime)
            {
                return false;
            }

            if (_useTargetPoint)
            {
                return TickToPoint(deltaTime, manager);
            }

            return TickToTarget(deltaTime, manager);
        }

        private bool TickToTarget(float deltaTime, ProjectileManager manager)
        {
            if (_target == null || _target.IsDead)
            {
                if (_allowSingleRetarget && !_retargetTried)
                {
                    _retargetTried = true;
                    _target = manager.TryFindNearestEnemy(_transform.position, _reacquireRange, _canTargetAir);
                }

                if (_target == null || _target.IsDead)
                {
                    return false;
                }
            }

            Vector3 aim = _target.transform.position;
            _targetPoint = aim;
            MoveTowards(aim, deltaTime);

            if ((_transform.position - aim).sqrMagnitude > _hitRadius * _hitRadius)
            {
                return true;
            }

            int instanceId = _target.GetInstanceID();
            if (!_hitTargets.Contains(instanceId))
            {
                _hitTargets.Add(instanceId);
                manager.ApplyDamageToEnemy(_target, _damage, _damageType, _halfPhysicalArmorPenetration);
            }

            if (_canPierce && _hitTargets.Count < _maxHitCount)
            {
                _target = manager.TryFindNearestEnemy(_transform.position, _reacquireRange, _canTargetAir, _hitTargets);
                return _target != null;
            }

            return false;
        }

        private bool TickToPoint(float deltaTime, ProjectileManager manager)
        {
            MoveTowards(_targetPoint, deltaTime);
            if ((_transform.position - _targetPoint).sqrMagnitude > _hitRadius * _hitRadius)
            {
                return true;
            }

            float radius = Mathf.Max(_hitRadius, _explosionRadius);
            manager.ApplyDamageInRadius(_targetPoint, radius, _damage, _damageType, _halfPhysicalArmorPenetration, _canTargetAir);
            return false;
        }

        private void MoveTowards(Vector3 destination, float deltaTime)
        {
            if (_moveType == ProjectileMoveType.Linear || _moveType == ProjectileMoveType.Ballistic || _moveType == ProjectileMoveType.Homing)
            {
                _transform.position = Vector3.MoveTowards(_transform.position, destination, _speed * Mathf.Max(0f, deltaTime));
            }
        }
    }
}
