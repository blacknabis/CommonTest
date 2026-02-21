using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Kingdom.App;
using Kingdom.Game;
using UnityEditor;
using UnityEngine;

namespace Kingdom.Editor
{
    public static class KingdomSpriteBindingTools
    {
        private const string EnemyConfigFolder = ConfigResourcePaths.EnemyAssetFolder;
        private const string TowerConfigFolder = ConfigResourcePaths.TowerAssetFolder;
        private const string BarracksSoldierConfigFolder = ConfigResourcePaths.BarracksSoldierAssetFolder;

        [MenuItem("Tools/Kingdom/Sprites/Apply Sample Runtime Paths")]
        public static void ApplySampleRuntimePaths()
        {
            int changedCount = 0;

            string[] enemyGuids = FindConfigAssetGuids(
                "t:EnemyConfig",
                EnemyConfigFolder,
                ConfigResourcePaths.LegacyEnemyAssetFolder);
            for (int i = 0; i < enemyGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(enemyGuids[i]);
                EnemyConfig config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
                if (config == null)
                {
                    continue;
                }

                string expected = string.IsNullOrWhiteSpace(config.EnemyId)
                    ? string.Empty
                    : $"Animations/Enemies/{config.EnemyId.Trim()}/{config.EnemyId.Trim()}";
                if (config.RuntimeAnimatorControllerPath == expected)
                {
                    continue;
                }

                config.RuntimeAnimatorControllerPath = expected;
                EditorUtility.SetDirty(config);
                changedCount++;
            }

            string[] towerGuids = FindConfigAssetGuids(
                "t:TowerConfig",
                TowerConfigFolder,
                ConfigResourcePaths.LegacyTowerAssetFolder);
            for (int i = 0; i < towerGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(towerGuids[i]);
                TowerConfig config = AssetDatabase.LoadAssetAtPath<TowerConfig>(path);
                if (config == null)
                {
                    continue;
                }

                bool changed = false;
                string towerId = string.IsNullOrWhiteSpace(config.TowerId) ? "BasicTower" : config.TowerId.Trim();
                string expectedRuntime = $"Sprites/Towers/{towerId}/L{{level}}";
                if (config.RuntimeSpriteResourcePath != expectedRuntime)
                {
                    config.RuntimeSpriteResourcePath = expectedRuntime;
                    changed = true;
                }

                if (config.Levels == null || config.Levels.Length == 0)
                {
                    config.Levels = new TowerLevelData[3];
                    changed = true;
                }

                if (config.Levels.Length < 3)
                {
                    TowerLevelData[] expanded = new TowerLevelData[3];
                    for (int level = 0; level < config.Levels.Length; level++)
                    {
                        expanded[level] = config.Levels[level];
                    }

                    config.Levels = expanded;
                    changed = true;
                }

                if (config.Levels.Length > 0)
                {
                    for (int level = 0; level < config.Levels.Length; level++)
                    {
                        TowerLevelData data = config.Levels[level];
                        string expectedLevelPath = $"Sprites/Towers/{towerId}/L{level + 1}";
                        if (data.SpriteResourcePath != expectedLevelPath)
                        {
                            data.SpriteResourcePath = expectedLevelPath;
                            config.Levels[level] = data;
                            changed = true;
                        }
                    }
                }

                string expectedSoldierPath = config.TowerType == TowerType.Barracks
                    ? "Sprites/Barracks/Soldier"
                    : string.Empty;

                if (config.BarracksSoldierConfig != null &&
                    config.BarracksSoldierConfig.RuntimeSpriteResourcePath != expectedSoldierPath)
                {
                    config.BarracksSoldierConfig.RuntimeSpriteResourcePath = expectedSoldierPath;
                    EditorUtility.SetDirty(config.BarracksSoldierConfig);
                    changed = true;
                }

                if (config.BarracksData.SoldierSpriteResourcePath != expectedSoldierPath)
                {
                    BarracksData barracksData = config.BarracksData;
                    barracksData.SoldierSpriteResourcePath = expectedSoldierPath;
                    config.BarracksData = barracksData;
                    changed = true;
                }

                if (!changed)
                {
                    continue;
                }

                EditorUtility.SetDirty(config);
                changedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[KingdomSpriteBindingTools] Applied sample runtime paths. changed={changedCount}");
        }

        [MenuItem("Tools/Kingdom/Sprites/Validate Runtime Sprite Bindings")]
        public static void ValidateRuntimeSpriteBindings()
        {
            int checkedCount = 0;
            int resolvedCount = 0;
            var missing = new List<string>();

            EnemyConfig[] enemyConfigs = ConfigResourcePaths.LoadAllEnemyConfigs();
            for (int i = 0; i < enemyConfigs.Length; i++)
            {
                EnemyConfig config = enemyConfigs[i];
                if (config == null)
                {
                    continue;
                }

                checkedCount++;
                if (TryResolveEnemyAnimator(config, out string resolvedAnimatorPath))
                {
                    resolvedCount++;
                }
                else
                {
                    missing.Add($"Enemy {config.EnemyId}: animator missing (configured={config.RuntimeAnimatorControllerPath}, resolved={resolvedAnimatorPath})");
                }
            }

            TowerConfig[] towerConfigs = ConfigResourcePaths.LoadAllTowerConfigs();
            for (int i = 0; i < towerConfigs.Length; i++)
            {
                TowerConfig config = towerConfigs[i];
                if (config == null)
                {
                    continue;
                }

                checkedCount++;
                if (TryResolveSprite(ExpandTowerTemplatePath(config.RuntimeSpriteResourcePath, config.TowerType, 0)))
                {
                    resolvedCount++;
                }
                else
                {
                    missing.Add($"Tower {config.TowerId} runtime: {config.RuntimeSpriteResourcePath}");
                }

                if (config.Levels != null)
                {
                    for (int level = 0; level < config.Levels.Length; level++)
                    {
                        checkedCount++;
                        string path = config.Levels[level].SpriteResourcePath;
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            // Legacy assets may omit per-level paths and rely on runtime template.
                            path = ExpandTowerTemplatePath(config.RuntimeSpriteResourcePath, config.TowerType, level);
                        }

                        if (TryResolveSprite(path))
                        {
                            resolvedCount++;
                        }
                        else
                        {
                            missing.Add($"Tower {config.TowerId} L{level + 1}: {path}");
                        }
                    }
                }

                if (config.TowerType == TowerType.Barracks)
                {
                    checkedCount++;
                    if (TryResolveBarracksSoldierSprite(config, out string resolvedPath, out string source))
                    {
                        resolvedCount++;
                    }
                    else
                    {
                        missing.Add($"Tower {config.TowerId} Soldier ({source}): {resolvedPath}");
                    }
                }
            }

            Debug.Log($"[KingdomSpriteBindingTools] Sprite binding validate done. checked={checkedCount}, resolved={resolvedCount}, missing={missing.Count}");
            for (int i = 0; i < missing.Count; i++)
            {
                Debug.LogWarning($"[KingdomSpriteBindingTools] Missing sprite binding: {missing[i]}");
            }
        }

