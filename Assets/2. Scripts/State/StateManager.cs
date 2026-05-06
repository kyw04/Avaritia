using System.Collections.Generic;

public class StateManager : Singleton<StateManager>
{
    List<IStateMachine> machines;
    
    public void Register(IStateMachine machine)
    {
        machines.Add(machine);
    }
        
    public void Unregister(IStateMachine machine)
    {
        machines.Remove(machine);
    }
        
    private void Update()
    {
        foreach (var machine in machines)
            machine.Execute();
    }

    private void FixedUpdate()
    {
        foreach (var machine in machines)
            machine.FixedExecute();
    }
}
