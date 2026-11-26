using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[System.Serializable]
public class MeleeAttackStrategy : IAttackStrategy
{
    public Vector2 hitboxPosition;
    public Vector2 hitboxSize;

    public void Offensive(IAttacker attacker, KnockbackSettings knockback, HitCameraSettings hitCamera, float damageMultiplier, ContactFilter2D filter, Transform target = null, BulletEnhancement enhancement = null, Vector2? aimDirection = null)
        => Offensive(attacker, knockback, hitCamera, damageMultiplier, filter, (HashSet<IDamageable>)null);

    // alreadyHit이 있으면 그 안의 대상은 건너뛰고, 새로 맞힌 대상을 넣는다(판정을 여러 번 검사해도 대상마다 한 번만 맞는다).
    public void Offensive(IAttacker attacker, KnockbackSettings knockback, HitCameraSettings hitCamera, float damageMultiplier, ContactFilter2D filter, HashSet<IDamageable> alreadyHit)
    {
        float scale = damageMultiplier;
        bool isCritical = attacker is IStatReadable readable && Random.Range(0f, 100f) < readable.GetStat<float>(StatType.CritRate);
        if (isCritical)
            scale *= CritConfig.Multiplier;
        var attackerEntity = attacker.SourceEntity;
        var pos = hitboxPosition;
        if (attacker.LookDirection < 0)
            pos.x = -pos.x;
        pos += (Vector2)attacker.Mono.transform.position;

        // 피해 처리 중 다른 공격이 같은 전략을 다시 부를 수 있어 호출마다 풀에서 빌린다.
        using (ListPool<Collider2D>.Get(out var hits))
        {
            Physics2D.OverlapBox(pos, hitboxSize, 0f, filter, hits);
            DebugExtension.DrawBox(pos, hitboxSize / 2f, Quaternion.identity, Color.green, 1.0f);
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(attacker.Mono.transform)) continue;
                if (hit.TryGetComponent<IDamageable>(out var d))
                {
                    if (alreadyHit != null && !alreadyHit.Add(d)) continue;
                    d.TakeDamage(attacker.GetDamageAgainst(hit.transform) * scale, attackerEntity, knockback, hitCamera, null, isCritical);
                }
            }
        }
    }
}
