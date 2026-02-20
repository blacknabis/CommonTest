using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Kingdom.Game;
using Kingdom.WorldMap;

namespace Kingdom.Editor
{
    public static class KingdomCoreDataBuilder
    {
        private const string DataRoot = "Assets/Resources/Data";
        private const string TowerConfigFolder = "Assets/Resources/Data/TowerConfigs";
        private const string HeroConfigFolder = "Assets/Resources/Data/HeroConfigs";
        private const string StageConfigPath = "Assets/Resources/Data/StageConfigs/World1_StageConfig.asset";

        [MenuItem("Tools/Kingdom/Rebuild Core Data Assets")]
        public static void RebuildCoreDataAssets()
        {
            EnsureFolder(DataRoot);
            EnsureFolder(TowerConfigFolder);
            EnsureFolder(HeroConfigFolder);

            BuildTowerConfigs();
            BuildHeroConfigs();
            RelinkStageWaveConfigs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[KingdomCoreDataBuilder] Core data assets rebuilt and saved.");
        }

        private static void BuildTowerConfigs()
        {
            BuildTowerConfig(TowerType.Archer, TowerTargetType.Both, DamageType.Physical, false, true, 1.0f, 34f, 0.75f, 2.2f);
            BuildTowerConfig(TowerType.Barracks, TowerTargetType.Ground, DamageType.Physical, false, false, 1.1f, 8f, 1.0f, 1.6f);
            BuildTowerConfig(TowerType.Mage, TowerTargetType.Both, DamageType.Magic, false, true, 1.4f, 34f, 1.35f, 2.0f);
            BuildTowerConfig(TowerType.Artillery, TowerTargetType.Ground, DamageType.Physical, true, false, 1.8f, 25f, 2.6f, 2.4f);
        }

        private static void BuildTowerConfig(
            TowerType type,
            TowerTargetType targetType,
            DamageType damageType,
            bool halfPhysicalArmorPenetration,
            bool canTargetAir,
            float costMod,
            float baseDamage,
            float baseCooldown,
            float baseRange)
        {
            string path = $"{TowerConfigFolder}/{type}.asset";
            TowerConfig config = AssetDatabase.LoadAssetAtPath<TowerConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<TowerConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            int l1Cost = Mathf.RoundToInt(70f * costMod);
            int l2Cost = Mathf.RoundToInt(l1Cost * 1.35f);
            int l3Cost = Mathf.RoundToInt(l2Cost * 1.45f);
            float baseScale = 0.65f;
            AttackDeliveryType delivery = type == TowerType.Barracks ? AttackDeliveryType.Melee : AttackDeliveryType.Projectile;
            string projectileId = type switch
            {
                TowerType.Archer => "Archer_Arrow",
                TowerType.Mage => "Mage_Bolt",
                TowerType.Artillery => "Artillery_Shell",
                _ => string.Empty
            };

            config.TowerId = type.ToString();
            config.TowerType = type;
            config.TargetType = targetType;
            config.DamageType = damageType;
            config.CanTargetAir = canTargetAir;
            config.HalfPhysicalArmorPenetration = halfPhysicalArmorPenetration;
            config.Levels = new TowerLevelData[3];
            config.Levels[0] = new TowerLevelData
            {
                Cost = l1Cost,
                Damage = baseDamage,
                Cooldown = Mathf.Max(0.08f, baseCooldown),
                Range = Mathf.Max(0.4f, baseRange),
                AttackDeliveryType = delivery,
                ProjectileProfileId = projectileId,
                SpriteOverride = null,
                VisualScale = baseScale
            };
            config.Levels[1] = new TowerLevelData
            {
                Cost = l2Cost,
                Damage = baseDamage * 1.3f,
                Cooldown = Mathf.Max(0.08f, baseCooldown * 0.95f),
                Range = baseRange * 1.05f,
                AttackDeliveryType = delivery,
                ProjectileProfileId = projectileId,
                SpriteOverride = null,
                VisualScale = baseScale + 0.08f
            };
            config.Levels[2] = new TowerLevelData
            {
                Cost = l3Cost,
                Damage = baseDamage * 1.3f * 1.3f,
                Cooldown = Mathf.Max(0.08f, baseCooldown * 0.95f * 0.95f),
                Range = baseRange * 1.05f * 1.1f,
                AttackDeliveryType = delivery,
                ProjectileProfileId = projectileId,
                SpriteOverride = null,
                VisualScale = baseScale + 0.16f
            };

            config.BarracksData = type == TowerType.Barracks
                ? new BarracksData { SquadSize = 3, RallyRange = Mathf.Max(0.6f, baseRange) }
                : new BarracksData { SquadSize = 0, RallyRange = 0f };

            EditorUtility.SetDirty(config);
        }

