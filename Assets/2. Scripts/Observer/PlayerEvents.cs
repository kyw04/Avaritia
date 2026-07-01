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
    public float Ratio { get; private set; }
    public PlayerHealthChangedEvent(float ratio) { Ratio = ratio; }
}

