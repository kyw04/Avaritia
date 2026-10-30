using System.Collections.Generic;
using UnityEngine;

public enum BuffValueType { Flat, Percent }
// 순서 = 직렬화 값이므로 새 항목은 맨 뒤에만 추가할 것.
public enum BuffTarget { Self, Target, NearestInRange, AllInRange }

[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public BuffValueType valueType;
    public float amount; // Flat: 그대로 더함. Percent: 10 = +10%, -20 = -20%
}

[System.Serializable]
public class BuffEffect : IAbilityEffect
{
    public List<StatModifier> modifiers = new();
    public float duration; // 0(기본값) = 만료되지 않음(장착 해제될 때만 사라짐). 양수면 그 초만큼 지속.
    public BuffTarget applyTo = BuffTarget.Self;
    public float radius = 5f;     // NearestInRange / AllInRange 전용
    public LayerMask targetLayer; // NearestInRange / AllInRange 전용
    // 비어있으면 항상 적용. 있으면 스탯을 읽을 때마다 평가해 모두 만족할 때만 버프가 반영된다
    // (Caster = 버프 받은 쪽, Target = 공격 대상 — 대상이 없는 일반 스탯 조회에선 null).
    [SerializeReference, SubclassSelector] public List<IAbilityCondition> activeWhile = new();
    // 버프 받은 대상을 따라다니는 파티클. duration이 양수면 버프가 끝날 때 같이 멈추고, 0이면 파티클 자체 길이만큼 재생된다.
    public PooledParticle particle;

    public void Apply(AbilityContext context)
    {
        foreach (var t in AbilityTargeting.Resolve(context, applyTo, radius, targetLayer))
        {
            if (!t.TryGetComponent<IBuffable>(out var buffable)) continue;

            // context.State identifies this specific binding (e.g. one particular copy of a held
            // Item), not the shared BuffEffect asset data — using `this` here would make two copies
            // of the same item collide on the same buff slot instead of stacking independently.
            foreach (var mod in modifiers)
                buffable.ApplyBuff(context.State, mod.statType, mod.valueType, mod.amount, duration, activeWhile);

            ParticleManager.Instance.SpawnFollow(particle, t, duration, context.State);
        }
    }
}
