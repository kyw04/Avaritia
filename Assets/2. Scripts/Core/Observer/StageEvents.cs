public struct StageNodeClearedEvent : ISubject
{
    public StageNode Node { get; private set; }
    public StageNodeClearedEvent(StageNode node) { Node = node; }
}

public struct StageNodeChangedEvent : ISubject
{
    public StageNode Previous { get; private set; }
    public StageNode Current { get; private set; }
    public StageNodeChangedEvent(StageNode previous, StageNode current) { Previous = previous; Current = current; }
}

public struct StageCompletedEvent : ISubject
{
    public StageData Stage { get; private set; }
    public StageCompletedEvent(StageData stage) { Stage = stage; }
}
