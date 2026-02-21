using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Game
{
    /// <summary>
    /// 최소 타워 배치/공격 루프.
    /// </summary>
    public class TowerManager : MonoBehaviour
    {
        private const string SkillLevelKeyPrefix = "Kingdom.SkillTree.SkillLevel.";

        public readonly struct TowerActionInfo
        {
            public readonly int TowerId;
            public readonly TowerType TowerType;
            public readonly int Level;
            public readonly int MaxLevel;
            public readonly int UpgradeCost;
            public readonly int SellRefund;
            public readonly Vector3 WorldPosition;
            public readonly bool SupportsRally;
            public readonly Vector3 RallyPoint;

            public TowerActionInfo(
                int towerId,
                TowerType towerType,
                int level,
                int maxLevel,
                int upgradeCost,
                int sellRefund,
                Vector3 worldPosition,
                bool supportsRally,
                Vector3 rallyPoint)
            {
                TowerId = towerId;
                TowerType = towerType;
                Level = level;
                MaxLevel = maxLevel;
                UpgradeCost = upgradeCost;
                SellRefund = sellRefund;
                WorldPosition = worldPosition;
                SupportsRally = supportsRally;
                RallyPoint = rallyPoint;
            }
        }

        public readonly struct BarracksDebugInfo
        {
            public readonly int TowerId;
            public readonly int SoldierTotal;
            public readonly int SoldierAlive;
            public readonly int SoldierDead;
            public readonly int SoldierRespawning;
            public readonly int SoldierIdle;
            public readonly int SoldierMoving;
            public readonly int SoldierBlocking;
            public readonly int AssignedEnemies;
            public readonly int BlockedEnemies;
            public readonly int TotalDeaths;
            public readonly int TotalRespawns;
            public readonly Vector3 RallyPoint;

            public BarracksDebugInfo(
                int towerId,
                int soldierTotal,
                int soldierAlive,
                int soldierDead,
                int soldierRespawning,
                int soldierIdle,
                int soldierMoving,
                int soldierBlocking,
                int assignedEnemies,
                int blockedEnemies,
                int totalDeaths,
                int totalRespawns,
                Vector3 rallyPoint)
            {
                TowerId = towerId;
                SoldierTotal = soldierTotal;
                SoldierAlive = soldierAlive;
                SoldierDead = soldierDead;
                SoldierRespawning = soldierRespawning;
                SoldierIdle = soldierIdle;
                SoldierMoving = soldierMoving;
                SoldierBlocking = soldierBlocking;
                AssignedEnemies = assignedEnemies;
                BlockedEnemies = blockedEnemies;
                TotalDeaths = totalDeaths;
                TotalRespawns = totalRespawns;
                RallyPoint = rallyPoint;
            }
        }

        private readonly List<EnemyRuntime> _aliveEnemies = new();
        private readonly List<TowerRuntime> _towers = new();
        private readonly List<Vector3> _towerSlots = new();
        private readonly List<int> _freeSlotIndices = new();
        private static readonly Dictionary<TowerType, Sprite> FallbackTowerSprites = new();
        private static readonly Dictionary<string, Sprite> RuntimeTextureSpriteCache = new();
        private static readonly string[] TowerSpriteResourcePrefixes =
        {
            "UI/Sprites/Towers/",
            "Sprites/Towers/",
            "Kingdom/Towers/Sprites/"
        };
        private static Sprite BarracksSoldierSprite;

        private SpawnManager _spawnManager;
        private InGameEconomyManager _economyManager;
        private ProjectileManager _projectileManager;
        private Transform _towerRoot;
        // private TowerConfig _towerConfig; // Removed in favor of dictionary
        private readonly Dictionary<TowerType, TowerConfig> _towerConfigs = new();
        private int _nextTowerId = 1;

        public int TowerCount => _towers.Count;
        public int RemainingSlots => _freeSlotIndices.Count;

        public int GetBuildCost(TowerType towerType)
        {
            if (_towerConfigs.TryGetValue(towerType, out var config) && config != null)
            {
                // Use Level 1 cost from data
                return Mathf.Max(1, config.BuildCost);
            }

            // Fallback legacy calculation
            int baseCost = 70;
            float multiplier = towerType switch
            {
                TowerType.Barracks => 1.1f,
                TowerType.Mage => 1.2f,
                TowerType.Artillery => 1.35f,
                _ => 1f
            };

            return Mathf.Max(1, Mathf.RoundToInt(baseCost * multiplier));
        }

        public bool CanBuild(TowerType towerType, int currentGold)
        {
            // Removed single config check


            if (_freeSlotIndices.Count <= 0)
            {
                return false;
            }

            return currentGold >= GetBuildCost(towerType);
        }

        public void Configure(
            SpawnManager spawnManager,
            InGameEconomyManager economyManager,
            ProjectileManager projectileManager,
            Transform towerRoot,
            IReadOnlyList<Vector3> towerSlots,
            TowerConfig /*unused legacy param*/ _ = null)
        {
            _spawnManager = spawnManager;
            _economyManager = economyManager;
            _projectileManager = projectileManager;
            _towerRoot = towerRoot;
            
            // Load tower configs for each type
            _towerConfigs.Clear();
            foreach (TowerType type in System.Enum.GetValues(typeof(TowerType)))
            {
                // Try load specific config using canonical/legacy fallback rules.
                var loaded = ConfigResourcePaths.LoadTowerConfig(type.ToString());
                if (loaded != null)
                {
                    TowerConfig runtimeConfig = Instantiate(loaded);
                    runtimeConfig.hideFlags = HideFlags.DontSave;

                    bool isInvalid = false;
                    if (runtimeConfig.Levels != null && runtimeConfig.Levels.Length > 0)
                    {
                        // Check if data is essentially empty (Range ~ 0)
                        if (runtimeConfig.Levels[0].Range < 0.1f) isInvalid = true;
                    }

                    if (runtimeConfig.Levels == null || runtimeConfig.Levels.Length == 0 || isInvalid)
                    {
                        Debug.Log($"[TowerManager] Config for {type} has invalid data (Range=0 or empty). Repopulating defaults.");
                        PopulateDefaultData(runtimeConfig, type);
                    }

                    EnsureTowerCombatRules(runtimeConfig, type);
                    NormalizeTowerLevels(runtimeConfig, type);
                    ApplyWorldMapMetaUpgrades(runtimeConfig, type);
                    _towerConfigs[type] = runtimeConfig;
                }
                else
                {
                    // Fallback generator
                    TowerConfig fallback = CreateFallbackTowerConfig(type);
                    EnsureTowerCombatRules(fallback, type);
                    NormalizeTowerLevels(fallback, type);
                    ApplyWorldMapMetaUpgrades(fallback, type);
                    _towerConfigs[type] = fallback;
                }
            }

            _towerSlots.Clear();
            if (towerSlots != null)
            {
                for (int i = 0; i < towerSlots.Count; i++)
                {
                    _towerSlots.Add(towerSlots[i]);
                }
            }

            if (_towerSlots.Count == 0)
            {
                _towerSlots.Add(new Vector3(-3f, -1.4f, 0f));
                _towerSlots.Add(new Vector3(-0.4f, -1.3f, 0f));
                _towerSlots.Add(new Vector3(2.2f, -1.2f, 0f));
            }

            RebuildFreeSlots();
            RebindSpawnEvents();
        }

        public bool TryBuildNextTower()
        {
            return TryBuildNextTower(TowerType.Archer);
        }

        public bool TryBuildNextTower(TowerType towerType)
        {
            int slotIndex = GetNextFreeSlotIndex();
            return TryBuildTowerAtSlot(towerType, slotIndex);
        }

        public bool TryBuildTowerAtSlot(TowerType towerType, int slotIndex)
        {
            if (_economyManager == null)
            {
                return false;
            }
            
            if (!_towerConfigs.TryGetValue(towerType, out var config) || config == null)
            {
                return false;
            }

            if (!IsValidSlotIndex(slotIndex) || !_freeSlotIndices.Contains(slotIndex))
            {
                return false;
            }

            int buildCost = GetBuildCost(towerType);
            if (!_economyManager.TrySpendGold(buildCost))
            {
                return false;
            }

            Vector3 position = _towerSlots[slotIndex];
            TowerRuntime runtime = CreateTowerRuntime(position, towerType, config, slotIndex, buildCost);
            _towers.Add(runtime);
            _freeSlotIndices.Remove(slotIndex);
            return true;
        }

        public bool TryFindTowerAtWorldPosition(Vector3 worldPosition, float maxDistance, out int towerId, out Vector3 towerWorldPosition)
        {
            towerId = -1;
            towerWorldPosition = Vector3.zero;
            if (_towers.Count <= 0)
            {
                return false;
            }

            float maxSqr = Mathf.Max(0.05f, maxDistance) * Mathf.Max(0.05f, maxDistance);
            float bestSqr = float.MaxValue;
            TowerRuntime best = null;

            for (int i = 0; i < _towers.Count; i++)
            {
                TowerRuntime tower = _towers[i];
                if (tower == null || tower.Transform == null)
                {
                    continue;
                }

                float sqr = (tower.Transform.position - worldPosition).sqrMagnitude;
                if (sqr > maxSqr || sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = tower;
            }

            if (best == null)
            {
                return false;
            }

            towerId = best.TowerId;
            towerWorldPosition = best.Transform.position;
            return true;
        }

        public bool TryGetTowerActionInfo(int towerId, out TowerActionInfo info)
        {
            info = default;
            TowerRuntime tower = FindTowerById(towerId);
            if (tower == null)
            {
                return false;
            }

            int maxLevel = tower.MaxLevel;
            int upgradeCost = tower.Level < maxLevel ? tower.GetUpgradeCost(0) : 0;
            int sellRefund = tower.GetSellRefund();
            info = new TowerActionInfo(
                tower.TowerId,
                tower.TowerType,
                tower.Level,
                maxLevel,
                upgradeCost,
                sellRefund,
                tower.Transform != null ? tower.Transform.position : Vector3.zero,
                tower.TowerType == TowerType.Barracks,
                tower.RallyPoint);
            return true;
        }

        public bool TryGetBarracksDebugInfo(int towerId, out BarracksDebugInfo info)
        {
            info = default;
            TowerRuntime tower = FindTowerById(towerId);
            if (tower == null || tower.TowerType != TowerType.Barracks)
            {
                return false;
            }

            info = tower.GetBarracksDebugInfo();
            return true;
        }

        public bool TrySetRallyPoint(int towerId, Vector3 worldPoint)
        {
            TowerRuntime tower = FindTowerById(towerId);
            if (tower == null || tower.TowerType != TowerType.Barracks || tower.Transform == null)
            {
                return false;
            }

            float range = 1.6f;
            if (_towerConfigs.TryGetValue(TowerType.Barracks, out var barracksConfig) && barracksConfig != null)
            {
                range = Mathf.Max(0.6f, barracksConfig.BarracksData.RallyRange > 0.05f
                    ? barracksConfig.BarracksData.RallyRange
                    : barracksConfig.AttackRange);
            }

            Vector3 origin = tower.Transform.position;
            Vector3 offset = worldPoint - origin;
            offset.z = 0f;
            if (offset.sqrMagnitude > range * range)
            {
                offset = offset.normalized * range;
            }

            tower.SetRallyPoint(origin + offset);
            return true;
        }

        public bool TryUpgradeTower(int towerId)
        {
            if (_economyManager == null)
            {
                return false;
            }

            TowerRuntime tower = FindTowerById(towerId);
            if (tower == null || !tower.CanUpgrade)
            {
                return false;
            }

            int upgradeCost = tower.GetUpgradeCost(GetBuildCost(tower.TowerType));
            if (!_economyManager.TrySpendGold(upgradeCost))
            {
                return false;
            }

            tower.Upgrade(upgradeCost);
            return true;
        }

        public bool TrySellTower(int towerId)
        {
            if (_economyManager == null)
            {
                return false;
            }

            TowerRuntime tower = FindTowerById(towerId);
            if (tower == null)
            {
                return false;
            }

            int refund = tower.GetSellRefund();
            _economyManager.AddGold(refund);
            tower.ReleaseBlockedEnemy();
            if (IsValidSlotIndex(tower.SlotIndex) && !_freeSlotIndices.Contains(tower.SlotIndex))
            {
                _freeSlotIndices.Add(tower.SlotIndex);
                _freeSlotIndices.Sort();
            }

            if (tower.Transform != null)
            {
                Destroy(tower.Transform.gameObject);
            }

            _towers.Remove(tower);
            return true;
        }

        public bool TryFindBuildableSlotAtWorldPosition(Vector3 worldPosition, float maxDistance, out int slotIndex, out Vector3 slotWorldPosition)
        {
            slotIndex = -1;
            slotWorldPosition = Vector3.zero;
            if (_freeSlotIndices.Count <= 0 || _towerSlots.Count <= 0)
            {
                return false;
            }

            float maxSqr = Mathf.Max(0.05f, maxDistance) * Mathf.Max(0.05f, maxDistance);
            float bestSqr = float.MaxValue;
            int bestIndex = -1;
            Vector3 bestWorld = Vector3.zero;

            for (int i = 0; i < _freeSlotIndices.Count; i++)
            {
                int candidateIndex = _freeSlotIndices[i];
                if (!IsValidSlotIndex(candidateIndex))
                {
                    continue;
                }

                Vector3 candidateWorld = _towerSlots[candidateIndex];
                float sqr = (candidateWorld - worldPosition).sqrMagnitude;
                if (sqr > maxSqr || sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                bestIndex = candidateIndex;
                bestWorld = candidateWorld;
            }

            if (bestIndex < 0)
            {
                return false;
            }

            slotIndex = bestIndex;
            slotWorldPosition = bestWorld;
            return true;
        }

        private void Update()
        {
            if (_towers.Count == 0 || _aliveEnemies.Count == 0 || _towerConfigs.Count == 0)
            {
                return;
            }

            float dt = Time.deltaTime;
            for (int i = 0; i < _towers.Count; i++)
            {
                var tower = _towers[i];
                if (tower == null) continue;
                if (_towerConfigs.TryGetValue(tower.TowerType, out var config) && config != null)
                {
                    tower.Tick(dt, _aliveEnemies, config);
                }
            }
        }

        private void OnDisable()
        {
            UnbindSpawnEvents();
        }

        private void RebindSpawnEvents()
        {
            UnbindSpawnEvents();
            if (_spawnManager == null)
            {
                return;
            }

            _spawnManager.EnemySpawned += HandleEnemySpawned;
            _spawnManager.EnemyKilled += HandleEnemyRemoved;
            _spawnManager.EnemyReachedGoal += HandleEnemyRemoved;
        }

        private void UnbindSpawnEvents()
        {
            if (_spawnManager == null)
            {
                return;
            }

            _spawnManager.EnemySpawned -= HandleEnemySpawned;
            _spawnManager.EnemyKilled -= HandleEnemyRemoved;
            _spawnManager.EnemyReachedGoal -= HandleEnemyRemoved;
        }

        private void HandleEnemySpawned(EnemyRuntime enemy, EnemyConfig config)
        {
            if (enemy != null && !_aliveEnemies.Contains(enemy))
            {
                _aliveEnemies.Add(enemy);
            }
        }

        private void HandleEnemyRemoved(EnemyRuntime enemy, EnemyConfig config)
        {
            _aliveEnemies.Remove(enemy);
            for (int i = 0; i < _towers.Count; i++)
            {
                _towers[i]?.HandleEnemyRemoved(enemy);
            }
        }

        private TowerRuntime CreateTowerRuntime(Vector3 position, TowerType towerType, TowerConfig config, int slotIndex, int initialSpendGold)
        {
            int towerId = _nextTowerId++;
            GameObject go = new GameObject($"Tower_{towerType}_{towerId}");
            if (_towerRoot != null)
            {
                go.transform.SetParent(_towerRoot, false);
            }

            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            
            // Initial sprite and scale from Level 1
            Sprite initialSprite = ResolveTowerLevelSprite(config, towerType, 0);
            float initialScale = 0.65f;
            if (config != null && config.Levels != null && config.Levels.Length > 0)
            {
                if (config.Levels[0].VisualScale > 0.05f) 
                {
                    initialScale = config.Levels[0].VisualScale;
                }
            }

            sr.sprite = initialSprite != null ? initialSprite : GetOrCreateFallbackTowerSprite(towerType);
            sr.color = initialSprite != null ? Color.white : GetTowerTint(towerType);
            sr.sortingOrder = 30;
            go.transform.localScale = new Vector3(initialScale, initialScale, 1f);

            return new TowerRuntime(towerId, slotIndex, go.transform, towerType, config, _projectileManager, initialSpendGold);
        }

        private static TowerConfig CreateFallbackTowerConfig(TowerType type)
        {
            var config = ScriptableObject.CreateInstance<TowerConfig>();
            config.hideFlags = HideFlags.DontSave;
            config.TowerId = $"Fallback_{type}";
            PopulateDefaultData(config, type);
            return config;
        }

        private static void PopulateDefaultData(TowerConfig config, TowerType type)
        {
            config.TowerType = type;
            
            // Base logic (L1 baseline)
            float damage = type == TowerType.Artillery ? 25f : (type == TowerType.Barracks ? 8f : 34f);
            float cooldown = type == TowerType.Mage ? 1.35f : (type == TowerType.Artillery ? 2.6f : (type == TowerType.Barracks ? 1.0f : 0.75f));
            float baseScale = 0.65f;
            float baseRange = type == TowerType.Barracks ? 1.6f : (type == TowerType.Mage ? 2.0f : (type == TowerType.Artillery ? 2.4f : 2.2f));

            config.Levels = new TowerLevelData[3];
            float costMod = type == TowerType.Mage ? 1.4f : (type == TowerType.Artillery ? 1.8f : (type == TowerType.Barracks ? 1.1f : 1f));
            int l1Cost = Mathf.RoundToInt(70f * costMod);
            int l2Cost = Mathf.RoundToInt(l1Cost * 1.35f);
            int l3Cost = Mathf.RoundToInt(l2Cost * 1.45f);
            string projectileId = type switch
            {
                TowerType.Archer => "Archer_Arrow",
                TowerType.Mage => "Mage_Bolt",
                TowerType.Artillery => "Artillery_Shell",
                _ => string.Empty
            };
            AttackDeliveryType delivery = type == TowerType.Barracks ? AttackDeliveryType.Melee : AttackDeliveryType.Projectile;

            // L1
            config.Levels[0] = new TowerLevelData
            {
                Cost = l1Cost,
                Damage = damage,
                Cooldown = cooldown,
                Range = baseRange,
                AttackDeliveryType = delivery,
                ProjectileProfileId = projectileId,
                SpriteOverride = null,
                VisualScale = baseScale
            };

            // L2: +30% damage, +5% range, -5% cooldown
            config.Levels[1] = new TowerLevelData
            {
                Cost = l2Cost,
                Damage = damage * 1.30f,
                Cooldown = Mathf.Max(0.08f, cooldown * 0.95f),
                Range = baseRange * 1.05f,
                AttackDeliveryType = delivery,
                ProjectileProfileId = projectileId,
                SpriteOverride = null,
                VisualScale = baseScale + 0.08f
            };

            // L3: +30% damage(from L2), +10% range(from L2), -5% cooldown(from L2)
            config.Levels[2] = new TowerLevelData
            {
                Cost = l3Cost,
                Damage = config.Levels[1].Damage * 1.30f,
                Cooldown = Mathf.Max(0.08f, config.Levels[1].Cooldown * 0.95f),
                Range = config.Levels[1].Range * 1.10f,
                AttackDeliveryType = delivery,
                ProjectileProfileId = projectileId,
                SpriteOverride = null,
                VisualScale = baseScale + 0.16f
            };
        }

        private static void EnsureTowerCombatRules(TowerConfig config, TowerType type)
        {
            if (config == null)
            {
                return;
            }

            config.TowerType = type;
            switch (type)
            {
                case TowerType.Archer:
                    config.TargetType = TowerTargetType.Both;
                    config.CanTargetAir = true;
                    config.DamageType = DamageType.Physical;
                    config.HalfPhysicalArmorPenetration = false;
                    break;
                case TowerType.Barracks:
                    config.TargetType = TowerTargetType.Ground;
                    config.CanTargetAir = false;
                    config.DamageType = DamageType.Physical;
                    config.HalfPhysicalArmorPenetration = false;
                    break;
                case TowerType.Mage:
                    config.TargetType = TowerTargetType.Both;
                    config.CanTargetAir = true;
                    config.DamageType = DamageType.Magic;
                    config.HalfPhysicalArmorPenetration = false;
                    break;
                case TowerType.Artillery:
                    config.TargetType = TowerTargetType.Ground;
                    config.CanTargetAir = false;
                    config.DamageType = DamageType.Physical;
                    config.HalfPhysicalArmorPenetration = true;
                    break;
            }

            if (type == TowerType.Barracks)
            {
                float fallbackRallyRange = config.Levels != null && config.Levels.Length > 0
                    ? Mathf.Max(0.6f, config.Levels[0].Range)
                    : 1.6f;
                if (config.BarracksData.RallyRange <= 0.05f)
                {
                    config.BarracksData.RallyRange = fallbackRallyRange;
                }
                if (config.BarracksData.SquadSize <= 0)
                {
                    config.BarracksData.SquadSize = 3;
                }

                // Soldier Combat Stats Fallback
                if (config.BarracksData.SoldierMaxHp <= 0f)
                {
                    config.BarracksData.SoldierMaxHp = 45f;
                }
                if (config.BarracksData.SoldierDamage <= 0f)
                {
                    float towerDamage = (config.Levels != null && config.Levels.Length > 0) ? config.Levels[0].Damage : 8f;
                    int finalSquadSize = Mathf.Max(1, config.BarracksData.SquadSize);
                    config.BarracksData.SoldierDamage = Mathf.Max(1f, towerDamage / finalSquadSize);
                }
                if (config.BarracksData.SoldierAttackCooldown <= 0f)
                {
                    float baseCooldown = (config.Levels != null && config.Levels.Length > 0)
                        ? config.Levels[0].Cooldown
                        : 1f;
                    config.BarracksData.SoldierAttackCooldown = Mathf.Max(0.22f, baseCooldown);
                }
                if (config.BarracksData.SoldierRespawnSec <= 0f)
                {
                    config.BarracksData.SoldierRespawnSec = 10f;
                }
            }
        }

        private static void NormalizeTowerLevels(TowerConfig config, TowerType type)
        {
            if (config == null)
            {
                return;
            }

            if (config.Levels == null || config.Levels.Length == 0)
            {
                PopulateDefaultData(config, type);
                return;
            }

            if (config.Levels.Length < 3)
            {
                TowerLevelData[] resized = new TowerLevelData[3];
                for (int i = 0; i < resized.Length; i++)
                {
                    resized[i] = i < config.Levels.Length ? config.Levels[i] : config.Levels[config.Levels.Length - 1];
                }

                config.Levels = resized;
            }

            TowerLevelData lv1 = config.Levels[0];
            lv1.Cost = Mathf.Max(1, lv1.Cost);
            lv1.Damage = Mathf.Max(1f, lv1.Damage);
            lv1.Range = Mathf.Max(0.4f, lv1.Range);
            lv1.Cooldown = Mathf.Max(0.08f, lv1.Cooldown);
            lv1.VisualScale = Mathf.Max(0.2f, lv1.VisualScale);
            if (type == TowerType.Barracks)
            {
                lv1.AttackDeliveryType = AttackDeliveryType.Melee;
            }
            else if (lv1.AttackDeliveryType == AttackDeliveryType.Melee)
            {
                lv1.AttackDeliveryType = AttackDeliveryType.Projectile;
            }
            config.Levels[0] = lv1;

            for (int i = 1; i < config.Levels.Length; i++)
            {
                TowerLevelData prev = config.Levels[i - 1];
                TowerLevelData cur = config.Levels[i];
                float rangeGrowth = i == 1 ? 1.05f : 1.10f;

                cur.Cost = Mathf.Max(cur.Cost, Mathf.CeilToInt(prev.Cost * 1.20f));
                cur.Damage = Mathf.Max(cur.Damage, prev.Damage * 1.30f);
                cur.Range = Mathf.Max(cur.Range, prev.Range * rangeGrowth);
                cur.Cooldown = Mathf.Max(0.08f, Mathf.Min(cur.Cooldown > 0f ? cur.Cooldown : prev.Cooldown, prev.Cooldown * 0.95f));
                cur.VisualScale = Mathf.Max(prev.VisualScale, cur.VisualScale);

                if (type == TowerType.Barracks)
                {
                    cur.AttackDeliveryType = AttackDeliveryType.Melee;
                    cur.ProjectileProfileId = string.Empty;
                }
                else if (cur.AttackDeliveryType == AttackDeliveryType.Melee)
                {
                    cur.AttackDeliveryType = AttackDeliveryType.Projectile;
                }

                config.Levels[i] = cur;
            }
        }

        private static void ApplyWorldMapMetaUpgrades(TowerConfig config, TowerType towerType)
        {
            if (config == null || config.Levels == null || config.Levels.Length <= 0)
            {
                return;
            }

            int categoryLevel = GetMetaCategoryLevel(towerType);
            if (categoryLevel <= 0)
            {
                return;
            }

            float damageFactor = 1f + (0.04f * categoryLevel);
            float rangeFactor = 1f + (0.03f * categoryLevel);
            float cooldownFactor = Mathf.Clamp(1f - (0.02f * categoryLevel), 0.5f, 1f);

            for (int i = 0; i < config.Levels.Length; i++)
            {
                TowerLevelData level = config.Levels[i];
                level.Damage = Mathf.Max(1f, level.Damage * damageFactor);
                level.Range = Mathf.Max(0.4f, level.Range * rangeFactor);
                level.Cooldown = Mathf.Max(0.08f, level.Cooldown * cooldownFactor);
                config.Levels[i] = level;
            }

            Debug.Log($"[TowerManager] Meta upgrade applied. type={towerType}, categoryLevel={categoryLevel}, dmgX={damageFactor:0.00}, rangeX={rangeFactor:0.00}, cdX={cooldownFactor:0.00}");
        }

        private static int GetMetaCategoryLevel(TowerType towerType)
        {
            string categoryPrefix = towerType switch
            {
                TowerType.Archer => "archers_",
                TowerType.Barracks => "barracks_",
                TowerType.Mage => "mages_",
                TowerType.Artillery => "artillery_",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(categoryPrefix))
            {
                return 0;
            }

            int total = 0;
            for (int i = 1; i <= 10; i++)
            {
                string key = $"{SkillLevelKeyPrefix}{categoryPrefix}t{i}";
                total += Mathf.Max(0, PlayerPrefs.GetInt(key, 0));
            }

            return total;
        }

        private static Sprite ResolveTowerLevelSprite(TowerConfig config, TowerType towerType, int levelIndex)
        {
            if (config == null)
            {
                return null;
            }

            int safeLevelIndex = Mathf.Max(0, levelIndex);
            if (config.Levels != null && safeLevelIndex < config.Levels.Length)
            {
                TowerLevelData levelData = config.Levels[safeLevelIndex];
                if (levelData.SpriteOverride != null)
                {
                    return levelData.SpriteOverride;
                }

                if (TryLoadSprite(levelData.SpriteResourcePath, out Sprite byLevelPath))
                {
                    return byLevelPath;
                }
            }

            string expandedConfigPath = ExpandTowerTemplatePath(config.RuntimeSpriteResourcePath, towerType, safeLevelIndex);
            if (TryLoadSprite(expandedConfigPath, out Sprite byConfigPath))
            {
                return byConfigPath;
            }

            List<string> candidates = BuildTowerSpritePathCandidates(config.TowerId, towerType, safeLevelIndex);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (TryLoadSprite(candidates[i], out Sprite fromConvention))
                {
                    return fromConvention;
                }
            }

            return null;
        }

        private static Sprite ResolveBarracksSoldierSprite(TowerConfig config)
        {
            if (config != null &&
                config.BarracksSoldierConfig != null &&
                TryLoadSprite(config.BarracksSoldierConfig.RuntimeSpriteResourcePath, out Sprite bySoldierConfigPath))
            {
                return bySoldierConfigPath;
            }

            if (config != null && TryLoadSprite(config.BarracksData.SoldierSpriteResourcePath, out Sprite byLegacyPath))
            {
                return byLegacyPath;
            }

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

            for (int i = 0; i < candidates.Count; i++)
            {
                if (TryLoadSprite(candidates[i], out Sprite loaded))
                {
                    return loaded;
                }
            }

            Sprite generatedFallback = GetOrCreateBarracksSoldierSprite();
            return generatedFallback != null ? generatedFallback : GetOrCreateFallbackTowerSprite(TowerType.Barracks);
        }

        private static List<string> BuildTowerSpritePathCandidates(string towerId, TowerType towerType, int levelIndex)
        {
            string typeName = towerType.ToString();
            string levelToken = Mathf.Max(1, levelIndex + 1).ToString();
            var candidates = new List<string>(24);

            for (int i = 0; i < TowerSpriteResourcePrefixes.Length; i++)
            {
                string prefix = TowerSpriteResourcePrefixes[i];
                AddTowerNameCandidates(candidates, prefix, towerId, levelToken);
                AddTowerNameCandidates(candidates, prefix, typeName, levelToken);
            }

            return candidates;
        }

        private static void AddTowerNameCandidates(List<string> candidates, string prefix, string towerName, string levelToken)
        {
            if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(towerName))
            {
                return;
            }

            string trimmed = towerName.Trim();
            TryAddCandidate(candidates, $"{prefix}{trimmed}_L{levelToken}");
            TryAddCandidate(candidates, $"{prefix}{trimmed}/L{levelToken}");
            TryAddCandidate(candidates, $"{prefix}{trimmed}/Level{levelToken}");
            TryAddCandidate(candidates, $"{prefix}{trimmed}");
        }

        private static void TryAddCandidate(List<string> candidates, string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate) || candidates.Contains(candidate))
            {
                return;
            }

            candidates.Add(candidate);
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

        private static bool TryLoadSprite(string resourcePath, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return false;
            }

            Sprite single = Resources.Load<Sprite>(resourcePath);
            if (single != null)
            {
                sprite = single;
                return true;
            }

            Sprite[] multiple = Resources.LoadAll<Sprite>(resourcePath);
            if (multiple == null || multiple.Length <= 0)
            {
                if (RuntimeTextureSpriteCache.TryGetValue(resourcePath, out Sprite cached) && cached != null)
                {
                    sprite = cached;
                    return true;
                }

                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture == null)
                {
                    texture = TryLoadTextureFromDisk(resourcePath);
                }

                if (texture == null)
                {
                    return false;
                }

                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0f),
                    Mathf.Max(16f, texture.width));
                RuntimeTextureSpriteCache[resourcePath] = sprite;
                return true;
            }

            System.Array.Sort(multiple, CompareSpriteByName);
            sprite = multiple[0];
            return sprite != null;
        }

        private static Texture2D TryLoadTextureFromDisk(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            string normalized = resourcePath.Replace('\\', '/').TrimStart('/');
            string absolutePath = System.IO.Path.Combine(Application.dataPath, "Resources", normalized + ".png");
            if (!System.IO.File.Exists(absolutePath))
            {
                return null;
            }

            byte[] bytes = System.IO.File.ReadAllBytes(absolutePath);
            if (bytes == null || bytes.Length <= 0)
            {
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            return texture;
        }

        private static int CompareSpriteByName(Sprite a, Sprite b)
        {
            if (a == null && b == null)
            {
                return 0;
            }

            if (a == null)
            {
                return 1;
            }

            if (b == null)
            {
                return -1;
            }

            return string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase);
        }

        private static Sprite GetOrCreateFallbackTowerSprite(TowerType towerType)
        {
            if (FallbackTowerSprites.TryGetValue(towerType, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite created = CreateFallbackTowerSprite(towerType);
            FallbackTowerSprites[towerType] = created;
            return created;
        }

        private static Sprite CreateFallbackTowerSprite(TowerType towerType)
        {
            const int size = 24;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(1f, 1f, 1f, 0f);
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x <= 1 || x >= size - 2 || y <= 1 || y >= size - 2;
                    bool emblem = IsFallbackEmblemPixel(towerType, x, y, size);
                    bool centerFill = x >= 3 && x <= size - 4 && y >= 3 && y <= size - 4;

                    if (border || emblem)
                    {
                        pixels[(y * size) + x] = Color.black;
                    }
                    else if (centerFill)
                    {
                        pixels[(y * size) + x] = Color.white;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite GetOrCreateBarracksSoldierSprite()
        {
            if (BarracksSoldierSprite != null)
            {
                return BarracksSoldierSprite;
            }

            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x <= 1 || x >= size - 2 || y <= 1 || y >= size - 2;
                    bool helmet = y >= size - 6 && x >= 3 && x <= size - 4;
                    bool body = y >= 3 && y <= size - 7 && x >= 3 && x <= size - 4;

                    Color color = new Color(1f, 1f, 1f, 0f);
                    if (border)
                    {
                        color = Color.black;
                    }
                    else if (helmet)
                    {
                        color = new Color(0.85f, 0.25f, 0.2f, 1f);
                    }
                    else if (body)
                    {
                        color = Color.white;
                    }

                    pixels[(y * size) + x] = color;
                }
            }

            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            BarracksSoldierSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return BarracksSoldierSprite;
        }

        private static bool IsFallbackEmblemPixel(TowerType towerType, int x, int y, int size)
        {
            int cx = size / 2;
            int cy = size / 2;
            return towerType switch
            {
                TowerType.Archer => (x == cx && y >= 4 && y <= size - 5)
                    || (y == cy && x >= cx - 4 && x <= cx + 4),
                TowerType.Barracks => (
                    (((x >= cx - 5 && x <= cx - 3) || (x >= cx + 3 && x <= cx + 5))
                    && y >= cy - 5 && y <= cy + 5)
                    || (y >= cy + 4 && y <= cy + 6 && x >= cx - 6 && x <= cx + 6)),
                TowerType.Mage => Mathf.Abs(x - cx) == Mathf.Abs(y - cy)
                    && Mathf.Abs(x - cx) <= 6,
                TowerType.Artillery => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= 20
                    && (x - cx) * (x - cx) + (y - cy) * (y - cy) >= 8,
                _ => false
            };
        }

        private static Color GetTowerTint(TowerType towerType)
        {
            return towerType switch
            {
                TowerType.Archer => new Color(0.25f, 0.45f, 0.85f, 1f),
                TowerType.Barracks => new Color(0.78f, 0.46f, 0.22f, 1f),
                TowerType.Mage => new Color(0.55f, 0.35f, 0.8f, 1f),
                TowerType.Artillery => new Color(0.7f, 0.45f, 0.2f, 1f),
                _ => new Color(0.3f, 0.55f, 0.9f, 1f)
            };
        }

        private void RebuildFreeSlots()
        {
            _freeSlotIndices.Clear();
            for (int i = 0; i < _towerSlots.Count; i++)
            {
                _freeSlotIndices.Add(i);
            }
        }

        private int GetNextFreeSlotIndex()
        {
            if (_freeSlotIndices.Count <= 0)
            {
                return -1;
            }

            return _freeSlotIndices[0];
        }

        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _towerSlots.Count;
        }

        private TowerRuntime FindTowerById(int towerId)
        {
            for (int i = 0; i < _towers.Count; i++)
            {
                TowerRuntime tower = _towers[i];
                if (tower != null && tower.TowerId == towerId)
                {
                    return tower;
                }
            }

            return null;
        }

        private sealed class TowerRuntime
        {
            private const float ArcherSniperCooldownSec = 10f;
            private const float MageDeathRayCooldownSec = 12f;
            private const float ArtilleryClusterCooldownSec = 6f;
            private const float BarracksThrowAxeCooldownSec = 3.5f;
            private const int MaxBarracksSoldierCount = 8;
            private const float BarracksSoldierIdleRadius = 0.58f;
            private const float BarracksSoldierCombatRadius = 0.42f;
            private const float BarracksEnemyBlockRadius = 0.52f;

            private readonly Transform _transform;
            private readonly TowerType _towerType;
            private readonly TowerConfig _config;
            private readonly ProjectileManager _projectileManager;
            private int _level = 1;
            private int _totalSpendGold;
            private float _cooldownLeft;
            private float _skillCooldownLeft;
            private readonly List<EnemyRuntime> _blockedEnemies = new();
            private readonly List<BarracksSoldierRuntime> _barracksSoldiers = new();
            private readonly Dictionary<EnemyRuntime, BarracksSoldierRuntime> _enemyToSoldier = new();
            private readonly HashSet<EnemyRuntime> _subscribedBlockedEnemies = new();
            private int _soldierDeathCount;
            private int _soldierRespawnCount;
            private Vector3 _rallyPoint;
            
            // Visual feedback state
            private Vector3 _baseScale;
            private Coroutine _visualRoutine;
            private SpriteRenderer _sr;

            public int TowerId { get; }
            public int SlotIndex { get; }
            public Transform Transform => _transform;
            public TowerType TowerType => _towerType;
            public int Level => _level;
            public int MaxLevel => _config != null && _config.Levels != null ? _config.Levels.Length : 3;
            public bool CanUpgrade => _config != null && _config.Levels != null && _level < _config.Levels.Length;
            public Vector3 RallyPoint => _rallyPoint;

            public TowerRuntime(
                int towerId,
                int slotIndex,
                Transform transform,
                TowerType towerType,
                TowerConfig config,
                ProjectileManager projectileManager,
                int initialSpendGold)
            {
                TowerId = towerId;
                SlotIndex = slotIndex;
                _transform = transform;
                _towerType = towerType;
                _config = config;
                _projectileManager = projectileManager;
                _totalSpendGold = Mathf.Max(0, initialSpendGold);
                _rallyPoint = _transform != null ? _transform.position : Vector3.zero;
                
                if (_transform != null)
                {
                    _baseScale = _transform.localScale;
                    _sr = _transform.GetComponent<SpriteRenderer>();
                }

                if (_towerType == TowerType.Barracks)
                {
                    EnsureBarracksSoldiers(GetBarracksSquadSize(_config));
                    UpdateBarracksSoldierVisuals(_rallyPoint, 0f);
                }
            }

            public BarracksDebugInfo GetBarracksDebugInfo()
            {
                int total = 0;
                int alive = 0;
                int dead = 0;
                int respawning = 0;
                int idle = 0;
                int moving = 0;
                int blocking = 0;

                for (int i = 0; i < _barracksSoldiers.Count; i++)
                {
                    BarracksSoldierRuntime soldier = _barracksSoldiers[i];
                    if (soldier == null)
                    {
                        continue;
                    }

                    total++;
                    if (soldier.IsAlive)
                    {
                        alive++;
                    }
                    else
                    {
                        dead++;
                    }

                    switch (soldier.State)
                    {
                        case BarracksSoldierState.Idle:
                            idle++;
                            break;
                        case BarracksSoldierState.Moving:
                            moving++;
                            break;
                        case BarracksSoldierState.Blocking:
                            blocking++;
                            break;
                        case BarracksSoldierState.Respawning:
                            respawning++;
                            break;
                    }
                }

                return new BarracksDebugInfo(
                    TowerId,
                    total,
                    alive,
                    dead,
                    respawning,
                    idle,
                    moving,
                    blocking,
                    _enemyToSoldier.Count,
                    _blockedEnemies.Count,
                    _soldierDeathCount,
                    _soldierRespawnCount,
                    _rallyPoint);
            }

            public void Tick(float deltaTime, List<EnemyRuntime> enemies, TowerConfig config)
            {
                if (_transform == null)
                {
                    return;
                }

                _skillCooldownLeft = Mathf.Max(0f, _skillCooldownLeft - deltaTime);
                if (_towerType == TowerType.Barracks)
                {
                    TickBarracks(enemies, config, deltaTime);
                    return;
                }

                _cooldownLeft -= deltaTime;
                if (_cooldownLeft > 0f)
                {
                    return;
                }

                // Get current level data
                int levelIndex = Mathf.Clamp(_level - 1, 0, _config.Levels.Length - 1);
                var levelData = _config.Levels[levelIndex];

                bool canTargetAir = config != null && config.CanTarget(true);
                EnemyRuntime target = FindClosestTarget(enemies, levelData.Range, config);
                if (target == null)
                {
                    return;
                }

                float baseDamage = levelData.Damage;
                DamageType damageType = ResolveDamageType(config, _towerType);
                bool halfPhysicalPen = config.HalfPhysicalArmorPenetration || _towerType == TowerType.Artillery;
                AttackDeliveryType deliveryType = ResolveAttackDeliveryType(levelData, _towerType, levelIndex);

                // Advanced tier hooks: each tower type gets one distinct skill behavior.
                if (TryExecuteAdvancedSingleTargetSkill(target, baseDamage, levelData, levelIndex))
                {
                    return;
                }

                if (deliveryType == AttackDeliveryType.HitScan || _projectileManager == null)
                {
                    Debug.Log($"[Tower] HitScan towerType={_towerType} level={_level} damage={baseDamage:0.0} type={damageType}");
                    target.ApplyDamage(baseDamage, damageType, halfPhysicalPen);
                    TryExecuteArtilleryCluster(enemies, target, target.transform.position, baseDamage, levelIndex);
                    _cooldownLeft = Mathf.Max(0.1f, levelData.Cooldown);
                    return;
                }

                string projectileProfileId = ResolveProjectileProfileId(levelData, _towerType);
                bool useTargetPoint = _towerType == TowerType.Artillery;
                Vector3 targetPoint = target.transform.position;
                var spawnData = new ProjectileSpawnData(
                    _transform.position,
                    target,
                    targetPoint,
                    useTargetPoint,
                    _towerType,
                    damageType,
                    baseDamage,
                    halfPhysicalPen,
                    canTargetAir,
                    levelData.Range + 1f,
                    projectileProfileId);
                _projectileManager.SpawnProjectile(spawnData);
                TryExecuteArtilleryCluster(enemies, target, targetPoint, baseDamage, levelIndex);
                _cooldownLeft = Mathf.Max(0.1f, levelData.Cooldown);
            }

            public void HandleEnemyRemoved(EnemyRuntime enemy)
            {
                if (enemy == null)
                {
                    return;
                }

                UnassignEnemy(enemy, false);
            }

            public void ReleaseBlockedEnemy()
            {
                ReleaseAllEnemyAssignments(true);
                DestroyBarracksSoldiers();
            }

            public void SetRallyPoint(Vector3 point)
            {
                _rallyPoint = point;
                if (_towerType != TowerType.Barracks)
                {
                    return;
                }

                ReleaseAllEnemyAssignments(true);

                for (int i = 0; i < _barracksSoldiers.Count; i++)
                {
                    BarracksSoldierRuntime soldier = _barracksSoldiers[i];
                    if (soldier == null)
                    {
                        continue;
                    }

                    soldier.SetRallyPoint(_rallyPoint, true);
                }

                UpdateBarracksSoldierVisuals(_rallyPoint, 0f);
            }

            private EnemyRuntime FindClosestTarget(List<EnemyRuntime> enemies, float range, TowerConfig config)
            {
                float rangeSqr = range * range;
                EnemyRuntime best = null;
                float bestSqr = float.MaxValue;
                Vector3 origin = _transform.position;

                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyRuntime enemy = enemies[i];
                    if (enemy == null || enemy.IsDead)
                    {
                        continue;
                    }

                    if (config != null && !config.CanTarget(enemy.IsFlying))
                    {
                        continue;
                    }

                    float sqr = (enemy.transform.position - origin).sqrMagnitude;
                    if (sqr > rangeSqr || sqr >= bestSqr)
                    {
                        continue;
                    }

                    bestSqr = sqr;
                    best = enemy;
                }

                return best;
            }

            private static DamageType ResolveDamageType(TowerConfig config, TowerType towerType)
            {
                return towerType switch
                {
                    TowerType.Mage => DamageType.Magic,
                    _ => config.DamageType
                };
            }

            private static AttackDeliveryType ResolveAttackDeliveryType(TowerLevelData levelData, TowerType towerType, int levelIndex)
            {
                if (towerType == TowerType.Barracks)
                {
                    return AttackDeliveryType.Melee;
                }

                // 최소 특수 경로: Mage Lv3는 즉시 판정(HitScan) 허용.
                if (towerType == TowerType.Mage && levelIndex >= 2)
                {
                    return AttackDeliveryType.HitScan;
                }

                if (levelData.AttackDeliveryType != AttackDeliveryType.Melee)
                {
                    return levelData.AttackDeliveryType;
                }

                return towerType == TowerType.Mage ? AttackDeliveryType.Projectile : AttackDeliveryType.Projectile;
            }

            private static string ResolveProjectileProfileId(TowerLevelData levelData, TowerType towerType)
            {
                if (!string.IsNullOrWhiteSpace(levelData.ProjectileProfileId))
                {
                    return levelData.ProjectileProfileId;
                }

                return towerType switch
                {
                    TowerType.Archer => "Archer_Arrow",
                    TowerType.Mage => "Mage_Bolt",
                    TowerType.Artillery => "Artillery_Shell",
                    _ => string.Empty
                };
            }

            private void TickBarracks(List<EnemyRuntime> enemies, TowerConfig config, float deltaTime)
            {
                if (config == null || config.Levels == null || config.Levels.Length == 0)
                {
                    return;
                }

                int levelIndex = Mathf.Clamp(_level - 1, 0, _config.Levels.Length - 1);
                var levelData = _config.Levels[levelIndex];

                int squadSize = GetBarracksSquadSize(config);
                EnsureBarracksSoldiers(squadSize);
                ApplyBarracksSoldierStats(config);

                float blockRange = Mathf.Max(0.6f, config.BarracksData.RallyRange > 0.05f ? config.BarracksData.RallyRange : levelData.Range);
                float blockRangeSqr = blockRange * blockRange;
                Vector3 origin = _rallyPoint;

                CleanupInvalidSoldierAssignments(origin, blockRangeSqr);

                int attemptsLeft = enemies != null ? enemies.Count : 0;
                while (enemies != null && attemptsLeft-- > 0)
                {
                    int availableSoldierCount = GetAvailableSoldierCount();
                    if (_blockedEnemies.Count >= availableSoldierCount || _blockedEnemies.Count >= squadSize)
                    {
                        break;
                    }

                    BarracksSoldierRuntime soldier = FindFirstAvailableSoldier();
                    if (soldier == null)
                    {
                        break;
                    }

                    EnemyRuntime candidate = FindClosestGroundEnemy(enemies, origin, blockRange, _blockedEnemies, true);
                    if (candidate == null)
                    {
                        break;
                    }

                    Vector3 blockAnchor = GetBlockAnchorForSoldier(origin, soldier, squadSize);
                    if (candidate.TryEnterBlock(TowerId, blockAnchor))
                    {
                        AssignEnemyToSoldier(candidate, soldier);
                    }
                    else
                    {
                        break;
                    }
                }

                UpdateBarracksSoldierVisuals(origin, deltaTime);

                EnemyRuntime primaryBlocked = GetPrimaryBlockedEnemy();
                if (primaryBlocked == null)
                {
                    return;
                }

                TryExecuteBarracksThrowingAxe(enemies, primaryBlocked, blockRange, levelData.Damage, levelIndex);
            }

            private int GetBarracksSquadSize(TowerConfig config)
            {
                int squadSize = config != null ? config.BarracksData.SquadSize : 0;
                if (squadSize <= 0)
                {
                    squadSize = 1;
                }

                return Mathf.Clamp(squadSize, 1, MaxBarracksSoldierCount);
            }

            private void ApplyBarracksSoldierStats(TowerConfig config)
            {
                if (config == null)
                {
                    return;
                }

                float hp = Mathf.Max(1f, config.BarracksData.SoldierMaxHp);
                float damage = Mathf.Max(1f, config.BarracksData.SoldierDamage);
                float attackCooldown = Mathf.Max(0.1f, config.BarracksData.SoldierAttackCooldown);
                float respawnSec = Mathf.Max(0.1f, config.BarracksData.SoldierRespawnSec);

                for (int i = 0; i < _barracksSoldiers.Count; i++)
                {
                    BarracksSoldierRuntime soldier = _barracksSoldiers[i];
                    if (soldier == null)
                    {
                        continue;
                    }

                    soldier.UpdateStats(hp, damage, attackCooldown, respawnSec);
                    soldier.SetRallyPoint(_rallyPoint, false);
                }
            }

            private int GetAvailableSoldierCount()
            {
                int count = 0;
                for (int i = 0; i < _barracksSoldiers.Count; i++)
                {
                    BarracksSoldierRuntime soldier = _barracksSoldiers[i];
                    if (soldier != null && soldier.CanEngage)
                    {
                        count++;
                    }
                }

                return count;
            }

            private BarracksSoldierRuntime FindFirstAvailableSoldier()
            {
                for (int i = 0; i < _barracksSoldiers.Count; i++)
                {
                    BarracksSoldierRuntime soldier = _barracksSoldiers[i];
                    if (soldier == null || !soldier.CanEngage || soldier.BlockTarget != null)
                    {
                        continue;
                    }

                    return soldier;
                }

                return null;
            }

            private EnemyRuntime GetPrimaryBlockedEnemy()
            {
                for (int i = 0; i < _blockedEnemies.Count; i++)
                {
                    EnemyRuntime blocked = _blockedEnemies[i];
                    if (blocked != null && !blocked.IsDead)
                    {
                        return blocked;
                    }
                }

                return null;
            }

            private void CleanupInvalidSoldierAssignments(Vector3 origin, float blockRangeSqr)
            {
                for (int i = 0; i < _barracksSoldiers.Count; i++)
                {
                    BarracksSoldierRuntime soldier = _barracksSoldiers[i];
                    if (soldier == null)
                    {
                        continue;
                    }

                    EnemyRuntime target = soldier.BlockTarget;
                    if (target == null)
                    {
                        continue;
                    }

                    bool invalid = !soldier.CanEngage || target.IsDead;
                    if (!invalid)
                    {
                        float sqr = (target.transform.position - origin).sqrMagnitude;
                        invalid = sqr > blockRangeSqr * 1.8f;
                    }

                    if (invalid)
                    {
                        UnassignEnemy(target, true);
                    }
                }

                for (int i = _blockedEnemies.Count - 1; i >= 0; i--)
                {
                    EnemyRuntime blocked = _blockedEnemies[i];
                    if (blocked == null || blocked.IsDead || !_enemyToSoldier.ContainsKey(blocked))
                    {
                        _blockedEnemies.RemoveAt(i);
                    }
                }
            }

            private void AssignEnemyToSoldier(EnemyRuntime enemy, BarracksSoldierRuntime soldier)
            {
                if (enemy == null || soldier == null)
                {
                    return;
                }

                if (!_blockedEnemies.Contains(enemy))
                {
                    _blockedEnemies.Add(enemy);
                }

                _enemyToSoldier[enemy] = soldier;
                soldier.AssignBlockTarget(enemy);

                if (_subscribedBlockedEnemies.Add(enemy))
                {
                    enemy.AttackPerformed += HandleBlockedEnemyAttack;
                }
            }

            private void ReleaseAllEnemyAssignments(bool releaseBlock)
            {
                if (_enemyToSoldier.Count <= 0)
                {
                    _blockedEnemies.Clear();
                    return;
                }

                List<EnemyRuntime> assignedEnemies = new List<EnemyRuntime>(_enemyToSoldier.Keys);
                for (int i = 0; i < assignedEnemies.Count; i++)
                {
                    UnassignEnemy(assignedEnemies[i], releaseBlock);
                }

                _blockedEnemies.Clear();
            }

            private void UnassignSoldier(BarracksSoldierRuntime soldier, bool releaseBlock)
            {
                if (soldier == null)
                {
                    return;
                }

                EnemyRuntime target = soldier.BlockTarget;
                if (target != null)
                {
                    UnassignEnemy(target, releaseBlock);
                }
                else
                {
                    soldier.ClearBlockTarget();
                }
            }

            private void UnassignEnemy(EnemyRuntime enemy, bool releaseBlock)
            {
                if (enemy == null)
                {
                    return;
                }

                if (_enemyToSoldier.TryGetValue(enemy, out BarracksSoldierRuntime soldier))
                {
                    _enemyToSoldier.Remove(enemy);
                    if (soldier != null && soldier.BlockTarget == enemy)
                    {
                        soldier.ClearBlockTarget();
                    }
                }

                if (_subscribedBlockedEnemies.Remove(enemy))
                {
                    enemy.AttackPerformed -= HandleBlockedEnemyAttack;
                }

                for (int i = _blockedEnemies.Count - 1; i >= 0; i--)
                {
                    if (_blockedEnemies[i] == enemy)
                    {
                        _blockedEnemies.RemoveAt(i);
                    }
                }

                if (releaseBlock && !enemy.IsDead)
                {
                    enemy.ReleaseBlock(TowerId);
                }
            }

            private void HandleSoldierDied(BarracksSoldierRuntime soldier)
            {
                _soldierDeathCount++;
                UnassignSoldier(soldier, true);
            }

            private void HandleSoldierRespawned(BarracksSoldierRuntime soldier)
            {
                _soldierRespawnCount++;
            }

            private void HandleBlockedEnemyAttack(EnemyRuntime enemy, float damage)
            {
                if (enemy == null || damage <= 0f)
                {
                    return;
                }

                if (!_enemyToSoldier.TryGetValue(enemy, out BarracksSoldierRuntime soldier) || soldier == null)
                {
                    UnassignEnemy(enemy, false);
                    return;
                }

                if (!soldier.IsAlive)
                {
                    UnassignEnemy(enemy, true);
                    return;
                }

                soldier.ApplyDamage(damage, DamageType.Physical, false);
            }

            private void EnsureBarracksSoldiers(int squadSize)
            {
                if (_towerType != TowerType.Barracks || _transform == null)
                {
                    return;
                }

                while (_barracksSoldiers.Count < squadSize)
                {
                    int soldierIndex = _barracksSoldiers.Count;
                    var go = new GameObject($"BarracksSoldier_{TowerId}_{soldierIndex + 1}");
                    Transform parent = _transform.parent != null ? _transform.parent : _transform;
                    go.transform.SetParent(parent, false);
                    Vector3 initPosition = _rallyPoint + GetFormationOffset(soldierIndex, squadSize, BarracksSoldierIdleRadius);
                    initPosition.z = -0.5f;
                    go.transform.position = initPosition;
                    go.transform.localScale = Vector3.one * 0.62f;

                    var renderer = go.AddComponent<SpriteRenderer>();
                    Sprite soldierSprite = ResolveBarracksSoldierSprite(_config);
                    renderer.sprite = soldierSprite;
                    renderer.color = soldierSprite == GetOrCreateFallbackTowerSprite(TowerType.Barracks)
                        ? new Color(1f, 0.1f, 0.9f, 1f)
                        : Color.white;
                    if (_sr != null)
                    {
                        renderer.sortingLayerID = _sr.sortingLayerID;
                        renderer.sortingOrder = _sr.sortingOrder + 100;
                    }
                    else
                    {
                        renderer.sortingOrder = 200;
                    }

                    var soldier = go.AddComponent<BarracksSoldierRuntime>();
                    soldier.Initialize(
                        TowerId,
                        soldierIndex + 1,
                        _config.BarracksData.SoldierMaxHp,
                        _config.BarracksData.SoldierDamage,
                        _config.BarracksData.SoldierAttackCooldown,
                        _config.BarracksData.SoldierRespawnSec,
                        _rallyPoint,
                        renderer);
                    soldier.Died += HandleSoldierDied;
                    soldier.Respawned += HandleSoldierRespawned;
                    _barracksSoldiers.Add(soldier);
                    Debug.Log($"[Barracks] Soldier visual created. towerId={TowerId}, idx={soldierIndex + 1}, pos={go.transform.position}, sort={renderer.sortingLayerName}/{renderer.sortingOrder}");
                }

                while (_barracksSoldiers.Count > squadSize)
                {
                    int lastIndex = _barracksSoldiers.Count - 1;
                    BarracksSoldierRuntime soldier = _barracksSoldiers[lastIndex];
                    _barracksSoldiers.RemoveAt(lastIndex);

                    if (soldier != null)
                    {
                        soldier.Died -= HandleSoldierDied;
                        soldier.Respawned -= HandleSoldierRespawned;
                        UnassignSoldier(soldier, true);
                        Object.Destroy(soldier.gameObject);
                    }
                }
            }

            private void DestroyBarracksSoldiers()
            {
                ReleaseAllEnemyAssignments(true);

                for (int i = _barracksSoldiers.Count - 1; i >= 0; i--)
                {
                    BarracksSoldierRuntime soldier = _barracksSoldiers[i];
                    if (soldier != null)
                    {
                        soldier.Died -= HandleSoldierDied;
                        soldier.Respawned -= HandleSoldierRespawned;
                        Object.Destroy(soldier.gameObject);
                    }
                }

                _barracksSoldiers.Clear();
            }

            private void UpdateBarracksSoldierVisuals(Vector3 rallyOrigin, float deltaTime)
            {
                if (_barracksSoldiers.Count <= 0)
                {
                    return;
                }

                int soldierCount = _barracksSoldiers.Count;
                for (int i = 0; i < soldierCount; i++)
                {
                    BarracksSoldierRuntime soldier = _barracksSoldiers[i];
                    if (soldier == null)
                    {
                        continue;
                    }

                    if (_sr != null)
                    {
                        soldier.SetSorting(_sr.sortingLayerID, _sr.sortingOrder + 100);
                    }

                    EnemyRuntime assignedEnemy = soldier.BlockTarget;
                    Vector3 desiredPos;
                    if (assignedEnemy != null && !assignedEnemy.IsDead)
                    {
                        desiredPos = assignedEnemy.transform.position + GetFormationOffset(i, soldierCount, BarracksSoldierCombatRadius);
                    }
                    else
                    {
                        desiredPos = rallyOrigin + GetFormationOffset(i, soldierCount, BarracksSoldierIdleRadius);
                    }

                    desiredPos.z = -0.5f;
                    soldier.Tick(deltaTime, desiredPos);
                }
            }

            private static Vector3 GetFormationOffset(int index, int count, float radius)
            {
                if (count <= 1)
                {
                    return Vector3.zero;
                }

                float angle = (Mathf.PI * 2f * index) / count;
                return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            }

            private static Vector3 GetBlockAnchorForSoldier(Vector3 rallyOrigin, BarracksSoldierRuntime soldier, int squadSize)
            {
                int index = 0;
                if (soldier != null)
                {
                    index = Mathf.Max(0, soldier.SoldierIndex - 1);
                }

                int clampedCount = Mathf.Clamp(squadSize, 1, MaxBarracksSoldierCount);
                Vector3 offset = GetFormationOffset(index, clampedCount, BarracksEnemyBlockRadius);
                Vector3 anchor = rallyOrigin + offset;
                anchor.z = rallyOrigin.z;
                return anchor;
            }

            private EnemyRuntime FindClosestGroundEnemy(
                List<EnemyRuntime> enemies,
                Vector3 origin,
                float range,
                List<EnemyRuntime> excluded = null,
                bool requireUnblocked = false)
            {
                if (enemies == null || enemies.Count == 0)
                {
                    return null;
                }

                float rangeSqr = range * range;
                EnemyRuntime best = null;
                float bestSqr = float.MaxValue;

                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyRuntime enemy = enemies[i];
                    if (enemy == null || enemy.IsDead || enemy.IsFlying)
                    {
                        continue;
                    }

                    if (enemy.IsBoss || (enemy.Config != null && !enemy.Config.CanBeBlocked))
                    {
                        continue;
                    }

                    if (requireUnblocked && enemy.IsBlocked)
                    {
                        continue;
                    }

                    if (excluded != null && excluded.Contains(enemy))
                    {
                        continue;
                    }

                    float sqr = (enemy.transform.position - origin).sqrMagnitude;
                    if (sqr > rangeSqr || sqr >= bestSqr)
                    {
                        continue;
                    }

                    bestSqr = sqr;
                    best = enemy;
                }

                return best;
            }

            private EnemyRuntime FindClosestGroundEnemyExcluding(List<EnemyRuntime> enemies, Vector3 origin, float range, EnemyRuntime excluded)
            {
                if (enemies == null || enemies.Count == 0)
                {
                    return null;
                }

                float rangeSqr = range * range;
                EnemyRuntime best = null;
                float bestSqr = float.MaxValue;

                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyRuntime enemy = enemies[i];
                    if (enemy == null || enemy.IsDead || enemy.IsFlying || enemy == excluded)
                    {
                        continue;
                    }

                    float sqr = (enemy.transform.position - origin).sqrMagnitude;
                    if (sqr > rangeSqr || sqr >= bestSqr)
                    {
                        continue;
                    }

                    bestSqr = sqr;
                    best = enemy;
                }

                return best;
            }

            private bool TryExecuteAdvancedSingleTargetSkill(EnemyRuntime target, float baseDamage, TowerLevelData levelData, int levelIndex)
            {
                if (target == null || levelIndex < 2 || _skillCooldownLeft > 0f)
                {
                    return false;
                }

                switch (_towerType)
                {
                    case TowerType.Archer:
                        // Sniper shot: low proc chance, big payoff, boss-safe fallback.
                        if (Random.value > 0.18f)
                        {
                            return false;
                        }

                        if (!target.TryApplyInstantKill())
                        {
                            target.ApplyDamage(baseDamage * 2.4f, DamageType.True, false);
                        }
                        _skillCooldownLeft = ArcherSniperCooldownSec;
                        _cooldownLeft = Mathf.Max(0.1f, levelData.Cooldown * 0.9f);
                        return true;

                    case TowerType.Mage:
                        // Death ray: magic burst with instant-kill attempt.
                        if (Random.value > 0.20f)
                        {
                            return false;
                        }

                        if (!target.TryApplyInstantKill())
                        {
                            target.ApplyDamage(baseDamage * 2.8f, DamageType.Magic, false);
                        }
                        _skillCooldownLeft = MageDeathRayCooldownSec;
                        _cooldownLeft = Mathf.Max(0.1f, levelData.Cooldown);
                        return true;

                    default:
                        return false;
                }
            }

            private void TryExecuteArtilleryCluster(List<EnemyRuntime> enemies, EnemyRuntime primaryTarget, Vector3 center, float baseDamage, int levelIndex)
            {
                if (_towerType != TowerType.Artillery || levelIndex < 2 || _skillCooldownLeft > 0f || enemies == null)
                {
                    return;
                }

                const float clusterRadius = 1.15f;
                float radiusSqr = clusterRadius * clusterRadius;
                float clusterDamage = Mathf.Max(1f, baseDamage * 0.55f);
                bool applied = false;

                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyRuntime enemy = enemies[i];
                    if (enemy == null || enemy.IsDead || enemy.IsFlying || enemy == primaryTarget)
                    {
                        continue;
                    }

                    float sqr = (enemy.transform.position - center).sqrMagnitude;
                    if (sqr > radiusSqr)
                    {
                        continue;
                    }

                    enemy.ApplyDamage(clusterDamage, DamageType.Physical, true);
                    applied = true;
                }

                if (applied)
                {
                    _skillCooldownLeft = ArtilleryClusterCooldownSec;
                }
            }

            private void TryExecuteBarracksThrowingAxe(List<EnemyRuntime> enemies, EnemyRuntime blockedTarget, float blockRange, float baseDamage, int levelIndex)
            {
                if (_towerType != TowerType.Barracks || levelIndex < 2 || _skillCooldownLeft > 0f || enemies == null)
                {
                    return;
                }

                EnemyRuntime supportTarget = FindClosestGroundEnemyExcluding(enemies, _rallyPoint, blockRange * 1.45f, blockedTarget);
                if (supportTarget == null)
                {
                    return;
                }

                supportTarget.ApplyDamage(Mathf.Max(1f, baseDamage * 0.65f), DamageType.Physical, false);
                _skillCooldownLeft = BarracksThrowAxeCooldownSec;
            }

            public int GetUpgradeCost(int /*unused*/ baseBuildCost)
                {
                    if (!CanUpgrade)
                    {
                        return 0;
                    }

                    // Next level cost
                    int nextLevelIndex = _level; // Current 1 (index 0) -> Next is 2 (index 1)
                    if (nextLevelIndex < _config.Levels.Length)
                    {
                        return Mathf.Max(1, _config.Levels[nextLevelIndex].Cost);
                    }
                    
                    return 9999;
                }

            public void Upgrade(int spentGold)
            {
                if (!CanUpgrade)
                {
                    return;
                }

                _level++;
                _totalSpendGold += Mathf.Max(0, spentGold);
                
                // Update visuals
                int levelIndex = Mathf.Clamp(_level - 1, 0, _config.Levels.Length - 1);
                var levelData = _config.Levels[levelIndex];
                
                if (_transform != null)
                {
                    float scale = levelData.VisualScale > 0.05f ? levelData.VisualScale : 0.65f + ((_level - 1) * 0.1f);
                    _baseScale = new Vector3(scale, scale, 1f);
                    _transform.localScale = _baseScale; // Immediate sync
                    
                    if (_sr != null)
                    {
                        Sprite resolvedLevelSprite = ResolveTowerLevelSprite(_config, _towerType, levelIndex);
                        if (resolvedLevelSprite != null)
                        {
                            _sr.sprite = resolvedLevelSprite;
                            _sr.color = Color.white;
                        }
                        else
                        {
                            bool fallbackVisual = _sr.sprite == null || _sr.sprite == GetOrCreateFallbackTowerSprite(_towerType);
                            if (fallbackVisual)
                            {
                                _sr.sprite = GetOrCreateFallbackTowerSprite(_towerType);
                                Color baseColor = GetTowerTint(_towerType);
                                _sr.color = Color.Lerp(baseColor, Color.white, (_level - 1) * 0.25f);
                            }
                            else
                            {
                                _sr.color = Color.white;
                            }
                        }
                    }
                    
                    // Do a visual punch
                    var mono = _transform.GetComponent<MonoBehaviour>();
                    if (mono != null)
                    {
                         mono.StartCoroutine(VisualPunchRoutine());
                    }
                }
            }

            private System.Collections.IEnumerator VisualPunchRoutine()
            {
                if (_transform == null) yield break;
                
                Vector3 start = _baseScale * 1.4f; // Pop up
                Vector3 end = _baseScale;
                float duration = 0.25f;
                float t = 0f;
                
                while (t < duration)
                {
                    t += Time.deltaTime;
                    if (_transform == null) yield break;
                    _transform.localScale = Vector3.Lerp(start, end, t / duration);
                    yield return null;
                }
                
                if (_transform != null) _transform.localScale = end;
            }

            public int GetSellRefund()
            {
                int baseGold = Mathf.Max(1, _totalSpendGold);
                return Mathf.Max(1, Mathf.RoundToInt(baseGold * 0.6f));
            }
        }
    }
}
