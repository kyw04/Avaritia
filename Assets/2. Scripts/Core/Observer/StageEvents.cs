public struct StageNodeClearedEvent : ISubject
{
    public StageNode Node { get; private set; }
    public bool Restored { get; private set; }
    public StageNodeClearedEvent(StageNode node, bool restored = false) { Node = node; Restored = restored; }
}

public struct StageNodeChangedEvent : ISubject
{
    public StageNode Previous { get; private set; }
    public StageNode Current { get; private set; }
    public bool Restored { get; private set; }
    public StageNodeChangedEvent(StageNode previous, StageNode current, bool restored = false) { Previous = previous; Current = current; Restored = restored; }
}

public struct StageCompletedEvent : ISubject
{
    public StageNode Stage { get; private set; }
    public StageCompletedEvent(StageNode stage) { Stage = stage; }
}

// 다시 시작. 런 상태를 가진 시스템(플레이어, 스테이지, 풀, 통계, UI)이 각자 초기화한다.
public struct RunResetEvent : ISubject { }
