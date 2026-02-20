using UnityEngine;

namespace Kingdom.Game
{
    public enum HeroRole
    {
        Tank = 0,
        RangedDps = 1,
        MagicDps = 2,
        Support = 3
    }

    [CreateAssetMenu(fileName = "HeroConfig", menuName = "Kingdom/Game/Hero Config")]
    public class HeroConfig : ScriptableObject
    {
        [Header("Identity")]
        public string HeroId = "DefaultHero";
        public string DisplayName = "Hero";
        public HeroRole Role = HeroRole.RangedDps;

        [Header("Base Stats")]
        public float MaxHp = 500f;
        public float MoveSpeed = 3.2f;
        public float AttackDamage = 30f;
        public float DamageMin;
        public float DamageMax;
        [Range(0f, 0.95f)] public float ArmorPercent;
        [Range(0f, 0.95f)] public float MagicResistPercent;
        public float AttackCooldown = 0.8f;
        public float AttackRange = 1.8f;
        public float RespawnSec = 15f;

        [Header("Growth")]
        [Min(1)] public int StartLevel = 1;
        [Min(1)] public int MaxLevel = 10;
        public float HpGrowthPerLevel = 45f;
        public float DamageGrowthPerLevel = 3f;
        public float ArmorGrowthPerLevel = 0.005f;
        public float MagicResistGrowthPerLevel = 0.005f;

        [Header("Skills")]
        public string PassiveSkillId;
        public string ActiveSkillId;
        public float ActiveCooldownSec = 12f;
        public float ActiveRange = 2.1f;

        public int ClampLevel(int level)
        {
            int maxLv = Mathf.Max(1, MaxLevel);
            int minLv = Mathf.Clamp(StartLevel, 1, maxLv);
            return Mathf.Clamp(level, minLv, maxLv);
        }

        public float GetMaxHp(int level)
        {
            int lv = ClampLevel(level);
            return Mathf.Max(1f, MaxHp + (HpGrowthPerLevel * (lv - 1)));
        }

        public float GetMoveSpeed(int level)
        {
            int lv = ClampLevel(level);
            return Mathf.Max(0.1f, MoveSpeed + (0.02f * (lv - 1)));
        }

        public float GetDamageMin(int level)
        {
            float baseMin = DamageMin > 0f ? DamageMin : Mathf.Max(1f, AttackDamage * 0.85f);
            return Mathf.Max(1f, baseMin + (DamageGrowthPerLevel * (ClampLevel(level) - 1)));
        }

        public float GetDamageMax(int level)
        {
            float baseMax = DamageMax > 0f ? DamageMax : Mathf.Max(1f, AttackDamage * 1.15f);
            float scaled = baseMax + (DamageGrowthPerLevel * (ClampLevel(level) - 1) * 1.1f);
            return Mathf.Max(GetDamageMin(level), scaled);
        }

        public float GetAttackCooldown(int level)
        {
            int lv = ClampLevel(level);
            return Mathf.Max(0.08f, AttackCooldown * Mathf.Pow(0.985f, lv - 1));
        }

        public float GetAttackRange(int level)
        {
            int lv = ClampLevel(level);
            return Mathf.Max(0.4f, AttackRange * (1f + (0.01f * (lv - 1))));
        }

        public float GetRespawnSec(int level)
        {
            int lv = ClampLevel(level);
            return Mathf.Max(2f, RespawnSec * Mathf.Pow(0.98f, lv - 1));
        }

        public float GetArmorPercent(int level)
        {
            int lv = ClampLevel(level);
            return Mathf.Clamp(ArmorPercent + (ArmorGrowthPerLevel * (lv - 1)), 0f, 0.95f);
        }

        public float GetMagicResistPercent(int level)
        {
            int lv = ClampLevel(level);
            return Mathf.Clamp(MagicResistPercent + (MagicResistGrowthPerLevel * (lv - 1)), 0f, 0.95f);
        }

        public float GetActiveCooldownSec(int level)
        {
            int lv = ClampLevel(level);
            return Mathf.Max(0.5f, ActiveCooldownSec * Mathf.Pow(0.99f, lv - 1));
        }

        public int GetRequiredXpForNextLevel(int level)
        {
            int lv = Mathf.Max(1, level);
            return 100 + (lv - 1) * 80;
        }
    }
}
