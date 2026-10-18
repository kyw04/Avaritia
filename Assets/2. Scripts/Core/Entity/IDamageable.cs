using UnityEngine;

public interface IDamageable
{
    // attacker: who dealt it, if known (null for effects) — used by on-hit/reflect abilities.
    // hitPoint: where it actually hit (e.g. the bullet), if known.
    // isCritical: the attacker rolled a crit — only used for feedback (e.g. damage popup color).
    void TakeDamage(float damage, Entity attacker = null, Vector3? hitPoint = null, bool isCritical = false);
    void Heal(float amount);
}