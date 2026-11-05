using System;
using System.Collections.Generic;

// 막을 수 있는 행동. 직렬화 값이므로 새 항목은 맨 뒤에만 추가할 것.
[Flags]
public enum ActionBlock
{
    None = 0,
    Move = 1,
    Attack = 2,   // 근접/사격과 무기 입력 메서드
    Skill = 4,
    Jump = 8,
    Dash = 16,
    Turn = 32,    // 바라보는 방향 전환
    Interact = 64, // 줍기, 상자 열기 등
}

// 출처(채널링, 디버프 등)별로 막은 행동을 모아 둔다. 한 출처가 풀려도 다른 출처의 제한은 남는다.
// 사용하는 쪽은 누가 막았는지 모르고 Can으로 할 수 있는지만 묻는다.
public class ActionRestrictions
{
    private readonly Dictionary<object, ActionBlock> sources = new();

    public ActionBlock Blocked { get; private set; }

    // 같은 출처는 덮어쓴다. None이면 해제와 같다.
    public void Set(object source, ActionBlock blocks)
    {
        if (blocks == ActionBlock.None)
            sources.Remove(source);
        else
            sources[source] = blocks;
        Recalculate();
    }

    public void Remove(object source)
    {
        if (sources.Remove(source))
            Recalculate();
    }

    public void Clear()
    {
        sources.Clear();
        Blocked = ActionBlock.None;
    }

    // 지정한 행동이 하나도 막혀 있지 않으면 true.
    public bool Can(ActionBlock action) => (Blocked & action) == 0;

    private void Recalculate()
    {
        var blocked = ActionBlock.None;
        foreach (var blocks in sources.Values)
            blocked |= blocks;
        Blocked = blocked;
    }
}
