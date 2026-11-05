using System;

// 걸리는 순간 진행 중인 행동을 끊는다. 막기(RestrictModule)와 달리 이후 행동은 막지 않는다.
[Serializable]
public class InterruptModule : DebuffModule
{
    public bool cancelAttack = true;
    public bool cancelChannel = true;

    public override void OnApply(ActiveDebuff debuff)
    {
        if (cancelAttack)
            debuff.Target.CancelAttack();
        if (cancelChannel)
            debuff.Target.CancelChannels();
    }
}
