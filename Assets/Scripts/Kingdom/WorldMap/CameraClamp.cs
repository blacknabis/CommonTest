using UnityEngine;

namespace Kingdom.WorldMap
{
    /// <summary>
    /// 월드맵 카메라의 이동 범위를 경계값으로 제한합니다.
    /// </summary>
    public class CameraClamp : MonoBehaviour
    {
        [Header("Bounds")]
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float minY = -5f;
        [SerializeField] private float maxY = 5f;

        private void LateUpdate()
        {
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
            transform.position = position;
        }

        public void SetBounds(float newMinX, float newMaxX, float newMinY, float newMaxY)
        {
            minX = newMinX;
            maxX = newMaxX;
            minY = newMinY;
            maxY = newMaxY;
        }
    }
}
