using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdom.WorldMap
{
    public enum StageDifficulty
    {
        Casual,
        Normal,
        Veteran,
    }

    [Serializable]
    public struct StageData
    {
        public int StageId;
        public string StageName;
        public StageDifficulty Difficulty;
        public Vector2 Position;
        public List<int> NextStageIds;
        public List<float> StarRequirements;
        public bool IsBoss;
        public bool IsUnlocked;
        public float BestTime;
    }

    [CreateAssetMenu(fileName = "StageConfig", menuName = "Kingdom/Stage Config")]
    public class StageConfig : ScriptableObject
    {
        public int WorldId;
        public string WorldName;
        public List<StageData> Stages;
    }
}
