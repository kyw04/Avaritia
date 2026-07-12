using System.Collections.Generic;

public abstract class StateBase<T> : IState where T : IStateOwner<T>
{
    public T Owner { get; private set; }
    protected IStateMachine Machine => Owner.Machine;
    
    public StateBase<T> Parent { get; private set; }
    public StateBase<T> CurrentChild { get; private set; }
    public Dictionary<System.Type, StateBase<T>> Children { get; } = new();
    
    protected StateBase(T owner)
    {
        Owner = owner;
    }
    
    public virtual void Enter() { }

    public virtual void Execute() { }
    public virtual void FixedExecute() { }

    public virtual void Exit() { }
    
    protected void AddChild(StateBase<T> child)
    {
        child.Parent = this;
        Children.Add(child.GetType(), child);
        
        CurrentChild ??= child;
    }
    
    internal void SetCurrentChild(StateBase<T> child)
    {
        CurrentChild = child;
    }
    
    internal void PropagateEnter()
    {
        Enter();
        CurrentChild?.PropagateEnter();
    }

    internal void PropagateExit()
    {
        CurrentChild?.PropagateExit();
        Exit();
    }

    internal void PropagateExecute()
    {
        Execute();
        CurrentChild?.PropagateExecute();
    }

    internal void PropagateFixedExecute()
    {
        FixedExecute();
        CurrentChild?.PropagateFixedExecute();
    }
}
