using UnityEngine;

namespace Kingdom.Game
{
    public enum DamageType
    {
        Physical = 0,
        Magic = 1,
        True = 2
    }

    public static class DamageCalculator
    {
        public static float CalculateFinalDamage(
            float baseDamage,
            EnemyConfig targetConfig,
            DamageType damageType,
            bool halfPhysicalArmorPenetration = false)
        {
            float damage = Mathf.Max(0f, baseDamage);
            if (damage <= 0f || targetConfig == null)
            {
                return damage;
            }

            float resistPercent = targetConfig.GetResistancePercent(damageType);
            if (damageType == DamageType.Physical && halfPhysicalArmorPenetration)
            {
                resistPercent *= 0.5f;
            }

            resistPercent = Mathf.Clamp(resistPercent, 0f, EnemyConfig.MaxResistanceCap);
            return damage * (1f - resistPercent);
        }
    }
}
