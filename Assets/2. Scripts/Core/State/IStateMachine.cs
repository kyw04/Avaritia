using System;

public interface IStateMachine : IAIController
{
    void AddTransition<From, To>(Func<bool> condition = null) where From : IState where To : IState;
    void ChangeState<S>() where S : IState;
}
