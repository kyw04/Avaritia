using UnityEngine;

// 발동할 때마다 modifier.amount만큼 버프가 쌓인다 (최대 maxStacks). 만료 없음.
// resetOnDamageTaken이면 시전자가 실제로 피해를 입는 순간 스택이 전부 사라진다.
[System.Serializable]
public class StackingBuffEffect : IAbilityEffect
{
    public StatModifier modifier = new();
    public int maxStacks = 1;
    public bool resetOnDamageTaken;

    public void Apply(AbilityContext context)
    {
        if (!context.Caster.Mono.TryGetComponent<IBuffable>(out var buffable)) return;

        float current = buffable.GetBuffAmount(context.State, modifier.statType, modifier.valueType);
        float max = modifier.amount * maxStacks;
        float next = Mathf.Abs(current + modifier.amount) > Mathf.Abs(max) ? max : current + modifier.amount;
        buffable.ApplyBuff(context.State, modifier.statType, modifier.valueType, next, 0f, null, resetOnDamageTaken);
    }
}