        [MenuItem("Tools/Kingdom/Sprites/Run Missing Hint Regression")]
        public static void RunMissingHintRegression()
        {
            MethodInfo appendHintMethod = typeof(GameScene).GetMethod(
                "AppendMissingFixHintSummary",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (appendHintMethod == null)
            {
                Debug.LogError("[KingdomSpriteBindingTools] Missing hint regression failed: GameScene.AppendMissingFixHintSummary method not found.");
                return;
            }

            var sampleMissing = new List<string>
            {
                "HeroConfig missing: Kingdom/Configs/Heroes/NoHero",
                "HeroConfig invalid HeroId: Kingdom/Configs/Heroes/NoId",
                "Hero sprite missing: heroId=Knight, action=attack, directPath=UI/Sprites/Heroes/InGame/Knight/attack_00, manifest=Sprites/Heroes/manifest, reason=missing",
                "WaveConfig missing: current stage has no wave config.",
                "Wave spawn entry missing: wave=1",
                "EnemyConfig reference missing: wave=1, entry=1",
                "Enemy animator missing: enemyId=Goblin, animatorPath=Animations/Enemies/Goblin/Goblin, reason=configured=(empty), conventional=Animations/Enemies/Goblin/Goblin",
                "TowerConfig missing: Kingdom/Configs/Towers/Barracks",
                "Tower level data missing: towerType=Barracks, config=Kingdom/Configs/Towers/Barracks",
                "Tower sprite missing: towerType=Barracks, towerId=Barracks, level=L1, levelPath=Sprites/Towers/Barracks/L1, runtimePath=Sprites/Towers/Barracks/L{level}, candidates=Sprites/Towers/Barracks/L1",
                "Barracks soldier sprite missing: towerId=Barracks, path=Sprites/Barracks/Soldier, candidates=Sprites/Barracks/Soldier"
            };

            var builder = new StringBuilder();
            try
            {
                appendHintMethod.Invoke(null, new object[] { builder, sampleMissing });
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[KingdomSpriteBindingTools] Missing hint regression failed: invoke exception: {ex.Message}");
                return;
            }

            string output = builder.ToString();
            string[] requiredHints =
            {
                "HeroConfig.HeroId 값을 채우세요.",
                "EnemyConfig.RuntimeAnimatorControllerPath를 유효한 Resources 경로로 지정하세요.",
                "TowerConfig.Levels[i].SpriteResourcePath를 지정하거나 TowerConfig.RuntimeSpriteResourcePath 템플릿을 설정하세요.",
                "TowerConfig.BarracksData.SoldierSpriteResourcePath를 지정하세요.",
                "WaveConfig.Waves[*].SpawnEntries[*].Enemy 참조를 지정하세요."
            };

            var missingHints = new List<string>();
            for (int i = 0; i < requiredHints.Length; i++)
            {
                if (output.IndexOf(requiredHints[i], System.StringComparison.Ordinal) < 0)
                {
                    missingHints.Add(requiredHints[i]);
                }
            }

            if (missingHints.Count <= 0)
            {
                Debug.Log($"[KingdomSpriteBindingTools] Missing hint regression passed. hints={requiredHints.Length}");
                return;
            }

            Debug.LogWarning($"[KingdomSpriteBindingTools] Missing hint regression failed. missing={missingHints.Count}\n{string.Join("\n", missingHints)}\n--- output ---\n{output}");
        }

        [MenuItem("Tools/Kingdom/Sprites/Migrate Barracks Soldier Config References")]
        public static void MigrateBarracksSoldierConfigReferences()
        {
            int towerCount = 0;
            int created = 0;
            int linked = 0;
            int skipped = 0;
            int failed = 0;

            EnsureAssetFolder(BarracksSoldierConfigFolder);
            string[] towerGuids = FindConfigAssetGuids(
                "t:TowerConfig",
                TowerConfigFolder,
                ConfigResourcePaths.LegacyTowerAssetFolder);
            for (int i = 0; i < towerGuids.Length; i++)
            {
                string towerPath = AssetDatabase.GUIDToAssetPath(towerGuids[i]);
                TowerConfig towerConfig = AssetDatabase.LoadAssetAtPath<TowerConfig>(towerPath);
                if (towerConfig == null)
                {
                    failed++;
                    Debug.LogWarning($"[KingdomSpriteBindingTools] Migration skip: TowerConfig load failed. path={towerPath}");
                    continue;
                }

                towerCount++;
                if (towerConfig.TowerType != TowerType.Barracks)
                {
                    skipped++;
                    continue;
                }

                string legacyPath = string.IsNullOrWhiteSpace(towerConfig.BarracksData.SoldierSpriteResourcePath)
                    ? string.Empty
                    : towerConfig.BarracksData.SoldierSpriteResourcePath.Trim();
                string fallbackPath = string.IsNullOrWhiteSpace(legacyPath) ? "Sprites/Barracks/Soldier" : legacyPath;

                BarracksSoldierConfig soldierConfig = towerConfig.BarracksSoldierConfig;
                if (soldierConfig == null)
                {
                    string towerAssetName = Path.GetFileNameWithoutExtension(towerPath);
                    string soldierAssetPath = $"{BarracksSoldierConfigFolder}/{towerAssetName}_Soldier.asset";
                    soldierConfig = AssetDatabase.LoadAssetAtPath<BarracksSoldierConfig>(soldierAssetPath);
                    if (soldierConfig == null)
                    {
                        soldierConfig = ScriptableObject.CreateInstance<BarracksSoldierConfig>();
                        soldierConfig.SoldierId = BuildDefaultSoldierId(towerConfig, towerAssetName);
                        soldierConfig.DisplayName = $"{towerConfig.TowerId} Soldier";
                        soldierConfig.RuntimeSpriteResourcePath = fallbackPath;
                        AssetDatabase.CreateAsset(soldierConfig, soldierAssetPath);
                        created++;
                    }

                    towerConfig.BarracksSoldierConfig = soldierConfig;
                    EditorUtility.SetDirty(towerConfig);
                    linked++;
                }

                bool soldierConfigChanged = false;
                if (string.IsNullOrWhiteSpace(soldierConfig.SoldierId))
                {
                    string towerAssetName = Path.GetFileNameWithoutExtension(towerPath);
                    soldierConfig.SoldierId = BuildDefaultSoldierId(towerConfig, towerAssetName);
                    soldierConfigChanged = true;
                }

                if (string.IsNullOrWhiteSpace(soldierConfig.DisplayName))
                {
                    soldierConfig.DisplayName = $"{towerConfig.TowerId} Soldier";
                    soldierConfigChanged = true;
                }

                if (string.IsNullOrWhiteSpace(soldierConfig.RuntimeSpriteResourcePath))
                {
                    soldierConfig.RuntimeSpriteResourcePath = fallbackPath;
                    soldierConfigChanged = true;
                }

                if (soldierConfigChanged)
                {
                    EditorUtility.SetDirty(soldierConfig);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AISpriteProcessor] MigrationSummary tower={towerCount} created={created} linked={linked} skipped={skipped} failed={failed}");
        }

        private static bool TryResolveSprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return false;
            }

            Sprite single = Resources.Load<Sprite>(resourcePath);
            if (single != null)
            {
                return true;
            }

            Sprite[] multiple = Resources.LoadAll<Sprite>(resourcePath);
            if (multiple != null && multiple.Length > 0)
            {
                return true;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            return texture != null;
        }

        private static bool TryResolveEnemyAnimator(EnemyConfig config, out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (config == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(config.RuntimeAnimatorControllerPath))
            {
                string configuredPath = config.RuntimeAnimatorControllerPath.Trim();
                RuntimeAnimatorController byConfig = Resources.Load<RuntimeAnimatorController>(configuredPath);
                if (byConfig != null)
                {
                    resolvedPath = configuredPath;
                    return true;
                }
            }

            if (string.IsNullOrWhiteSpace(config.EnemyId))
            {
                return false;
            }

            string enemyId = config.EnemyId.Trim();
            string conventionalPath = $"Animations/Enemies/{enemyId}/{enemyId}";
            RuntimeAnimatorController byConvention = Resources.Load<RuntimeAnimatorController>(conventionalPath);
            if (byConvention != null)
            {
                resolvedPath = conventionalPath;
                return true;
            }

            resolvedPath = conventionalPath;
            return false;
        }

        private static string ExpandTowerTemplatePath(string templatePath, TowerType towerType, int levelIndex)
        {
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                return string.Empty;
            }

            int level = Mathf.Max(1, levelIndex + 1);
            string expanded = templatePath.Replace("{tower}", towerType.ToString());
            expanded = expanded.Replace("{level}", level.ToString());
            return expanded;
        }

