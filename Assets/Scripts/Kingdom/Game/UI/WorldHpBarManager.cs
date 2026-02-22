using System.Collections.Generic;
using Common.Extensions;
using UnityEngine;

namespace Kingdom.Game.UI
{
    /// <summary>
    /// Manages a pool of WorldHpBars and assigns them to active targets.
    /// </summary>
    public class WorldHpBarManager : MonoBehaviour
    {
        public static WorldHpBarManager Instance { get; private set; }

        [SerializeField] private GameObject _hpBarPrefab;
        [SerializeField] private int _initialPoolSize = 20;
        [SerializeField] private Transform _canvasRoot;

        private List<WorldHpBar> _pool = new List<WorldHpBar>();
        private Dictionary<ISelectableTarget, WorldHpBar> _activeBars = new Dictionary<ISelectableTarget, WorldHpBar>();

        private void Awake()
        {
            if (Instance.IsNotNull() && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreateNewBar();
            }
        }

        public void ConfigureRuntime(GameObject hpBarPrefab, Transform root = null, int initialPoolSize = 20)
        {
            if (hpBarPrefab.IsNotNull())
            {
                _hpBarPrefab = hpBarPrefab;
            }

            if (root.IsNotNull())
            {
                _canvasRoot = root;
            }

            _initialPoolSize = Mathf.Max(1, initialPoolSize);
            if (_hpBarPrefab.IsNull())
            {
                return;
            }

            int targetPoolCount = Mathf.Max(_initialPoolSize, 1);
            for (int i = _pool.Count; i < targetPoolCount; i++)
            {
                CreateNewBar();
            }
        }

        private WorldHpBar CreateNewBar()
        {
            if (_hpBarPrefab.IsNull()) return null;
            
            GameObject go = Instantiate(_hpBarPrefab, _canvasRoot.IsNotNull() ? _canvasRoot : transform);
            WorldHpBar bar = go.GetComponent<WorldHpBar>();
            if (bar.IsNull())
            {
                bar = go.AddComponent<WorldHpBar>();
                Debug.LogWarning("[WorldHpBarManager] WorldHpBar component was missing on prefab instance. Added at runtime.");
            }

            go.SetActive(false);
            _pool.Add(bar);
            return bar;
        }

        public void TrackTarget(ISelectableTarget target)
        {
            if (target.IsNull() || _activeBars.ContainsKey(target)) return;

            WorldHpBar bar = GetAvailableBar();
            if (bar.IsNotNull())
            {
                bar.gameObject.SetActive(true);
                bar.SetTarget(target);
                _activeBars.Add(target, bar);
            }
        }

        public void UntrackTarget(ISelectableTarget target)
        {
            if (_activeBars.TryGetValue(target, out WorldHpBar bar))
            {
                if (bar.IsNotNull())
                {
                    bar.SetTarget(null);
                    bar.gameObject.SetActive(false);
                }

                _activeBars.Remove(target);
            }
        }

        private WorldHpBar GetAvailableBar()
        {
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                WorldHpBar bar = _pool[i];
                if (bar.IsNull())
                {
                    _pool.RemoveAt(i);
                    continue;
                }

                if (!bar.gameObject.activeSelf)
                {
                    return bar;
                }
            }

            return CreateNewBar();
        }

        private void LateUpdate()
        {
            // 죽은 타켓들 정리
            List<ISelectableTarget> toRemove = null;
            foreach (var kvp in _activeBars)
            {
                if (!kvp.Key.IsAlive)
                {
                    if (toRemove == null) toRemove = new List<ISelectableTarget>();
                    toRemove.Add(kvp.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (var target in toRemove)
                {
                    UntrackTarget(target);
                }
            }
        }
    }
}
