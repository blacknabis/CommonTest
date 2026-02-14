using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Kingdom.WorldMap;

namespace Kingdom.Editor
{
    public static class WorldMapStageNodeValidationTool
    {
        private const string WorldMapPrefabPath = "Assets/Resources/UI/WorldMapView.prefab";
        private const string StageConfigPath = "Assets/Resources/Data/StageConfigs/World1_StageConfig.asset";

        [MenuItem("Kingdom/WorldMap/Validate Stage Node Binding")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldMapPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[WorldMap] Prefab not found: {WorldMapPrefabPath}");
                return;
            }

            UIStageNode[] nodes = prefab.GetComponentsInChildren<UIStageNode>(true);
            if (nodes == null || nodes.Length == 0)
            {
                Debug.LogWarning("[WorldMap] No UIStageNode found in WorldMapView prefab.");
            }

            var nodeIdMap = new Dictionary<int, UIStageNode>();
            for (int i = 0; i < nodes.Length; i++)
            {
                UIStageNode node = nodes[i];
                int stageId = node.StageId;
                if (stageId <= 0)
                {
                    Debug.LogError($"[WorldMap] Invalid StageId on node: {GetNodePath(node.transform)}", node);
                    continue;
                }

                if (nodeIdMap.TryGetValue(stageId, out UIStageNode duplicate))
                {
                    Debug.LogError(
                        $"[WorldMap] Duplicate StageId={stageId} in prefab. '{GetNodePath(duplicate.transform)}' and '{GetNodePath(node.transform)}'",
                        node);
                    continue;
                }

                nodeIdMap.Add(stageId, node);
            }

            StageConfig config = AssetDatabase.LoadAssetAtPath<StageConfig>(StageConfigPath);
            if (config == null || config.Stages == null)
            {
                Debug.LogWarning($"[WorldMap] StageConfig not found or empty: {StageConfigPath}");
                return;
            }

            var configIds = config.Stages.Select(stage => stage.StageId).ToHashSet();
            var nodeIds = nodeIdMap.Keys.ToHashSet();

            foreach (int configId in configIds)
            {
                if (!nodeIds.Contains(configId))
                {
                    Debug.LogWarning($"[WorldMap] StageConfig has StageId={configId}, but prefab node is missing.");
                }
            }

            foreach (int nodeId in nodeIds)
            {
                if (!configIds.Contains(nodeId))
                {
                    Debug.LogWarning($"[WorldMap] Prefab has StageId={nodeId}, but StageConfig data is missing.");
                }
            }

            Debug.Log($"[WorldMap] Validation completed. Nodes={nodeIds.Count}, StageData={configIds.Count}");
        }

        private static string GetNodePath(Transform target)
        {
            if (target == null)
            {
                return "<null>";
            }

            var stack = new Stack<string>();
            Transform cursor = target;
            while (cursor != null)
            {
                stack.Push(cursor.name);
                cursor = cursor.parent;
            }

            return string.Join("/", stack);
        }
    }
}
