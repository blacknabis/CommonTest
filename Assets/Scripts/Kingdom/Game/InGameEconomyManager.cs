using System;
using UnityEngine;

namespace Kingdom.Game
{
    /// <summary>
    /// 전투 중 골드/생명력 수치와 수급을 담당.
    /// </summary>
    public class InGameEconomyManager : MonoBehaviour
    {
        private int _gold;
        private int _lives;

        private SpawnManager _spawnManager;
        private GameStateController _stateController;

        public int Gold => _gold;
        public int Lives => _lives;

        public event Action<int, int> ResourceChanged;

        public void Configure(int initialGold, int initialLives, SpawnManager spawnManager, GameStateController stateController)
        {
            _gold = Mathf.Max(0, initialGold);
            _lives = Mathf.Max(0, initialLives);
            _spawnManager = spawnManager;
            _stateController = stateController;

            RebindSpawnEvents();
            NotifyChanged();
        }

        public bool TrySpendGold(int amount)
        {
            int spend = Mathf.Max(0, amount);
            if (_gold < spend)
            {
                return false;
            }

            _gold -= spend;
            NotifyChanged();
            return true;
        }

        public void AddGold(int amount)
        {
            _gold = Mathf.Max(0, _gold + Mathf.Max(0, amount));
            NotifyChanged();
        }

        private void OnDisable()
        {
            UnbindSpawnEvents();
        }

        private void RebindSpawnEvents()
        {
            UnbindSpawnEvents();
            if (_spawnManager == null)
            {
                return;
            }

            _spawnManager.EnemyKilled += HandleEnemyKilled;
            _spawnManager.EnemyReachedGoal += HandleEnemyReachedGoal;
        }

        private void UnbindSpawnEvents()
        {
            if (_spawnManager == null)
            {
                return;
            }

            _spawnManager.EnemyKilled -= HandleEnemyKilled;
            _spawnManager.EnemyReachedGoal -= HandleEnemyReachedGoal;
        }

        private void HandleEnemyKilled(EnemyRuntime enemy, EnemyConfig config)
        {
            int bounty = config != null ? Mathf.Max(0, config.GoldBounty) : 1;
            AddGold(bounty);
        }

        private void HandleEnemyReachedGoal(EnemyRuntime enemy, EnemyConfig config)
        {
            int damage = config != null ? Mathf.Max(1, config.DamageToBase) : 1;
            _lives = Mathf.Max(0, _lives - damage);
            NotifyChanged();

            if (_lives <= 0 && _stateController != null)
            {
                _stateController.ForceResult();
            }
        }

        private void NotifyChanged()
        {
            ResourceChanged?.Invoke(_lives, _gold);
        }
    }
}
