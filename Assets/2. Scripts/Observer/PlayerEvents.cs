public struct PlayerIdleEvent : ISubject { }

public struct PlayerMovedEvent : ISubject
{
    public float Speed { get; private set; }
    public PlayerMovedEvent(float speed) { Speed = speed; }
}
public struct PlayerJumpedEvent : ISubject
{
    public JumpData Data { get; private set; }
    public PlayerJumpedEvent(JumpData data) { Data = data; }
}
public struct PlayerFallingEvent : ISubject { }
public struct PlayerLandedEvent : ISubject { }
public struct PlayerTurnEvent : ISubject { }
public struct PlayerDashEvent : ISubject { }

public struct PlayerAttackStartEvent : ISubject
{
    public AttackData Data { get; private set; }
    public PlayerAttackStartEvent(AttackData data)  { Data = data; } 
}

public struct PlayerAttackBufferEvent : ISubject { }

public struct PlayerAttackEndEvent : ISubject { }

public struct PlayerHealthChangedEvent : ISubject
{
    public float Max { get; private set; }
    public float Current { get; private set; }
    public PlayerHealthChangedEvent(float max, float current) { Max = max; Current = current; }
}

public struct PlayerDashCountChangedEvent : ISubject
{
    public int Current { get; private set; }
    public int Max { get; private set; }
    public PlayerDashCountChangedEvent(int current, int max) { Current = current; Max = max; }
}

