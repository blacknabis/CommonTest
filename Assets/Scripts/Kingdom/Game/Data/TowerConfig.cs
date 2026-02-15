using System;
using UnityEngine;

namespace Kingdom.Game
{
    public enum TowerType
    {
        Archer = 0,
        Barracks = 1,
        Mage = 2,
        Artillery = 3
    }

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

            float armorPercent = 0f;
            switch (damageType)
            {
                case DamageType.Physical:
                    armorPercent = Mathf.Clamp(targetConfig.ArmorPhysical, 0f, 100f);
                    if (halfPhysicalArmorPenetration)
                    {
                        return damage * (1f - (armorPercent / 200f));
                    }

                    return damage * (1f - (armorPercent / 100f));

                case DamageType.Magic:
                    armorPercent = Mathf.Clamp(targetConfig.ArmorMagic, 0f, 100f);
                    return damage * (1f - (armorPercent / 100f));

                case DamageType.True:
                default:
                    return damage;
            }
        }
    }

    [Serializable]
    public struct BarracksData
    {
        public int SquadSize;
        public float RallyRange;
    }

    [Serializable]
    public struct TowerLevelData
    {
        public int Cost;
        public float Damage;
        public float Cooldown;
        public float Range;
        public Sprite SpriteOverride;
        public float VisualScale;
    }

    [CreateAssetMenu(fileName = "TowerConfig", menuName = "Kingdom/Game/Tower Config")]
    public class TowerConfig : ScriptableObject
    {
        public string TowerId = "BasicTower";
        public TowerType TowerType = TowerType.Archer;
        
        [Header("Damage Settings")]
        public DamageType DamageType = DamageType.Physical;
        public bool HalfPhysicalArmorPenetration;
        public bool CanTargetAir = true;
        public BarracksData BarracksData;

        [Header("Levels")]
        public TowerLevelData[] Levels = new TowerLevelData[3];

        // Legacy/Convenience properties for backward compatibility
        public int BuildCost => Levels != null && Levels.Length > 0 ? Levels[0].Cost : 70;
        public float AttackRange => Levels != null && Levels.Length > 0 ? Levels[0].Range : 2.2f;
        public float AttackCooldown => Levels != null && Levels.Length > 0 ? Levels[0].Cooldown : 0.75f;
        public float AttackDamage => Levels != null && Levels.Length > 0 ? Levels[0].Damage : 34f;
    }

    [CreateAssetMenu(fileName = "HeroConfig", menuName = "Kingdom/Game/Hero Config")]
    public class HeroConfig : ScriptableObject
    {
        public string HeroId = "DefaultHero";
        public string DisplayName = "Hero";
        public float MaxHp = 500f;
        public float MoveSpeed = 3.2f;
        public float AttackDamage = 30f;
        public float AttackCooldown = 0.8f;
        public float AttackRange = 1.8f;
    }

    [CreateAssetMenu(fileName = "SpellConfig", menuName = "Kingdom/Game/Spell Config")]
    public class SpellConfig : ScriptableObject
    {
        public string SpellId = "spell";
        public string DisplayName = "Spell";
        [Min(0f)] public float CooldownSeconds = 20f;
        [Min(0f)] public float EarlyCallCooldownReductionSeconds = 4f;
    }
}