        private static void BuildHeroConfigs()
        {
            BuildHeroConfig(new HeroProfile
            {
                HeroId = "DefaultHero",
                DisplayName = "Knight",
                Role = HeroRole.Tank,
                MaxHp = 620f,
                MoveSpeed = 3.0f,
                AttackDamage = 26f,
                DamageMin = 23f,
                DamageMax = 31f,
                ArmorPercent = 0.22f,
                MagicResistPercent = 0.14f,
                AttackCooldown = 0.9f,
                AttackRange = 1.7f,
                RespawnSec = 12.5f,
                HpGrowth = 52f,
                DamageGrowth = 2.6f,
                ArmorGrowth = 0.006f,
                MagicResistGrowth = 0.004f,
                ActiveSkillId = "shieldslam",
                ActiveCooldownSec = 8.5f,
                ActiveRange = 2.1f
            });

            BuildHeroConfig(new HeroProfile
            {
                HeroId = "ArcherHero",
                DisplayName = "Ranger",
                Role = HeroRole.RangedDps,
                MaxHp = 460f,
                MoveSpeed = 3.5f,
                AttackDamage = 28f,
                DamageMin = 24f,
                DamageMax = 33f,
                ArmorPercent = 0.08f,
                MagicResistPercent = 0.12f,
                AttackCooldown = 0.72f,
                AttackRange = 2.5f,
                RespawnSec = 14f,
                HpGrowth = 35f,
                DamageGrowth = 3.2f,
                ArmorGrowth = 0.003f,
                MagicResistGrowth = 0.004f,
                ActiveSkillId = "multishot",
                ActiveCooldownSec = 9f,
                ActiveRange = 3f
            });

            BuildHeroConfig(new HeroProfile
            {
                HeroId = "MageHero",
                DisplayName = "Mage",
                Role = HeroRole.MagicDps,
                MaxHp = 420f,
                MoveSpeed = 3.2f,
                AttackDamage = 33f,
                DamageMin = 28f,
                DamageMax = 39f,
                ArmorPercent = 0.06f,
                MagicResistPercent = 0.2f,
                AttackCooldown = 0.85f,
                AttackRange = 2.2f,
                RespawnSec = 15f,
                HpGrowth = 33f,
                DamageGrowth = 3.8f,
                ArmorGrowth = 0.0025f,
                MagicResistGrowth = 0.0055f,
                ActiveSkillId = "arcaneburst",
                ActiveCooldownSec = 10.5f,
                ActiveRange = 2.8f
            });
        }

        private static void BuildHeroConfig(HeroProfile profile)
        {
            string path = $"{HeroConfigFolder}/{profile.HeroId}.asset";
            HeroConfig config = AssetDatabase.LoadAssetAtPath<HeroConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<HeroConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            config.HeroId = profile.HeroId;
            config.DisplayName = profile.DisplayName;
            config.Role = profile.Role;
            config.MaxHp = profile.MaxHp;
            config.MoveSpeed = profile.MoveSpeed;
            config.AttackDamage = profile.AttackDamage;
            config.DamageMin = profile.DamageMin;
            config.DamageMax = profile.DamageMax;
            config.ArmorPercent = profile.ArmorPercent;
            config.MagicResistPercent = profile.MagicResistPercent;
            config.AttackCooldown = profile.AttackCooldown;
            config.AttackRange = profile.AttackRange;
            config.RespawnSec = profile.RespawnSec;
            config.StartLevel = 1;
            config.MaxLevel = 10;
            config.HpGrowthPerLevel = profile.HpGrowth;
            config.DamageGrowthPerLevel = profile.DamageGrowth;
            config.ArmorGrowthPerLevel = profile.ArmorGrowth;
            config.MagicResistGrowthPerLevel = profile.MagicResistGrowth;
            config.PassiveSkillId = string.Empty;
            config.ActiveSkillId = profile.ActiveSkillId;
            config.ActiveCooldownSec = profile.ActiveCooldownSec;
            config.ActiveRange = profile.ActiveRange;

            EditorUtility.SetDirty(config);
        }

        private static void RelinkStageWaveConfigs()
        {
            StageConfig stageConfig = AssetDatabase.LoadAssetAtPath<StageConfig>(StageConfigPath);
            if (stageConfig == null || stageConfig.Stages == null || stageConfig.Stages.Count == 0)
            {
                Debug.LogWarning("[KingdomCoreDataBuilder] StageConfig missing or empty. Wave relink skipped.");
                return;
            }

            bool changed = false;
            List<StageData> stages = stageConfig.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                StageData stage = stages[i];
                if (stage.StageId <= 0)
                {
                    continue;
                }

                WaveConfig wave = AssetDatabase.LoadAssetAtPath<WaveConfig>($"Assets/Resources/Data/WaveConfigs/Stage_{stage.StageId}_WaveConfig.asset");
                if (wave == null)
                {
                    continue;
                }

                if (stage.WaveConfig == wave)
                {
                    continue;
                }

                stage.WaveConfig = wave;
                stages[i] = stage;
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            stageConfig.Stages = stages;
            EditorUtility.SetDirty(stageConfig);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private struct HeroProfile
        {
            public string HeroId;
            public string DisplayName;
            public HeroRole Role;
            public float MaxHp;
            public float MoveSpeed;
            public float AttackDamage;
            public float DamageMin;
            public float DamageMax;
            public float ArmorPercent;
            public float MagicResistPercent;
            public float AttackCooldown;
            public float AttackRange;
            public float RespawnSec;
            public float HpGrowth;
            public float DamageGrowth;
            public float ArmorGrowth;
            public float MagicResistGrowth;
            public string ActiveSkillId;
            public float ActiveCooldownSec;
            public float ActiveRange;
        }
    }
}
