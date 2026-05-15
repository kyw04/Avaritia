using System.Collections.Generic;

public class StateMachineBase<T> : IStateMachine
{
    public T Owner { get; private set;}
    
    public StateBase<T> currentState; // 로그용으로 현제 public 기존 private
    private Dictionary<System.Type, StateBase<T>> states = new();
    private HashSet<(System.Type from, System.Type to)> transitionTable = new();

    protected StateMachineBase(T owner)
    {
        this.Owner = owner;

        StateManager.Instance.Register(this);
    }

    public void AddState(StateBase<T> state)
    {
        states.Add(state.GetType(), state);
    }
    
    protected void AddTransition<From, To>() where From : StateBase<T> where To : StateBase<T>
    {
        transitionTable.Add((typeof(From), typeof(To)));
    }

    private bool CanTransition(System.Type from, System.Type to)
    {
        // 테이블이 비어있으면 모두 허용
        if (transitionTable.Count == 0) return true;
        return transitionTable.Contains((from, to));
    }
    
    private StateBase<T> GetActiveLeaf()
    {
        var node = currentState;
        while (node?.CurrentChild != null)
            node = node.CurrentChild;
        return node;
    }
    
    public void ChangeState<S>() where S : StateBase<T>
    {
        var targetType = typeof(S);
        var activeLeaf = GetActiveLeaf();

        if (activeLeaf != null && !CanTransition(activeLeaf.GetType(), targetType))
            return;

        // 최상위 상태 전환
        if (states.TryGetValue(targetType, out var topState))
        {
            currentState?.PropagateExit();
            currentState = topState;
            currentState.PropagateEnter();
            return;
        }

        // 트리에서 목표 경로 탐색
        foreach (var root in states.Values)
        {
            var targetPath = FindPath(root, targetType);
            if (targetPath == null) continue;

            // 현재 활성 경로
            var currentPath = GetActivePath(root);

            // LCA 찾기
            int lcaIndex = FindLcaIndex(currentPath, targetPath);

            // LCA 아래부터만 Exit
            for (int i = currentPath.Count - 1; i > lcaIndex; i--)
                currentPath[i].Exit();

            // 목표 경로로 currentChild 포인터 설정
            for (int i = lcaIndex; i < targetPath.Count - 1; i++)
                targetPath[i].SetCurrentChild(targetPath[i + 1]);

            // LCA 아래부터만 Enter
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
        int len = System.Math.Min(currentPath.Count, targetPath.Count);

        for (int i = 0; i < len; i++)
        {
            if (currentPath[i].GetType() == targetPath[i].GetType())
                lca = i;
            else
                break;
        }
        return lca;
    }
    
    private List<StateBase<T>> FindPath(StateBase<T> node, System.Type targetType)
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
