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

    private void Update()
    {
        foreach (var machine in machines.ToArray())
            machine.Execute();
    }

    private void FixedUpdate()
    {
        foreach (var machine in machines.ToArray())
            machine.FixedExecute();
    }
}
