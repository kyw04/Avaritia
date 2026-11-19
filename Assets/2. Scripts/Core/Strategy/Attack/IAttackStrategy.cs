using UnityEngine;

public interface IAttackStrategy
{
    // aimDirection이 있으면 대상 대신 그 방향으로 쏜다(조준 시간 동안 미리 정한 방향).
    void Offensive(IAttacker attacker, KnockbackSettings knockback, HitCameraSettings hitCamera, float damageMultiplier, ContactFilter2D filter, 
        Transform target = null, BulletEnhancement enhancement = null, Vector2? aimDirection = null);
}
