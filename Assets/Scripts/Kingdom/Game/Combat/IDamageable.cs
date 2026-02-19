namespace Kingdom.Game
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(float amount, DamageType damageType = DamageType.Physical, bool halfPhysicalArmorPenetration = false);
    }
}
