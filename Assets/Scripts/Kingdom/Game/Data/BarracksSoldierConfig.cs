using UnityEngine;

namespace Kingdom.Game
{
    [CreateAssetMenu(fileName = "BarracksSoldierConfig", menuName = "Kingdom/Game/Barracks Soldier Config")]
    public class BarracksSoldierConfig : ScriptableObject
    {
        [Header("Identity")]
        public string SoldierId = "BarracksSoldier_Default";
        public string DisplayName = "Barracks Soldier";

        [Header("Visual")]
        [Tooltip("Resources path without extension, e.g. Sprites/Barracks/Soldiers/BarracksSoldier_Default")]
        public string RuntimeSpriteResourcePath;

        [Tooltip("Resources path without extension, e.g. Animations/Barracks/Barracks_Soldier/Barracks_Soldier")]
        public string RuntimeAnimatorControllerPath;
    }
}
