using System.Collections.Generic;
using Kingdom.WorldMap;
using UnityEditor;
using UnityEngine;

namespace Kingdom.Editor
{
    /// <summary>
    /// WorldMapView 프리팹에 UIStageNode를 자동 배치하는 도구입니다.
    /// </summary>
    public static class WorldMapStageNodeAutoLayoutTool
    {
        private const string WorldMapPrefabPath = "Assets/Resources/UI/WorldMapView.prefab";
        private const string StageNodePrefabPath = "Assets/Resources/UI/Components/WorldMap/UIStageNode.prefab";
        private const string StageConfigAssetPath = "Assets/Resources/Data/StageConfigs/World1_StageConfig.asset";

        [MenuItem("Kingdom/WorldMap/Setup Stage Nodes In WorldMapView")]
        public static void Setup()
        {
            GameObject worldMapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldMapPrefabPath);
            GameObject nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageNodePrefabPath);
            StageConfig stageConfig = AssetDatabase.LoadAssetAtPath<StageConfig>(StageConfigAssetPath);

            if (worldMapPrefab == null)
            {
                Debug.LogError($"[WorldMap] 프리팹을 찾을 수 없습니다: {WorldMapPrefabPath}");
                return;
            }

            if (nodePrefab == null)
            {
                Debug.LogError($"[WorldMap] UIStageNode 프리팹을 찾을 수 없습니다: {StageNodePrefabPath}");
                return;
            }

            List<int> stageIds = BuildStageIdList(stageConfig);
            if (stageIds.Count == 0)
            {
                stageIds.Add(1);
                stageIds.Add(2);
                stageIds.Add(3);
                stageIds.Add(4);
                stageIds.Add(5);
            }

            GameObject root = PrefabUtility.LoadPrefabContents(WorldMapPrefabPath);
            try
            {
                Transform stageRoot = EnsureStageRoot(root.transform);
                ClearChildren(stageRoot);

                for (int i = 0; i < stageIds.Count; i++)
                {
                    int stageId = stageIds[i];
                    GameObject instance = PrefabUtility.InstantiatePrefab(nodePrefab, stageRoot) as GameObject;
                    if (instance == null)
                    {
                        continue;
                    }

                    instance.name = $"StageNode_{stageId}";

                    RectTransform rect = instance.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchorMin = new Vector2(0.5f, 0.5f);
                        rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchoredPosition = GetLayoutPosition(stageId, i);
                    }

                    UIStageNode node = instance.GetComponent<UIStageNode>();
                    if (node != null)
                    {
                        SerializedObject so = new SerializedObject(node);
                        so.FindProperty("stageId").intValue = stageId;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, WorldMapPrefabPath);
                Debug.Log($"[WorldMap] Stage node 자동 배치 완료: {stageIds.Count}개");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static List<int> BuildStageIdList(StageConfig stageConfig)
        {
            var ids = new List<int>();
            if (stageConfig == null || stageConfig.Stages == null)
            {
                return ids;
            }

            for (int i = 0; i < stageConfig.Stages.Count; i++)
            {
                int id = stageConfig.Stages[i].StageId;
                if (id <= 0 || ids.Contains(id))
                {
                    continue;
                }

                ids.Add(id);
            }

            ids.Sort();
            return ids;
        }

        private static Transform EnsureStageRoot(Transform parent)
        {
            Transform existing = parent.Find("StageNodes");
            if (existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject("StageNodes", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            // 배경 위, HUD 아래 레이어로 정렬
            rect.SetSiblingIndex(2);
            return go.transform;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        private static Vector2 GetLayoutPosition(int stageId, int fallbackIndex)
        {
            // 현재 월드맵 UI 배경 기준 수동 튜닝 좌표
            switch (stageId)
            {
                case 1: return new Vector2(-80f, 30f);
                case 2: return new Vector2(160f, 140f);
                case 3: return new Vector2(20f, -20f);
                case 4: return new Vector2(190f, -120f);
                case 5: return new Vector2(-140f, -170f);
            }

            // 데이터가 늘어난 경우 간단한 곡선형 fallback 배치
            float x = -220f + fallbackIndex * 110f;
            float y = Mathf.Sin(fallbackIndex * 0.7f) * 120f;
            return new Vector2(x, y);
        }
    }
}
