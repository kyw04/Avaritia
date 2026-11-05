using System;
using System.Collections.Generic;

// 디버프가 걸려 있는 동안 스탯을 바꾼다(보통 음수). scaleWithStacks면 수치 × 중첩 수.
// 버프는 만료 없이 걸고, 디버프가 끝나면 DebuffController가 출처(ActiveDebuff) 단위로 걷어낸다.
[Serializable]
public class StatDebuffModule : DebuffModule
{
    public List<StatModifier> modifiers = new();
    public bool scaleWithStacks = true;

    public override void OnApply(ActiveDebuff debuff) => ApplyBuffs(debuff);

    public override void OnStacksChanged(ActiveDebuff debuff) => ApplyBuffs(debuff);

    // 같은 출처/스탯/값 종류는 덮어쓰이므로, 중첩이 바뀌면 같은 슬롯을 새 수치로 다시 건다.
    private void ApplyBuffs(ActiveDebuff debuff)
    {
        int multiplier = scaleWithStacks ? debuff.Stacks : 1;
        foreach (var mod in modifiers)
            debuff.Target.ApplyBuff(debuff, mod.statType, mod.valueType, mod.amount * multiplier, 0f);
    }
}
