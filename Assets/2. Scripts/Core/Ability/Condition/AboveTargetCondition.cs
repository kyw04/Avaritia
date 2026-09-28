using UnityEngine;

// 시전자의 발(콜라이더 아래쪽)이 대상 콜라이더 중심보다 높을 때 = 위에서 공격 중.
[System.Serializable]
public class AboveTargetCondition : IAbilityCondition
{
    public bool IsMet(AbilityContext context)
    {
        if (context.Target == null) return false;
        if (!context.Caster.Mono.TryGetComponent<Collider2D>(out var casterCol)) return false;
        if (!context.Target.TryGetComponent<Collider2D>(out var targetCol)) return false;
        return casterCol.bounds.min.y > targetCol.bounds.center.y;
    }
}
