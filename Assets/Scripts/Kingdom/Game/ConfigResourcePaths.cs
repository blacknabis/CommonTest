using UnityEngine;
using Kingdom.WorldMap;

namespace Kingdom.Game
{
    /// <summary>
    /// Canonical Resources path rules for runtime configs.
    /// New rule: Kingdom/Configs/<Category>/<Id>
    /// Legacy paths are kept as read fallback during migration.
    /// </summary>
    public static class ConfigResourcePaths
    {
        public const string HeroPrefix = "Kingdom/Configs/Heroes/";
        public const string TowerPrefix = "Kingdom/Configs/Towers/";
        public const string WavePrefix = "Kingdom/Configs/Waves/";
        public const string StagePrefix = "Kingdom/Configs/Stages/";
        public const string EnemyPrefix = "Kingdom/Configs/Enemies/";
        public const string BarracksSoldierPrefix = "Kingdom/Configs/BarracksSoldiers/";

        private const string LegacyHeroPrefix = "Data/HeroConfigs/";
        private const string LegacyTowerPrefix = "Data/TowerConfigs/";
        private const string LegacyWavePrefix = "Data/WaveConfigs/";
        private const string LegacyStagePrefix = "Data/StageConfigs/";
        private const string LegacyEnemyPrefix = "Kingdom/Enemies/Config/";
        private const string LegacyBarracksSoldierPrefix = "Data/BarracksSoldierConfigs/";

        public static HeroConfig LoadHeroConfig(string heroId)
        {
            return LoadById<HeroConfig>(heroId, HeroPrefix, LegacyHeroPrefix);
        }

        public static TowerConfig LoadTowerConfig(string towerId)
        {
            return LoadById<TowerConfig>(towerId, TowerPrefix, LegacyTowerPrefix);
        }

        public static WaveConfig LoadWaveConfigByStageId(int stageId)
        {
            if (stageId <= 0)
            {
                return null;
            }

            string id = $"Stage_{stageId}_WaveConfig";
            return LoadById<WaveConfig>(id, WavePrefix, LegacyWavePrefix);
        }

        public static StageConfig LoadStageConfigByWorldId(int worldId)
        {
            if (worldId <= 0)
            {
                return null;
            }

            string id = $"World{worldId}_StageConfig";
            return LoadById<StageConfig>(id, StagePrefix, LegacyStagePrefix);
        }

        public static EnemyConfig[] LoadAllEnemyConfigs()
        {
            return LoadAllWithFallback<EnemyConfig>(EnemyPrefix.TrimEnd('/'), LegacyEnemyPrefix.TrimEnd('/'));
        }

        public static TowerConfig[] LoadAllTowerConfigs()
        {
            return LoadAllWithFallback<TowerConfig>(TowerPrefix.TrimEnd('/'), LegacyTowerPrefix.TrimEnd('/'));
        }

        public static T[] LoadAllWithFallback<T>(string canonicalFolder, string legacyFolder) where T : Object
        {
            T[] canonical = Resources.LoadAll<T>(canonicalFolder);
            if (canonical != null && canonical.Length > 0)
            {
                return canonical;
            }

            return Resources.LoadAll<T>(legacyFolder);
        }

        private static T LoadById<T>(string id, string canonicalPrefix, string legacyPrefix) where T : Object
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            string trimmed = id.Trim();
            T byCanonical = Resources.Load<T>(canonicalPrefix + trimmed);
            if (byCanonical != null)
            {
                return byCanonical;
            }

            return Resources.Load<T>(legacyPrefix + trimmed);
        }

#if UNITY_EDITOR
        public const string HeroAssetFolder = "Assets/Resources/Kingdom/Configs/Heroes";
        public const string TowerAssetFolder = "Assets/Resources/Kingdom/Configs/Towers";
        public const string WaveAssetFolder = "Assets/Resources/Kingdom/Configs/Waves";
        public const string StageAssetFolder = "Assets/Resources/Kingdom/Configs/Stages";
        public const string EnemyAssetFolder = "Assets/Resources/Kingdom/Configs/Enemies";
        public const string BarracksSoldierAssetFolder = "Assets/Resources/Kingdom/Configs/BarracksSoldiers";

        public const string LegacyHeroAssetFolder = "Assets/Resources/Data/HeroConfigs";
        public const string LegacyTowerAssetFolder = "Assets/Resources/Data/TowerConfigs";
        public const string LegacyWaveAssetFolder = "Assets/Resources/Data/WaveConfigs";
        public const string LegacyStageAssetFolder = "Assets/Resources/Data/StageConfigs";
        public const string LegacyEnemyAssetFolder = "Assets/Resources/Kingdom/Enemies/Config";
        public const string LegacyBarracksSoldierAssetFolder = "Assets/Resources/Data/BarracksSoldierConfigs";
#endif
    }
}
