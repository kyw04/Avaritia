
public interface IStateMachine
{
    void Execute();
    void FixedExecute();
    void AddTransition<From, To>() where From : IState where To :IState;
    void ChangeState<S>() where S : IState;
    void Init();
}
