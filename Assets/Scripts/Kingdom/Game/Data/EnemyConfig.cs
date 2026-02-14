using UnityEngine;

namespace Kingdom.Game
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Kingdom/Game/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        public string EnemyId = "Enemy";
        public Sprite Sprite;
        public Color Tint = Color.white;
        [Range(0.2f, 2f)] public float VisualScale = 0.6f;
        public float HP = 100f;
        [Range(0f, 100f)] public float ArmorPhysical;
        [Range(0f, 100f)] public float ArmorMagic;
        public float MoveSpeed = 2.5f;
        public int GoldBounty = 5;
        public int DamageToBase = 1;
        public bool IsFlying;
        public bool IsBoss;
        public bool IsInstaKillImmune;
    }
}
