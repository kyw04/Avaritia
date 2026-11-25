using System;
using System.Collections.Generic;

public class StateMachineBase<T> : IStateMachine where T : IStateOwner<T>
{
    public T Owner { get; private set;}
    
    private StateBase<T> currentState;
    private Dictionary<Type, StateBase<T>> states = new();
    // 자식까지 포함한 모든 상태. ChangeState가 트리를 뒤지며 경로 리스트를 만들지 않고 바로 찾는다.
    private readonly Dictionary<Type, StateBase<T>> allStates = new();
    private Dictionary<(Type from, Type to), Func<bool>> transitionTable = new();
    // ChangeState가 실제로 상태를 바꿀 때마다 증가. Execute 도중 상태가 바뀌었는지 판단한다.
    private int changeCount;

    protected StateMachineBase(T owner)
    {
        Owner = owner;
    }

    public virtual void Init()
    {
        StateManager.Instance.Register(this);
    }

    // 상태 객체를 새로 만들지 않고 상태를 넣은 직후(첫 ChangeState 전)로 되돌린다. 빠져나오는 상태의 Exit는 부르지 않는다.
    protected void ResetStates()
    {
        currentState = null;
        foreach (var state in states.Values)
            state.ResetChildren();
    }

    public void AddState(StateBase<T> state)
    {
        states.Add(state.GetType(), state);
        RegisterRecursive(state);
    }

    // 같은 타입이 여러 곳에 있으면 먼저 넣은 쪽을 쓴다(예전 FindPath가 먼저 찾던 쪽).
    private void RegisterRecursive(StateBase<T> state)
    {
        allStates.TryAdd(state.GetType(), state);
        foreach (var child in state.Children.Values)
            RegisterRecursive(child);
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

        changeCount++;
        if (states.TryGetValue(targetType, out var topState))
        {
            currentState?.PropagateExit();
            currentState = topState;
            currentState.PropagateEnter();
            return;
        }

        if (!allStates.TryGetValue(targetType, out var target))
            return;

        var root = target;
        while (root.Parent != null)
            root = root.Parent;

        // 목표가 속한 루트에서 활성 자식을 따라 내려가며, 목표의 조상이기도 한 가장 깊은 상태(lca)와 활성 경로 끝(leaf)을 찾는다.
        var lca = root;
        while (lca.CurrentChild != null && IsAncestorOrSelf(lca.CurrentChild, target))
            lca = lca.CurrentChild;
        var leaf = lca;
        while (leaf.CurrentChild != null)
            leaf = leaf.CurrentChild;

        // 이미 목표가 활성 경로의 끝이면 그 상태만 다시 시작한다.
        if (leaf == target)
        {
            target.Exit();
            target.Enter();
            currentState = root;
            return;
        }

        for (var node = leaf; node != lca; node = node.Parent)
            node.Exit();

        for (var node = target; node != lca; node = node.Parent)
            node.Parent.SetCurrentChild(node);

        EnterDownTo(lca, target);
        currentState = root;
    }

    private static bool IsAncestorOrSelf(StateBase<T> node, StateBase<T> of)
    {
        for (var n = of; n != null; n = n.Parent)
            if (n == node)
                return true;
        return false;
    }

    // lca 바로 아래부터 node까지 위에서 아래 순서로 Enter한다.
    private static void EnterDownTo(StateBase<T> lca, StateBase<T> node)
    {
        if (node == lca)
            return;
        EnterDownTo(lca, node.Parent);
        node.Enter();
    }
    
    // 부모부터 자식 순으로 실행하다가 중간에 상태가 바뀌면 멈춘다. 이미 빠져나온 자식 상태가
    // 같은 프레임에 이어서 실행되면 새 상태의 연출(애니메이션 등)을 덮어쓰기 때문이다.
    public void Execute()
    {
        int version = changeCount;
        for (var node = currentState; node != null && version == changeCount; node = node.CurrentChild)
            node.Execute();
    }

    public void FixedExecute()
    {
        int version = changeCount;
        for (var node = currentState; node != null && version == changeCount; node = node.CurrentChild)
            node.FixedExecute();
    }
}
