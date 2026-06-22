using System;
using System.Collections.Generic;

public class StateMachineBase<T> : IStateMachine where T : IStateOwner<T>
{
    public T Owner { get; private set;}
    
    private StateBase<T> currentState;
    private Dictionary<Type, StateBase<T>> states = new();
    private Dictionary<(Type from, Type to), Func<bool>> transitionTable = new();

    protected StateMachineBase(T owner)
    {
        Owner = owner;
    }

    public virtual void Init()
    {
        StateManager.Instance.Register(this);
    }
    
    public void AddState(StateBase<T> state)
    {
        states.Add(state.GetType(), state);
    }
    
    public void AddTransition<From, To>(Func<bool> condition = null) where From : IState where To : IState
    {
        transitionTable[(typeof(From), typeof(To))] = condition;
    }

    private bool CanTransition(Type to)
    {
        if (transitionTable.Count == 0) return true;

        var node = currentState;
        while (node != null)
        {
            if (transitionTable.TryGetValue((node.GetType(), to), out var condition))
                return condition == null || condition();
            node = node.CurrentChild;
        }
        return false;
    }
    
    private StateBase<T> GetActiveLeaf()
    {
        var node = currentState;
        while (node?.CurrentChild != null)
            node = node.CurrentChild;
        return node;
    }

    public void ChangeState<S>() where S : IState
    {
        var targetType = typeof(S);

        if (currentState != null && !CanTransition(targetType))
            return;

        if (states.TryGetValue(targetType, out var topState))
        {
            currentState?.PropagateExit();
            currentState = topState;
            currentState.PropagateEnter();
            return;
        }

        foreach (var root in states.Values)
        {
            var targetPath = FindPath(root, targetType);
            if (targetPath == null) continue;

            var currentPath = GetActivePath(root);
            int lcaIndex = FindLcaIndex(currentPath, targetPath);

            for (int i = currentPath.Count - 1; i > lcaIndex; i--)
                currentPath[i].Exit();

            for (int i = lcaIndex; i < targetPath.Count - 1; i++)
                targetPath[i].SetCurrentChild(targetPath[i + 1]);

            for (int i = lcaIndex + 1; i < targetPath.Count; i++)
                targetPath[i].Enter();

            currentState = targetPath[0];
            return;
        }
    }

    private List<StateBase<T>> GetActivePath(StateBase<T> root)
    {
        var path = new List<StateBase<T>>();
        var node = root;

        while (node != null)
        {
            path.Add(node);
            node = node.CurrentChild;
        }
        return path;
    }

    private int FindLcaIndex(List<StateBase<T>> currentPath, List<StateBase<T>> targetPath)
    {
        int lca = -1;
        int len = Math.Min(currentPath.Count, targetPath.Count);

        for (int i = 0; i < len; i++)
        {
            if (currentPath[i].GetType() == targetPath[i].GetType())
                lca = i;
            else
                break;
        }
        return lca;
    }
    
    private List<StateBase<T>> FindPath(StateBase<T> node, Type targetType)
    {
        if (node.GetType() == targetType)
            return new List<StateBase<T>> { node };

        foreach (var child in node.Children.Values)
        {
            var path = FindPath(child, targetType);
            if (path == null) continue;

            path.Insert(0, node);
            return path;
        }
        return null;
    }
    
    public void Execute()
    {
        currentState?.PropagateExecute();
    }

    public void FixedExecute()
    {
        currentState?.PropagateFixedExecute();
    }
}
