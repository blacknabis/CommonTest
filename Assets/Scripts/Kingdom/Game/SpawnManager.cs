using System;
using System.Collections;
using System.Collections.Generic;
using Common.Extensions;
using UnityEngine;
using Kingdom.Game.UI;

namespace Kingdom.Game
{
    /// <summary>
    /// Spawns enemies for wave data.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private Transform enemyRoot;
        private readonly Dictionary<EnemyRuntime, EnemyConfig> _enemyConfigMap = new();
        private static Sprite _fallbackEnemySprite;
        private static readonly Dictionary<string, Sprite> RuntimeTextureSpriteCache = new();
        private static readonly HashSet<string> EnemyAnimatorLogCache = new();
        private static readonly string[] EnemySpriteResourcePrefixes =
        {
            "UI/Sprites/Enemies/",
            "Sprites/Enemies/",
            "Kingdom/Enemies/Sprites/"
        };

        public event Action<EnemyRuntime, EnemyConfig> EnemySpawned;
        public event Action<EnemyRuntime, EnemyConfig> EnemyReachedGoal;
        public event Action<EnemyRuntime, EnemyConfig> EnemyKilled;
        public event Action WaveSpawnCompleted;

        public void SetEnemyRoot(Transform root)
        {
            enemyRoot = root;
        }

        public Coroutine SpawnWave(
            WaveConfig.WaveData wave,
            PathManager pathManager)
        {
            return StartCoroutine(CoSpawnWave(wave, pathManager));
        }

