using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kingdom.WorldMap
{
    [Serializable]
    public class StageNodeClickedEvent : UnityEvent<int>
    {
    }

    /// <summary>
    /// 월드맵의 스테이지 노드를 생성/관리하고 선택 이벤트를 중계합니다.
    /// </summary>
    public class WorldMapManager : MonoBehaviour
    {
        private static WorldMapManager _instance;
        public static WorldMapManager Instance => _instance;

        [Header("World Config")]
        [SerializeField] private StageConfig stageConfig;
        [SerializeField] private string stageConfigResourcePath = "Data/StageConfigs/World1_StageConfig";
        [SerializeField] private int currentWorldId = 1;

        [Header("Node Spawn")]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private Transform nodeRoot;

        [Header("Events")]
        [SerializeField] private StageNodeClickedEvent onNodeClickedEvent = new StageNodeClickedEvent();

        public event UnityAction<int> StageNodeClicked;

        private readonly Dictionary<int, GameObject> nodeInstances = new Dictionary<int, GameObject>();

        public int CurrentWorldId => currentWorldId;
        public StageConfig CurrentStageConfig => stageConfig;
        public IReadOnlyDictionary<int, GameObject> NodeInstances => nodeInstances;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (stageConfig == null)
            {
                stageConfig = Resources.Load<StageConfig>(stageConfigResourcePath);
            }

            if (stageConfig != null)
            {
                currentWorldId = stageConfig.WorldId;
            }
        }

        private void Start()
        {
            BuildWorldNodes();
        }

        public void SetWorld(int worldId, StageConfig config = null)
        {
            currentWorldId = worldId;
            stageConfig = config != null ? config : Resources.Load<StageConfig>($"Data/StageConfigs/World{worldId}_StageConfig");
            BuildWorldNodes();
        }

        public void BuildWorldNodes()
        {
            ClearNodes();

            if (stageConfig == null || stageConfig.Stages == null)
            {
                Debug.LogWarning("[WorldMapManager] StageConfig가 비어 있어 노드 생성이 스킵됩니다.");
                return;
            }

            if (nodePrefab == null || nodeRoot == null)
            {
                Debug.LogWarning("[WorldMapManager] nodePrefab 또는 nodeRoot가 없어 노드 생성이 스킵됩니다.");
                return;
            }

            for (int i = 0; i < stageConfig.Stages.Count; i++)
            {
                StageData stageData = stageConfig.Stages[i];
                GameObject node = Instantiate(nodePrefab, nodeRoot);
                node.name = $"StageNode_{stageData.StageId}";
                node.transform.localPosition = new Vector3(stageData.Position.x, stageData.Position.y, 0f);

                RegisterNode(stageData.StageId, node);
            }
        }

        public void RegisterNode(int stageId, GameObject node)
        {
            if (node == null)
            {
                return;
            }

            nodeInstances[stageId] = node;

            Button button = node.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnNodeClicked(stageId));
            }
        }

        public GameObject GetNode(int stageId)
        {
            nodeInstances.TryGetValue(stageId, out GameObject node);
            return node;
        }

        public void OnNodeClicked(int stageId)
        {
            Debug.Log($"[WorldMapManager] Node clicked: {stageId}");
            StageNodeClicked?.Invoke(stageId);
            onNodeClickedEvent?.Invoke(stageId);
        }

        private void ClearNodes()
        {
            foreach (var pair in nodeInstances)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }

            nodeInstances.Clear();
        }
    }
}
