using System;

// 디버프가 걸려 있는 동안 행동을 막는다(기절 = 전부, 침묵 = Skill, 속박 = Move 등).
// 해제는 DebuffController가 출처(ActiveDebuff) 단위로 한다.
[Serializable]
public class RestrictModule : DebuffModule
{
    public ActionBlock blocks = ActionBlock.Move | ActionBlock.Attack | ActionBlock.Skill | ActionBlock.Jump | ActionBlock.Dash;

    public override void OnApply(ActiveDebuff debuff) => debuff.Target.Restrictions.Set(debuff, blocks);
}
