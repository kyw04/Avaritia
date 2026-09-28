public interface IDamageable
{
    // attacker: who dealt it, if known (null for bullets/effects) — used by on-hit/reflect abilities.
    void TakeDamage(float damage, Entity attacker = null);
    void Heal(float amount);
}