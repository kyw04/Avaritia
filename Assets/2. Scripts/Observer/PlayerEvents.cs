public struct PlayerIdleEvent : ISubject { }

public struct PlayerMovedEvent : ISubject
{
    public float Speed { get; private set; }
    public PlayerMovedEvent(float speed) { Speed = speed; }
}
public struct PlayerJumpedEvent : ISubject { }
public struct PlayerFallingEvent : ISubject { }
public struct PlayerLandedEvent : ISubject { }
public struct PlayerTurnEvent : ISubject { }

public struct PlayerAttackEvent : ISubject
{
    public int Count { get; private set; }
    public PlayerAttackEvent(int cnt)  { Count = cnt; } 
}
public struct PlayerEndAttackEvent : ISubject { }

