using UnityEngine;

namespace Kingdom.Game
{
    [CreateAssetMenu(fileName = "HeroConfig", menuName = "Kingdom/Game/Hero Config")]
    public class HeroConfig : ScriptableObject
    {
        public string HeroId = "DefaultHero";
        public string DisplayName = "Hero";
        public float MaxHp = 500f;
        public float MoveSpeed = 3.2f;
        public float AttackDamage = 30f;
        public float AttackCooldown = 0.8f;
        public float AttackRange = 1.8f;
    }
}
