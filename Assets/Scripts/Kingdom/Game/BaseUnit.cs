using UnityEngine;

namespace Kingdom.Game
{
    /// <summary>
    /// Base class for all combat units (Enemies, Soldiers, Heroes).
    /// Provides common HP management and IDamageable implementation.
    /// </summary>
    public abstract class BaseUnit : MonoBehaviour, IDamageable, ISelectableTarget
    {
        [SerializeField] protected float _currentHp;
        [SerializeField] protected float _maxHp;

        public float CurrentHp => _currentHp;
        public float MaxHp => _maxHp;
        public bool IsAlive => _currentHp > 0;

        // ISelectableTarget 구현
        public virtual string DisplayName => name;
        public virtual Vector3 Position => transform.position;
        public float HpRatio => _maxHp > 0 ? Mathf.Clamp01(_currentHp / _maxHp) : 0f;
        public virtual float AttackPower => 0f;
        public virtual float DefensePower => 0f;
        public virtual string UnitType => "Unit";

        public virtual void OnSelected() { }
        public virtual void OnDeselected() { }

        /// <summary>
        /// Initialize the unit with MaxHP.
        /// </summary>
        protected virtual void InitializeHealth(float maxHp)
        {
            _maxHp = Mathf.Max(1f, maxHp);
            _currentHp = _maxHp;
        }

        /// <summary>
        /// Apply damage to the unit.
        /// Derived classes should override this to implement specific damage calculation logic (e.g. Armor, Magic Resist).
        /// </summary>
        public virtual void ApplyDamage(float amount, DamageType damageType = DamageType.Physical, bool halfPhysicalArmorPenetration = false)
        {
            if (!IsAlive) return;

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

        protected void RestoreHealthToFull()
        {
            _currentHp = _maxHp;
        }

        protected virtual void Die()
        {
        }
    }
}
