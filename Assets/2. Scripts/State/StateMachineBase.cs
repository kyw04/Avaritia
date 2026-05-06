using System.Collections.Generic;

public class StateMachineBase<T> : IStateMachine
{
    public T Owner { get; private set;}
    private IState currentState;

    private Dictionary<System.Type, IState> states = new();

    protected StateMachineBase(T owner)
    {
        this.Owner = owner;

        StateManager.Instance.Register(this);
    }

    public void AddState(IState state)
    {
        states.Add(state.GetType(), state);
    }
        
    public void ChangeState<S>() where S : IState
    {
        if (states.TryGetValue(typeof(S), out var newSate))
        {
            currentState?.Exit();
            currentState = newSate;
            currentState?.Enter();
        }
    }

    public void Execute()
    {
        currentState?.Execute();
    }

    public void FixedExecute()
    {
        currentState?.FixedExecute();
    }
}
