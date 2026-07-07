public struct EnemyIdleEvent : ISubject { }

public struct EnemyMovedEvent : ISubject
{
    public float Speed { get; private set; }
    public EnemyMovedEvent(float speed) { Speed = speed; }
}

public struct EnemyDeadEvent : ISubject { }

public struct EnemyAttackEvent : ISubject
{
    public AttackData Data { get; private set; }
    public EnemyAttackEvent(AttackData data)  { Data = data; }
}

public struct EnemyHealthChangedEvent : ISubject
{
    public float Ratio { get; private set; }
    public EnemyHealthChangedEvent(float ratio) { Ratio = ratio; }
}