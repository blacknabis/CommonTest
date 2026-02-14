using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// 스테이지 노드 간 연결 경로를 LineRenderer로 렌더링하고 상태별 스타일/점선 애니메이션을 처리합니다.
    /// </summary>
    public class StagePathRenderer : MonoBehaviour
    {
        private struct PathKey
        {
            public int From;
            public int To;

            public PathKey(int from, int to)
            {
                From = from;
                To = to;
            }
        }

        [Header("Path Root")]
        [SerializeField] private Transform lineRoot;
        [SerializeField] private LineRenderer linePrefab;

        [Header("Style")]
        [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 0.9f);
        [SerializeField] private Color unlockedColor = new Color(0.55f, 0.85f, 1f, 1f);
        [SerializeField] private Color completedColor = new Color(1f, 0.95f, 0.35f, 1f);
        [SerializeField] private float lockedWidth = 0.11f;
        [SerializeField] private float unlockedWidth = 0.15f;
        [SerializeField] private float completedWidth = 0.2f;

        [Header("Dash Animation")]
        [SerializeField] private bool animateDashOnCompleted = true;
        [SerializeField] private float dashSpeed = 1.2f;
        [SerializeField] private string textureOffsetProperty = "_MainTex";

        [Header("Runtime")]
        [SerializeField] private float refreshInterval = 0.2f;

        private readonly Dictionary<PathKey, LineRenderer> _lineByPath = new Dictionary<PathKey, LineRenderer>();
        private readonly List<LineRenderer> _completedLines = new List<LineRenderer>();

        private Coroutine _refreshRoutine;
        private float _dashOffset;

        private void Start()
        {
            if (lineRoot == null)
            {
                lineRoot = transform;
            }

            BuildPaths();

            if (_refreshRoutine != null)
            {
                StopCoroutine(_refreshRoutine);
            }

            _refreshRoutine = StartCoroutine(CoRefreshStyles());
        }

        private void Update()
        {
            if (!animateDashOnCompleted || _completedLines.Count == 0)
            {
                return;
            }

            _dashOffset += Time.unscaledDeltaTime * dashSpeed;

            for (int i = 0; i < _completedLines.Count; i++)
            {
                LineRenderer line = _completedLines[i];
                if (line == null || line.material == null)
                {
                    continue;
                }

                Vector2 offset = new Vector2(_dashOffset, 0f);
                line.material.SetTextureOffset(textureOffsetProperty, offset);
            }
        }

        public void BuildPaths()
        {
            ClearLines();

            if (WorldMapManager.Instance == null || WorldMapManager.Instance.CurrentStageConfig == null)
            {
                return;
            }

            StageConfig config = WorldMapManager.Instance.CurrentStageConfig;
            List<StageData> stages = config.Stages;
            if (stages == null)
            {
                return;
            }

            for (int i = 0; i < stages.Count; i++)
            {
                StageData from = stages[i];
                if (from.NextStageIds == null)
                {
                    continue;
                }

                GameObject fromNode = WorldMapManager.Instance.GetNode(from.StageId);
                if (fromNode == null)
                {
                    continue;
                }

                for (int j = 0; j < from.NextStageIds.Count; j++)
                {
                    int toStageId = from.NextStageIds[j];
                    GameObject toNode = WorldMapManager.Instance.GetNode(toStageId);
                    if (toNode == null)
                    {
                        continue;
                    }

                    LineRenderer line = CreateLineRenderer(from.StageId, toStageId);
                    line.positionCount = 2;
                    line.useWorldSpace = true;
                    line.SetPosition(0, fromNode.transform.position);
                    line.SetPosition(1, toNode.transform.position);
                }
            }

            RefreshStyles();
        }

        public void RefreshStyles()
        {
            _completedLines.Clear();

            if (WorldMapManager.Instance == null || WorldMapManager.Instance.CurrentStageConfig == null)
            {
                return;
            }

            List<StageData> stages = WorldMapManager.Instance.CurrentStageConfig.Stages;
            if (stages == null)
            {
                return;
            }

            Dictionary<int, StageData> map = new Dictionary<int, StageData>();
            for (int i = 0; i < stages.Count; i++)
            {
                map[stages[i].StageId] = stages[i];
            }

            foreach (var pair in _lineByPath)
            {
                PathKey key = pair.Key;
                LineRenderer line = pair.Value;
                if (line == null)
                {
                    continue;
                }

                bool hasFrom = map.TryGetValue(key.From, out StageData from);
                bool hasTo = map.TryGetValue(key.To, out StageData to);
                if (!hasFrom || !hasTo)
                {
                    continue;
                }

                bool fromCleared = from.BestTime > 0f;
                bool toUnlocked = to.IsUnlocked;

                if (fromCleared)
                {
                    ApplyLineStyle(line, completedColor, completedWidth);
                    _completedLines.Add(line);
                }
                else if (toUnlocked)
                {
                    ApplyLineStyle(line, unlockedColor, unlockedWidth);
                }
                else
                {
                    ApplyLineStyle(line, lockedColor, lockedWidth);
                }
            }
        }

        private IEnumerator CoRefreshStyles()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(Mathf.Max(0.05f, refreshInterval));

            while (true)
            {
                RefreshStyles();
                yield return wait;
            }
        }

        private LineRenderer CreateLineRenderer(int fromStageId, int toStageId)
        {
            LineRenderer line;
            if (linePrefab != null)
            {
                line = Instantiate(linePrefab, lineRoot);
            }
            else
            {
                GameObject go = new GameObject($"Path_{fromStageId}_{toStageId}");
                go.transform.SetParent(lineRoot, false);
                line = go.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.textureMode = LineTextureMode.Tile;
                line.numCapVertices = 4;
                line.sortingOrder = -1;
            }

            line.name = $"Path_{fromStageId}_{toStageId}";
            if (line.material != null)
            {
                line.material = new Material(line.material);
            }

            PathKey key = new PathKey(fromStageId, toStageId);
            _lineByPath[key] = line;
            return line;
        }

        private void ApplyLineStyle(LineRenderer line, Color color, float width)
        {
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
        }

        private void ClearLines()
        {
            foreach (var pair in _lineByPath)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }

            _lineByPath.Clear();
            _completedLines.Clear();
        }
    }
}
