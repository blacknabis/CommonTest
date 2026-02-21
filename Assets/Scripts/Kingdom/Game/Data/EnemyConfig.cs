using UnityEngine;
using UnityEngine.Serialization;

namespace Kingdom.Game
{
    public enum EnemyType
    {
        Ground = 0,
        Flying = 1,
        Boss = 2
    }

    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Kingdom/Game/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        public const float MaxResistanceCap = 0.95f;

        [Header("Identity")]
        public string EnemyId = "Enemy";
        public string DisplayName = "Enemy";
        public EnemyType EnemyType = EnemyType.Ground;

        [Header("Visual")]
        [Tooltip("Optional Resources path (without extension) for runtime animator controller loading. Example: Animations/Enemies/Goblin/Goblin")]
        public string RuntimeAnimatorControllerPath;
        public Color Tint = Color.white;
        [Range(0.2f, 2f)] public float VisualScale = 0.6f;

        [Header("Combat")]
        [FormerlySerializedAs("HP")] public float MaxHp = 100f;
        [Range(0f, MaxResistanceCap)] public float ArmorPercent;
        [Range(0f, MaxResistanceCap)] public float MagicResistPercent;
        public float AttackDamageMin = 5f;
        public float AttackDamageMax = 10f;
        public float AttackCooldownSec = 1.0f;
        public float AttackRange = 0.9f;
        [Range(0f, 100f)] public float ArmorPhysical;
        [Range(0f, 100f)] public float ArmorMagic;
        public float MoveSpeed = 2.5f;

        [Header("Rewards")]
        [FormerlySerializedAs("GoldBounty")] public int BountyGold = 5;
        public int DamageToBase = 1;

        [Header("Behavior")]
        public bool CanBeBlockedByBarracks = true;
        [Range(0f, 1f)] public float DodgeChance;
        public float RegenHpPerSec;
        public float DeathExplosionRadius;
        public float DeathExplosionDamage;

        [Header("Immunity")]
        [Tooltip("Legacy compatibility flags. Prefer EnemyType + CanBeBlockedByBarracks.")]
        public bool IsFlying;
        public bool IsBoss;
        public bool IsInstaKillImmune;

        public bool IsFlyingUnit => EnemyType == EnemyType.Flying || IsFlying;
        public bool IsBossUnit => EnemyType == EnemyType.Boss || IsBoss;
        public bool CanBeBlocked => CanBeBlockedByBarracks && !IsFlyingUnit && !IsBossUnit;
        public int GoldBounty => BountyGold;
        public float HP => MaxHp;

        public float GetResistancePercent(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Physical:
                    return ResolvePercent(ArmorPercent, ArmorPhysical);
                case DamageType.Magic:
                    return ResolvePercent(MagicResistPercent, ArmorMagic);
                case DamageType.True:
                default:
                    return 0f;
            }
        }

        private static float ResolvePercent(float normalizedPercent, float legacyPercent)
        {
            if (normalizedPercent > 0f)
            {
                return Mathf.Clamp(normalizedPercent, 0f, MaxResistanceCap);
            }

            if (legacyPercent > 0f)
            {
                return Mathf.Clamp01(legacyPercent / 100f);
            }

            return 0f;
        }
    }
}
