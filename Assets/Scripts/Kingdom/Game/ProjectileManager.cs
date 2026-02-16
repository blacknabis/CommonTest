using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Game
{
    public class ProjectileManager : MonoBehaviour
    {
        private const string ProfileResourcePrefix = "Data/ProjectileProfiles/";
        private const bool EnableProjectileDebugLog = true;

        private readonly List<EnemyRuntime> _aliveEnemies = new();
        private readonly List<ProjectileRuntime> _projectiles = new();
        private readonly Dictionary<string, ProjectileProfile> _profileCache = new();
        private readonly Dictionary<TowerType, ProjectileProfile> _fallbackProfiles = new();

        private SpawnManager _spawnManager;
        private Transform _projectileRoot;
        private int _nextProjectileId = 1;
        private Sprite _fallbackProjectileSprite;

        public int ActiveProjectileCount => _projectiles.Count;

        public void Configure(SpawnManager spawnManager, Transform projectileRoot)
        {
            _spawnManager = spawnManager;
            _projectileRoot = projectileRoot;
            RebindSpawnEvents();
            EnsureFallbackProfiles();
        }

        public void SpawnProjectile(in ProjectileSpawnData data)
        {
            EnsureFallbackProfiles();
            ProjectileProfile profile = ResolveProfile(data.ProjectileProfileId, data.TowerType);
            GameObject go = new GameObject($"Projectile_{data.TowerType}_{_nextProjectileId}");
            if (_projectileRoot != null)
            {
                go.transform.SetParent(_projectileRoot, false);
            }

            go.transform.position = data.StartPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetFallbackProjectileSprite();
            sr.sortingOrder = 50;
            sr.color = GetProjectileColor(data.TowerType);

            if (profile != null && profile.HitRadius > 0.01f)
            {
                float size = Mathf.Clamp(profile.HitRadius * 6f, 0.06f, 0.4f);
                go.transform.localScale = new Vector3(size, size, 1f);
            }

            var runtime = new ProjectileRuntime(_nextProjectileId++, go.transform, profile, data);
            _projectiles.Add(runtime);

            if (EnableProjectileDebugLog)
            {
                Debug.Log($"[Projectile] Spawn towerType={data.TowerType} moveType={profile.MoveType} dmg={data.Damage:0.0} useTargetPoint={data.UseTargetPoint}");
            }
        }

        public EnemyRuntime TryFindNearestEnemy(Vector3 center, float maxRange, bool canTargetAir, HashSet<int> excluded = null)
        {
            float bestSqr = float.MaxValue;
            EnemyRuntime best = null;
            float rangeSqr = Mathf.Max(0.1f, maxRange) * Mathf.Max(0.1f, maxRange);

            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyRuntime enemy = _aliveEnemies[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                if (!canTargetAir && enemy.IsFlying)
                {
                    continue;
                }

                int id = enemy.GetInstanceID();
                if (excluded != null && excluded.Contains(id))
                {
                    continue;
                }

                float sqr = (enemy.transform.position - center).sqrMagnitude;
                if (sqr > rangeSqr || sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = enemy;
            }

            return best;
        }

        public void ApplyDamageToEnemy(EnemyRuntime enemy, float damage, DamageType damageType, bool halfPhysicalArmorPenetration)
        {
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            if (EnableProjectileDebugLog)
            {
                string enemyId = enemy.Config != null ? enemy.Config.EnemyId : "Unknown";
                Debug.Log($"[Projectile] HitSingle enemy={enemyId} damage={damage:0.0} type={damageType} halfPen={halfPhysicalArmorPenetration}");
            }

            enemy.ApplyDamage(damage, damageType, halfPhysicalArmorPenetration);
        }

        public void ApplyDamageInRadius(Vector3 center, float radius, float damage, DamageType damageType, bool halfPhysicalArmorPenetration, bool canTargetAir)
        {
            float radiusSqr = Mathf.Max(0.01f, radius) * Mathf.Max(0.01f, radius);
            for (int i = 0; i < _aliveEnemies.Count; i++)
            {
                EnemyRuntime enemy = _aliveEnemies[i];
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                if (!canTargetAir && enemy.IsFlying)
                {
                    continue;
                }

                if ((enemy.transform.position - center).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                if (EnableProjectileDebugLog)
                {
                    string enemyId = enemy.Config != null ? enemy.Config.EnemyId : "Unknown";
                    Debug.Log($"[Projectile] HitAoE enemy={enemyId} radius={radius:0.00} damage={damage:0.0} type={damageType} halfPen={halfPhysicalArmorPenetration}");
                }

                enemy.ApplyDamage(damage, damageType, halfPhysicalArmorPenetration);
            }
        }

        private void Update()
        {
            if (_projectiles.Count == 0)
            {
                return;
            }

            float dt = Time.deltaTime;
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                ProjectileRuntime projectile = _projectiles[i];
                bool alive = projectile != null && projectile.Tick(dt, this);
                if (alive)
                {
                    continue;
                }

                if (projectile != null)
                {
                    Transform projectileTransform = projectile.Transform;
                    if (projectileTransform != null)
                    {
                        Destroy(projectileTransform.gameObject);
                    }
                }

                _projectiles.RemoveAt(i);
            }
        }

        private ProjectileProfile ResolveProfile(string profileId, TowerType towerType)
        {
            if (!string.IsNullOrWhiteSpace(profileId))
            {
                if (_profileCache.TryGetValue(profileId, out ProjectileProfile cached) && cached != null)
                {
                    return cached;
                }

                ProjectileProfile loaded = Resources.Load<ProjectileProfile>(ProfileResourcePrefix + profileId);
                if (loaded != null)
                {
                    _profileCache[profileId] = loaded;
                    return loaded;
                }
            }

            if (_fallbackProfiles.TryGetValue(towerType, out ProjectileProfile fallback) && fallback != null)
            {
                return fallback;
            }

            return _fallbackProfiles[TowerType.Archer];
        }

        private void EnsureFallbackProfiles()
        {
            if (_fallbackProfiles.Count > 0)
            {
                return;
            }

            _fallbackProfiles[TowerType.Archer] = CreateFallbackProfile("Archer_Arrow", ProjectileMoveType.Homing, 11f, 1.6f, 0.12f, 0f, false, 1);
            _fallbackProfiles[TowerType.Mage] = CreateFallbackProfile("Mage_Bolt", ProjectileMoveType.Homing, 9f, 1.8f, 0.14f, 0f, false, 1);
            _fallbackProfiles[TowerType.Artillery] = CreateFallbackProfile("Artillery_Shell", ProjectileMoveType.Ballistic, 6f, 2.4f, 0.16f, 1.1f, false, 1);
            _fallbackProfiles[TowerType.Barracks] = CreateFallbackProfile("Melee_None", ProjectileMoveType.Linear, 0f, 0.1f, 0.1f, 0f, false, 1);
        }

        private static ProjectileProfile CreateFallbackProfile(
            string id,
            ProjectileMoveType moveType,
            float speed,
            float maxLifetime,
            float hitRadius,
            float explosionRadius,
            bool canPierce,
            int maxHitCount)
        {
            ProjectileProfile profile = ScriptableObject.CreateInstance<ProjectileProfile>();
            profile.hideFlags = HideFlags.DontSave;
            profile.ProjectileId = id;
            profile.MoveType = moveType;
            profile.Speed = speed;
            profile.MaxLifetime = maxLifetime;
            profile.HitRadius = hitRadius;
            profile.ExplosionRadius = explosionRadius;
            profile.CanPierce = canPierce;
            profile.MaxHitCount = maxHitCount;
            return profile;
        }

        private Sprite GetFallbackProjectileSprite()
        {
            if (_fallbackProjectileSprite != null)
            {
                return _fallbackProjectileSprite;
            }

            Texture2D tex = new Texture2D(6, 6, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[36];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _fallbackProjectileSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 24f);
            return _fallbackProjectileSprite;
        }

        private static Color GetProjectileColor(TowerType towerType)
        {
            return towerType switch
            {
                TowerType.Archer => new Color(0.9f, 0.95f, 1f, 1f),
                TowerType.Mage => new Color(0.7f, 0.5f, 1f, 1f),
                TowerType.Artillery => new Color(0.95f, 0.65f, 0.3f, 1f),
                _ => Color.white
            };
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
        }
    }
}
