public struct BossIdleEvent : ISubject { }
public struct BossMovedEvent : ISubject
{
    public float Speed { get; private set; }
    public BossMovedEvent(float speed) { Speed = speed; }
}
public struct BossJumpedEvent : ISubject { }
public struct BossFallingEvent : ISubject { }
public struct BossLandedEvent : ISubject { }
public struct BossDeadEvent : ISubject { }

public struct BossAttackStartEvent : ISubject
{
    public AttackData Data { get; private set; }
    public BossAttackStartEvent(AttackData data)  { Data = data; }
}

public struct BossHealthChangedEvent : ISubject
{
    public float Ratio { get; private set; }
    public BossHealthChangedEvent(float ratio) { Ratio = ratio; }
}