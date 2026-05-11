using System.Collections.Generic;

public abstract class StateBase<T> : IState
{
    public T Owner { get; private set; }
    
    public StateBase<T> parent;
    public Dictionary<System.Type, StateBase<T>> children = new();
    
    protected StateBase(T owner)
    {
        this.Owner = owner;
    }
    
    public virtual void Enter() { }

    public virtual void Execute() { }
    public virtual void FixedExecute() { }

    public virtual void Exit() { }
    
    public void AddChild(StateBase<T> child)
    {
        child.parent = this;
        children.Add(child.GetType(), child);
    }
}
