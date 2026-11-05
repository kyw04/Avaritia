using UnityEngine;

// 대상에게 디버프를 건다. 건 쪽은 시전자라서, 시전자의 DebuffDuration 보너스가 지속 시간에 더해진다.
[System.Serializable]
public class DebuffEffect : IAbilityEffect
{
    public DebuffData debuff;
    public float duration = 3f;
    public BuffTarget applyTo = BuffTarget.Target;
    public float radius = 5f;     // NearestInRange / AllInRange 전용
    public LayerMask targetLayer; // NearestInRange / AllInRange 전용

    public void Apply(AbilityContext context)
    {
        var inflictor = context.Caster.Mono as Entity;
        foreach (var t in AbilityTargeting.Resolve(context, applyTo, radius, targetLayer))
            if (t.TryGetComponent<Entity>(out var entity))
                entity.Debuffs.Apply(debuff, duration, inflictor);
    }
}
