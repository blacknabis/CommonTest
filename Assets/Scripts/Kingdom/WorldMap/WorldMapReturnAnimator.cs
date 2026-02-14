using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// 게임 종료 후 월드맵으로 복귀할 때 카메라/노드 연출을 재생합니다.
    /// </summary>
    public class WorldMapReturnAnimator : MonoBehaviour
    {
        private struct PendingReturnData
        {
            public int StageId;
            public bool IsCleared;
            public float ClearTimeSeconds;
            public StageDifficulty Difficulty;
        }

        [Header("Targets")]
        [SerializeField] private Camera targetCamera;

        [Header("Flow")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private float cameraMoveDuration = 0.75f;
        [SerializeField] private float highlightDuration = 0.9f;
        [SerializeField] private float unlockInterval = 0.18f;

        [Header("Node Highlight")]
        [SerializeField] private Vector3 highlightScale = new Vector3(1.2f, 1.2f, 1f);
        [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.35f, 1f);

        private static bool s_hasPendingData;
        private static PendingReturnData s_pendingData;

        private Coroutine _playRoutine;

        public static void SetPendingReturnData(int stageId, bool isCleared, float clearTimeSeconds, StageDifficulty difficulty)
        {
            s_pendingData = new PendingReturnData
            {
                StageId = stageId,
                IsCleared = isCleared,
                ClearTimeSeconds = clearTimeSeconds,
                Difficulty = difficulty,
            };

            s_hasPendingData = true;
        }

        private void OnEnable()
        {
            if (!playOnEnable || !s_hasPendingData)
            {
                return;
            }

            PlayPending();
        }

        public void PlayPending()
        {
            if (!s_hasPendingData)
            {
                return;
            }

            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
            }

            _playRoutine = StartCoroutine(CoPlaySequence(s_pendingData));
        }

        private IEnumerator CoPlaySequence(PendingReturnData data)
        {
            if (WorldMapManager.Instance == null)
            {
                yield break;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            GameObject clearedNode = WorldMapManager.Instance.GetNode(data.StageId);
            if (clearedNode == null)
            {
                s_hasPendingData = false;
                yield break;
            }

            yield return CoMoveCameraToNode(clearedNode.transform.position);

            if (data.IsCleared)
            {
                yield return CoHighlightNode(clearedNode.transform);
                yield return CoUnlockNextNodes(data.StageId);
            }

            s_hasPendingData = false;
            _playRoutine = null;
        }

        private IEnumerator CoMoveCameraToNode(Vector3 worldPosition)
        {
            if (targetCamera == null)
            {
                yield break;
            }

            Vector3 from = targetCamera.transform.position;
            Vector3 to = new Vector3(worldPosition.x, worldPosition.y, from.z);

            float elapsed = 0f;
            while (elapsed < cameraMoveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, cameraMoveDuration));
                t = 1f - Mathf.Pow(1f - t, 3f);
                targetCamera.transform.position = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            targetCamera.transform.position = to;
        }

        private IEnumerator CoHighlightNode(Transform nodeTransform)
        {
            Vector3 baseScale = nodeTransform.localScale;

            Image image = nodeTransform.GetComponent<Image>();
            SpriteRenderer spriteRenderer = nodeTransform.GetComponent<SpriteRenderer>();

            Color baseColor = Color.white;
            if (image != null)
            {
                baseColor = image.color;
            }
            else if (spriteRenderer != null)
            {
                baseColor = spriteRenderer.color;
            }

            float elapsed = 0f;
            while (elapsed < highlightDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, highlightDuration));
                float pulse = Mathf.Sin(t * Mathf.PI * 3f) * 0.5f + 0.5f;

                nodeTransform.localScale = Vector3.Lerp(baseScale, Vector3.Scale(baseScale, highlightScale), pulse);

                Color lerped = Color.Lerp(baseColor, highlightColor, pulse);
                if (image != null)
                {
                    image.color = lerped;
                }
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = lerped;
                }

                yield return null;
            }

            nodeTransform.localScale = baseScale;
            if (image != null)
            {
                image.color = baseColor;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = baseColor;
            }
        }

        private IEnumerator CoUnlockNextNodes(int clearedStageId)
        {
            StageConfig config = WorldMapManager.Instance.CurrentStageConfig;
            if (config == null || config.Stages == null)
            {
                yield break;
            }

            for (int i = 0; i < config.Stages.Count; i++)
            {
                StageData data = config.Stages[i];
                if (data.StageId != clearedStageId || data.NextStageIds == null)
                {
                    continue;
                }

                for (int j = 0; j < data.NextStageIds.Count; j++)
                {
                    int nextStageId = data.NextStageIds[j];
                    GameObject nextNode = WorldMapManager.Instance.GetNode(nextStageId);
                    if (nextNode == null)
                    {
                        continue;
                    }

                    StageNode stageNode = nextNode.GetComponent<StageNode>();
                    if (stageNode != null && stageNode.CurrentState == StageNodeState.Locked)
                    {
                        stageNode.SetState(StageNodeState.Unlocked);
                    }

                    yield return CoUnlockPulse(nextNode.transform);
                    yield return new WaitForSecondsRealtime(unlockInterval);
                }

                yield break;
            }
        }

        private IEnumerator CoUnlockPulse(Transform nodeTransform)
        {
            Vector3 baseScale = nodeTransform.localScale;
            Vector3 targetScale = baseScale * 1.15f;

            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                nodeTransform.localScale = Vector3.Lerp(baseScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                nodeTransform.localScale = Vector3.Lerp(targetScale, baseScale, t);
                yield return null;
            }

            nodeTransform.localScale = baseScale;
        }
    }
}