        private static string BuildDefaultSoldierId(TowerConfig towerConfig, string towerAssetName)
        {
            string baseId = !string.IsNullOrWhiteSpace(towerConfig.TowerId)
                ? towerConfig.TowerId.Trim()
                : towerAssetName;
            if (string.IsNullOrWhiteSpace(baseId))
            {
                baseId = "Barracks";
            }

            return $"{SanitizeId(baseId)}_Soldier";
        }

        private static string SanitizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Unknown";
            }

            var builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (char.IsLetterOrDigit(ch) || ch == '_')
                {
                    builder.Append(ch);
                }
                else
                {
                    builder.Append('_');
                }
            }

            return builder.ToString();
        }

        private static void EnsureAssetFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string[] parts = assetFolderPath.Split('/');
            if (parts.Length < 2 || parts[0] != "Assets")
            {
                return;
            }

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

        private static bool TryResolveBarracksSoldierSprite(TowerConfig config, out string resolvedPath, out string source)
        {
            resolvedPath = string.Empty;
            source = "none";
            if (config == null)
            {
                return false;
            }

            if (config.BarracksSoldierConfig != null &&
                !string.IsNullOrWhiteSpace(config.BarracksSoldierConfig.RuntimeSpriteResourcePath))
            {
                string configPath = config.BarracksSoldierConfig.RuntimeSpriteResourcePath.Trim();
                if (TryResolveSprite(configPath))
                {
                    resolvedPath = configPath;
                    source = "BarracksSoldierConfig";
                    return true;
                }

                resolvedPath = configPath;
                source = "BarracksSoldierConfig";
            }

            if (!string.IsNullOrWhiteSpace(config.BarracksData.SoldierSpriteResourcePath))
            {
                string legacyPath = config.BarracksData.SoldierSpriteResourcePath.Trim();
                if (TryResolveSprite(legacyPath))
                {
                    resolvedPath = legacyPath;
                    source = "Legacy";
                    return true;
                }

                resolvedPath = legacyPath;
                source = source == "none" ? "Legacy" : $"{source}+Legacy";
            }

            List<string> candidates = BuildBarracksSoldierPathCandidates(config);
            for (int i = 0; i < candidates.Count; i++)
            {
                string candidate = candidates[i];
                if (TryResolveSprite(candidate))
                {
                    resolvedPath = candidate;
                    source = "Convention";
                    return true;
                }
            }

            if (candidates.Count > 0)
            {
                resolvedPath = string.Join(", ", candidates);
                source = source == "none" ? "Convention" : $"{source}+Convention";
            }

            return false;
        }

        private static List<string> BuildBarracksSoldierPathCandidates(TowerConfig config)
        {
            var candidates = new List<string>
            {
                "UI/Sprites/Barracks/Soldier",
                "Sprites/Barracks/Soldier",
                "UI/Sprites/Towers/Barracks/Soldier",
                "Sprites/Towers/Barracks/Soldier",
                "Kingdom/Towers/Sprites/Barracks/Soldier"
            };

            if (config != null && !string.IsNullOrWhiteSpace(config.TowerId))
            {
                string towerId = config.TowerId.Trim();
                candidates.Add($"UI/Sprites/Towers/{towerId}/Soldier");
                candidates.Add($"Sprites/Towers/{towerId}/Soldier");
                candidates.Add($"Kingdom/Towers/Sprites/{towerId}/Soldier");
            }

            if (config != null && config.BarracksSoldierConfig != null && !string.IsNullOrWhiteSpace(config.BarracksSoldierConfig.SoldierId))
            {
                string soldierId = config.BarracksSoldierConfig.SoldierId.Trim();
                candidates.Add($"UI/Sprites/Barracks/Soldiers/{soldierId}");
                candidates.Add($"Sprites/Barracks/Soldiers/{soldierId}");
                candidates.Add($"Kingdom/Barracks/Soldiers/{soldierId}");
            }

            return candidates;
        }

        private static string[] FindConfigAssetGuids(string filter, string primaryFolder, string legacyFolder)
        {
            if (AssetDatabase.IsValidFolder(primaryFolder))
            {
                string[] primary = AssetDatabase.FindAssets(filter, new[] { primaryFolder });
                if (primary != null && primary.Length > 0)
                {
                    return primary;
                }
            }

            if (AssetDatabase.IsValidFolder(legacyFolder))
            {
                return AssetDatabase.FindAssets(filter, new[] { legacyFolder });
            }

            return new string[0];
        }
    }
}