        private IEnumerator CoSpawnWave(WaveConfig.WaveData wave, PathManager pathManager)
        {
            if (wave.SpawnEntries == null)
            {
                yield break;
            }

            for (int i = 0; i < wave.SpawnEntries.Count; i++)
            {
                WaveConfig.SpawnEntry entry = wave.SpawnEntries[i];

                if (entry.SpawnDelay > 0f)
                {
                    yield return new WaitForSeconds(entry.SpawnDelay);
                }

                int spawnCount = Mathf.Max(0, entry.Count);
                float spawnInterval = Mathf.Max(0.05f, entry.SpawnInterval);

                for (int j = 0; j < spawnCount; j++)
                {
                    SpawnOne(entry, pathManager);
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            WaveSpawnCompleted?.Invoke();
        }

        private void SpawnOne(WaveConfig.SpawnEntry entry, PathManager pathManager)
        {
            List<Vector3> path;
            if (pathManager == null || !pathManager.TryGetPath(entry.PathId, out path))
            {
                Debug.LogWarning($"[SpawnManager] Path not found. PathId={entry.PathId}");
                return;
            }

            GameObject enemyGo = new GameObject($"Enemy_{entry.Enemy?.EnemyId ?? "Unknown"}");
            if (enemyRoot != null)
            {
                enemyGo.transform.SetParent(enemyRoot, true);
            }
            enemyGo.layer = ResolveEnemyLayer(entry.Enemy);

            // Assigns a default sprite renderer for combat visibility.
            var renderer = enemyGo.AddComponent<SpriteRenderer>();
            CircleCollider2D selectionCollider = enemyGo.GetComponent<CircleCollider2D>();
            if (selectionCollider.IsNull())
            {
                selectionCollider = enemyGo.AddComponent<CircleCollider2D>();
            }

            selectionCollider.isTrigger = true;
            selectionCollider.radius = 0.35f;

            EnemyConfig config = entry.Enemy;
            Sprite resolvedSprite = ResolveEnemySprite(config);
            RuntimeAnimatorController animatorController = ResolveEnemyAnimatorController(config);
            if (animatorController.IsNull())
            {
                string enemyId = config != null ? config.EnemyId : "(null)";
                Debug.LogError($"[SpawnManager] Animator is required but not found. enemyId={enemyId}");
                return;
            }

            if (animatorController.IsNotNull())
            {
                Animator animator = enemyGo.GetComponent<Animator>();
                if (animator.IsNull())
                {
                    animator = enemyGo.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController = animatorController;
            }
            LogEnemyAnimatorResolutionOnce(config, animatorController);
            Sprite[] idleFrames = Array.Empty<Sprite>();
            Sprite[] moveFrames = Array.Empty<Sprite>();
            Sprite[] attackFrames = Array.Empty<Sprite>();
            Sprite[] dieFrames = Array.Empty<Sprite>();

            renderer.sprite = moveFrames != null && moveFrames.Length > 0 && moveFrames[0] != null
                ? moveFrames[0]
                : resolvedSprite;
            renderer.color = ResolveEnemyColor(config, IsFallbackEnemySprite(resolvedSprite));
            renderer.sortingOrder = 20;
            enemyGo.transform.localScale = Vector3.one * ResolveEnemyScale(config);

            EnemyRuntime enemy = enemyGo.AddComponent<EnemyRuntime>();
            enemy.Initialize(config, path, renderer, idleFrames, moveFrames, attackFrames, dieFrames);
            enemy.ReachedGoal += OnEnemyReachedGoal;
            enemy.Killed += OnEnemyKilled;
            enemy.DeathBurstTriggered += OnEnemyDeathBurstTriggered;

            if (WorldHpBarManager.Instance.IsNotNull())
            {
                WorldHpBarManager.Instance.TrackTarget(enemy);
            }

            _enemyConfigMap[enemy] = config;

            EnemySpawned?.Invoke(enemy, config);
        }

        private void OnEnemyReachedGoal(EnemyRuntime enemy)
        {
            _enemyConfigMap.TryGetValue(enemy, out EnemyConfig config);
            enemy.ReachedGoal -= OnEnemyReachedGoal;
            enemy.Killed -= OnEnemyKilled;
            enemy.DeathBurstTriggered -= OnEnemyDeathBurstTriggered;
            _enemyConfigMap.Remove(enemy);
            EnemyReachedGoal?.Invoke(enemy, config);
            Destroy(enemy.gameObject);
        }

        private void OnEnemyKilled(EnemyRuntime enemy)
        {
            _enemyConfigMap.TryGetValue(enemy, out EnemyConfig config);
            enemy.ReachedGoal -= OnEnemyReachedGoal;
            enemy.Killed -= OnEnemyKilled;
            enemy.DeathBurstTriggered -= OnEnemyDeathBurstTriggered;
            _enemyConfigMap.Remove(enemy);
            EnemyKilled?.Invoke(enemy, config);

            float deathDelay = enemy != null ? enemy.GetDeathVisualDuration() : 0f;
            if (deathDelay > 0f && enemy != null)
            {
                StartCoroutine(CoDestroyEnemyAfterDelay(enemy.gameObject, deathDelay));
            }
            else if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        private IEnumerator CoDestroyEnemyAfterDelay(GameObject enemyObject, float delay)
        {
            if (enemyObject == null)
            {
                yield break;
            }

            float safeDelay = Mathf.Max(0f, delay);
            if (safeDelay > 0f)
            {
                yield return new WaitForSeconds(safeDelay);
            }

            if (enemyObject != null)
            {
                Destroy(enemyObject);
            }
        }

        private void OnEnemyDeathBurstTriggered(EnemyRuntime source, float radius, float damage, Vector3 center)
        {
            if (source == null || damage <= 0f || radius <= 0f)
            {
                return;
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            float radiusSqr = radius * radius;
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour == source)
                {
                    continue;
                }

                if (behaviour is EnemyRuntime)
                {
                    continue;
                }

                if (behaviour is not IDamageable damageable || !damageable.IsAlive)
                {
                    continue;
                }

                float sqr = (behaviour.transform.position - center).sqrMagnitude;
                if (sqr > radiusSqr)
                {
                    continue;
                }

                damageable.ApplyDamage(damage, DamageType.Physical, false);
            }
        }

        private static Sprite GetFallbackEnemySprite()
        {
            if (_fallbackEnemySprite != null)
            {
                return _fallbackEnemySprite;
            }

            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _fallbackEnemySprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                16f);
            return _fallbackEnemySprite;
        }

        private static Sprite ResolveEnemySprite(EnemyConfig config)
        {
            if (config != null && !string.IsNullOrWhiteSpace(config.EnemyId))
            {
                string enemyId = config.EnemyId.Trim();
                for (int i = 0; i < EnemySpriteResourcePrefixes.Length; i++)
                {
                    string prefix = EnemySpriteResourcePrefixes[i];
                    if (TryLoadSprite(prefix + enemyId, out Sprite byId))
                    {
                        return byId;
                    }

                    if (TryLoadSprite($"{prefix}{enemyId}/{enemyId}", out Sprite byFolderId))
                    {
                        return byFolderId;
                    }
                }
            }

            return GetFallbackEnemySprite();
        }

        private static RuntimeAnimatorController ResolveEnemyAnimatorController(EnemyConfig config)
        {
            if (config == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(config.RuntimeAnimatorControllerPath))
            {
                RuntimeAnimatorController byConfig =
                    Resources.Load<RuntimeAnimatorController>(config.RuntimeAnimatorControllerPath.Trim());
                if (byConfig != null)
                {
                    return byConfig;
                }
            }

            if (string.IsNullOrWhiteSpace(config.EnemyId))
            {
                return null;
            }

            string enemyId = config.EnemyId.Trim();
            string conventionalPath = $"Animations/Enemies/{enemyId}/{enemyId}";
            return Resources.Load<RuntimeAnimatorController>(conventionalPath);
        }

        private static void LogEnemyAnimatorResolutionOnce(EnemyConfig config, RuntimeAnimatorController controller)
        {
            if (config == null)
            {
                return;
            }

            string enemyId = string.IsNullOrWhiteSpace(config.EnemyId) ? "(empty)" : config.EnemyId.Trim();
            string key = $"{enemyId}|{(controller != null ? "ok" : "none")}";
            if (!EnemyAnimatorLogCache.Add(key))
            {
                return;
            }

            string configuredPath = string.IsNullOrWhiteSpace(config.RuntimeAnimatorControllerPath)
                ? "(empty)"
                : config.RuntimeAnimatorControllerPath.Trim();
            string loadedName = controller != null ? controller.name : "(null)";
            Debug.Log(
                $"[SpawnManager] Enemy animator resolve. enemyId={enemyId}, configuredPath={configuredPath}, loaded={loadedName}");
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

            Array.Sort(multiple, CompareSpriteByName);
            sprite = multiple[0];
            return sprite != null;
        }

        private static bool TryLoadSprites(string resourcePath, out Sprite[] sprites)
        {
            sprites = Array.Empty<Sprite>();
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return false;
            }

            Sprite[] multiple = Resources.LoadAll<Sprite>(resourcePath);
            if (multiple != null && multiple.Length > 0)
            {
                Array.Sort(multiple, CompareSpriteByName);
                sprites = multiple;
                return true;
            }

            if (TryLoadSprite(resourcePath, out Sprite single) && single != null)
            {
                sprites = new[] { single };
                return true;
            }

            return false;
        }

        private static void ResolveEnemyAnimationClips(
            EnemyConfig config,
            Sprite resolvedSprite,
            out Sprite[] idleFrames,
            out Sprite[] moveFrames,
            out Sprite[] attackFrames,
            out Sprite[] dieFrames)
        {
            idleFrames = Array.Empty<Sprite>();
            moveFrames = Array.Empty<Sprite>();
            attackFrames = Array.Empty<Sprite>();
            dieFrames = Array.Empty<Sprite>();

            string runtimePath = string.Empty;
            string enemyId = config != null ? (config.EnemyId ?? string.Empty).Trim() : string.Empty;

            bool parsedFromMultiSheet = TrySplitEnemyActionFramesFromSingleSheet(
                runtimePath,
                out Sprite[] idleBySheet,
                out Sprite[] moveBySheet,
                out Sprite[] attackBySheet,
                out Sprite[] dieBySheet);

            if (parsedFromMultiSheet)
            {
                idleFrames = idleBySheet;
                moveFrames = moveBySheet;
                attackFrames = attackBySheet;
                dieFrames = dieBySheet;
            }
            else if (TryLoadSprites(runtimePath, out Sprite[] byPath))
            {
                moveFrames = byPath;
            }

            if ((moveFrames == null || moveFrames.Length <= 0) && !string.IsNullOrWhiteSpace(enemyId))
            {
                for (int i = 0; i < EnemySpriteResourcePrefixes.Length; i++)
                {
                    string prefix = EnemySpriteResourcePrefixes[i];
                    if (TryLoadSprites(prefix + enemyId, out Sprite[] byId))
                    {
                        moveFrames = byId;
                        break;
                    }

                    if (TryLoadSprites($"{prefix}{enemyId}/{enemyId}", out Sprite[] byFolderId))
                    {
                        moveFrames = byFolderId;
                        break;
                    }
                }
            }

            if (!parsedFromMultiSheet)
            {
                idleFrames = TryResolveEnemyActionFrames("idle", runtimePath, enemyId, moveFrames);
                attackFrames = TryResolveEnemyActionFrames("attack", runtimePath, enemyId, moveFrames);
                dieFrames = TryResolveEnemyActionFrames("die", runtimePath, enemyId, attackFrames);
            }

            if (moveFrames == null || moveFrames.Length <= 0)
            {
                moveFrames = resolvedSprite != null
                    ? new[] { resolvedSprite }
                    : new[] { GetFallbackEnemySprite() };
            }

            if (idleFrames == null || idleFrames.Length <= 0)
            {
                idleFrames = moveFrames;
            }

            if (attackFrames == null || attackFrames.Length <= 0)
            {
                attackFrames = moveFrames;
            }

            if (dieFrames == null || dieFrames.Length <= 0)
            {
                dieFrames = attackFrames;
            }
        }

        private static bool TrySplitEnemyActionFramesFromSingleSheet(
            string resourcePath,
            out Sprite[] idleFrames,
            out Sprite[] moveFrames,
            out Sprite[] attackFrames,
            out Sprite[] dieFrames)
        {
            idleFrames = Array.Empty<Sprite>();
            moveFrames = Array.Empty<Sprite>();
            attackFrames = Array.Empty<Sprite>();
            dieFrames = Array.Empty<Sprite>();

            if (!TryLoadSprites(resourcePath, out Sprite[] allSprites) || allSprites.Length <= 0)
            {
                return false;
            }

            idleFrames = FilterSpritesByActionAliases(allSprites, "idle");
            moveFrames = FilterSpritesByActionAliases(allSprites, "walk", "move", "run");
            attackFrames = FilterSpritesByActionAliases(allSprites, "attack", "atk");
            dieFrames = FilterSpritesByActionAliases(allSprites, "die", "death", "dead");

            // 단일 액션 파일(idle_*, attack_*)은 기존 후보 탐색 경로를 타야 하므로
            // 액션 토큰이 2개 이상 존재할 때만 "multi 시트 분해"로 인정한다.
            int actionGroupsDetected = 0;
            if (idleFrames.Length > 0) actionGroupsDetected++;
            if (moveFrames.Length > 0) actionGroupsDetected++;
            if (attackFrames.Length > 0) actionGroupsDetected++;
            if (dieFrames.Length > 0) actionGroupsDetected++;
            return actionGroupsDetected >= 2;
        }

        private static Sprite[] TryResolveEnemyActionFrames(string action, string runtimePath, string enemyId, Sprite[] fallback)
        {
            var candidates = BuildEnemyActionCandidates(action, runtimePath, enemyId);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (TryLoadSprites(candidates[i], out Sprite[] loaded) && loaded.Length > 0)
                {
                    return loaded;
                }
            }

            return fallback ?? Array.Empty<Sprite>();
        }

