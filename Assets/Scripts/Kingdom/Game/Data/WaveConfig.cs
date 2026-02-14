using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.Game
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Kingdom/Game/Wave Config")]
    public class WaveConfig : ScriptableObject
    {
        [Serializable]
        public struct SpawnEntry
        {
            public EnemyConfig Enemy;
            public int Count;
            public float SpawnInterval;
            public int PathId;
            public float SpawnDelay;
        }

        [Serializable]
        public struct WaveData
        {
            public int WaveIndex;
            public List<SpawnEntry> SpawnEntries;
            public int BonusGoldOnEarlyCall;
            public bool IsBossWave;
        }

        public int StageId;
        public int InitialGold = 100;
        public int InitialLives = 20;
        public int[] StarThresholds = { 20, 15 };
        public List<WaveData> Waves = new();
    }
}
