using System.Collections.Generic;

public class StateManager : Singleton<StateManager>
{
    private List<IAIController> machines = new();

    public void Register(IAIController machine)
    {
        machines.Add(machine);
    }

    public void Unregister(IAIController machine)
    {
        machines.Remove(machine);
    }

    // 실행 중 등록/해제돼도 이번 프레임은 시작 시점 목록대로 돌도록 복사해 두는 버퍼. 매 프레임 ToArray로 새로 만들지 않는다.
    private readonly List<IAIController> snapshot = new();

    private void Update()
    {
        snapshot.Clear();
        snapshot.AddRange(machines);
        for (int i = 0; i < snapshot.Count; i++)
            snapshot[i].Execute();
    }

    private void FixedUpdate()
    {
        snapshot.Clear();
        snapshot.AddRange(machines);
        for (int i = 0; i < snapshot.Count; i++)
            snapshot[i].FixedExecute();
    }
}
