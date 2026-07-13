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
public class BuffEffect : ISkillEffect
{
    public List<StatModifier> modifiers = new();
    public float duration;
    public BuffTarget applyTo = BuffTarget.Self;

    public void Apply(IAttacker caster, Transform target)
    {
        Transform t = applyTo == BuffTarget.Self ? caster.Mono.transform : target;
        if (t == null || !t.TryGetComponent<IBuffable>(out var buffable)) return;

        foreach (var mod in modifiers)
            buffable.ApplyBuff(this, mod.statType, mod.valueType, mod.amount, duration);
    }
}