        private static Sprite[] FilterSpritesByActionAliases(Sprite[] sprites, params string[] aliases)
        {
            if (sprites == null || sprites.Length <= 0 || aliases == null || aliases.Length <= 0)
            {
                return Array.Empty<Sprite>();
            }

            var filtered = new List<Sprite>(sprites.Length);
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite == null || !SpriteNameContainsAnyAliasToken(sprite.name, aliases))
                {
                    continue;
                }

                filtered.Add(sprite);
            }

            if (filtered.Count <= 0)
            {
                return Array.Empty<Sprite>();
            }

            filtered.Sort(CompareSpriteByActionFrame);
            return filtered.ToArray();
        }

        private static bool SpriteNameContainsAnyAliasToken(string name, string[] aliases)
        {
            if (string.IsNullOrWhiteSpace(name) || aliases == null || aliases.Length <= 0)
            {
                return false;
            }

            string[] tokens = name.Split('_');
            for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
            {
                string token = tokens[tokenIndex];
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                for (int aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
                {
                    if (string.Equals(token, aliases[aliasIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int CompareSpriteByActionFrame(Sprite a, Sprite b)
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

            int aIndex = ParseTrailingFrameIndex(a.name);
            int bIndex = ParseTrailingFrameIndex(b.name);
            if (aIndex != bIndex)
            {
                return aIndex.CompareTo(bIndex);
            }

            return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
        }

        private static int ParseTrailingFrameIndex(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return int.MaxValue;
            }

            string[] tokens = name.Split('_');
            if (tokens.Length <= 0)
            {
                return int.MaxValue;
            }

            string last = tokens[tokens.Length - 1];
            return int.TryParse(last, out int parsed) ? parsed : int.MaxValue;
        }

        private static List<string> BuildEnemyActionCandidates(string action, string runtimePath, string enemyId)
        {
            var candidates = new List<string>(20);
            if (!string.IsNullOrWhiteSpace(runtimePath))
            {
                AddUniquePath(candidates, ReplaceActionToken(runtimePath, action));
                AddUniquePath(candidates, AppendActionSegment(runtimePath, action));
                AddUniquePath(candidates, $"{runtimePath}_{action}");
                AddUniquePath(candidates, $"{action}_{runtimePath}");
            }

            if (!string.IsNullOrWhiteSpace(enemyId))
            {
                for (int i = 0; i < EnemySpriteResourcePrefixes.Length; i++)
                {
                    string prefix = EnemySpriteResourcePrefixes[i];
                    AddUniquePath(candidates, $"{prefix}{enemyId}/{action}");
                    AddUniquePath(candidates, $"{prefix}{action}_{enemyId}");
                    AddUniquePath(candidates, $"{prefix}{enemyId}_{action}");
                    AddUniquePath(candidates, $"{prefix}{action}_{enemyId}_Processed");
                }
            }

            return candidates;
        }

        private static string AppendActionSegment(string resourcePath, string action)
        {
            if (string.IsNullOrWhiteSpace(resourcePath) || string.IsNullOrWhiteSpace(action))
            {
                return string.Empty;
            }

            int slash = resourcePath.LastIndexOf('/');
            if (slash < 0)
            {
                return $"{action}/{resourcePath}";
            }

            string dir = resourcePath.Substring(0, slash);
            return $"{dir}/{action}";
        }

        private static string ReplaceActionToken(string resourcePath, string action)
        {
            if (string.IsNullOrWhiteSpace(resourcePath) || string.IsNullOrWhiteSpace(action))
            {
                return resourcePath ?? string.Empty;
            }

            string[] prefixedTokens =
            {
                "idle_",
                "walk_",
                "run_",
                "move_",
                "attack_",
                "die_",
                "death_",
                "dead_"
            };

            for (int i = 0; i < prefixedTokens.Length; i++)
            {
                string token = prefixedTokens[i];
                int index = resourcePath.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (index >= 0 && (index == 0 || resourcePath[index - 1] == '/'))
                {
                    return $"{resourcePath.Substring(0, index)}{action}_{resourcePath.Substring(index + token.Length)}";
                }
            }

            string[] segments = resourcePath.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                if (IsActionSegment(segments[i]))
                {
                    segments[i] = action;
                    return string.Join("/", segments);
                }
            }

            return resourcePath;
        }

        private static bool IsActionSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return false;
            }

            return string.Equals(segment, "idle", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(segment, "walk", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(segment, "run", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(segment, "move", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(segment, "attack", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(segment, "die", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(segment, "death", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(segment, "dead", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddUniquePath(List<string> candidates, string path)
        {
            if (candidates == null || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (string.Equals(candidates[i], path, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            candidates.Add(path);
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

            if (TryParseFrameIndices(a.name, out int aRow, out int aCol) &&
                TryParseFrameIndices(b.name, out int bRow, out int bCol))
            {
                int rowComp = aRow.CompareTo(bRow);
                if (rowComp != 0)
                {
                    return rowComp;
                }

                int colComp = aCol.CompareTo(bCol);
                if (colComp != 0)
                {
                    return colComp;
                }
            }

            return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseFrameIndices(string name, out int row, out int col)
        {
            row = int.MaxValue;
            col = int.MaxValue;
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string[] tokens = name.Split('_');
            if (tokens.Length < 2)
            {
                return false;
            }

            bool hasRow = int.TryParse(tokens[tokens.Length - 2], out row);
            bool hasCol = int.TryParse(tokens[tokens.Length - 1], out col);
            return hasRow && hasCol;
        }

        private static bool IsFallbackEnemySprite(Sprite sprite)
        {
            return sprite == null || sprite == GetFallbackEnemySprite();
        }

        private static Color ResolveEnemyColor(EnemyConfig config, bool fallbackSprite)
        {
            if (config == null)
            {
                return new Color(0.75f, 0.85f, 1f, 1f);
            }

            if (HasExplicitTint(config.Tint))
            {
                return config.Tint;
            }

            if (!fallbackSprite)
            {
                return Color.white;
            }

            if (config.IsBossUnit)
            {
                return new Color(0.75f, 0.2f, 0.2f, 1f);
            }

            int hash = Mathf.Abs((config.EnemyId ?? "Enemy").GetHashCode());
            float h = (hash % 360) / 360f;
            Color c = Color.HSVToRGB(h, 0.45f, 0.95f);
            c.a = 1f;
            return c;
        }

        private static bool HasExplicitTint(Color tint)
        {
            const float epsilon = 0.0001f;
            return tint.a > 0f
                && (Mathf.Abs(tint.r - 1f) > epsilon
                    || Mathf.Abs(tint.g - 1f) > epsilon
                    || Mathf.Abs(tint.b - 1f) > epsilon
                    || Mathf.Abs(tint.a - 1f) > epsilon);
        }

        private static float ResolveEnemyScale(EnemyConfig config)
        {
            if (config == null)
            {
                return 0.6f;
            }

            return Mathf.Clamp(config.VisualScale, 0.2f, 2f);
        }

        private static int ResolveEnemyLayer(EnemyConfig config)
        {
            string preferredLayer = (config != null && config.IsFlyingUnit)
                ? "FlyingEnemy"
                : "GroundEnemy";
            int layer = LayerMask.NameToLayer(preferredLayer);
            return layer >= 0 ? layer : 0;
        }

    }

}
