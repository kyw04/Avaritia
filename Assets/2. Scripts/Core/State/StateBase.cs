using System.Collections.Generic;

public abstract class StateBase<T> : IState where T : IStateOwner<T>
{
    public T Owner { get; private set; }
    protected IStateMachine Machine => Owner.Machine;
    
    public StateBase<T> Parent { get; private set; }
    public StateBase<T> CurrentChild { get; private set; }
    public Dictionary<System.Type, StateBase<T>> Children { get; } = new();
    private StateBase<T> defaultChild;

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
        
        defaultChild ??= child;
        CurrentChild ??= child;
    }

    // 막 만든 상태처럼 각 단계의 현재 자식을 처음 추가한 자식으로 되돌린다.
    internal void ResetChildren()
    {
        CurrentChild = defaultChild;
        foreach (var child in Children.Values)
            child.ResetChildren();
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
}
