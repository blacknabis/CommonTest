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

    public enum AttackDeliveryType
    {
        Projectile = 0,
        HitScan = 1,
        Melee = 2
    }

    public enum TowerTargetType
    {
        Ground = 0,
        Air = 1,
        Both = 2
    }

    public enum ProjectileMoveType
    {
        Homing = 0,
        Ballistic = 1,
        Linear = 2
    }

    [Serializable]
    public struct BarracksData
    {
        public int SquadSize;
        public float RallyRange;
        [Tooltip("Optional Resources path (without extension) for barracks soldier sprite. Example: Sprites/Barracks/Soldier")]
        public string SoldierSpriteResourcePath;

        [Header("Soldier Combat Stats")]
        public float SoldierMaxHp;
        public float SoldierDamage;
        public float SoldierAttackCooldown;
        public float SoldierRespawnSec;
    }

    [Serializable]
    public struct TowerLevelData
    {
        public int Cost;
        public float Damage;
        public float Cooldown;
        public float Range;
        public AttackDeliveryType AttackDeliveryType;
        public string ProjectileProfileId;
        public Sprite SpriteOverride;
        [Tooltip("Optional Resources path (without extension) for this level sprite. Example: Sprites/Towers/Archer_L1")]
        public string SpriteResourcePath;
        public float VisualScale;
    }

    [CreateAssetMenu(fileName = "ProjectileProfile", menuName = "Kingdom/Game/Projectile Profile")]
    public class ProjectileProfile : ScriptableObject
    {
        public string ProjectileId = "Projectile_Default";
        public ProjectileMoveType MoveType = ProjectileMoveType.Homing;
        public float Speed = 10f;
        public float MaxLifetime = 1.8f;
        public float HitRadius = 0.12f;
        public float ExplosionRadius = 0f;
        public bool CanPierce;
        public int MaxHitCount = 1;
    }

    [CreateAssetMenu(fileName = "TowerConfig", menuName = "Kingdom/Game/Tower Config")]
    public class TowerConfig : ScriptableObject
    {
        public string TowerId = "BasicTower";
        public TowerType TowerType = TowerType.Archer;
        [Tooltip("Optional fallback Resources path (without extension) for runtime tower sprite. Supports {level} and {tower} tokens.")]
        public string RuntimeSpriteResourcePath;
        
        [Header("Damage Settings")]
        public DamageType DamageType = DamageType.Physical;
        public TowerTargetType TargetType = TowerTargetType.Both;
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

        public bool CanTarget(bool isFlying)
        {
            TowerTargetType resolved = ResolveTargetType();
            return resolved switch
            {
                TowerTargetType.Both => true,
                TowerTargetType.Air => isFlying,
                _ => !isFlying
            };
        }

        public TowerTargetType ResolveTargetType()
        {
            // Legacy fallback path for older assets.
            if (TargetType == TowerTargetType.Both)
            {
                return TargetType;
            }

            if (CanTargetAir && TargetType == TowerTargetType.Ground)
            {
                return TowerTargetType.Both;
            }

            return TargetType;
        }
    }

}
