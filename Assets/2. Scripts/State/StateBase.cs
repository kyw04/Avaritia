public abstract class StateBase<T> : IState
{
    public T Owner { get; private set; }
        
    protected StateBase(T owner)
    {
        this.Owner = owner;
    }
    
    public virtual void Enter() { }

    public virtual void Execute() { }
    public virtual void FixedExecute() { }

    public virtual void Exit() { }
}
