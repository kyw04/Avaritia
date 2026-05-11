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
        states.TryGetValue(typeof(S), out var value);
        StateBase<T> newState = value as S;
        if (currentState != null)
        {
            if (currentState.children.TryGetValue(typeof(S), out var childrenState))
            {
                newState = childrenState;
            }
            if (currentState.parent != null && currentState.parent.children.TryGetValue(typeof(S), out var parentState))
            {
                newState = parentState;
            }
        }

        if (newState == null)
            return;
        
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();

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
