using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Game
{
    /// <summary>
    /// 웨이포인트 경로를 제공하는 최소 Path 매니저.
    /// </summary>
    public class PathManager : MonoBehaviour
    {
        [SerializeField] private List<Transform> defaultPathPoints = new();

        private readonly Dictionary<int, List<Vector3>> _cachedPaths = new();

        private void Awake()
        {
            BuildDefaultPath();
        }

        public bool TryGetPath(int pathId, out List<Vector3> path)
        {
            if (_cachedPaths.TryGetValue(pathId, out path) && path != null && path.Count > 0)
            {
                return true;
            }

            path = null;
            return false;
        }

        public void SetDefaultPathPoints(IReadOnlyList<Transform> points)
        {
            defaultPathPoints.Clear();
            if (points != null)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i] != null)
                    {
                        defaultPathPoints.Add(points[i]);
                    }
                }
            }

            BuildDefaultPath();
        }

        private void BuildDefaultPath()
        {
            _cachedPaths.Clear();

            if (defaultPathPoints == null || defaultPathPoints.Count == 0)
            {
                _cachedPaths[0] = new List<Vector3>
                {
                    new Vector3(-6f, 0f, 0f),
                    new Vector3(0f, 0f, 0f),
                    new Vector3(6f, 0f, 0f)
                };
                return;
            }

            var defaultPath = new List<Vector3>(defaultPathPoints.Count);
            for (int i = 0; i < defaultPathPoints.Count; i++)
            {
                if (defaultPathPoints[i] != null)
                {
                    defaultPath.Add(defaultPathPoints[i].position);
                }
            }

            if (defaultPath.Count > 0)
            {
                _cachedPaths[0] = defaultPath;
            }
        }
    }
}
