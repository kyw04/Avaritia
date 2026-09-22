using System.Collections.Generic;
using UnityEngine;

public enum BuffValueType { Flat, Percent }
public enum BuffTarget { Self, Target }

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

    public void Apply(AbilityContext context)
    {
        Transform t = applyTo == BuffTarget.Self ? context.Caster.Mono.transform : context.Target;
        if (t == null || !t.TryGetComponent<IBuffable>(out var buffable)) return;

        // context.State identifies this specific binding (e.g. one particular copy of a held
        // Item), not the shared BuffEffect asset data — using `this` here would make two copies
        // of the same item collide on the same buff slot instead of stacking independently.
        foreach (var mod in modifiers)
            buffable.ApplyBuff(context.State, mod.statType, mod.valueType, mod.amount, duration);
    }
}
