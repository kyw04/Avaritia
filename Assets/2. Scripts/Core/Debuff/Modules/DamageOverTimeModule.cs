using System;
using UnityEngine;

// 디버프가 걸려 있는 동안 tickInterval마다 피해를 준다(화상, 출혈, 중독 등).
// 틱당 피해 = damagePerTick + 건 쪽 공격력 × attackDamageRatio (× 중첩 수). 건 쪽 공격력은 걸거나 다시 걸 때의 값으로 고정한다.
// 다시 걸어도 틱 타이머는 이어서 센다(다시 걸 때마다 틱이 밀리지 않게).
// 피해를 준 쪽은 Inflictor라서, 지속 피해로 죽어도 건 쪽의 처치로 친다.
[Serializable]
public class DamageOverTimeModule : DebuffModule
{
    [Min(0.05f)] public float tickInterval = 0.5f;
    [Min(0)] public float damagePerTick = 1f;
    [Min(0)] public float attackDamageRatio;
    public bool scaleWithStacks = true;
    // 걸리는 순간 한 번 바로 피해를 줄지.
    public bool tickOnApply;
    // 기본: 넉백, 피격 연출, 채널링 중단, 적중 효과를 일으키지 않고 회피도 무시한다. 방어력은 적용한다.
    public DamageFlags flags = DamageFlags.DamageOverTime | DamageFlags.NoKnockback | DamageFlags.NoHitReaction
                               | DamageFlags.NoInterrupt | DamageFlags.NoOnHitEffects | DamageFlags.IgnoreEvasion;

    private class State
    {
        public float NextTickTime;
        public float InflictorDamage;
    }

    public override void OnApply(ActiveDebuff debuff)
    {
        var state = debuff.GetState<State>(this);
        state.InflictorDamage = InflictorDamage(debuff);
        state.NextTickTime = Time.time + tickInterval;
        if (tickOnApply)
            Deal(debuff, state);
    }

    public override void OnReapply(ActiveDebuff debuff) =>
        debuff.GetState<State>(this).InflictorDamage = InflictorDamage(debuff);

    public override void OnTick(ActiveDebuff debuff)
    {
        var state = debuff.GetState<State>(this);
        // 끝나는 시각보다 뒤의 틱은 주지 않는다(프레임이 늦게 와도 지속 시간만큼만).
        if (Time.time < state.NextTickTime || state.NextTickTime > debuff.EndTime)
            return;

        state.NextTickTime += tickInterval;
        Deal(debuff, state);
    }

    private void Deal(ActiveDebuff debuff, State state)
    {
        float amount = damagePerTick + state.InflictorDamage * attackDamageRatio;
        if (scaleWithStacks)
            amount *= debuff.Stacks;
        if (amount > 0f)
            debuff.Target.TakeDamage(new DamageInfo(amount, debuff.Inflictor, flags: flags));
    }

    private static float InflictorDamage(ActiveDebuff debuff) =>
        debuff.Inflictor != null ? debuff.Inflictor.Damage : 0f;
}
