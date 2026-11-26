using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// 효과를 받을 대상을 고른다.
// Self = 시전자, Target = 트리거/호출자가 넘긴 context.Target,
// NearestInRange = 시전자 주변 radius 안 layer 중 가장 가까운 하나, AllInRange = 그 안의 전부.
public static class AbilityTargeting
{
    // 결과는 result에 채운다(호출하는 쪽이 ListPool 등에서 빌린 리스트를 넘겨 할당을 피한다).
    public static void Resolve(AbilityContext context, BuffTarget applyTo, float radius, LayerMask layer, List<Transform> result)
    {
        result.Clear();
        var caster = context.Caster.Mono.transform;

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
                Overlap(caster, radius, layer, result);
                if (applyTo == BuffTarget.NearestInRange && result.Count > 1)
                    KeepNearest(caster.position, result);
                break;
        }
    }

    // 한 엔티티의 콜라이더 여러 개가 중복으로 잡히지 않도록 Entity 루트로 모은다. 시전자 자신은 제외.
    private static void Overlap(Transform caster, float radius, LayerMask layer, List<Transform> found)
    {
        using (ListPool<Collider2D>.Get(out var hits))
        {
            Physics2D.OverlapCircle(caster.position, radius, ContactFilters.ForLayer(layer), hits);
            foreach (var hit in hits)
            {
                var entity = hit.GetComponentInParent<Entity>();
                var t = entity != null ? entity.transform : hit.transform;
                if (t != caster && !found.Contains(t)) found.Add(t);
            }
        }
    }

    // 가장 가까운 하나만 남긴다(정렬 람다의 클로저 할당 없이).
    private static void KeepNearest(Vector3 from, List<Transform> list)
    {
        int best = 0;
        float bestDist = (list[0].position - from).sqrMagnitude;
        for (int i = 1; i < list.Count; i++)
        {
            float dist = (list[i].position - from).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }
        var nearest = list[best];
        list.Clear();
        list.Add(nearest);
    }
}
