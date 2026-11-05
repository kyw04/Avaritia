using UnityEngine;

public interface IDamageable
{
    void TakeDamage(DamageInfo info);
    void Heal(float amount);
}

public static class DamageableExtensions
{
    // 일반 타격용 축약형. attacker: 준 쪽(모르면 null), hitPoint: 맞은 위치, isCritical: 연출용.
    public static void TakeDamage(this IDamageable target, float damage, Entity attacker = null,
        Vector3? hitPoint = null, bool isCritical = false) =>
        target.TakeDamage(new DamageInfo(damage, attacker, hitPoint, isCritical));
}
