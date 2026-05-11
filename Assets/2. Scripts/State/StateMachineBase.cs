using System.Collections.Generic;

public class StateMachineBase<T> : IStateMachine
{
    public T Owner { get; private set;}
    
    private StateBase<T> currentState;
    private Dictionary<System.Type, StateBase<T>> states = new();

    protected StateMachineBase(T owner)
    {
        this.Owner = owner;

        StateManager.Instance.Register(this);
    }

    public void AddState(StateBase<T> state)
    {
        states.Add(state.GetType(), state);
    }
        
    public void ChangeState<S>() where S : StateBase<T>
    {
        if (currentState != null && currentState.children.TryGetValue(typeof(S), out var child))
        {
            currentState?.Exit();
            currentState = child;
            currentState?.Enter();
            return;
        }
        
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
