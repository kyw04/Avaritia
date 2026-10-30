using System.Collections.Generic;
using UnityEngine;

// 효과를 받을 대상을 고른다.
// Self = 시전자, Target = 트리거/호출자가 넘긴 context.Target,
// NearestInRange = 시전자 주변 radius 안 layer 중 가장 가까운 하나, AllInRange = 그 안의 전부.
public static class AbilityTargeting
{
    public static List<Transform> Resolve(AbilityContext context, BuffTarget applyTo, float radius, LayerMask layer)
    {
        var caster = context.Caster.Mono.transform;
        var result = new List<Transform>();

        switch (applyTo)
        {
            case BuffTarget.Self:
                result.Add(caster);
                break;
            case BuffTarget.Target:
                if (context.Target != null) result.Add(context.Target);
                break;
            case BuffTarget.NearestInRange:
            case BuffTarget.AllInRange:
                result.AddRange(Overlap(caster, radius, layer));
                if (applyTo == BuffTarget.NearestInRange && result.Count > 1)
                {
                    result.Sort((a, b) => (a.position - caster.position).sqrMagnitude
                        .CompareTo((b.position - caster.position).sqrMagnitude));
                    result.RemoveRange(1, result.Count - 1);
                }
                break;
        }
        return result;
    }

    // 한 엔티티의 콜라이더 여러 개가 중복으로 잡히지 않도록 Entity 루트로 모은다. 시전자 자신은 제외.
    private static List<Transform> Overlap(Transform caster, float radius, LayerMask layer)
    {
        var found = new List<Transform>();
        foreach (var hit in Physics2D.OverlapCircleAll(caster.position, radius, layer))
        {
            var entity = hit.GetComponentInParent<Entity>();
            var t = entity != null ? entity.transform : hit.transform;
            if (t != caster && !found.Contains(t)) found.Add(t);
        }
        return found;
    }
}
