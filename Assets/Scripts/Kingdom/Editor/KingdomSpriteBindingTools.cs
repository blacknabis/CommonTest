using System.Collections.Generic;
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
        private const string EnemyConfigFolder = "Assets/Resources/Kingdom/Enemies/Config";
        private const string TowerConfigFolder = "Assets/Resources/Data/TowerConfigs";

        [MenuItem("Tools/Kingdom/Sprites/Apply Sample Runtime Paths")]
        public static void ApplySampleRuntimePaths()
        {
            int changedCount = 0;

            string[] enemyGuids = AssetDatabase.FindAssets("t:EnemyConfig", new[] { EnemyConfigFolder });
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
                    : $"Sprites/Enemies/{config.EnemyId.Trim()}";
                if (config.RuntimeSpriteResourcePath == expected)
                {
                    continue;
                }

                config.RuntimeSpriteResourcePath = expected;
                EditorUtility.SetDirty(config);
                changedCount++;
            }

            string[] towerGuids = AssetDatabase.FindAssets("t:TowerConfig", new[] { TowerConfigFolder });
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

            EnemyConfig[] enemyConfigs = Resources.LoadAll<EnemyConfig>("Kingdom/Enemies/Config");
            for (int i = 0; i < enemyConfigs.Length; i++)
            {
                EnemyConfig config = enemyConfigs[i];
                if (config == null)
                {
                    continue;
                }

                checkedCount++;
                if (TryResolveSprite(config.RuntimeSpriteResourcePath))
                {
                    resolvedCount++;
                }
                else
                {
                    missing.Add($"Enemy {config.EnemyId}: {config.RuntimeSpriteResourcePath}");
                }
            }

            TowerConfig[] towerConfigs = Resources.LoadAll<TowerConfig>("Data/TowerConfigs");
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

                if (!string.IsNullOrWhiteSpace(config.BarracksData.SoldierSpriteResourcePath))
                {
                    checkedCount++;
                    if (TryResolveSprite(config.BarracksData.SoldierSpriteResourcePath))
                    {
                        resolvedCount++;
                    }
                    else
                    {
                        missing.Add($"Tower {config.TowerId} Soldier: {config.BarracksData.SoldierSpriteResourcePath}");
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
                EditorUtility.DisplayDialog("Missing Hint Regression", "실패: GameScene의 힌트 생성 메서드를 찾지 못했습니다.", "확인");
                return;
            }

            var sampleMissing = new List<string>
            {
                "HeroConfig missing: Data/HeroConfigs/NoHero",
                "HeroConfig invalid HeroId: Data/HeroConfigs/NoId",
                "Hero sprite missing: heroId=Knight, action=attack, directPath=UI/Sprites/Heroes/InGame/Knight/attack_00, manifest=Sprites/Heroes/manifest, reason=missing",
                "WaveConfig missing: current stage has no wave config.",
                "Wave spawn entry missing: wave=1",
                "EnemyConfig reference missing: wave=1, entry=1",
                "Enemy action sprite missing: enemyId=Goblin, runtimePath=Sprites/Enemies/Goblin, move=Sprites/Enemies/Goblin/walk, missing=[action=attack, candidates=Sprites/Enemies/Goblin/attack]",
                "TowerConfig missing: Data/TowerConfigs/Barracks",
                "Tower level data missing: towerType=Barracks, config=Data/TowerConfigs/Barracks",
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
                EditorUtility.DisplayDialog("Missing Hint Regression", "실패: 힌트 생성 메서드 호출 중 예외가 발생했습니다.", "확인");
                return;
            }

            string output = builder.ToString();
            string[] requiredHints =
            {
                "HeroConfig.HeroId 값을 채우세요.",
                "EnemyConfig.RuntimeSpriteResourcePath를 유효한 Resources 경로로 지정하세요.",
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
                EditorUtility.DisplayDialog("Missing Hint Regression", $"통과: {requiredHints.Length}개 힌트 확인", "확인");
                return;
            }

            Debug.LogWarning($"[KingdomSpriteBindingTools] Missing hint regression failed. missing={missingHints.Count}\n{string.Join("\n", missingHints)}\n--- output ---\n{output}");
            EditorUtility.DisplayDialog("Missing Hint Regression", $"실패: 누락 힌트 {missingHints.Count}개", "확인");
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
    }
}
