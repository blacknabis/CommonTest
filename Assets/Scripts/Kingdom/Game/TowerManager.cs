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

        private readonly List<EnemyRuntime> _aliveEnemies = new();
        private readonly List<TowerRuntime> _towers = new();
        private readonly List<Vector3> _towerSlots = new();
        private readonly List<int> _freeSlotIndices = new();

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
                // Try load specific config: Data/TowerConfigs/Archer, etc.
                var loaded = Resources.Load<TowerConfig>($"Data/TowerConfigs/{type}");
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

                    ApplyWorldMapMetaUpgrades(runtimeConfig, type);
                    _towerConfigs[type] = runtimeConfig;
                }
                else
                {
                    // Fallback generator
                    TowerConfig fallback = CreateFallbackTowerConfig(type);
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
            Sprite initialSprite = null;
            float initialScale = 0.65f;
            if (config != null && config.Levels != null && config.Levels.Length > 0)
            {
                initialSprite = config.Levels[0].SpriteOverride;
                if (config.Levels[0].VisualScale > 0.05f) 
                {
                    initialScale = config.Levels[0].VisualScale;
                }
            }

            sr.sprite = initialSprite != null ? initialSprite : CreateFallbackTowerSprite();
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
            
            // Base logic
            float damage = type == TowerType.Artillery ? 25f : (type == TowerType.Barracks ? 5f : 34f);
            float cooldown = type == TowerType.Mage ? 1.2f : 0.75f;
            float baseScale = 0.65f;
            float baseRange = type == TowerType.Barracks ? 1.6f : (type == TowerType.Mage ? 2.0f : (type == TowerType.Artillery ? 2.4f : 2.2f));
            
            // Barracks specific
            if (type == TowerType.Barracks)
            {
                config.BarracksData = new BarracksData { SquadSize = 3, RallyRange = baseRange };
            }

            config.Levels = new TowerLevelData[3];
            for (int i = 0; i < 3; i++)
            {
                float multiplier = 1f + (i * 0.4f); // Cost multiplier
                float statMultiplier = 1f + (i * 0.3f); // Dmg multiplier
                float cooldownReduc = Mathf.Max(0.5f, 1f - (i * 0.1f)); // Cooldown reduction

                config.Levels[i] = new TowerLevelData
                {
                    Cost = Mathf.RoundToInt(70 * multiplier), // Base cost 70 for all? Or vary?
                    Damage = damage * statMultiplier,
                    Cooldown = Mathf.Max(0.2f, cooldown * cooldownReduc),
                    Range = baseRange + (i * 0.2f),
                    AttackDeliveryType = type == TowerType.Barracks
                        ? AttackDeliveryType.Melee
                        : (type == TowerType.Mage ? AttackDeliveryType.Projectile : AttackDeliveryType.Projectile),
                    ProjectileProfileId = type == TowerType.Archer
                        ? "Archer_Arrow"
                        : (type == TowerType.Mage
                            ? "Mage_Bolt"
                            : (type == TowerType.Artillery ? "Artillery_Shell" : string.Empty)),
                    SpriteOverride = null,
                    VisualScale = baseScale + (i * 0.1f)
                };
                
                // Adjust base cost per type if needed (e.g. Mage higher)
                float costMod = type == TowerType.Mage ? 1.4f : (type == TowerType.Artillery ? 1.8f : (type == TowerType.Barracks ? 1.1f : 1f));
                config.Levels[i].Cost = Mathf.RoundToInt(config.Levels[i].Cost * costMod);
            }
            
            config.DamageType = type == TowerType.Mage ? DamageType.Magic : DamageType.Physical;
            config.HalfPhysicalArmorPenetration = type == TowerType.Artillery;
            config.CanTargetAir = type != TowerType.Barracks && type != TowerType.Artillery;
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

        private static Sprite CreateFallbackTowerSprite()
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
        }

        private static Color GetTowerTint(TowerType towerType)
        {
            return towerType switch
            {
                TowerType.Archer => new Color(0.25f, 0.45f, 0.85f, 1f),
                TowerType.Barracks => new Color(0.42f, 0.62f, 0.24f, 1f),
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
            private readonly Transform _transform;
            private readonly TowerType _towerType;
            private readonly TowerConfig _config;
            private readonly ProjectileManager _projectileManager;
            private int _level = 1;
            private int _totalSpendGold;
            private float _cooldownLeft;
            private EnemyRuntime _blockedEnemy;
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
            }

            public void Tick(float deltaTime, List<EnemyRuntime> enemies, TowerConfig config)
            {
                if (_transform == null)
                {
                    return;
                }

                _cooldownLeft -= deltaTime;
                if (_cooldownLeft > 0f)
                {
                    return;
                }

                if (_towerType == TowerType.Barracks)
                {
                    TickBarracks(enemies, config);
                    return;
                }

                // Get current level data
                int levelIndex = Mathf.Clamp(_level - 1, 0, _config.Levels.Length - 1);
                var levelData = _config.Levels[levelIndex];

                bool canTargetAir = ResolveCanTargetAir(config, _towerType);
                EnemyRuntime target = FindClosestTarget(enemies, levelData.Range, canTargetAir);
                if (target == null)
                {
                    return;
                }

                float baseDamage = levelData.Damage;
                DamageType damageType = ResolveDamageType(config, _towerType);
                bool halfPhysicalPen = config.HalfPhysicalArmorPenetration || _towerType == TowerType.Artillery;
                AttackDeliveryType deliveryType = ResolveAttackDeliveryType(levelData, _towerType, levelIndex);

                if (deliveryType == AttackDeliveryType.HitScan || _projectileManager == null)
                {
                    Debug.Log($"[Tower] HitScan towerType={_towerType} level={_level} damage={baseDamage:0.0} type={damageType}");
                    target.ApplyDamage(baseDamage, damageType, halfPhysicalPen);
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
                _cooldownLeft = Mathf.Max(0.1f, levelData.Cooldown);
            }

            public void HandleEnemyRemoved(EnemyRuntime enemy)
            {
                if (enemy == null || _blockedEnemy == null)
                {
                    return;
                }

                if (_blockedEnemy == enemy)
                {
                    _blockedEnemy.ReleaseBlock(TowerId);
                    _blockedEnemy = null;
                }
            }

            public void ReleaseBlockedEnemy()
            {
                if (_blockedEnemy != null)
                {
                    _blockedEnemy.ReleaseBlock(TowerId);
                    _blockedEnemy = null;
                }
            }

            public void SetRallyPoint(Vector3 point)
            {
                _rallyPoint = point;
            }

            private EnemyRuntime FindClosestTarget(List<EnemyRuntime> enemies, float range, bool canTargetAir)
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

                    if (!canTargetAir && enemy.IsFlying)
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

            private static bool ResolveCanTargetAir(TowerConfig config, TowerType towerType)
            {
                if (towerType == TowerType.Barracks)
                {
                    return false;
                }

                return config.CanTargetAir;
            }

            private void TickBarracks(List<EnemyRuntime> enemies, TowerConfig config)
            {
                int levelIndex = Mathf.Clamp(_level - 1, 0, _config.Levels.Length - 1);
                var levelData = _config.Levels[levelIndex];
                
                float blockRange = Mathf.Max(0.6f, config.BarracksData.RallyRange > 0.05f ? config.BarracksData.RallyRange : levelData.Range);
                float blockRangeSqr = blockRange * blockRange;
                Vector3 origin = _rallyPoint;

                if (_blockedEnemy != null)
                {
                    bool invalid = _blockedEnemy.IsDead;
                    if (!invalid)
                    {
                        float sqr = (_blockedEnemy.transform.position - origin).sqrMagnitude;
                        invalid = sqr > blockRangeSqr * 1.8f;
                    }

                    if (invalid)
                    {
                        _blockedEnemy.ReleaseBlock(TowerId);
                        _blockedEnemy = null;
                    }
                }

                if (_blockedEnemy == null)
                {
                    EnemyRuntime candidate = FindClosestGroundEnemy(enemies, blockRange);
                    if (candidate != null && candidate.TryEnterBlock(TowerId, origin))
                    {
                        _blockedEnemy = candidate;
                    }
                }

                if (_blockedEnemy == null)
                {
                    _cooldownLeft = 0.1f;
                    return;
                }

                _blockedEnemy.ApplyDamage(levelData.Damage, DamageType.Physical, false);
                _cooldownLeft = Mathf.Max(0.22f, levelData.Cooldown);
            }

            private EnemyRuntime FindClosestGroundEnemy(List<EnemyRuntime> enemies, float range)
            {
                float rangeSqr = range * range;
                EnemyRuntime best = null;
                float bestSqr = float.MaxValue;
                Vector3 origin = _transform.position;

                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyRuntime enemy = enemies[i];
                    if (enemy == null || enemy.IsDead || enemy.IsFlying)
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
                    
                    if (_sr != null && levelData.SpriteOverride != null)
                    {
                        _sr.sprite = levelData.SpriteOverride;
                    }
                    else if (_sr != null && levelData.SpriteOverride == null)
                    {
                        // Tint change fallback if no sprite
                        Color baseColor = GetTowerTint(_towerType);
                        _sr.color = Color.Lerp(baseColor, Color.white, (_level - 1) * 0.25f);
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
