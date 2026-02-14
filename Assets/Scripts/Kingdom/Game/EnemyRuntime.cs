using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Game
{
    /// <summary>
    /// 테스트용 적 런타임 엔티티. 경로를 따라 이동하고 도착/사망 이벤트를 발행한다.
    /// </summary>
    public class EnemyRuntime : MonoBehaviour
    {
        private EnemyConfig _config;
        private List<Vector3> _path;
        private int _pathIndex;
        private float _hp;

        public event Action<EnemyRuntime> ReachedGoal;
        public event Action<EnemyRuntime> Killed;

        public void Initialize(EnemyConfig config, List<Vector3> path)
        {
            _config = config;
            _path = path;
            _pathIndex = 0;
            _hp = config != null ? Mathf.Max(1f, config.HP) : 1f;

            if (_path != null && _path.Count > 0)
            {
                transform.position = _path[0];
            }
        }

        private void Update()
        {
            if (_path == null || _path.Count == 0 || _pathIndex >= _path.Count)
            {
                return;
            }

            float speed = _config != null ? Mathf.Max(0.1f, _config.MoveSpeed) : 1f;
            Vector3 target = _path[_pathIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if ((transform.position - target).sqrMagnitude <= 0.0001f)
            {
                _pathIndex++;
                if (_pathIndex >= _path.Count)
                {
                    ReachedGoal?.Invoke(this);
                }
            }
        }

        public void ApplyDamage(float amount)
        {
            _hp -= amount;
            if (_hp <= 0f)
            {
                Killed?.Invoke(this);
            }
        }
    }
}
