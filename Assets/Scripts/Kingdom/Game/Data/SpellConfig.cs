using UnityEngine;

namespace Kingdom.Game
{
    [CreateAssetMenu(fileName = "SpellConfig", menuName = "Kingdom/Game/Spell Config")]
    public class SpellConfig : ScriptableObject
    {
        public string SpellId = "spell";
        public string DisplayName = "Spell";
        [Min(0f)] public float CooldownSeconds = 20f;
        [Min(0f)] public float EarlyCallCooldownReductionSeconds = 4f;
    }
}
