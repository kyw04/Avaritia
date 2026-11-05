using System.Collections.Generic;
using UnityEngine;

// 대상에게 걸려 있는 디버프 하나. 모듈이 거는 버프 등의 출처 키로도 쓴다.
// 같은 DebuffData는 대상당 하나만 있고, 다시 걸면 이 인스턴스를 갱신한다.
public class ActiveDebuff
{
    public DebuffData Data { get; }
    public Entity Target { get; }
    // 마지막으로 건 쪽. 없을 수도 있다(환경 등).
    public Entity Inflictor { get; internal set; }
    // 마지막으로 정한 길이. 상태 아이콘 채움 비율용.
    public float Duration { get; internal set; }
    public float EndTime { get; internal set; }
    public int Stacks { get; internal set; } = 1;
    public float Remaining => EndTime - Time.time;

    // 걸릴 때 대상에게 영향을 주는 모듈만 골라 둔다. 디버프가 끝날 때까지 이 모듈들만 호출된다.
    internal List<DebuffModule> Modules { get; }

    // 모듈별 런타임 상태(틱 타이머 등). 모듈은 공유 에셋이라 상태를 여기 둔다.
    private readonly Dictionary<DebuffModule, object> moduleStates = new();

    internal ActiveDebuff(DebuffData data, Entity target, List<DebuffModule> modules)
    {
        Data = data;
        Target = target;
        Modules = modules;
    }

    public T GetState<T>(DebuffModule module) where T : class, new()
    {
        if (!moduleStates.TryGetValue(module, out var state))
            moduleStates[module] = state = new T();
        return (T)state;
    }
}
